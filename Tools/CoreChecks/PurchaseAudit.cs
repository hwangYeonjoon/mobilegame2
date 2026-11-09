using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ScrapGuard.Core;

// Calendar-matched local simulations. No Play checkout, network calls or human results.
static class PurchaseAudit
{
    internal static readonly string[] Cohorts={"none","ads_only","kit_free","pass_free","both_free","kit_paid","pass_paid","both_paid"};
    internal const string KitFirst="both_free_kit_first";
    internal static readonly string[] Policies={"natural_legacy","controlled_avoid"};
    internal sealed class Decision {public int day,gems,required;public string outcome;}
    internal sealed class Round {public int day,slot;public bool farm;public GrowthAudit.Attempt battle;}
    internal sealed class Day
    {
        public int day,clearedThrough,runs,paidGems,ledgerEnd;public float playSeconds,activeSeconds;
        public bool finished;public GrowthAudit.Snapshot state;
    }
    internal sealed class Journey
    {
        public int index,seed,clearedThrough,completedDay,paidGems,freeEarned,freeSpent,paidGranted,paidSpent,proposedWon,adsViewed;
        public string cohort,policy,status;public float playSeconds,activeSeconds;
        public GrowthAudit.Snapshot final;
        public List<Round> runs=new List<Round>();public List<Day> days=new List<Day>();
        public List<Decision> decisions=new List<Decision>();public List<GrowthAudit.Entry> ledger=new List<GrowthAudit.Entry>();
    }
    internal static int Seed(int seed,int stage,int attempt,bool farm)=>seed+stage*7919+(attempt-1)*104729+(farm?1000003:0);
    internal static void Claims(Economy e,GrowthAudit.Journey work,int stage,int attempt)
    {
        GrowthAudit.ClaimBenefits(e,work,stage,attempt);
        for(int tier=0;tier<Growth.PassStages.Length;tier++)foreach(bool premium in new[]{false,true})
            if(Growth.PassReady(e.State,tier,premium))
                if(!GrowthAudit.Act(e,work,stage,attempt,premium?"pass_premium_claim":"pass_free_claim",tier.ToString(),()=>e.ClaimGrowthPass(tier,premium)))throw new Exception("Earned pass claim rejected");
    }
    internal static void Purchases(Economy e,GrowthAudit.Journey work,Journey j,int day,int stage,int attempt)
    {
        bool pass=j.cohort.StartsWith("pass_")||j.cohort.StartsWith("both_");
        bool kit=j.cohort.StartsWith("kit_")||j.cohort.StartsWith("both_");
        bool needsPass=pass&&!Growth.PassOwned(e.State),needsKit=kit&&e.State.dailyGrowthKits==0;
        if(j.cohort==KitFirst&&needsKit&&e.State.gems>=Growth.KitPrice)
        {
            if(!GrowthAudit.Act(e,work,stage,attempt,"buy_kit","growth_kit",()=>e.Buy("growth_kit")))throw new Exception("Affordable kit-first purchase rejected");
            needsKit=false;
        }
        int wanted=(needsPass?Growth.PassPrice:0)+(needsKit?Growth.KitPrice:0);
        if(wanted==0)return;
        if(j.cohort.EndsWith("_paid")&&e.State.gems<wanted)
        {
            // Same default pack suggestion as the game UI. This is NOT an actual purchase.
            var offer=CommerceCatalog.SmallestPack(wanted-e.State.gems);
            if(offer==null)throw new Exception("No catalog top-up covers planned purchases");
            if(!GrowthAudit.Act(e,work,stage,attempt,"simulated_topup",offer.Id,()=>{PremiumWallet.Grant(e.State,offer.Gems);return CommandResult.Ok("Offline funded scenario only");}))throw new Exception("Top-up failed");
        }
        if(needsPass)
        {
            if(e.State.gems<Growth.PassPrice)
            {
                // Both-free reserves for the pass first; never silently treats saved gems as spent.
                j.decisions.Add(new Decision{day=day,gems=e.State.gems,required=Growth.PassPrice,outcome="reserve_for_pass"});return;
            }
            if(!GrowthAudit.Act(e,work,stage,attempt,"buy_pass","growth_pass",()=>e.Buy("growth_pass")))throw new Exception("Affordable pass rejected");
        }
        if(needsKit)
        {
            if(e.State.gems<Growth.KitPrice){j.decisions.Add(new Decision{day=day,gems=e.State.gems,required=Growth.KitPrice,outcome="kit_unaffordable"});return;}
            if(!GrowthAudit.Act(e,work,stage,attempt,"buy_kit","growth_kit",()=>e.Buy("growth_kit")))throw new Exception("Affordable daily kit rejected");
        }
    }
    internal static Journey Play(int index,int seed,string cohort,string policy,int days,int maxStage,int slots)
    {
        var state=GameState.New();var now=GrowthAudit.Start;var e=new Economy(state,()=>now);
        var j=new Journey{index=index,seed=seed,cohort=cohort,policy=policy};
        var work=new GrowthAudit.Journey{strategy="balanced",benefits=cohort=="ads_only"?"daily_plus_rewarded":"daily_claims",ledger=j.ledger};
        var attempts=new int[Campaign.Count];var farms=new int[Campaign.Count];int losses=0;
        for(int day=1;day<=days;day++)
        {
            if(j.clearedThrough<maxStage)
            {
                now=GrowthAudit.Start.AddDays(day-1);e.RefreshDay();
                for(int slot=1;slot<=slots&&j.clearedThrough<maxStage;slot++)
                {
                    int frontier=j.clearedThrough;int beforeAds=j.ledger.Count(x=>x.action.StartsWith("rewarded_"));
                    Claims(e,work,frontier,attempts[frontier]+1);
                    Purchases(e,work,j,day,frontier,attempts[frontier]+1);
                    Claims(e,work,frontier,attempts[frontier]+1);
                    int adSeconds=30*(j.ledger.Count(x=>x.action.StartsWith("rewarded_"))-beforeAds);
                    now=now.AddSeconds(adSeconds);j.activeSeconds+=adSeconds;
                    bool farm=losses>=2&&frontier>0;int stage=farm?Growth.FarmStage(state):frontier;
                    int attempt=farm?++farms[stage]:++attempts[stage];
                    var preparation=GrowthAudit.Prepare(e,work,stage,attempt);var before=GrowthAudit.Capture(state);
                    int combatSeed=Seed(seed,stage,attempt,farm);string id="purchase-audit-"+index+"-"+j.runs.Count;
                    if(!GrowthAudit.Act(e,work,stage,attempt,"battle_start",id,()=>e.BeginRun(id,stage)))throw new Exception("Unreachable stage");
                    var c=GrowthAudit.Simulate(state,stage,id,combatSeed,policy,out var recorder);
                    if(!GrowthAudit.Act(e,work,stage,attempt,"battle_finish",id,()=>e.FinishCampaign(c)))throw new Exception("Settlement rejected");
                    var receipt=state.receipts.Find(x=>x.id==id);
                    j.runs.Add(new Round{day=day,slot=slot,farm=farm,battle=new GrowthAudit.Attempt{sequence=j.runs.Count+1,stage=stage+1,attempt=attempt,seed=combatSeed,
                        mission=c.Definition.Mission.ToString(),before=before,after=GrowthAudit.Capture(state),preparation=preparation,won=c.Won,bossReached=c.BossArenaActive,
                        seconds=c.Elapsed,hp=c.Hp,bossSeconds=c.BossCombatSeconds,bossRemaining=c.Report().bossRemaining,cause=((DamageCause)c.Report().cause).ToString(),
                        damageReceived=c.DamageReceived,healing=c.HealingReceived,bossContactDamage=c.BossContactDamage,bossWarningDamage=c.BossWarningDamage,
                        kills=c.Kills,level=c.Level,machines=c.MachinesBuilt,peakBossWarnings=c.PeakBossWarnings,
                        reward=new GrowthAudit.Wallet{gold=receipt.gold,parts=receipt.parts,gems=receipt.gems},fieldGold=c.TaskBonusGold,fieldParts=c.TaskBonusParts}});
                    j.playSeconds+=c.Elapsed;j.activeSeconds+=c.Elapsed+30;now=now.AddSeconds(c.Elapsed+30);
                    if(farm)losses=0;else if(c.Won){j.clearedThrough++;losses=0;}else losses++;
                    beforeAds=j.ledger.Count(x=>x.action.StartsWith("rewarded_"));
                    // Claim the final run's mission/result/pass rewards before the next service day.
                    Claims(e,work,stage,attempt);
                    adSeconds=30*(j.ledger.Count(x=>x.action.StartsWith("rewarded_"))-beforeAds);now=now.AddSeconds(adSeconds);j.activeSeconds+=adSeconds;
                    if(j.clearedThrough==maxStage)j.completedDay=day;
                }
            }
            // A finished campaign freezes: no forced shopping/farming just to use the remaining budget.
            j.days.Add(new Day{day=day,clearedThrough=j.clearedThrough,runs=j.runs.Count,paidGems=state.paidGems,ledgerEnd=j.ledger.Count,
                playSeconds=j.playSeconds,activeSeconds=j.activeSeconds,finished=j.completedDay>0,state=GrowthAudit.Capture(state)});
        }
        j.final=GrowthAudit.Capture(state);j.paidGems=state.paidGems;j.status=j.completedDay>0?"target_cleared":"calendar_budget_exhausted";
        Totals(j);Validate(j,days,maxStage,slots);return j;
    }
    internal static void Totals(Journey j)
    {
        j.freeEarned=j.freeSpent=j.paidGranted=j.paidSpent=j.proposedWon=j.adsViewed=0;
        foreach(var r in j.ledger)
        {
            int paid=r.paidAfter-r.paidBefore,free=r.delta.gems-paid;
            j.freeEarned+=Math.Max(0,free);j.freeSpent+=Math.Max(0,-free);j.paidGranted+=Math.Max(0,paid);j.paidSpent+=Math.Max(0,-paid);
            if(r.action=="simulated_topup")j.proposedWon+=CommerceCatalog.Find(r.target).ProposedWon;
            if(r.action.StartsWith("rewarded_"))j.adsViewed++;
        }
    }
    internal static void Validate(Journey j,int days,int maxStage,int slots)
    {
        var w=GrowthAudit.Funds(GameState.New());int paid=0;
        foreach(var r in j.ledger)
        {
            if(r.before.gold!=w.gold||r.before.parts!=w.parts||r.before.gems!=w.gems||r.paidBefore!=paid)throw new Exception("Unlogged purchase balance");
            if(r.after.gold-r.before.gold!=r.delta.gold||r.after.parts-r.before.parts!=r.delta.parts||r.after.gems-r.before.gems!=r.delta.gems)throw new Exception("Invalid purchase delta");
            if(r.paidAfter<0||r.paidAfter>r.after.gems||r.after.gold<0||r.after.parts<0||r.after.gems<0)throw new Exception("Invalid wallet provenance");
            if(r.action=="simulated_topup")
            {var offer=CommerceCatalog.Find(r.target);if(!j.cohort.EndsWith("_paid")||offer==null||r.delta.gems!=offer.Gems||r.paidAfter-paid!=offer.Gems)throw new Exception("Unfunded top-up");}
            else if(r.paidAfter>paid)throw new Exception("Paid gems appeared without a top-up");
            w=r.after;paid=r.paidAfter;
        }
        if(w.gold!=j.final.wallet.gold||w.parts!=j.final.wallet.parts||w.gems!=j.final.wallet.gems||paid!=j.paidGems)throw new Exception("Final provenance mismatch");
        if(j.days.Count!=days||j.runs.Count>days*slots||j.clearedThrough>maxStage)throw new Exception("Calendar mismatch");
        if(j.ledger.Where(x=>x.action=="buy_kit").GroupBy(x=>x.serviceDate).Any(g=>g.Count()>1)||j.ledger.Count(x=>x.action=="buy_pass")>1)throw new Exception("Purchase limit mismatch");
        int frontier=1;var attempts=new int[Campaign.Count];var farms=new int[Campaign.Count];
        foreach(var r in j.runs)
        {
            var b=r.battle;
            if(b.before.totalRuns!=b.sequence-1||b.after.totalRuns!=b.sequence)throw new Exception("Run count mismatch");
            if(r.farm?!b.before.clearedStages.Contains(b.stage):b.stage!=frontier)throw new Exception("Future stage or invalid farming");
            int n=r.farm?++farms[b.stage-1]:++attempts[b.stage-1];
            if(n!=b.attempt||b.seed!=Seed(j.seed,b.stage-1,n,r.farm))throw new Exception("Unpaired seed");
            if(!r.farm&&b.won)frontier++;
        }
        if(j.clearedThrough!=frontier-1)throw new Exception("Clear mismatch");
    }
    public static int Run(string output,int seedCount=3,int days=14,int maxStage=42,int slots=6,string selection=null)
    {
        if(seedCount<1||seedCount>10||days<1||days>60||maxStage<1||maxStage>Campaign.Count||slots<1||slots>12)throw new ArgumentOutOfRangeException();
        var selected=selection==null?Cohorts:selection.Split(',');
        if(selected.Distinct().Count()!=selected.Length||selected.Any(x=>!Cohorts.Contains(x)&&x!=KitFirst))throw new ArgumentException("Unknown/duplicate cohort");
        var jobs=new List<(int index,int seed,string cohort,string policy)>();int[] seeds={17,41,71,91,123};
        foreach(var cohort in selected)foreach(var policy in Policies)for(int s=0;s<seedCount;s++)jobs.Add((jobs.Count,s<5?seeds[s]:127+(s-5)*37,cohort,policy));
        var results=new List<Journey>();Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        Parallel.ForEach(jobs,new ParallelOptions{MaxDegreeOfParallelism=2},job=>
        {
            var j=Play(job.index,job.seed,job.cohort,job.policy,days,maxStage,slots);lock(results)results.Add(j);
            Console.WriteLine(job.cohort+" "+job.policy+" seed="+job.seed+" clear="+j.clearedThrough+" runs="+j.runs.Count+" paid="+j.paidGranted+" gems");
        });
        var result=new{kind="purchase_calendar_simulation_not_human",schema=1,build=AppRelease.Build,rules=Combat.RulesVersion,seedCount,days,maxStage,slots,
            start=GrowthAudit.Start,strategy="balanced",robotTarget=5,cohorts=selected,policies=Policies,
            packs=CommerceCatalog.Offers.Where(x=>x.Gems>0).Select(x=>new{id=x.Id,gems=x.Gems,proposedWon=x.ProposedWon}).ToArray(),
            assumptions=new[]{"Same calendar days and sortie slots, not equal combat minutes. Report active time separately, including 30s menus per sortie and 30s per assumed successful ad.",
                "Every cohort claims earned attendance, missions and FREE operation-pass rewards. Only ads_only assumes successful ads; no ad skips or revives.",
                "Free cohorts never receive paid gems. Paid cohorts use earned gems first and the UI's smallest sufficient pack only for the remaining planned purchase cost; retail price is an offline proposal, never a charge or current Play price.",
                "Both cohorts prioritize the pass, then the daily kit. Both-free saves for the pass and skips kits while reserving. This is one explicit purchasing policy, not all player choices.",
                "Sensitivity both_free_kit_first instead buys today's kit as soon as affordable, then saves remaining earned gems for the pass. It never tops up.",
                "Two frontier losses trigger one replay of the highest cleared operation; that farming battle may also lose. Farming consumes a sortie, never grants a fabricated victory or first-clear bonus.",
                "Every battle uses the real Combat and FinishCampaign. Only legitimately earned gear is equipped/upgraded with the balanced policy. No future progression is injected.",
                "Stage/attempt seeds are paired; farming has a separate seed stream. Progression, loot and combat trajectories can diverge. Natural and avoidance policies are separate observations, not matched movement controls.",
                "Stop spending/playing after the target is cleared; later day checkpoints are frozen. Unreached late stages are censored, not inferred difficulty or zero-level observations.",
                "No guilds, extra free event income, other gem products, paid cosmetics, returns/refunds or human behavior are modeled. No actual purchases, ads or network traffic."},
            journeys=results.OrderBy(j=>j.index).ToArray()};
        File.WriteAllText(output,JsonSerializer.Serialize(result,GrowthAudit.Json));return 0;
    }
    public static int StudyProfiles(string folder)
    {
        Directory.CreateDirectory(folder);
        foreach(int gems in new[]{0,100,1000})if(File.Exists(Path.Combine(folder,"study-"+gems+"-qa-profile.json")))throw new IOException("Refusing to overwrite a study profile");
        foreach(int gems in new[]{0,100,1000})
        {
            string path=Path.Combine(folder,"study-"+gems+"-qa-profile.json");if(File.Exists(path))throw new IOException("Refusing to overwrite a study profile");
            var s=GameState.New();s.testProgress=true;s.gems=gems;s.paidGems=0;
            File.WriteAllText(path,JsonSerializer.Serialize(s,GrowthAudit.Json));
        }
        File.WriteAllText(Path.Combine(folder,"README.txt"),"OFFLINE QA FIXTURES ONLY. Injected test gems, not earned currency or real purchases. Use a fresh manual .qa installation and keep separate from the economic audit. Consent is NOT pre-enabled. Never install into a production profile.\n");
        return 0;
    }
    public static int StudyClone(string input,string folder)
    {
        var original=JsonSerializer.Deserialize<GameState>(File.ReadAllText(input),GrowthAudit.Json);
        if(original==null||!original.testProgress||!string.IsNullOrEmpty(original.activeRun))throw new ArgumentException("Requires an idle, explicitly test-marked offline QA profile.");
        Directory.CreateDirectory(folder);string a=Path.Combine(folder,"control-qa-profile.json"),b=Path.Combine(folder,"funded-qa-profile.json");
        if(File.Exists(a)||File.Exists(b))throw new IOException("Refusing to overwrite a study pair");
        var funded=GrowthAudit.Clone(original);funded.gems=checked(funded.gems+1000);
        File.WriteAllText(a,JsonSerializer.Serialize(original,GrowthAudit.Json));File.WriteAllText(b,JsonSerializer.Serialize(funded,GrowthAudit.Json));
        File.WriteAllText(Path.Combine(folder,"README.txt"),"OFFLINE MANUAL .qa ONLY. Identical checkpoint; funded copy adds 1,000 artificial FREE test gems. Original file is unchanged. No paid receipt, day reset, stage unlock or consent change. Not real willingness-to-pay evidence. Keep participant profiles out of git.\n");return 0;
    }
}
