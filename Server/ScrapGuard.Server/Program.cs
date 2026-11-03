using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using ScrapGuard.Core;
using ScrapGuard.Server;

var builder = WebApplication.CreateBuilder(args);
string databasePath = builder.Configuration["GAME_DB"] ?? "data/scrapguard.db";
if (builder.Configuration["backup-database"] is string backup)
{
    DatabaseMaintenance.Backup(databasePath,backup); Console.WriteLine("BACKUP_VERIFIED " + Path.GetFullPath(backup)); return;
}
if (builder.Configuration["verify-backup"] is string verify)
{
    DatabaseMaintenance.Verify(verify); Console.WriteLine("DATABASE_VERIFIED"); return;
}
var operations = new ServerOperations(builder.Configuration);
var clientRelease = new ClientRelease(builder.Configuration);
builder.Services.Configure<ForwardedHeadersOptions>(o => ServerOperations.ConfigureProxy(o,builder.Configuration["TRUSTED_PROXY_IPS"]));
builder.WebHost.UseUrls(builder.Configuration["SERVER_URLS"] ?? "http://127.0.0.1:5187");
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 2 * 1024 * 1024);
builder.Services.ConfigureHttpJsonOptions(o => { o.SerializerOptions.IncludeFields = true; o.SerializerOptions.PropertyNameCaseInsensitive = true; });
builder.Services.AddSingleton(new GameRepository(databasePath));
builder.Services.AddSingleton<GooglePlayPurchases>();
builder.Services.AddSingleton<IGoogleIdentityToken,GoogleIdentityToken>();
builder.Services.AddSingleton<GoogleIdentityFlow>();
builder.Services.AddHostedService<PurchaseReconciler>();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext,string>(ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 180, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy("register",ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddConcurrencyLimiter("commands",options => { options.PermitLimit = 2; options.QueueLimit = 8; options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst; });
    o.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(new ServerReply { message = "요청이 많아요. 잠시 뒤 다시 시도해 주세요." },token);
    };
});
var app = builder.Build();
app.UseForwardedHeaders();
app.UseRateLimiter();
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.CacheControl = "no-store";
    try
    {
        if(!ClientRelease.Exempt(ctx.Request.Path.Value??"",ctx.Request.Method) && !clientRelease.Accepts(ctx.Request.Headers[AppRelease.BuildHeader].ToString(),ctx.Request.Headers[AppRelease.RulesHeader].ToString()))
        {
            ctx.Response.StatusCode=426;await ctx.Response.WriteAsJsonAsync(clientRelease.Rejection());return;
        }
        if (operations.Maintenance && ctx.Request.Method != "GET")
        {
            ctx.Response.Headers.RetryAfter = "60"; throw new ApiFault(503,"서버 점검 중이에요. 진행 기록을 보관했으니 잠시 뒤 동기화해 주세요.");
        }
        await next(ctx);
    }
    catch (ApiFault ex) { ctx.Response.StatusCode = ex.Status; await ctx.Response.WriteAsJsonAsync(ex.Reply ?? new ServerReply { success = false, message = ex.Message }); }
    catch (BadHttpRequestException) { ctx.Response.StatusCode = 400; await ctx.Response.WriteAsJsonAsync(new ServerReply { message = "요청 형식이 올바르지 않아요." }); }
});
string Token(HttpContext ctx)
{
    string header = ctx.Request.Headers.Authorization.ToString();
    if (!header.StartsWith("Bearer ", StringComparison.Ordinal)) throw new ApiFault(401, "계정 연결이 필요해요.");
    return header.Substring(7);
}
app.MapGet("/v1/release", () => clientRelease.Policy);
app.MapGet("/health", () => new { status = "ok", version = AppRelease.Version, commerce = "disabled", maintenance = operations.Maintenance });
app.MapGet("/health/ready", () => DatabaseMaintenance.Ready(databasePath) ? Results.Ok(new { status = "ready" }) : Results.Json(new { status = "unavailable" },statusCode:503));
app.MapPost("/v1/auth/guest", (GuestRequest body, GameRepository repo) => { operations.CheckRegistration(body.inviteCode); return repo.Register(body.secret); }).RequireRateLimiting("register");
app.MapPost("/v1/auth/google/challenge",(HttpContext ctx,GoogleChallengeRequest body,GoogleIdentityFlow flow)=>flow.Begin(body.link?Token(ctx):null,body)).RequireRateLimiting("register");
app.MapPost("/v1/auth/google/finish",(HttpContext ctx,GoogleLoginRequest body,GoogleIdentityFlow flow)=>flow.Finish(body.link?Token(ctx):null,body)).RequireRateLimiting("register");
app.MapGet("/v1/bootstrap", (HttpContext ctx, GameRepository repo) => repo.Bootstrap(Token(ctx)));
app.MapPost("/v1/commands", (HttpContext ctx, GameCommand body, GameRepository repo) => repo.Apply(Token(ctx), body)).RequireRateLimiting("commands");
app.MapGet("/v1/commerce", (HttpContext ctx, GameRepository repo,GooglePlayPurchases play) => repo.Commerce(Token(ctx),play.Enabled));
app.MapPost("/v1/commerce/verify", (HttpContext ctx,PurchaseRequest body,GameRepository repo,GooglePlayPurchases play) => repo.VerifyPurchase(Token(ctx),body,play,ctx.RequestAborted)).RequireRateLimiting("commands");
app.MapGet("/v1/profiles", (HttpContext ctx, GameRepository repo, string after = "") => repo.Gallery(Token(ctx), after));
app.MapGet("/v1/profiles/{id}", (HttpContext ctx, string id, GameRepository repo) => repo.GetProfile(Token(ctx), id));
app.MapPost("/v1/profiles/{id}/like", (HttpContext ctx, string id, GameCommand body, GameRepository repo) => repo.Like(Token(ctx), id.ToUpperInvariant(), body.enabled));
app.MapPost("/v1/profiles/{id}/block", (HttpContext ctx, string id, GameCommand body, GameRepository repo) => { repo.Block(Token(ctx), id.ToUpperInvariant(), body.enabled); return new ServerReply { success = true, message = body.enabled ? "프로필을 차단했어요." : "차단을 해제했어요." }; });
app.MapPost("/v1/profiles/{id}/report", (HttpContext ctx, string id, GameCommand body, GameRepository repo) => { repo.Report(Token(ctx), id.ToUpperInvariant(), body.value); return new ServerReply { success = true, message = "신고를 접수했어요." }; });
app.MapGet("/v1/weekly", (HttpContext ctx, GameRepository repo) => repo.Weekly(Token(ctx)));
app.MapGet("/v1/skill", (HttpContext ctx, GameRepository repo) => repo.Weekly(Token(ctx),true));
app.MapGet("/v1/guild", (HttpContext ctx, GameRepository repo) => repo.Guild(Token(ctx)));
app.MapGet("/v1/blocks", (HttpContext ctx, GameRepository repo) => repo.Blocks(Token(ctx)));
app.MapDelete("/v1/account", (HttpContext ctx, GameRepository repo) => { repo.Delete(Token(ctx)); return new ServerReply { success = true, message = "서버 계정을 삭제했어요." }; });
app.Run();
