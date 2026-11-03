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
public sealed class GooglePlayPurchases : IPlayPurchases
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
    public async Task<List<string>> Voided(CancellationToken ct)
    {
        var result=new List<string>();string page="";
        do
        {
            using var doc=await Get("purchases/voidedpurchases?type=0&maxResults=1000"+(page.Length>0?"&token="+Uri.EscapeDataString(page):""),ct);
            var x=doc.RootElement;if(x.TryGetProperty("voidedPurchases",out var list))foreach(var p in list.EnumerateArray())if(p.TryGetProperty("purchaseToken",out var token))result.Add(token.GetString());
            page=x.TryGetProperty("tokenPagination",out var pagination)&&pagination.TryGetProperty("nextPageToken",out var next)?next.GetString():"";
        }while(!string.IsNullOrEmpty(page));return result;
    }
}
public sealed class PurchaseReconciler(GooglePlayPurchases play,GameRepository repo,ILogger<PurchaseReconciler> logger,ServiceMonitor monitor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if(!play.Enabled)return;
        while(!ct.IsCancellationRequested)
        {
            try{foreach(string token in await play.Voided(ct))repo.RevokePurchase(token);monitor.RefundResult(true);logger.LogInformation("Play refund reconciliation completed");}
            catch(OperationCanceledException)when(ct.IsCancellationRequested){break;}
            catch(Exception ex){monitor.RefundResult(false);logger.LogWarning("Play refund reconciliation failed ({Type}); retry scheduled",ex.GetType().Name);}
            await Task.Delay(TimeSpan.FromHours(1),ct);
        }
    }
}
