using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
namespace ScrapGuard.Server;
public sealed partial class GameRepository
{
    void InitializeAds(SqliteConnection db)
    {
        Execute(db,null,"""
        CREATE TABLE IF NOT EXISTS ad_claims(
          id TEXT PRIMARY KEY, owner TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
          request_id TEXT NOT NULL, placement TEXT NOT NULL, service_date TEXT NOT NULL, result_id TEXT NOT NULL,
          gold INTEGER NOT NULL, parts INTEGER NOT NULL, status TEXT NOT NULL, unit TEXT NOT NULL, test INTEGER NOT NULL,
          created TEXT NOT NULL, expires TEXT NOT NULL, transaction_id TEXT UNIQUE, UNIQUE(owner,request_id));
        CREATE INDEX IF NOT EXISTS ad_claim_owner ON ad_claims(owner,status,service_date);
        """);
    }
    void ExpireAds(SqliteConnection db,SqliteTransaction tx,string owner)
    {Execute(db,tx,"UPDATE ad_claims SET status='expired' WHERE owner=$owner AND status='pending' AND (expires<=$now OR service_date<$day)",("$owner",owner),("$now",clock().ToString("O")),("$day",Catalog.ServiceDate(clock())));}
    bool HasAdReservation(SqliteConnection db,SqliteTransaction tx,string owner,string placement)
    {
        ExpireAds(db,tx,owner);
        using var q=Sql(db,tx,"SELECT COUNT(*) FROM ad_claims WHERE owner=$owner AND placement=$p AND status='pending'",("$owner",owner),("$p",placement));return (long)q.ExecuteScalar()>0;
    }
    bool HasRunRevive(SqliteConnection db,SqliteTransaction tx,string owner,string run)
    {
        using var q=Sql(db,tx,"SELECT COUNT(*) FROM ad_claims WHERE owner=$owner AND result_id=$run AND placement='revive' AND status='granted'",("$owner",owner),("$run",run));
        return (long)q.ExecuteScalar()>0;
    }
    AdClaim ReadAd(SqliteConnection db,SqliteTransaction tx,Account a,string id)
    {
        using var q=Sql(db,tx,"SELECT placement,status,gold,parts,unit,expires,test,result_id FROM ad_claims WHERE id=$id AND owner=$owner",("$id",id),("$owner",a.Id));
        using var r=q.ExecuteReader();if(!r.Read())throw new ApiFault(404,"광고 보상 요청을 찾을 수 없어요.");
        return new AdClaim{id=id,userId=a.Id,placement=r.GetString(0),status=r.GetString(1),gold=r.GetInt32(2),parts=r.GetInt32(3),unitId=r.GetString(4),expires=r.GetString(5),test=r.GetInt32(6)!=0,runId=r.GetString(7),reply=Reply(a)};
    }
    public AdClaim CreateAdClaim(string token,GameCommand request,AdPolicy policy)
    {
        if(request==null||!System.Text.RegularExpressions.Regex.IsMatch(request.requestId??"","^[a-f0-9]{32}$")||request.value is not ("parts" or "result" or "revive"))throw new ApiFault(400,"올바른 보상 요청이 필요해요.");
        lock(gate)
        {
            using var db=Open();using var tx=db.BeginTransaction();var a=Load(db,tx,token);new Economy(a.State,clock).RefreshDay();ExpireAds(db,tx,a.Id);
            using(var q=Sql(db,tx,"SELECT id,placement FROM ad_claims WHERE owner=$owner AND request_id=$req",("$owner",a.Id),("$req",request.requestId)))
            using(var r=q.ExecuteReader())if(r.Read())
            {string id=r.GetString(0);if(r.GetString(1)!=request.value)throw new ApiFault(409,"요청 내용이 변경됐어요.");r.Close();var existing=ReadAd(db,tx,a,id);tx.Commit();return existing;}
            if(!policy.Enabled&&!a.State.adSkip)throw new ApiFault(503,"현재 광고 보상을 이용할 수 없어요.");
            if(request.value=="revive")
            {
                if(string.IsNullOrEmpty(a.State.activeRun)||(Endgame.IsChallenge(a.State.activeMode)||a.State.activeMode==BossTraining.Mode||a.State.activeMode==SurvivalTraining.Mode||a.State.activeMode==Apex.Mode)||a.RunRules!=Combat.RulesVersion)throw new ApiFault(409,"진행 중인 일반 작전에서만 부활할 수 있어요.");
                if(HasRunRevive(db,tx,a.Id,a.State.activeRun))throw new ApiFault(409,"이 작전의 부활 권리는 이미 지급했어요. 보상 내역에서 확인해 주세요.");
            }
            else if(!string.IsNullOrEmpty(a.State.activeRun))throw new ApiFault(409,"출격을 마친 뒤 보상을 받아 주세요.");
            // One outstanding placement per account prevents parallel devices reserving the same result.
            if(HasAdReservation(db,tx,a.Id,request.value))throw new ApiFault(409,"확인 중인 광고 보상이 있어요. 보상 내역에서 확인해 주세요.");
            var probe=Decode<GameState>(Encode(a.State));var valid=request.value=="revive"?CommandResult.Ok("긴급 수리"):new Economy(probe,clock).ClaimDemoReward(request.value,"probe");
            if(!valid.Success)throw new ApiFault(400,valid.Message);
            string claim=Guid.NewGuid().ToString("N");int gold=probe.gold-a.State.gold,parts=probe.parts-a.State.parts;
            Execute(db,tx,"INSERT INTO ad_claims(id,owner,request_id,placement,service_date,result_id,gold,parts,status,unit,test,created,expires) VALUES($id,$owner,$req,$p,$day,$result,$gold,$parts,'pending',$unit,$test,$created,$expires)",
                ("$id",claim),("$owner",a.Id),("$req",request.requestId),("$p",request.value),("$day",a.State.serviceDate),("$result",request.value=="revive"?a.State.activeRun:request.value=="result"?a.State.lastResultId:""),("$gold",gold),("$parts",parts),("$unit",policy.UnitId),("$test",policy.Mode=="test"?1:0),("$created",clock().ToString("O")),("$expires",clock().AddMinutes(20).ToString("O")));
            if(a.State.adSkip)GrantAd(db,tx,a,claim,"skip:"+claim,false);
            var result=ReadAd(db,tx,a,claim);tx.Commit();return result;
        }
    }
    void GrantAd(SqliteConnection db,SqliteTransaction tx,Account a,string id,string transaction,bool test)
    {
        string placement,day,result;int gold,parts;
        using(var q=Sql(db,tx,"SELECT placement,service_date,result_id,gold,parts FROM ad_claims WHERE id=$id",("$id",id)))
        using(var r=q.ExecuteReader()){r.Read();placement=r.GetString(0);day=r.GetString(1);result=r.GetString(2);gold=r.GetInt32(3);parts=r.GetInt32(4);}
        new Economy(a.State,clock).RefreshDay();
        if(day!=a.State.serviceDate)throw new ApiFault(409,"보상 일자가 지났어요.");
        if(placement=="revive")
        {
            if(a.State.activeRun!=result||(Endgame.IsChallenge(a.State.activeMode)||a.State.activeMode==BossTraining.Mode||a.State.activeMode==SurvivalTraining.Mode||a.State.activeMode==Apex.Mode)||HasRunRevive(db,tx,a.Id,result))throw new ApiFault(409,"종료됐거나 이미 부활 권리를 받은 작전이에요.");
        }
        else if(placement=="parts") {if(a.State.dailyRewardParts>=2)throw new ApiFault(409,"오늘 부품 보상을 모두 받았어요.");a.State.dailyRewardParts++;}
        else
        {
            if(a.State.dailyRewardGold>=3)throw new ApiFault(409,"오늘 전투 추가 보상을 모두 받았어요.");
            a.State.dailyRewardGold++;if(a.State.lastResultId==result)a.State.lastResultBonus=true;
        }
        if(test)a.State.testProgress=true;
        a.State.gold=checked(a.State.gold+gold);a.State.parts=checked(a.State.parts+parts);a.Revision++;
        a.State.receipts.Add(new Receipt{id=id,kind=placement=="revive"?"verified_revive":"verified_ad",gold=gold,parts=parts});
        Execute(db,tx,"UPDATE accounts SET state=$state,revision=$revision WHERE id=$id",("$state",Encode(a.State)),("$revision",a.Revision),("$id",a.Id));
        Execute(db,tx,"UPDATE ad_claims SET status='granted',transaction_id=$transaction WHERE id=$id",("$transaction",transaction),("$id",id));
        foreach(var item in new[]{("gold",gold,a.State.gold),("parts",parts,a.State.parts)})if(item.Item2!=0)
            Execute(db,tx,"INSERT INTO ledger VALUES($owner,$id,$currency,$delta,$balance,'ad_reward',$time)",("$owner",a.Id),("$id","ad:"+id),("$currency",item.Item1),("$delta",item.Item2),("$balance",item.Item3),("$time",clock().ToString("O")));
    }
    public void CompleteAd(VerifiedAd ad,AdPolicy policy)
    {
        if(!policy.Enabled)throw new ApiFault(503,"광고 보상 확인이 중지됐어요.");
        if(ad.Unit!=policy.UnitId&&ad.Unit!=policy.CallbackUnit)throw new ApiFault(400,"광고 단위 설정이 일치하지 않아요.");
        if(ad.Amount!=1||ad.Reward!="reward")throw new ApiFault(400,"광고 보상 수량 또는 이름이 일치하지 않아요.");
        lock(gate)
        {
            using var db=Open();using var tx=db.BeginTransaction();string owner,status,unit,created,expires;bool test;
            using(var q=Sql(db,tx,"SELECT owner,status,unit,created,expires,test FROM ad_claims WHERE id=$id",("$id",ad.Claim)))
            using(var r=q.ExecuteReader()) {if(!r.Read())throw new ApiFault(404,"알 수 없는 광고 요청이에요.");owner=r.GetString(0);status=r.GetString(1);unit=r.GetString(2);created=r.GetString(3);expires=r.GetString(4);test=r.GetInt32(5)!=0;}
            if(owner!=ad.User||(ad.Unit!=unit&&ad.Unit!=unit.Split('/').Last())||test!=(policy.Mode=="test"))throw new ApiFault(400,"광고 요청 정보가 일치하지 않아요.");
            using(var q=Sql(db,tx,"SELECT id FROM ad_claims WHERE transaction_id=$id",("$id",ad.Transaction)))
            {var previous=q.ExecuteScalar() as string;if(previous!=null){if(previous!=ad.Claim)throw new ApiFault(409,"이미 사용한 광고 거래예요.");tx.Commit();return;}}
            if(status=="granted"){tx.Commit();return;} // Skip and delayed callback cannot double grant.
            if(status!="pending"||clock()>=DateTimeOffset.Parse(expires))throw new ApiFault(409,"취소되거나 만료된 광고 요청이에요.");
            long start=DateTimeOffset.Parse(created).ToUnixTimeMilliseconds(),end=DateTimeOffset.Parse(expires).ToUnixTimeMilliseconds();
            if(ad.Timestamp<start-30000||ad.Timestamp>end||ad.Timestamp>clock().AddMinutes(1).ToUnixTimeMilliseconds())throw new ApiFault(400,"광고 시각이 올바르지 않아요.");
            GrantAd(db,tx,Load(db,tx,owner,true),ad.Claim,ad.Transaction,test);tx.Commit();
        }
    }
    public AdClaim AdClaimStatus(string token,string id,bool cancel=false)
    {
        lock(gate)
        {
            using var db=Open();using var tx=db.BeginTransaction();var a=Load(db,tx,token);ExpireAds(db,tx,a.Id);
            if(cancel)Execute(db,tx,"UPDATE ad_claims SET status='canceled' WHERE id=$id AND owner=$owner AND status='pending'",("$id",id),("$owner",a.Id));
            var result=ReadAd(db,tx,a,id);tx.Commit();return result;
        }
    }
    public List<AdClaim> RecentAds(string token)
    {
        lock(gate)
        {
            using var db=Open();using var tx=db.BeginTransaction();var a=Load(db,tx,token);ExpireAds(db,tx,a.Id);
            var ids=new List<string>();using(var q=Sql(db,tx,"SELECT id FROM ad_claims WHERE owner=$owner ORDER BY created DESC LIMIT 10",("$owner",a.Id)))using(var r=q.ExecuteReader())while(r.Read())ids.Add(r.GetString(0));
            var list=ids.Select(id=>ReadAd(db,tx,a,id)).ToList();tx.Commit();return list;
        }
    }
}
