using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
namespace ScrapGuard.Server;

public sealed partial class GameRepository
{
    void InitializeMetrics(SqliteConnection db)
    {
        Execute(db,null,"""
        CREATE TABLE IF NOT EXISTS metrics_consent(owner TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,enabled INTEGER NOT NULL);
        CREATE TABLE IF NOT EXISTS outfit_previews(owner TEXT REFERENCES accounts(id) ON DELETE CASCADE,outfit INTEGER NOT NULL,day TEXT NOT NULL,created TEXT NOT NULL,PRIMARY KEY(owner,outfit,day));
        CREATE TABLE IF NOT EXISTS outfit_sales(owner TEXT REFERENCES accounts(id) ON DELETE CASCADE,request_id TEXT NOT NULL,outfit INTEGER NOT NULL,created TEXT NOT NULL,PRIMARY KEY(owner,request_id));
        CREATE TABLE IF NOT EXISTS metrics_activity(owner TEXT REFERENCES accounts(id) ON DELETE CASCADE,day TEXT NOT NULL,seen TEXT NOT NULL,PRIMARY KEY(owner,day));
        CREATE TABLE IF NOT EXISTS metrics_meta(id INTEGER PRIMARY KEY,started TEXT NOT NULL);
        CREATE INDEX IF NOT EXISTS preview_period ON outfit_previews(outfit,created,owner);
        CREATE INDEX IF NOT EXISTS sales_period ON outfit_sales(outfit,created,owner);
        CREATE INDEX IF NOT EXISTS activity_period ON metrics_activity(seen,owner);
        """);
        Execute(db,null,"INSERT OR IGNORE INTO metrics_meta VALUES(1,$now)",("$now",clock().ToUniversalTime().ToString("O")));
    }
    static bool MetricsConsent(SqliteConnection db,SqliteTransaction tx,string id)
    {using var q=Sql(db,tx,"SELECT enabled FROM metrics_consent WHERE owner=$id",("$id",id));return Convert.ToInt32(q.ExecuteScalar()??0)==1;}
    bool MetricsConsent(string id){using var db=Open();return MetricsConsent(db,null,id);}
    CommandResult SetMetricsConsent(SqliteConnection db,SqliteTransaction tx,Account a,bool enabled)
    {
        Execute(db,tx,"INSERT INTO metrics_consent VALUES($id,$enabled) ON CONFLICT(owner) DO UPDATE SET enabled=$enabled",("$id",a.Id),("$enabled",enabled?1:0));
        if(!enabled)Execute(db,tx,"DELETE FROM outfit_previews WHERE owner=$id",("$id",a.Id));
        return CommandResult.Ok(enabled?"온라인 의상 체험 통계에 동의했어요.":"체험 통계를 끄고 서버의 체험 기록을 삭제했어요.");
    }
    public void RecordOutfitPreview(string token,int outfit)
    {
        if(!Outfits.Valid(outfit))throw new ApiFault(400,"없는 의상이에요.");
        lock(gate)
        {
            using var db=Open();using var tx=db.BeginTransaction();var a=Load(db,tx,token);
            if(!MetricsConsent(db,tx,a.Id))throw new ApiFault(403,"체험 통계 동의가 필요해요.");
            if(!Outfits.Owned(a.State,outfit))Execute(db,tx,"INSERT OR IGNORE INTO outfit_previews VALUES($id,$outfit,$day,$now)",("$id",a.Id),("$outfit",outfit),("$day",clock().UtcDateTime.ToString("yyyy-MM-dd")),("$now",clock().ToUniversalTime().ToString("O")));
            Execute(db,tx,"DELETE FROM outfit_previews WHERE created<$cutoff",("$cutoff",clock().AddDays(-90).ToUniversalTime().ToString("O")));tx.Commit();
        }
    }
    void RecordMetricsCommand(SqliteConnection db,SqliteTransaction tx,Account a,GameCommand c)
    {
        Execute(db,tx,"INSERT INTO metrics_activity VALUES($id,$day,$now) ON CONFLICT(owner,day) DO UPDATE SET seen=$now",("$id",a.Id),("$day",clock().UtcDateTime.ToString("yyyy-MM-dd")),("$now",clock().ToUniversalTime().ToString("O")));
        int outfit=c.type=="cosmetic_buy"?Outfits.Find(c.value):0;
        if(outfit>0)Execute(db,tx,"INSERT INTO outfit_sales VALUES($id,$request,$outfit,$now)",("$id",a.Id),("$request",c.requestId),("$outfit",outfit),("$now",clock().ToUniversalTime().ToString("O")));
    }
    public void PurgeMetrics()
    {
        using var db=Open();using var tx=db.BeginTransaction();
        string cutoff=clock().AddDays(-90).ToUniversalTime().ToString("O");
        Execute(db,tx,"DELETE FROM outfit_previews WHERE created<$cutoff",("$cutoff",cutoff));
        Execute(db,tx,"DELETE FROM metrics_activity WHERE seen<$cutoff",("$cutoff",cutoff));tx.Commit();
    }
    public object MetricsDashboard(bool includeTests=false)
    {
        using var db=Open();using(var clean=Sql(db,null,"DELETE FROM outfit_previews WHERE created<$cutoff",("$cutoff",clock().AddDays(-90).ToUniversalTime().ToString("O"))))clean.ExecuteNonQuery();
        Execute(db,null,"DELETE FROM metrics_activity WHERE seen<$cutoff",("$cutoff",clock().AddDays(-90).ToUniversalTime().ToString("O")));
        using var tx=db.BeginTransaction(deferred:true);var accounts=ReadRows(db,tx,"SELECT id,state FROM accounts").Select(x=>(Id:(string)x["id"],State:Decode<GameState>((string)x["state"]))).Where(x=>includeTests||!x.State.testProgress).ToList();
        string filter=includeTests?"1=1":"COALESCE(json_extract(a.state,'$.testProgress'),0)=0";
        var owned=new int[Outfits.Count+1];var equipped=new int[Outfits.Count+1];
        foreach(var a in accounts){Social.Normalize(a.State);for(int i=1;i<=Outfits.Count;i++){if(Outfits.Owned(a.State,i))owned[i]++;if(Outfits.Active(a.State,Outfits.Hero(i))==i)equipped[i]++;}}
        var themes=new List<object>();
        for(int id=1;id<ShowcaseThemes.Names.Length;id++)themes.Add(new{id,name=ShowcaseThemes.Names[id],earned=id>2,owners=accounts.Count(a=>ShowcaseThemes.Owned(a.State,id)),equipped=accounts.Count(a=>a.State.showcaseTheme==id)});
        var windows=new List<object>();
        foreach(int days in new[]{7,30,90})
        {
            string from=clock().AddDays(-days).ToUniversalTime().ToString("O"),until=clock().ToUniversalTime().ToString("O");
            var rows=new List<object>();
            for(int id=1;id<=Outfits.Count;id++)
            {
                var totals=ReadRows(db,tx,$"""
                WITH cohort AS (SELECT p.owner,MIN(p.created) first_seen FROM outfit_previews p JOIN accounts a ON a.id=p.owner
                WHERE {filter} AND p.outfit=$id AND p.created >= $from AND p.created <= $until GROUP BY p.owner)
                SELECT (SELECT COUNT(*) FROM cohort) previews,
                (SELECT COUNT(*) FROM cohort c WHERE EXISTS(SELECT 1 FROM outfit_sales s WHERE s.owner=c.owner AND s.outfit=$id AND julianday(s.created)>=julianday(c.first_seen) AND julianday(s.created)<=julianday(c.first_seen)+7 AND julianday(s.created)<=julianday($until))) converted,
                (SELECT COUNT(*) FROM outfit_sales s JOIN accounts a ON a.id=s.owner WHERE {filter} AND s.outfit=$id AND s.created >= $from AND s.created <= $until) purchases
                """,("$id",id),("$from",from),("$until",until))[0];
                rows.Add(new{id,name=Outfits.Label(id),collection=Outfits.Collections[Outfits.Collection(id)],previews=totals["previews"],converted=totals["converted"],purchases=totals["purchases"],owners=owned[id],equipped=equipped[id]});
            }
            var flows=ReadRows(db,tx,$"SELECT l.type,SUM(CASE WHEN l.delta>0 THEN l.delta ELSE 0 END) gained,SUM(CASE WHEN l.delta<0 THEN -l.delta ELSE 0 END) spent,COUNT(*) events FROM ledger l JOIN accounts a ON a.id=l.owner WHERE {filter} AND l.currency='gems' AND julianday(l.created)>=julianday($from) AND julianday(l.created)<=julianday($until) GROUP BY l.type ORDER BY l.type",("$from",from),("$until",until));
            var active=ReadRows(db,tx,$"SELECT COUNT(DISTINCT m.owner) n FROM metrics_activity m JOIN accounts a ON a.id=m.owner WHERE {filter} AND julianday(m.seen)>=julianday($from) AND julianday(m.seen)<=julianday($until)",("$from",from),("$until",until))[0]["n"];
            windows.Add(new{days,from,until,activeAccounts=active,outfits=rows,gemFlows=flows});
        }
        var started=ReadRows(db,tx,"SELECT started FROM metrics_meta WHERE id=1")[0]["started"];
        tx.Commit();return new{generatedAt=clock().ToUniversalTime().ToString("O"),captureStarted=started,includeTests,accounts=accounts.Count,themes,windows,content=Content(),refunds=RefundHealth(),notes=new[]{"전환: 동의한 미보유 체험 계정 중 첫 체험 이후 7일 안에 구매한 비율. 최근 체험은 관찰 중이며 연결 실패 시 누락될 수 있음.","보유/장착은 현재 스냅샷. 활동은 기간 중 게임 명령을 실행한 계정(UTC 일자). 과거 구매 이벤트는 소급하지 않음.","보석 원장 단위이며 원화 매출이 아님. play_purchase는 부채 상계 후 지갑 증가, play_refund는 남은 유료 보석 회수. 환불 부채와 현금 환불액을 소비로 해석하지 말 것.","테스트 계정 기본 제외. 계정 삭제 시 관련 통계도 삭제됨."}};
    }
}

// Retention also runs when no players preview outfits and no operator opens a report.
public sealed class MetricsRetention(GameRepository repo,ILogger<MetricsRetention> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while(!ct.IsCancellationRequested)
        {
            try{repo.PurgeMetrics();}
            catch(Exception ex){logger.LogWarning("Metrics retention failed ({Type})",ex.GetType().Name);}
            await Task.Delay(TimeSpan.FromHours(1),ct);
        }
    }
}
