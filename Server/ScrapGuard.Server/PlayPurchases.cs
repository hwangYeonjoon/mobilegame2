using System.Net.Http.Headers;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using ScrapGuard.Core;
namespace ScrapGuard.Server;

public sealed record VerifiedPurchase(string Token,string Product,string Binding,bool Purchased,bool Canceled,bool Test,int Quantity);
public interface IPlayPurchases
{
    bool Enabled {get;}
    bool LicenseTest => true;
    Task<VerifiedPurchase> Verify(PurchaseRequest request,CancellationToken ct);
}
// Server configuration separates real purchases from isolated license-test progress.
public interface IRefundPlay : IPlayPurchases
{
    Task<VerifiedPurchase> Lookup(string token,CancellationToken ct);
    Task<List<string>> Voided(DateTimeOffset from,DateTimeOffset until,CancellationToken ct);
}
public sealed class GooglePlayPurchases : IRefundPlay
{
    readonly HttpClient http=new(){Timeout=TimeSpan.FromSeconds(20)};
    readonly string package;
    GoogleCredential credential;
    public CommercePolicy Policy {get;}
    public bool Enabled => Policy.Enabled;
    public bool LicenseTest => Policy.LicenseTest;
    public GooglePlayPurchases(IConfiguration c)
    {
        package=c["PLAY_PACKAGE_NAME"];
        Policy=new CommercePolicy(c);
    }
    async Task<JsonDocument> Get(string path,CancellationToken ct)
    {
        if(!Enabled)throw new ApiFault(503,"Play 결제가 설정되지 않았어요.");
        credential??=(await GoogleCredential.GetApplicationDefaultAsync(ct)).CreateScoped("https://www.googleapis.com/auth/androidpublisher");
        string token=await credential.UnderlyingCredential.GetAccessTokenForRequestAsync(null,ct);
        using var req=new HttpRequestMessage(HttpMethod.Get,"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/"+Uri.EscapeDataString(package)+"/"+path);
        req.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
        using var response=await http.SendAsync(req,ct);
        if(!response.IsSuccessStatusCode)throw new ApiFault(503,"Google 구매 상태를 확인하지 못했어요. 잠시 후 복원을 시도해 주세요.");
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    }
    public async Task<VerifiedPurchase> Verify(PurchaseRequest r,CancellationToken ct)
    {
        if(r==null||CommerceCatalog.Find(r.productId)==null||string.IsNullOrEmpty(r.purchaseToken)||r.purchaseToken.Length>4096)throw new ApiFault(400,"올바른 구매 토큰이 필요해요.");
        using var doc=await Get("purchases/products/"+Uri.EscapeDataString(r.productId)+"/tokens/"+Uri.EscapeDataString(r.purchaseToken),ct);var x=doc.RootElement;
        int state=x.GetProperty("purchaseState").GetInt32();
        if(x.TryGetProperty("productId",out var product)&&product.GetString()!=r.productId)throw new ApiFault(400,"구매 상품이 일치하지 않아요.");
        return new VerifiedPurchase(r.purchaseToken,r.productId,x.TryGetProperty("obfuscatedExternalAccountId",out var binding)?binding.GetString():"",state==0,state==1,x.TryGetProperty("purchaseType",out var type)&&type.GetInt32()==0,x.TryGetProperty("quantity",out var quantity)?quantity.GetInt32():1);
    }
    public async Task<VerifiedPurchase> Lookup(string token,CancellationToken ct)
    {
        if(string.IsNullOrEmpty(token)||token.Length>4096)throw new ApiFault(400,"Invalid purchase token.");
        using var doc=await Get("purchases/productsv2/tokens/"+Uri.EscapeDataString(token),ct);var x=doc.RootElement;
        var items=x.GetProperty("productLineItem");if(items.GetArrayLength()!=1)throw new ApiFault(409,"Unsupported multi-item purchase.");
        var item=items[0];string state=x.GetProperty("purchaseStateContext").GetProperty("purchaseState").GetString();
        int quantity=item.TryGetProperty("productOfferDetails",out var details)&&details.TryGetProperty("quantity",out var q)?q.GetInt32():1;
        return new(token,item.GetProperty("productId").GetString(),x.TryGetProperty("obfuscatedExternalAccountId",out var binding)?binding.GetString():"",state=="PURCHASED",state=="CANCELLED",x.TryGetProperty("testPurchaseContext",out _),quantity);
    }
    public async Task<List<string>> Voided(DateTimeOffset from,DateTimeOffset until,CancellationToken ct)
    {
        var result=new List<string>();string page="";var seen=new HashSet<string>();
        do
        {
            using var doc=await Get("purchases/voidedpurchases?type=0&maxResults=1000&startTime="+from.ToUnixTimeMilliseconds()+"&endTime="+until.ToUnixTimeMilliseconds()+""+(page.Length>0?"&token="+Uri.EscapeDataString(page):""),ct);
            var x=doc.RootElement;if(x.TryGetProperty("voidedPurchases",out var list))foreach(var p in list.EnumerateArray())if(p.TryGetProperty("purchaseToken",out var token))result.Add(token.GetString());
            page=x.TryGetProperty("tokenPagination",out var pagination)&&pagination.TryGetProperty("nextPageToken",out var next)?next.GetString():"";
        if(!string.IsNullOrEmpty(page)&&!seen.Add(page))throw new ApiFault(503,"Repeated Google pagination token.");
        }while(!string.IsNullOrEmpty(page));return result;
    }
}
public sealed class RefundService(IRefundPlay play,GameRepository repo,Func<DateTimeOffset> clock=null)
{
    public async Task Run(CancellationToken ct)
    {
        if(!play.Enabled)return;
        foreach(var job in repo.RefundJobs())
        {
            try {var p=await play.Lookup(repo.UnprotectPurchase(job.Cipher),ct);if((job.Kind=="cancel"||job.Kind=="voidedPurchaseNotification")&&!p.Canceled)throw new ApiFault(503,"Cancellation not visible yet.");repo.ApplyRefundVerification(p,play.LicenseTest);repo.FinishRefundJob(job,true);}
            catch(OperationCanceledException)when(ct.IsCancellationRequested){throw;}
            catch{repo.FinishRefundJob(job,false);}
        }
        Exception scanError=null;var now=clock?.Invoke()??DateTimeOffset.UtcNow;var cursor=repo.RefundCursor();
        if(cursor==null||now-cursor.Value>=TimeSpan.FromHours(1))
        {
            var floor=now.AddDays(-30).AddMinutes(1);var from=cursor?.AddHours(-24)??floor;if(from<floor)from=floor;
            try {foreach(var token in await play.Voided(from,now,ct))repo.RevokePurchase(token);
                repo.CompleteRefundScan(now,cursor!=null&&cursor<floor);}
            catch(OperationCanceledException)when(ct.IsCancellationRequested){throw;}
            catch(Exception ex){scanError=ex;}
        }
        foreach(var row in repo.PurchaseChecks(play.LicenseTest))
        {
            try {var p=await play.Lookup(repo.UnprotectPurchase(row.Cipher),ct);repo.ApplyRefundVerification(p,play.LicenseTest);repo.FinishPurchaseCheck(row,true);}
            catch(OperationCanceledException)when(ct.IsCancellationRequested){throw;}
            catch{repo.FinishPurchaseCheck(row,false);}
        }
        repo.PurgeRefundInbox();
        if(scanError!=null)throw scanError;
    }
}
public sealed class PurchaseReconciler(GooglePlayPurchases play,GameRepository repo,ILogger<PurchaseReconciler> logger,ServiceMonitor monitor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if(!play.Enabled)return;var service=new RefundService(play,repo);
        while(!ct.IsCancellationRequested)
        {
            try{await service.Run(ct);monitor.RefundResult(true);}
            catch(OperationCanceledException)when(ct.IsCancellationRequested){break;}
            catch(Exception ex){monitor.RefundResult(false);logger.LogWarning("Play reconciliation failed ({Type}); retry scheduled",ex.GetType().Name);}
            await Task.Delay(TimeSpan.FromMinutes(1),ct);
        }
    }
}
