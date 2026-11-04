using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
namespace ScrapGuard.Server;

public sealed record OperatorAction(string RequestId,string Operator,string Account,string Action,string Reason,int Gold=0,int Parts=0);
public sealed partial class GameRepository
{
    void InitializeOperations(SqliteConnection db)
    {
        Execute(db,null,"""
        CREATE TABLE IF NOT EXISTS moderation(owner TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,hidden INTEGER NOT NULL);
        CREATE TABLE IF NOT EXISTS operator_audit(
          request_id TEXT PRIMARY KEY, actor TEXT NOT NULL, owner TEXT REFERENCES accounts(id) ON DELETE SET NULL,
          action TEXT NOT NULL, reason TEXT NOT NULL, payload_hash TEXT NOT NULL, created TEXT NOT NULL);
        """);
    }
    static bool Moderated(SqliteConnection db,SqliteTransaction tx,string owner)
    {
        using var q=Sql(db,tx,"SELECT hidden FROM moderation WHERE owner=$owner",("$owner",owner));return Convert.ToInt32(q.ExecuteScalar()??0)!=0;
    }
    public object OperationsSummary()
    {
        using var db=Open();
        long Count(string sql){using var q=Sql(db,null,sql);return (long)q.ExecuteScalar();}
        return new {
            accounts=Count("SELECT COUNT(*) FROM accounts"),reports=Count("SELECT COUNT(*) FROM reports"),
            activeRuns=Count("SELECT COUNT(*) FROM accounts WHERE run_snapshot IS NOT NULL"),
            purchases=Count("SELECT COUNT(*) FROM purchases"),refunds=Count("SELECT COUNT(*) FROM purchases WHERE revoked=1"),
            pendingAds=Count("SELECT COUNT(*) FROM ad_claims WHERE status='pending' AND expires>strftime('%Y-%m-%dT%H:%M:%f+00:00','now')"),
            auditEntries=Count("SELECT COUNT(*) FROM operator_audit"),
            economy=ReadRows(db,"SELECT currency,type,SUM(delta) AS delta,COUNT(*) AS events FROM ledger GROUP BY currency,type ORDER BY currency,type")
        };
    }
    static List<Dictionary<string,object>> ReadRows(SqliteConnection db,string query,params (string,object)[] args)
        =>ReadRows(db,null,query,args);
    static List<Dictionary<string,object>> ReadRows(SqliteConnection db,SqliteTransaction tx,string query,params (string,object)[] args)
    {
        using var q=Sql(db,tx,query,args);using var r=q.ExecuteReader();var rows=new List<Dictionary<string,object>>();
        while(r.Read()){var row=new Dictionary<string,object>();for(int i=0;i<r.FieldCount;i++)row[r.GetName(i)]=r.IsDBNull(i)?null:r.GetValue(i);rows.Add(row);}return rows;
    }
    public object OperationsReports()=>OperationsList("reports");
    public object OperationsList(string table)
    {
        using var db=Open();
        return table switch {
            "reports"=>ReadRows(db,"SELECT viewer,target,reason,created FROM reports ORDER BY created LIMIT 100"),
            "audit"=>ReadRows(db,"SELECT request_id,actor,owner,action,reason,created FROM operator_audit ORDER BY created DESC LIMIT 100"),
            _=>throw new ApiFault(400,"알 수 없는 운영 목록이에요.")
        };
    }
    public object OperationsAccount(string id)
    {
        using var db=Open();var a=Load(db,null,id,true);
        return new {
            id=a.Id,name=a.State.displayName,a.Revision,a.State.gold,a.State.parts,a.State.gems,a.State.paidGems,a.State.gemDebt,a.State.adSkip,a.State.testProgress,
            hidden=Moderated(db,null,a.Id),
            purchases=ReadRows(db,"SELECT product,granted,remaining,revoked,created FROM purchases WHERE owner=$owner ORDER BY created DESC LIMIT 100",("$owner",id)),
            ledger=ReadRows(db,"SELECT request_id,currency,delta,balance,type,created FROM ledger WHERE owner=$owner ORDER BY created DESC LIMIT 100",("$owner",id)),
            ads=ReadRows(db,"SELECT id,placement,status,gold,parts,created,expires FROM ad_claims WHERE owner=$owner ORDER BY created DESC LIMIT 20",("$owner",id))
        }; // Never expose authentication tokens, Google subjects or purchase tokens.
    }
    public object Operate(OperatorAction action)
    {
        if(action==null||!System.Text.RegularExpressions.Regex.IsMatch(action.RequestId??"","^[a-f0-9]{32}$")||string.IsNullOrWhiteSpace(action.Operator)||action.Operator.Length>80||string.IsNullOrWhiteSpace(action.Reason)||action.Reason.Length>500)
            throw new ApiFault(400,"운영자, 사유와 고유 요청 ID가 필요해요.");
        if(action.Action is not ("hide" or "unhide" or "resolve" or "compensate")||action.Gold<0||action.Gold>10000||action.Parts<0||action.Parts>100||action.Action!="compensate"&&(action.Gold!=0||action.Parts!=0)||action.Action=="compensate"&&action.Gold+action.Parts==0)
            throw new ApiFault(400,"운영 작업 또는 보상 범위를 확인해 주세요.");
        lock(gate)
        {
            using var db=Open();using var tx=db.BeginTransaction();string hash=Hash(Encode(action));
            using(var q=Sql(db,tx,"SELECT payload_hash FROM operator_audit WHERE request_id=$id",("$id",action.RequestId)))
            {var previous=q.ExecuteScalar() as string;if(previous!=null){if(previous!=hash)throw new ApiFault(409,"같은 운영 요청 ID에 다른 내용이 있어요.");return new { success=true,duplicate=true };}}
            var a=Load(db,tx,action.Account,true);
            if(action.Action=="hide"||action.Action=="unhide")
            {
                Execute(db,tx,"INSERT INTO moderation VALUES($owner,$hidden) ON CONFLICT(owner) DO UPDATE SET hidden=$hidden",("$owner",a.Id),("$hidden",action.Action=="hide"?1:0));
                a.State.publicProfile=false;
                if(action.Action=="hide")a.State.displayName="수호대원";
            }
            if(action.Action=="compensate")
            {
                a.State.gold=checked(a.State.gold+action.Gold);a.State.parts=checked(a.State.parts+action.Parts);
                foreach(var item in new[]{("gold",action.Gold,a.State.gold),("parts",action.Parts,a.State.parts)})if(item.Item2>0)
                    Execute(db,tx,"INSERT INTO ledger VALUES($owner,$id,$currency,$delta,$balance,'support_compensation',$time)",("$owner",a.Id),("$id",action.RequestId),("$currency",item.Item1),("$delta",item.Item2),("$balance",item.Item3),("$time",clock().ToString("O")));
            }
            if(action.Action=="resolve"||action.Action=="hide")Execute(db,tx,"DELETE FROM reports WHERE target=$id",("$id",a.Id));
            a.Revision++;
            Execute(db,tx,"UPDATE accounts SET state=$state,revision=$revision WHERE id=$id",("$state",Encode(a.State)),("$revision",a.Revision),("$id",a.Id));
            Execute(db,tx,"INSERT INTO operator_audit VALUES($id,$actor,$owner,$action,$reason,$hash,$time)",("$id",action.RequestId),("$actor",action.Operator),("$owner",a.Id),("$action",action.Action),("$reason",action.Reason),("$hash",hash),("$time",clock().ToString("O")));
            tx.Commit();return new {success=true,duplicate=false};
        }
    }
}
