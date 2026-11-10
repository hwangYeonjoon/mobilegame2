using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using ScrapGuard.Core;
static class Content034Audit
{
    #pragma warning disable CS0649 // JSON diagnostic input fields.
    public sealed class Input {public string source;public List<Profile> profiles;}
    public sealed class Profile {public int seed,day;public string policy;public GrowthAudit.Snapshot late,early;}
    #pragma warning restore CS0649
    static GameState Restore(GrowthAudit.Snapshot snap)
    {
        var s=GameState.New();s.tutorialCompleted=true;s.heroes[0].level=snap.heroLevel;
        s.gold=snap.wallet.gold;s.parts=snap.wallet.parts;s.gems=snap.wallet.gems;
        s.clearedStages=snap.clearedStages.Select(i=>i-1).ToList();s.unlockedStage=snap.unlockedStage-1;
        foreach(var g in snap.equipped){var item=s.items.Find(i=>i.id==s.equipped[g.slot]);item.definition=g.definition;item.rarity=g.rarity;item.level=g.level;}
        Campaign.Normalize(s);return s;
    }
    static object Metrics(Combat c)=>new{c.Won,c.Elapsed,c.Kills,c.Hp,c.MaxHp,c.Damage,c.DamageDealt,c.DamageReceived,c.BossCombatSeconds,c.BossPatterns,c.PeakBossWarnings,c.MachineActiveSeconds,c.LowHealthSeconds};
    public static int Run(string input,string output)
    {
        int currentRules=Combat.RulesVersion;if(currentRules!=25)throw new InvalidOperationException("0.34 audit is historical. Use commit 34657f3 or content035 audit for current rules.");
        var source=JsonSerializer.Deserialize<Input>(File.ReadAllText(input),GrowthAudit.Json);var purchases=new List<object>();var journeys=new List<object>();
        foreach(var p in source.profiles)
        {
            var baseline=Restore(p.early);var purchased=GrowthAudit.Clone(baseline);var e=new Economy(purchased,()=>GrowthAudit.Start);
            // Explicit test funding, not a real transaction or free-income assumption.
            purchased.gems+=RobotStyles.PackPrice;
            if(!e.BuyRobotStylePack().Success)throw new Exception("Pack rejected");
            var q=GrowthPaths.Quote(purchased,0);if(q!=null&&q.count>0&&!e.UpgradePath(0,q.count,q.id,q.from).Success)throw new Exception("Quote rejected");
            int stage=baseline.unlockedStage,seed=PurchaseAudit.Seed(p.seed,stage,1,false);
            var a=GrowthAudit.Simulate(baseline,stage,"before",seed,p.policy,out var ra);
            var b=GrowthAudit.Simulate(purchased,stage,"after",seed,p.policy,out var rb);
            purchases.Add(new{p.seed,p.policy,stage=stage+1,gems=900,upgrade=q,before=GrowthAudit.Capture(baseline),after=GrowthAudit.Capture(purchased),beforeCombat=Metrics(a),afterCombat=Metrics(b)});
            var s=Restore(p.late);var now=GrowthAudit.Start.AddDays(p.day);var economy=new Economy(s,()=>now);var rows=new List<object>();
            var work=new GrowthAudit.Journey{strategy="balanced",benefits="daily_claims"};int runs=0;bool stopped=false;
            while(s.clearedStages.Count<Campaign.Count&&!stopped)
            {
                int current=s.unlockedStage;bool won=false;
                for(int attempt=1;attempt<=6&&!won;attempt++)
                {
                    if(runs%6==0){now=GrowthAudit.Start.AddDays(p.day+runs/6);economy.RefreshDay();if(economy.CanAttend)economy.Attend();}
                    if(!s.salvageClaimed&&s.dailySupplySeconds>=300)economy.ClaimSalvage();
                    GrowthAudit.Prepare(economy,work,current,attempt);var entry=GrowthAudit.Capture(s);
                    int combatSeed=PurchaseAudit.Seed(p.seed,current,attempt,false);string id="tail-"+p.seed+"-"+runs;
                    if(!economy.BeginRun(id,current).Success)throw new Exception("Begin rejected");
                    var c=GrowthAudit.Simulate(s,current,id,combatSeed,p.policy,out var replay);
                    now=now.AddSeconds(c.Elapsed+30);if(!economy.FinishCampaign(c).Success)throw new Exception("Settlement rejected");
                    rows.Add(new{stage=current+1,attempt,seed=combatSeed,entry,result=Metrics(c),s.dailySupplySeconds,s.salvageClaimed});runs++;won=c.Won;
                }
                if(!won)stopped=true;
            }
            journeys.Add(new{p.seed,p.policy,sourceDay=p.day,completed=s.clearedStages.Count,runs,rows,final=GrowthAudit.Capture(s)});
            Console.WriteLine(p.policy+" seed="+p.seed+" completed="+s.clearedStages.Count+" runs="+runs);
            File.WriteAllText(output,JsonSerializer.Serialize(new{schema=1,build=AppRelease.Build,rules=Combat.RulesVersion,campaignCount=Campaign.Count,source=source.source,kind="offline_diagnostic_not_human",assumptions=new[]{"Restore only hero 0, equipped gear, wallet and cleared IDs from archived actually earned profiles; other inventory/claims are not restored.","Pack pair: day-3 snapshot, explicit 900 test gems, pack then maximum affordable weapon upgrade; baseline leaves its wallet unspent. This tests the displayed action, not optimal free-vs-paid fairness or willingness to pay.","Expansion: continue from first 42-clear day, maximum six attempts per operation, six runs/day, ordinary-play field supply, balanced legal upgrades, no advertisements or premium purchases. No repeat-only supply missions.","Natural and avoiding bots are not humans; extension completion is capped by the six-attempt stopping rule. Historical profiles are not a new 60-stage day-1 economy forecast."},purchases,journeys},GrowthAudit.Json));
        }
        return 0;
    }
}
