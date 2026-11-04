using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Google.Apis.Auth;
using ScrapGuard.Core;
namespace ScrapGuard.Server;

public sealed class RtdnPolicy
{
    public bool Enabled {get;}
    public string Audience {get;} public string Email {get;} public string Subscription {get;} public string Package {get;}
    public RtdnPolicy(IConfiguration c)
    {
        Enabled=c.GetValue("RTDN_ENABLED",false);Audience=c["RTDN_AUDIENCE"]??"";Email=c["RTDN_SERVICE_ACCOUNT_EMAIL"]??"";
        Subscription=c["RTDN_SUBSCRIPTION"]??"";Package=c["PLAY_PACKAGE_NAME"]??AppRelease.Package;
        if(Enabled && (!new CommercePolicy(c).Enabled || !Uri.TryCreate(Audience,UriKind.Absolute,out var uri)||uri.Scheme!="https"||!Email.EndsWith(".gserviceaccount.com",StringComparison.Ordinal)||!Regex.IsMatch(Subscription,@"^projects/[^/]+/subscriptions/[^/]+$")||Package!=AppRelease.Package))
            throw new InvalidOperationException("RTDN requires commerce, HTTPS audience, push service account, subscription and game package.");
    }
}
public interface IRtdnIdentity { Task Validate(string bearer,RtdnPolicy policy); }
public sealed class GoogleRtdnIdentity : IRtdnIdentity
{
    public async Task Validate(string bearer,RtdnPolicy policy)
    {
        if(string.IsNullOrEmpty(bearer)||bearer.Length>16000)throw new ApiFault(401,"Invalid push identity.");
        try
        {
            var p=await GoogleJsonWebSignature.ValidateAsync(bearer,new GoogleJsonWebSignature.ValidationSettings{Audience=[policy.Audience]});
            if(!p.EmailVerified||p.Email!=policy.Email||(p.Issuer!="accounts.google.com"&&p.Issuer!="https://accounts.google.com"))throw new ApiFault(401,"Unexpected push identity.");
        }
        catch(InvalidJwtException){throw new ApiFault(401,"Invalid push identity.");}
    }
}
public sealed record RtdnEvent(string MessageId,string Digest,string Kind,string Token);
public static class RtdnParser
{
    public static RtdnEvent Parse(string json,RtdnPolicy policy)
    {
        try
        {
            if(json==null||Encoding.UTF8.GetByteCount(json)>32768)throw new FormatException();
            using var outer=JsonDocument.Parse(json);var root=outer.RootElement;
            if(root.GetProperty("subscription").GetString()!=policy.Subscription)throw new ApiFault(403,"Unexpected subscription.");
            var message=root.GetProperty("message");string id=message.GetProperty("messageId").GetString();
            if(!Regex.IsMatch(id??"",@"^[A-Za-z0-9_-]{1,200}$"))throw new FormatException();
            byte[] bytes=Convert.FromBase64String(message.GetProperty("data").GetString());
            using var body=JsonDocument.Parse(bytes);var data=body.RootElement;
            if(data.GetProperty("packageName").GetString()!=policy.Package)throw new ApiFault(403,"Unexpected package.");
            string[] kinds=["oneTimeProductNotification","voidedPurchaseNotification","testNotification","subscriptionNotification","pendingRefundReviewNotification"];
            var present=kinds.Where(k=>data.TryGetProperty(k,out _)).ToArray();if(present.Length!=1)throw new FormatException();
            string kind=present[0],token="";
            if(kind=="oneTimeProductNotification"||kind=="voidedPurchaseNotification")
            {
                var item=data.GetProperty(kind);
                if(kind=="voidedPurchaseNotification"&&item.GetProperty("productType").GetInt32()!=2)kind="unsupported";
                else {token=item.GetProperty("purchaseToken").GetString();if(string.IsNullOrEmpty(token)||token.Length>4096)throw new FormatException();}
            }
            if(kind=="oneTimeProductNotification"&&data.GetProperty(kind).GetProperty("notificationType").GetInt32()==2)kind="cancel";
            return new(id,Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)),kind,token);
        }
        catch(Exception e)when(e is JsonException or FormatException or InvalidOperationException or KeyNotFoundException or ArgumentNullException){throw new ApiFault(400,"Malformed RTDN message.");}
    }
}
public sealed class RtdnReceiver(RtdnPolicy policy,IRtdnIdentity identity,GameRepository repo)
{
    public async Task Receive(string authorization,string body)
    {
        if(!policy.Enabled)throw new ApiFault(503,"RTDN is disabled.");
        if(!authorization.StartsWith("Bearer ",StringComparison.Ordinal))throw new ApiFault(401,"Push authentication required.");
        await identity.Validate(authorization[7..],policy);
        repo.EnqueueRtdn(RtdnParser.Parse(body,policy)); // Only acknowledge after a durable commit.
    }
}
