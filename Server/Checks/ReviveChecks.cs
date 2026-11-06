using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using ScrapGuard.Core;
using ScrapGuard.Server;
static class ReviveChecks
{
    public static void Run(string folder,Action<bool,string> check)
    {
        var now=DateTimeOffset.UtcNow;
        var repo=new GameRepository(Path.Combine(folder,"revive.db"),()=>now);
        string token=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');repo.Register(token);
        ServerReply Apply(GameCommand command){command.revision=repo.Bootstrap(token).revision;return repo.Apply(token,command);}
        var policy=new AdPolicy(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string>{{"ADS_MODE","test"},{"ADMOB_REWARDED_UNIT_ID",AdPolicy.TestUnit}}).Build());
        GameCommand Request()=>new("ad_request"){value="revive"};
        void Reject(Action action,int status,string label){try{action();throw new Exception("Not rejected: "+label);}catch(ApiFault e){check(e.Status==status,label);}}
        Reject(()=>repo.CreateAdClaim(token,Request(),policy),409,"revive requires an active campaign run");
        var start=Apply(new GameCommand("battle_start"){stage=0});
        var c=new Combat(start.state,0,start.ticket.id,start.ticket.seed);var replay=new ReplayRecorder();
        while(!c.Ended)
        {
            if(c.Choosing){int skill=c.Choices[0];c.Choose(skill);replay.Choose(skill);}
            c.Tick(ReplayRecorder.Step,replay.Move(new V2()));
        }
        check(c.CanRevive,"actual defeat reaches eligible revive state");now=now.AddMinutes(5);
        c.Revive();replay.Revive();c.Abandon();
        var finish=new GameCommand("battle_finish",start.ticket.id){replay=replay.Steps};
        Reject(()=>Apply(finish),400,"server rejects forged revive without SSV");
        var request=Request();var claim=repo.CreateAdClaim(token,request,policy);
        check(claim.runId==start.ticket.id&&claim.gold==0&&claim.parts==0,"revive claim binds exact run and grants no currency");
        check(repo.CreateAdClaim(token,request,policy).id==claim.id,"revive reservation is idempotent");
        Reject(()=>repo.CreateAdClaim(token,Request(),policy),409,"parallel revive claims rejected");
        var callback=new VerifiedAd(claim.id,claim.userId,policy.CallbackUnit,"revive-transaction",now.ToUnixTimeMilliseconds(),"reward",1);
        repo.CompleteAd(callback,policy);repo.CompleteAd(callback,policy);
        var granted=repo.AdClaimStatus(token,claim.id);
        check(granted.status=="granted"&&granted.runId==start.ticket.id,"duplicate callback retains one run entitlement");
        Reject(()=>repo.CreateAdClaim(token,Request(),policy),409,"second granted revive for same run rejected");
        var settled=Apply(finish);var receipt=settled.state.receipts.Find(r=>r.id==start.ticket.id);
        check(receipt.report.revived,"server accepts and records verified revive replay");
        check(repo.Bootstrap(token).state.dailyRewardParts==0&&repo.Bootstrap(token).state.dailyRewardGold==0,"revive does not consume unrelated ad quotas");
        var next=Apply(new GameCommand("battle_start"){stage=0});
        var old=repo.AdClaimStatus(token,claim.id);
        check(old.runId!=next.ticket.id,"old grant cannot masquerade as current run in recovery UI");
        var pending=repo.CreateAdClaim(token,Request(),policy);
        Apply(new GameCommand("battle_cancel"));
        Reject(()=>repo.CompleteAd(new VerifiedAd(pending.id,pending.userId,policy.CallbackUnit,"abandoned-revive",now.ToUnixTimeMilliseconds(),"reward",1),policy),409,"late SSV cannot revive a canceled run");
        check(repo.Bootstrap(token).state.receipts.Count(r=>r.kind=="verified_revive")==1,"only one durable revive grant receipt exists");
    }
}
