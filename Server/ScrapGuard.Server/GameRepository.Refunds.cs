using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
namespace ScrapGuard.Server;

public sealed record RefundJob(string Id,string Cipher,int Attempts,string Kind);
public sealed record PurchaseCheck(string Key,string Cipher,string Product,string Owner,bool? Test);
public sealed partial class GameRepository
{
    Lazy<IDataProtector> purchaseProtector;
    void InitializeRefunds(SqliteConnection db,string path)
    {
        purchaseProtector=new(()=>{
            var folder=Directory.CreateDirectory(Path.GetFullPath(path)+".keys");
            if(!OperatingSystem.IsWindows())File.SetUnixFileMode(folder.FullName,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
            return DataProtectionProvider.Create(folder,x=>x.SetApplicationName("ScrapGuard")).CreateProtector("play-purchase-v1");
        });
        var columns=ReadRows(db,"PRAGMA table_info(purchases)").Select(x=>(string)x["name"]).ToHashSet();
        foreach(var column in new[]{("token_cipher","TEXT"),("verified_at","TEXT"),("attempt_at","TEXT"),("verify_error","TEXT"),("is_test","INTEGER")})
            if(!columns.Contains(column.Item1))Execute(db,null,$"ALTER TABLE purchases ADD COLUMN {column.Item1} {column.Item2}");
        Execute(db,null,"""
        CREATE TABLE IF NOT EXISTS refund_inbox(id TEXT PRIMARY KEY,digest TEXT NOT NULL,kind TEXT NOT NULL,token_hash TEXT NOT NULL,cipher TEXT NOT NULL,status TEXT NOT NULL,attempts INTEGER NOT NULL DEFAULT 0,next_attempt TEXT NOT NULL,created TEXT NOT NULL,error TEXT);
        CREATE INDEX IF NOT EXISTS refund_pending ON refund_inbox(status,next_attempt);
        CREATE TABLE IF NOT EXISTS refund_cursor(id INTEGER PRIMARY KEY CHECK(id=1),completed TEXT,gap INTEGER NOT NULL DEFAULT 0);
        INSERT OR IGNORE INTO refund_cursor(id) VALUES(1);
        """);
    }
    string ProtectPurchase(string token)=>purchaseProtector.Value.Protect(token);
    public string UnprotectPurchase(string cipher)=>purchaseProtector.Value.Unprotect(cipher);
    public void EnqueueRtdn(RtdnEvent e)
    {
        lock(gate)
        {
            using var db=Open();using var tx=db.BeginTransaction();
            using(var q=Sql(db,tx,"SELECT digest FROM refund_inbox WHERE id=$id",("$id",e.MessageId)))
            {var previous=q.ExecuteScalar() as string;if(previous!=null){if(previous!=e.Digest)throw new ApiFault(409,"Push message collision.");return;}}
            using(var q=Sql(db,tx,"SELECT COUNT(*) FROM refund_inbox WHERE status='pending'"))if((long)q.ExecuteScalar()>=100000)throw new ApiFault(503,"Push backlog is full.");
            string now=clock().ToUniversalTime().ToString("O");
            Execute(db,tx,"INSERT INTO refund_inbox(id,digest,kind,token_hash,cipher,status,next_attempt,created) VALUES($id,$digest,$kind,$hash,$cipher,$status,$now,$now)",
                ("$id",e.MessageId),("$digest",e.Digest),("$kind",e.Kind),("$hash",string.IsNullOrEmpty(e.Token)?"":Hash(e.Token)),("$cipher",string.IsNullOrEmpty(e.Token)?"":ProtectPurchase(e.Token)),("$status",string.IsNullOrEmpty(e.Token)?"ignored":"pending"),("$now",now));
            tx.Commit();
        }
    }
    public List<RefundJob> RefundJobs()
    {using var db=Open();return ReadRows(db,"SELECT id,cipher,attempts,kind FROM refund_inbox WHERE status='pending' AND next_attempt<=$now ORDER BY next_attempt,id LIMIT 25",("$now",clock().ToUniversalTime().ToString("O"))).Select(x=>new RefundJob((string)x["id"],(string)x["cipher"],Convert.ToInt32(x["attempts"]),(string)x["kind"])).ToList();}
    public void FinishRefundJob(RefundJob job,bool success)
    {
        using var db=Open();Execute(db,null,"UPDATE refund_inbox SET status=$status,attempts=attempts+1,next_attempt=$next,error=$error,cipher=CASE WHEN $ok=1 THEN '' ELSE cipher END WHERE id=$id AND status='pending'",
            ("$status",success?"done":"pending"),("$next",clock().AddMinutes(Math.Min(60,Math.Pow(2,Math.Min(job.Attempts,6)))).ToUniversalTime().ToString("O")),("$error",success?null:"verification_failed"),("$ok",success?1:0),("$id",job.Id));
    }
    public List<PurchaseCheck> PurchaseChecks(bool test)
    {
        using var db=Open();return ReadRows(db,"SELECT token_hash,token_cipher,product,owner,is_test FROM purchases WHERE revoked=0 AND owner IS NOT NULL AND token_cipher IS NOT NULL AND (is_test=$test OR is_test IS NULL) AND (attempt_at IS NULL OR attempt_at<$due) ORDER BY COALESCE(attempt_at,''),token_hash LIMIT 100",
            ("$test",test?1:0),("$due",clock().AddDays(-1).ToUniversalTime().ToString("O"))).Select(x=>new PurchaseCheck((string)x["token_hash"],(string)x["token_cipher"],(string)x["product"],(string)x["owner"],x["is_test"]==null?null:Convert.ToInt32(x["is_test"])==1)).ToList();
    }
    public void FinishPurchaseCheck(PurchaseCheck row,bool success)
    {using var db=Open();Execute(db,null,"UPDATE purchases SET attempt_at=$now,verified_at=CASE WHEN $ok=1 THEN $now ELSE verified_at END,verify_error=$error WHERE token_hash=$key",("$now",clock().ToUniversalTime().ToString("O")),("$ok",success?1:0),("$error",success?null:"verification_failed"),("$key",row.Key));}
    public void ApplyRefundVerification(VerifiedPurchase p,bool test)
    {
        if(!p.Purchased&&!p.Canceled)throw new ApiFault(503,"Purchase state is not settled.");
        if(p.Test!=test||p.Quantity!=1||CommerceCatalog.Find(p.Product)==null)throw new ApiFault(409,"Unexpected purchase context.");
        using(var db=Open())
        using(var q=Sql(db,null,"SELECT owner,product,is_test FROM purchases WHERE token_hash=$key",("$key",Hash(p.Token))))
        using(var r=q.ExecuteReader())if(r.Read()&&!r.IsDBNull(0))
        {
            if(r.GetString(1)!=p.Product||Hash("play:"+r.GetString(0)).ToLowerInvariant()!=p.Binding||!r.IsDBNull(2)&&(r.GetInt32(2)==1)!=p.Test)throw new ApiFault(409,"Purchase binding mismatch.");
        }
        if(p.Canceled)RevokePurchase(p.Token);
    }
    public DateTimeOffset? RefundCursor()
    {using var db=Open();using var q=Sql(db,null,"SELECT completed FROM refund_cursor WHERE id=1");return q.ExecuteScalar() is string value?DateTimeOffset.Parse(value):null;}
    public void CompleteRefundScan(DateTimeOffset until,bool gap)
    {using var db=Open();Execute(db,null,"UPDATE refund_cursor SET completed=$time,gap=MAX(gap,$gap) WHERE id=1",("$time",until.ToUniversalTime().ToString("O")),("$gap",gap?1:0));}
    public object RefundHealth()
    {
        using var db=Open();
        var cursor=ReadRows(db,"SELECT completed,gap FROM refund_cursor WHERE id=1")[0];
        var pending=ReadRows(db,"SELECT COUNT(*) AS count,MIN(created) AS oldest,COALESCE(MAX(attempts),0) AS attempts FROM refund_inbox WHERE status='pending'")[0];
        var coverage=ReadRows(db,"SELECT COALESCE(SUM(CASE WHEN token_cipher IS NULL THEN 1 ELSE 0 END),0) AS missingTokens,COALESCE(SUM(CASE WHEN verify_error IS NOT NULL THEN 1 ELSE 0 END),0) AS failures,MIN(verified_at) AS oldestVerified FROM purchases WHERE owner IS NOT NULL AND revoked=0")[0];
        bool attention=Convert.ToInt64(cursor["gap"])>0||Convert.ToInt64(coverage["missingTokens"])>0||Convert.ToInt64(coverage["failures"])>0||Convert.ToInt64(pending["attempts"])>0||pending["oldest"] is string oldest&&clock()-DateTimeOffset.Parse(oldest)>TimeSpan.FromMinutes(10);
        return new {cursor,pending,coverage,needsAttention=attention};
    }
    public void PurgeRefundInbox(){using var db=Open();Execute(db,null,"DELETE FROM refund_inbox WHERE status<>'pending' AND created<$cutoff",("$cutoff",clock().AddDays(-35).ToUniversalTime().ToString("O")));}
}
