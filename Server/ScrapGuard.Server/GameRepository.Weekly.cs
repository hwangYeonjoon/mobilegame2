using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
namespace ScrapGuard.Server;
public sealed partial class GameRepository
{
    void RecordWeekly(SqliteConnection db,SqliteTransaction tx,Account a,Combat simulation)
    {
        if(simulation.Mode!=Endgame.WeeklyMode || simulation.Elapsed<30 || a.State.testProgress)return;
        var snapshot=Decode<GameState>(a.RunSnapshot);
        Execute(db,tx,"""
            INSERT INTO weekly_records(week,owner,score,achieved,hero,skin,rules) VALUES($week,$owner,$score,$time,$hero,$skin,$rules)
            ON CONFLICT(week,owner) DO UPDATE SET score=excluded.score,achieved=excluded.achieved,hero=excluded.hero,skin=excluded.skin,rules=excluded.rules
            WHERE excluded.score>weekly_records.score OR excluded.rules<>weekly_records.rules
            """,("$week",a.State.activeWeek),("$owner",a.Id),("$score",simulation.CoreScore),("$time",clock().ToUniversalTime().ToString("O")),
            ("$hero",snapshot.selectedHero),("$skin",snapshot.selectedSkin),("$rules",Combat.RulesVersion));
    }
    const string VisibleWeekly = """
        FROM weekly_records w JOIN accounts a ON a.id=w.owner
        WHERE w.week=$week AND w.rules=$rules AND json_extract(a.state,'$.publicProfile')=1
        AND COALESCE(json_extract(a.state,'$.testProgress'),0)=0
        AND NOT EXISTS(SELECT 1 FROM blocks b WHERE (b.viewer=$viewer AND b.target=a.id) OR (b.target=$viewer AND b.viewer=a.id))
        """;
    public WeeklyBoard Weekly(string token)
    {
        using var db=Open();using var tx=db.BeginTransaction();var viewer=Load(db,tx,token);
        string week=Endgame.Week(clock());var result=new WeeklyBoard {week=week};
        var args=new (string,object)[]{("$week",week),("$rules",Combat.RulesVersion),("$viewer",viewer.Id)};
        using(var query=Sql(db,tx,"SELECT a.id,json_extract(a.state,'$.displayName'),w.score,w.hero,w.skin "+VisibleWeekly+" ORDER BY w.score DESC,w.achieved,w.owner LIMIT 50",args))
        using(var r=query.ExecuteReader())while(r.Read())result.entries.Add(new RankEntry {rank=result.entries.Count+1,id=r.GetString(0),name=r.GetString(1),score=r.GetInt32(2),hero=r.GetInt32(3),skin=r.GetInt32(4)});
        using(var query=Sql(db,tx,"SELECT place,score FROM (SELECT a.id,w.score,ROW_NUMBER() OVER(ORDER BY w.score DESC,w.achieved,w.owner) place "+VisibleWeekly+") WHERE id=$viewer",args))
        using(var r=query.ExecuteReader())if(r.Read()){result.myRank=r.GetInt32(0);result.myScore=r.GetInt32(1);}
        return result;
    }
}
