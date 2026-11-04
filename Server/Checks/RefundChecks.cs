using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using ScrapGuard.Core;
using ScrapGuard.Server;
static class RefundChecks
{
    sealed class Play : IRefundPlay
    {
        public bool Enabled=>true;public bool LicenseTest=>false;public bool FailList,FailLookup;public string Binding;public HashSet<string> Canceled=new();
        public Task<VerifiedPurchase> Verify(PurchaseRequest p,CancellationToken ct)=>Task.FromResult(new VerifiedPurchase(p.purchaseToken,p.productId,Binding,true,false,false,1));
        public Task<VerifiedPurchase> Lookup(string token,CancellationToken ct)=>FailLookup?throw new IOException("offline"):Task.FromResult(new VerifiedPurchase(token,"gems_600",Binding,!Canceled.Contains(token),Canceled.Contains(token),false,1));
        public Task<List<string>> Voided(DateTimeOffset from,DateTimeOffset until,CancellationToken ct)=>FailList?throw new IOException("offline"):Task.FromResult(new List<string>());
    }
    sealed class Identity : IRtdnIdentity
    {public Task Validate(string bearer,RtdnPolicy p){if(bearer!="valid")throw new ApiFault(401,"Invalid");return Task.CompletedTask;}}
    public static void Run(string folder,Action<bool,string> check)
    {
        var now=new DateTimeOffset(2026,11,4,0,0,0,TimeSpan.Zero);string path=Path.Combine(folder,"refunds.db"),key=new string('a',43);var repo=new GameRepository(path,()=>now);repo.Register(key);
        var play=new Play{Binding=repo.Commerce(key,false).accountBinding};
        void Purchase(string token)=>repo.VerifyPurchase(key,new PurchaseRequest{productId="gems_600",purchaseToken=token},play).GetAwaiter().GetResult();
        void Run()=>new RefundService(play,repo,()=>now).Run(default).GetAwaiter().GetResult();
        void Fails(Action action,int status,string name){try{action();throw new Exception("Expected "+name);}catch(ApiFault e){check(e.Status==status,name);}}
        object Sql(string sql){using var db=new SqliteConnection("Data Source="+path);db.Open();using var q=db.CreateCommand();q.CommandText=sql;return q.ExecuteScalar();}
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string>{{"COMMERCE_MODE","license_test"},{"PLAY_PACKAGE_NAME",AppRelease.Package},{"RTDN_ENABLED","true"},{"RTDN_AUDIENCE","https://example.com/v1/commerce/rtdn"},{"RTDN_SERVICE_ACCOUNT_EMAIL","push@example.iam.gserviceaccount.com"},{"RTDN_SUBSCRIPTION","projects/example/subscriptions/refunds"}}).Build();
        var policy=new RtdnPolicy(config);var receiver=new RtdnReceiver(policy,new Identity(),repo);
        string Message(string id,string token,string package=null)=>JsonSerializer.Serialize(new{subscription=policy.Subscription,message=new{messageId=id,data=Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{packageName=package??AppRelease.Package,voidedPurchaseNotification=new{purchaseToken=token,productType=2}})))}});
        void Push(string id,string token)=>receiver.Receive("Bearer valid",Message(id,token)).GetAwaiter().GetResult();
        Fails(()=>receiver.Receive("Bearer bad",Message("1","paid-one")).GetAwaiter().GetResult(),401,"RTDN rejects invalid provider identity");
        Fails(()=>receiver.Receive("Bearer valid",Message("1","paid-one","wrong")).GetAwaiter().GetResult(),403,"RTDN rejects other package");
        Fails(()=>RtdnParser.Parse("{}",policy),400,"malformed push rejected");
        Purchase("paid-one");check(!((string)Sql("SELECT token_cipher FROM purchases")).Contains("paid-one"),"purchase token stored encrypted");
        string backup=Path.Combine(folder,"refund-backup.db");DatabaseMaintenance.Backup(path,backup);
        var backupRepo=new GameRepository(backup,()=>now.AddDays(2));check(backupRepo.UnprotectPurchase(backupRepo.PurchaseChecks(false)[0].Cipher)=="paid-one","backup restores both database and decryptable key ring");
        Push("1","paid-one");Push("1","paid-one");check((long)Sql("SELECT COUNT(*) FROM refund_inbox")==1,"RTDN receipt deduplicates durably");
        Fails(()=>Push("1","changed"),409,"RTDN message collision cannot overwrite token");
        Run();check(repo.Bootstrap(key).state.gems==600&&(long)Sql("SELECT attempts FROM refund_inbox")==1,"eventual API cancellation retries without premature revocation");
        now=now.AddMinutes(2);play.Canceled.Add("paid-one");repo=new GameRepository(path,()=>now);Run();Run();
        check(repo.Bootstrap(key).state.gems==0&&(string)Sql("SELECT status FROM refund_inbox")=="done","durable inbox survives restart and revokes once");
        check((string)Sql("SELECT cipher FROM refund_inbox")==""&&Sql("SELECT token_cipher FROM purchases") is DBNull,"completed refunds erase decryptable tokens");
        Purchase("long-outage");repo.CompleteRefundScan(now,false);now=now.AddDays(40);play.Canceled.Add("long-outage");Run();
        check(repo.Bootstrap(key).state.gems==0&&(long)Sql("SELECT gap FROM refund_cursor")==1,"stored token recovers refund beyond 30-day list window");
        Purchase("list-outage");now=now.AddDays(2);play.Canceled.Add("list-outage");play.FailList=true;var checkpoint=repo.RefundCursor();try{Run();}catch(IOException){}
        check(repo.Bootstrap(key).state.gems==0&&repo.RefundCursor()==checkpoint,"individual recovery continues when voided list fails without advancing cursor");play.FailList=false;
        Purchase("retry-later");now=now.AddDays(2);play.FailLookup=true;Push("2","retry-later");Run();
        check(repo.Bootstrap(key).state.gems==600&&(long)Sql("SELECT COUNT(*) FROM purchases WHERE verify_error IS NOT NULL AND revoked=0")==1,"API failure keeps entitlement and exposes verification failure");play.FailLookup=false;
        play.Binding="wrong";now=now.AddMinutes(2);play.Canceled.Add("retry-later");Run();check(repo.Bootstrap(key).state.gems==600,"binding mismatch cannot revoke account purchase");play.Binding=repo.Commerce(key,false).accountBinding;
        play.Canceled.Add("before-grant");Push("3","before-grant");Run();Fails(()=>Purchase("before-grant"),409,"early refund tombstone prevents later grant");
        repo.Delete(key);check((long)Sql("SELECT COUNT(*) FROM purchases WHERE token_cipher IS NOT NULL")==0&&(long)Sql("SELECT COUNT(*) FROM refund_inbox WHERE status='pending'")==0,"deleting account clears queued and stored tokens");
        check(ClientRelease.Exempt("/v1/commerce/rtdn","POST"),"Google push bypasses game build headers");
    }
}
