using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ScrapGuard.Core;
static class GrowthValueAudit
{
#pragma warning disable CS0649
    public sealed class Input {public string source,note;public List<Profile> profiles;}
    public sealed class Profile {public int seed,stage;public string policy;public GrowthAudit.Snapshot snapshot;}
#pragma warning restore CS0649
    public sealed class Row
    {
        public int seed,stage,targetLevel,extraLevels,farmRuns,farmWins,gemSpend,cosmeticSpend,paidGrant;
        public string policy,path,offer;public bool goalReached,won,purchaseUseful;
        public float goalSeconds,combatSeconds,activeSeconds,targetSeconds;
        public GrowthAudit.Snapshot start,entry,final;public List<GrowthAudit.Entry> ledger=new List<GrowthAudit.Entry>();
    }
    static GameState Restore(Profile p)
    {
        var a=p.snapshot;var s=GameState.New();s.tutorialCompleted=true;s.heroes[0].level=a.heroLevel;
        s.gold=a.wallet.gold;s.parts=a.wallet.parts;s.gems=a.wallet.gems;s.unlockedStage=a.unlockedStage-1;s.clearedStages=a.clearedStages.Select(x=>x-1).ToList();
        foreach(var g in a.equipped){var item=s.items.Find(i=>i.id==s.equipped[g.slot]);item.level=g.level;item.definition=g.definition;item.rarity=g.rarity;}
        return s;
    }
    static Row Play(Profile p,string path,int fixedRepeats=-1)
    {
        var s=Restore(p);var now=GrowthAudit.Start;var e=new Economy(s,()=>now);var kit=Growth.CurrentKit(s);var quote=Growth.KitQuote(s,kit.id,0);
        var r=new Row{seed=p.seed,stage=p.stage,policy=p.policy,path=path,offer=kit.id,start=GrowthAudit.Capture(s),targetLevel=quote?.after.to??20,extraLevels=quote?.ExtraLevels??0,purchaseUseful=(quote?.ExtraLevels??0)>0};
        var work=new GrowthAudit.Journey{ledger=r.ledger};
        void Act(string action,Func<CommandResult> f){if(!GrowthAudit.Act(e,work,p.stage-1,1,action,path,f))throw new Exception("Rejected "+action);}
        if(path=="cosmetic_only"&&s.gems>=600){Act("cosmetic",()=>Social.Buy(s,Outfits.Key));r.cosmeticSpend=600;}
        bool paid=path.StartsWith("paid_");
        if(paid&&r.purchaseUseful)
        {
            // Explicitly funded sensitivity scenario, not a Play charge or a revenue estimate.
            Act("test_paid_grant",()=>{PremiumWallet.Grant(s,100);return CommandResult.Ok("audit funding");});r.paidGrant=100;
            Act("kit_and_upgrade",()=>e.BuyGrowthKitAndUpgrade(kit.id,0,quote.after.count,quote.after.id,quote.after.from,s.selectedHero));r.gemSpend=100;
        }
        void Upgrade(bool all=false)
        {
            int level=s.items.Find(i=>i.id==s.equipped[0]).level;int limit=all?30:r.targetLevel-level;if(limit<=0)return;
            var q=GrowthPaths.Quote(s,0,limit);if(q!=null&&q.count>0)Act("upgrade",()=>e.UpgradePath(0,q.count,q.id,q.from));
        }
        bool Goal()=>s.items.Find(i=>i.id==s.equipped[0]).level>=r.targetLevel;
        void Battle(int stage,bool farm,int attempt)
        {
            string id="value-"+r.ledger.Count;Act("start",()=>e.BeginRun(id,stage));
            var c=GrowthAudit.Simulate(s,stage,id,PurchaseAudit.Seed(p.seed,stage,attempt,farm),p.policy,out var replay);
            now=now.AddSeconds(c.Elapsed+30);Act("finish",()=>e.FinishCampaign(c));r.combatSeconds+=c.Elapsed;r.activeSeconds+=c.Elapsed+30;
            if(farm){r.farmRuns++;if(c.Won)r.farmWins++;}else{r.won=c.Won;r.targetSeconds=c.Elapsed;}
            if(s.dailySupplySeconds>=300&&!s.salvageClaimed)Act("field_supply",e.ClaimSalvage);
        }
        Upgrade();r.goalReached=Goal();if(r.goalReached)r.goalSeconds=0;
        int farmStage=Growth.FarmStage(s);
        int budget=fixedRepeats>=0?fixedRepeats:6;
        for(int n=0;n<budget&&farmStage>=0;n++)
        {
            if(fixedRepeats<0&&Goal())break;
            Battle(farmStage,true,n+1);Upgrade(path=="paid_and_play");
            if(!r.goalReached&&Goal()){r.goalReached=true;r.goalSeconds=r.activeSeconds;}
        }
        r.entry=GrowthAudit.Capture(s);Battle(p.stage-1,false,1);r.final=GrowthAudit.Capture(s);
        if(!r.goalReached)r.goalSeconds=-1;
        return r;
    }
    public static int Run(string input,string output)
    {
        var source=JsonSerializer.Deserialize<Input>(File.ReadAllText(input),GrowthAudit.Json);var results=new List<Row>();
        Parallel.ForEach(source.profiles,new ParallelOptions{MaxDegreeOfParallelism=2},p=>{
            var free=Play(p,"free");var cosmetic=Play(p,"cosmetic_only");var skip=Play(p,"paid_skip");var stack=Play(p,"paid_and_play",free.farmRuns);
            if(free.entry.attack!=cosmetic.entry.attack||free.farmRuns!=cosmetic.farmRuns||free.won!=cosmetic.won)throw new Exception("Cosmetics changed free progression");
            lock(results){results.AddRange(new[]{free,cosmetic,skip,stack});Console.WriteLine("value "+p.policy+" seed="+p.seed+" stage="+p.stage+" extra="+free.extraLevels+" free repeats="+free.farmRuns);}
        });
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        File.WriteAllText(output,JsonSerializer.Serialize(new{schema=1,build=AppRelease.Build,rules=Combat.RulesVersion,kind="checkpoint_time_value_not_human",source=source.source,
            assumptions=new[]{source.note,"Three checkpoints, ten seeds per policy. Same earned loadout, wallet and stage/attempt seeds across four paths. Only weapon growth selected; no equipment switching, attendance, missions, pass, ads or calendar rollover.","Free repeats latest cleared stage up to six attempts until the weapon reaches the level one pack could fund. Real victories/failures settle their own rewards. One free field supply is claimed after 300 verified seconds; losses count per normal rules.","Paid skip grants 100 test paid gems and buys exactly once only if incremental weapon levels exist; it performs no dedicated farm runs. Paid-and-play performs the same number of farming attempts as free, then spends earned materials on the weapon. Free cosmetics spends 600 existing gems only when available.","All then attempt the checkpoint operation once at a matched seed. Reached-target time includes simulated combat plus assumed 30-second menus per battle. Unreached goals are censored (-1), never counted as zero savings. This is not campaign duration, real purchase conversion, long-term inflation or human victory probability."},rows=results.OrderBy(x=>x.stage).ThenBy(x=>x.policy).ThenBy(x=>x.seed).ThenBy(x=>x.path)},GrowthAudit.Json));return 0;
    }
}
