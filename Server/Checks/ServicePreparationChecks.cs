using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ScrapGuard.Core;
using ScrapGuard.Server;
static class ServicePreparationChecks
{
    sealed class Provider : IPlayPurchases
    {
        public bool Enabled=>true;
        public bool LicenseTest{get;set;}
        public string Binding;public bool Test;
        public Task<VerifiedPurchase> Verify(PurchaseRequest p,CancellationToken ct)=>Task.FromResult(new VerifiedPurchase(p.purchaseToken,p.productId,Binding,true,false,Test,1));
    }
    public static void Run(string folder,Action<bool,string> check)
    {
        IConfiguration Config(params (string,string)[] values)=>new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(x=>x.Item1,x=>x.Item2)).Build();
        void Fails(Action a,int code,string name){try{a();throw new Exception("Expected failure: "+name);}catch(ApiFault e){check(e.Status==code,name);}}
        void BadConfig(Action a,string name){try{a();throw new Exception("Expected invalid config: "+name);}catch(InvalidOperationException){check(true,name);}}
        string Secret()=>Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
        check(!new CommercePolicy(Config()).Enabled,"commerce disabled without external configuration");
        BadConfig(()=>new CommercePolicy(Config(("COMMERCE_MODE","production"),("PLAY_PACKAGE_NAME","game"))),"real payments require explicit release gate");
        BadConfig(()=>new CommercePolicy(Config(("COMMERCE_MODE","typo"))),"invalid commerce mode fails closed");
        var production=new CommercePolicy(Config(("COMMERCE_MODE","production"),("PLAY_PACKAGE_NAME","game"),("COMMERCE_RELEASE_READY","true"),("COMMERCE_SALES_ENABLED","true"),("COMMERCE_DISABLED_PRODUCTS","ad_skip_lifetime")));
        check(production.SaleProducts.Length==4&&!production.LicenseTest,"production can stop one product without disabling verification");
        var now=DateTimeOffset.UtcNow;string key=Secret(),viewer=Secret();var repo=new GameRepository(Path.Combine(folder,"service-prep.db"),()=>now);var initial=repo.Register(key);repo.Register(viewer);
        var provider=new Provider{Binding=repo.Commerce(key,false).accountBinding};
        var purchase=new PurchaseRequest{productId="gems_600",purchaseToken="real-purchase"};
        var paid=repo.VerifyPurchase(key,purchase,provider).GetAwaiter().GetResult();
        check(paid.state.paidGems==600&&!paid.state.testProgress,"verified production payment preserves ordinary progression");
        check(repo.VerifyPurchase(key,purchase,provider).GetAwaiter().GetResult().state.gems==600,"production retry cannot grant twice");
        provider.Test=true;Fails(()=>repo.VerifyPurchase(key,new PurchaseRequest{productId="gems_600",purchaseToken="test-on-prod"},provider).GetAwaiter().GetResult(),400,"production rejects license-test receipts");provider.Test=false;
        repo.RevokePurchase("real-purchase");check(repo.Bootstrap(key).state.gems==0,"production refund uses same durable ledger");
        var disabledSales=new CommercePolicy(Config(("COMMERCE_MODE","production"),("PLAY_PACKAGE_NAME","game"),("COMMERCE_RELEASE_READY","true")));
        check(repo.Commerce(key,disabledSales).enabled&&!repo.Commerce(key,disabledSales).salesEnabled,"sales pause retains receipt restoration availability");
        var off=new AdPolicy(Config());
        GameCommand Request(string p="parts")=>new("ad_request"){value=p};
        Fails(()=>repo.CreateAdClaim(key,Request(),off),503,"unconfigured ads cannot create reward claims");
        BadConfig(()=>new AdPolicy(Config(("ADS_MODE","production"),("ADMOB_REWARDED_UNIT_ID",AdPolicy.TestUnit))),"production ads cannot use sample units");
        var ads=new AdPolicy(Config(("ADS_MODE","test"),("ADMOB_REWARDED_UNIT_ID",AdPolicy.TestUnit)));
        var request=Request();var claim=repo.CreateAdClaim(key,request,ads);
        check(repo.CreateAdClaim(key,request,ads).id==claim.id,"claim request retries reuse reservation");
        Fails(()=>repo.CreateAdClaim(key,Request(),ads),409,"parallel ad requests cannot reserve same placement");
        Fails(()=>repo.AdClaimStatus(viewer,claim.id),404,"other account cannot inspect ad claim");
        VerifiedAd Callback(AdClaim c,string transaction)=>new(c.id,c.userId,ads.CallbackUnit,transaction,now.ToUnixTimeMilliseconds(),"reward",1);
        var callback=Callback(claim,"google-one");
        Fails(()=>repo.CompleteAd(callback with{User="other"},ads),400,"SSV must match reserved account");
        Fails(()=>repo.CompleteAd(callback with{Unit="wrong"},ads),400,"SSV must match game ad unit");
        Fails(()=>repo.CompleteAd(callback with{Amount=10},ads),400,"SSV amount cannot control game economy");
        Fails(()=>repo.CompleteAd(callback with{Timestamp=now.AddDays(-1).ToUnixTimeMilliseconds()},ads),400,"stale SSV timestamp rejected");
        int parts=repo.Bootstrap(key).state.parts;repo.CompleteAd(callback,ads);repo.CompleteAd(callback,ads);
        check(repo.Bootstrap(key).state.parts==parts+10&&repo.AdClaimStatus(key,claim.id).status=="granted","duplicate SSV grants once with persisted receipt");
        check(repo.Bootstrap(key).state.testProgress,"test ad rewards mark test progression");
        Fails(()=>repo.VerifyPurchase(key,new PurchaseRequest{productId="gems_600",purchaseToken="real-on-test-progress"},provider).GetAwaiter().GetResult(),409,"test progress cannot mix with production purchases");
        check(!repo.Commerce(key,production).salesEnabled,"test account sees production sales disabled");
        var second=repo.CreateAdClaim(key,Request(),ads);
        Fails(()=>repo.CompleteAd(Callback(second,"google-one"),ads),409,"transaction cannot be reused across claims");
        repo.AdClaimStatus(key,second.id,true);Fails(()=>repo.CompleteAd(Callback(second,"canceled"),ads),409,"canceled reward cannot grant later");
        var third=repo.CreateAdClaim(key,Request(),ads);now=now.AddMinutes(21);
        check(repo.AdClaimStatus(key,third.id).status=="expired","expired reservation releases slot");
        Fails(()=>repo.CompleteAd(Callback(third,"late"),ads),409,"expired callback cannot consume released quota");
        var fourth=repo.CreateAdClaim(key,Request(),ads);repo.CompleteAd(Callback(fourth,"google-two"),ads);
        Fails(()=>repo.CreateAdClaim(key,Request(),ads),400,"SSV daily parts quota enforced");
        check(repo.Bootstrap(key).state.dailyRewardParts==2,"cancellation and expiry do not consume completed quota");
        now=now.AddDays(1);
        provider.LicenseTest=true;provider.Test=true;
        repo.VerifyPurchase(key,new PurchaseRequest{productId="ad_skip_lifetime",purchaseToken="verified-skip"},provider).GetAwaiter().GetResult();
        var skipRequest=Request();var skipped=repo.CreateAdClaim(key,skipRequest,off);
        check(skipped.status=="granted"&&repo.Bootstrap(key).state.dailyRewardParts==1,"verified skip uses reserved server reward without an ad impression");
        repo.CreateAdClaim(key,skipRequest,off);check(repo.Bootstrap(key).state.dailyRewardParts==1,"skip retry shares exactly-once grant");
        repo.RevokePurchase("verified-skip");
        Fails(()=>repo.CreateAdClaim(key,Request(),off),503,"refunded skip cannot bypass disabled ads");
        var monitor=new ServiceMonitor();monitor.RefundResult(false);
        check(JsonSerializer.Serialize(monitor.Snapshot(true)).Contains("\"needsAttention\":true"),"refund reconciliation failure becomes observable");
        monitor.RefundResult(true);check(JsonSerializer.Serialize(monitor.Snapshot(true)).Contains("\"needsAttention\":false"),"successful reconciliation clears alert");
        // Cryptographic verification uses raw URL bytes, not reconstructed query values.
        using var signer=ECDsa.Create(ECCurve.NamedCurves.nistP256);
        string pem=signer.ExportSubjectPublicKeyInfoPem();
        string Sign(string raw)=>"?"+raw+"&signature="+Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(signer.SignData(Encoding.UTF8.GetBytes(raw),HashAlgorithmName.SHA256,DSASignatureFormat.Rfc3279DerSequence))+"&key_id=1";
        string data="ad_unit="+ads.CallbackUnit+"&custom_data=abc&reward_amount=1&reward_item=reward&timestamp="+now.ToUnixTimeMilliseconds()+"&transaction_id=crypto-one&user_id=USER";
        string signed=Sign(data);
        check(AdMobVerifier.VerifyWithKey(signed,pem).Claim=="abc","valid ECDSA DER SSV signature accepted");
        Fails(()=>AdMobVerifier.VerifyWithKey(signed.Replace("custom_data=abc","custom_data=xyz"),pem),400,"tampered signed claim rejected");
        Fails(()=>AdMobVerifier.VerifyWithKey(signed+"&user_id=forged",pem),400,"unsigned trailing parameters rejected");
        Fails(()=>AdMobVerifier.VerifyWithKey(Sign(data+"&user_id=duplicate"),pem),400,"duplicate signed parameters rejected");
        check(ClientRelease.Exempt("/v1/ads/ssv","GET")&&!ClientRelease.Exempt("/v1/ads/claims","POST"),"only provider callback bypasses client version headers");
        ServerReply Apply(GameCommand c){c.revision=repo.Bootstrap(key).revision;return repo.Apply(key,c);}
        Apply(new GameCommand("visibility"){enabled=true});repo.Report(viewer,initial.playerId,"name");
        var hide=new OperatorAction(Guid.NewGuid().ToString("N"),"qa-operator",initial.playerId,"hide","Inappropriate public profile");repo.Operate(hide);
        Fails(()=>repo.GetProfile(viewer,initial.playerId),404,"moderated account is removed from public gallery");
        Fails(()=>Apply(new GameCommand("visibility"){enabled=true}),403,"moderated user cannot immediately republish");
        Fails(()=>Apply(new GameCommand("rename"){value="again"}),403,"moderated user cannot replace reviewed name");
        var compensation=new OperatorAction(Guid.NewGuid().ToString("N"),"qa-operator",initial.playerId,"compensate","Service interruption",1000,10);
        int gold=repo.Bootstrap(key).state.gold;repo.Operate(compensation);repo.Operate(compensation);
        check(repo.Bootstrap(key).state.gold==gold+1000,"support compensation retry is idempotent");
        Fails(()=>repo.Operate(compensation with{Gold=2000}),409,"audit request collision rejected");
        Fails(()=>repo.Operate(compensation with{RequestId=Guid.NewGuid().ToString("N"),Gold=10001}),400,"operator compensation capped");
        Fails(()=>repo.Operate(compensation with{RequestId=Guid.NewGuid().ToString("N"),Reason=""}),400,"operator action requires a reason");
        string account=JsonSerializer.Serialize(repo.OperationsAccount(initial.playerId));
        check(!account.Contains(key)&&!account.Contains("token_hash")&&!account.Contains("real-purchase"),"support export omits credentials and purchase tokens");
        repo.Operate(new OperatorAction(Guid.NewGuid().ToString("N"),"qa-operator",initial.playerId,"unhide","Review complete"));
        check(!repo.Bootstrap(key).state.publicProfile,"unhide preserves user choice to remain private");
        Apply(new GameCommand("visibility"){enabled=true});check(repo.GetProfile(viewer,initial.playerId)!=null,"reviewed user may opt back into gallery");
        check(JsonSerializer.Serialize(repo.OperationsSummary()).Contains("support_compensation"),"operator summary aggregates economic changes");
        repo.Delete(key);check(JsonSerializer.Serialize(repo.OperationsList("audit")).Contains("qa-operator"),"operator audit survives account deletion without credentials");
    }
}
