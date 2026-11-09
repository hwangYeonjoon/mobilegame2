using System;
using System.Linq;
using ScrapGuard.Core;

static class GrowthAuditChecks
{
    static int checks;
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    static GrowthAudit.Journey Journey(string strategy="balanced",string benefits="campaign_only")=>new GrowthAudit.Journey{strategy=strategy,benefits=benefits};
    public static int Run()
    {
        var s=GameState.New();var e=new Economy(s,()=>GrowthAudit.Start);
        var original=System.Text.Json.JsonSerializer.Serialize(s,GrowthAudit.Json);
        var q=GrowthAudit.UpgradeQuote(s,0);var h=GrowthAudit.UpgradeQuote(s,3);
        Check(q.gold==300&&q.parts==2&&h.gold==500&&h.parts==3,"quotes use real level-one prices");
        Check(original==System.Text.Json.JsonSerializer.Serialize(s,GrowthAudit.Json),"quote funds never mutate original account");
        var j=Journey();var prep=GrowthAudit.Prepare(e,j,0,1);
        Check(prep.upgradeActions>0&&s.gold>=0&&s.parts>=0&&Catalog.Attack(s)>130&&Catalog.Health(s)>1180,"preparation spends earned starting balance and changes actual stats");
        j.final=GrowthAudit.Capture(s);GrowthAudit.ValidateLedger(j);Check(true,"upgrade ledger reconciles");
        s=GameState.New();e=new Economy(s,()=>GrowthAudit.Start);j=Journey("passive");
        Check(!GrowthAudit.Prepare(e,j,0,1).visited&&s.gold==1800,"passive first run intentionally defers affordable upgrades");
        s.totalRuns=3;Check(GrowthAudit.Prepare(e,j,0,1).upgradeActions==1,"passive visit buys at most one upgrade");
        s.gold=s.parts=0;q=GrowthAudit.UpgradeQuote(s,0);
        Check(!q.canAfford&&q.missingGold>0&&q.missingParts>0&&!e.Upgrade(s.equipped[0]).Success,"resource shortages cannot grant levels");

        s=GameState.New();e=new Economy(s,()=>GrowthAudit.Start);j=Journey();
        GrowthAudit.Act(e,j,0,1,"battle_start","loss",()=>e.BeginRun("loss",0));
        GrowthAudit.Act(e,j,0,1,"battle_finish","loss",()=>e.FinishRun("loss",0,false,10,60));
        Check(s.unlockedStage==0&&s.items.Count==3&&s.gems==0&&s.gold==2040&&s.parts==32,"failure credits only real failure rewards and cannot unlock future stages");
        Check(!GrowthAudit.Act(e,j,0,1,"duplicate","loss",()=>e.FinishRun("loss",0,false,10,60)),"duplicate reward rejected");
        j.final=GrowthAudit.Capture(s);GrowthAudit.ValidateLedger(j);
        j.ledger[1].delta.gold++;bool rejected=false;try{GrowthAudit.ValidateLedger(j);}catch(Exception){rejected=true;}
        Check(rejected,"ledger detects injected income");

        s=GameState.New();e=new Economy(s,()=>GrowthAudit.Start);j=Journey("balanced","daily_plus_rewarded");
        GrowthAudit.ClaimBenefits(e,j,0,1);var gold=s.gold;var parts=s.parts;
        GrowthAudit.ClaimBenefits(e,j,0,1);
        Check(gold==4000&&parts==46&&s.gold==gold&&s.parts==parts,"attendance and two parts ads are capped within same service day");
        for(int i=0;i<4;i++)
        {
            GrowthAudit.Act(e,j,0,i+1,"battle_start","cap"+i,()=>e.BeginRun("cap"+i,0));
            GrowthAudit.Act(e,j,0,i+1,"battle_finish","cap"+i,()=>e.FinishRun("cap"+i,0,false,200,60));
            GrowthAudit.ClaimBenefits(e,j,0,i+1);
        }
        Check(s.dailyRewardGold==3&&j.ledger.Count(x=>x.action=="missions")==1,"result ad and daily mission limits use actual accumulated progress");
        j.final=GrowthAudit.Capture(s);GrowthAudit.ValidateLedger(j);

        s=GameState.New();e=new Economy(s,()=>GrowthAudit.Start);j=Journey();GrowthAudit.Prepare(e,j,0,1);
        GrowthAudit.Act(e,j,0,1,"battle_start","integration",()=>e.BeginRun("integration",0));
        var snapshot=GrowthAudit.Clone(s);var c=GrowthAudit.Simulate(s,0,"integration",17,"controlled_avoid",out var record);
        var replay=ReplayRecorder.Verify(snapshot,0,"integration",17,record.Steps,600);
        Check(c.Ended&&c.Won&&replay.Won==c.Won&&replay.Kills==c.Kills&&Math.Abs(replay.Hp-c.Hp)<.02f,"economic simulation reproduces through server replay verifier");
        GrowthAudit.Act(e,j,0,1,"battle_finish","integration",()=>e.FinishCampaign(c));
        Check(s.receipts.Last().gold==Campaign.Get(0).Gold+c.TaskBonusGold&&s.unlockedStage==1,"settlement includes actual field rewards and first-clear unlock");
        int oldLevel=s.items.Find(x=>x.id==s.equipped[0]).level;
        GrowthAudit.Prepare(e,j,1,1);
        var weapon=s.items.Find(x=>x.id==s.equipped[0]);
        Check(weapon.rarity==Campaign.Get(0).FirstRarity&&weapon.level>=oldLevel&&j.ledger.Any(x=>x.action=="transfer"),"earned higher rarity uses legal level transfer");
        j.final=GrowthAudit.Capture(s);GrowthAudit.ValidateLedger(j);
        Console.WriteLine("Growth audit checks: "+checks+" passed");return 0;
    }
}
