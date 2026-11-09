using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ScrapGuard.Core;

// Offline economic trajectories, never an estimate of the average human player.
static class GrowthAudit
{
    internal static readonly DateTimeOffset Start=new DateTimeOffset(2026,11,9,3,0,0,TimeSpan.Zero);
    internal static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true,WriteIndented=true};
    internal static readonly string[] Strategies={"passive","balanced","attack_first"};
    internal static readonly string[] Benefits={"campaign_only","daily_claims","daily_plus_rewarded"};
    internal static readonly string[] Policies={"natural_legacy","controlled_ignore","controlled_avoid"};
    internal sealed class Wallet {public int gold,parts,gems;}
    internal sealed class Gear {public int slot,definition,rarity,level;public string name;}
    internal sealed class Snapshot
    {
        public int attack,health,heroLevel,unlockedStage,totalRuns;public int[] clearedStages;
        public Wallet wallet;public Gear[] equipped;
    }
    internal sealed class Entry
    {
        public int sequence,stage,attempt;public string action,target;public Wallet before,after,delta;
        public int paidBefore,paidAfter;public string serviceDate;
    }
    internal sealed class Quote
    {
        public string target;public int slot,level,gold,parts,missingGold,missingParts;public bool canAfford;
    }
    internal sealed class Preparation
    {
        public bool visited;public int upgradeActions;public Quote nextUpgrade;
    }
    internal sealed class Attempt
    {
        public int stage,attempt,seed,sequence;public string mission,cause;
        public Snapshot before,after;public Preparation preparation;
        public bool won,bossReached;public float seconds,hp,bossSeconds,bossRemaining;
        public float damageReceived,healing,bossContactDamage,bossWarningDamage;
        public int kills,level,machines,peakBossWarnings;
        public Wallet reward;public int fieldGold,fieldParts;
    }
    internal sealed class Journey
    {
        public int index,seed,maxStage,maxAttempts,clearedThrough,stoppedAt,completedRuns;
        public string strategy,benefits,policy,status;public float simulatedPlaySeconds;
        public Snapshot final;public List<Attempt> attempts=new List<Attempt>();public List<Entry> ledger=new List<Entry>();
    }
    internal static GameState Clone(GameState state)=>JsonSerializer.Deserialize<GameState>(JsonSerializer.Serialize(state,Json),Json);
    internal static Wallet Funds(GameState s)=>new Wallet{gold=s.gold,parts=s.parts,gems=s.gems};
    static Wallet Delta(Wallet a,Wallet b)=>new Wallet{gold=b.gold-a.gold,parts=b.parts-a.parts,gems=b.gems-a.gems};
    internal static Snapshot Capture(GameState s)=>new Snapshot{attack=Catalog.Attack(s),health=Catalog.Health(s),heroLevel=s.heroes[s.selectedHero].level,
        unlockedStage=s.unlockedStage+1,totalRuns=s.totalRuns,clearedStages=s.clearedStages.Select(i=>i+1).ToArray(),wallet=Funds(s),
        equipped=s.equipped.Select((id,slot)=>{var i=s.items.Find(x=>x.id==id);return new Gear{slot=slot,definition=i.definition,name=Catalog.Items[i.definition].Name,rarity=i.rarity,level=i.level};}).ToArray()};
    internal static bool Act(Economy e,Journey j,int stage,int attempt,string action,string target,Func<CommandResult> command)
    {
        var before=Funds(e.State);int paidBefore=e.State.paidGems;var result=command();var after=Funds(e.State);
        if(!result.Success)
        {
            if(before.gold!=after.gold||before.parts!=after.parts||before.gems!=after.gems||paidBefore!=e.State.paidGems)throw new Exception("Rejected action changed wallet: "+action);
            return false;
        }
        if(after.gold<0||after.parts<0||after.gems<0)throw new Exception("Negative economic balance");
        j.ledger.Add(new Entry{sequence=j.ledger.Count+1,stage=stage+1,attempt=attempt,action=action,target=target,before=before,after=after,delta=Delta(before,after),paidBefore=paidBefore,paidAfter=e.State.paidGems,serviceDate=e.State.serviceDate});
        return true;
    }
    static CommandResult Upgrade(Economy e,int slot)=>slot==3?e.UpgradeHero():e.Upgrade(e.State.equipped[slot]);
    internal static Quote UpgradeQuote(GameState state,int slot)
    {
        // Query the real command on a disposable clone. These probe funds never enter a journey.
        var copy=Clone(state);copy.gold=copy.parts=1000000;var e=new Economy(copy,()=>Start,false);
        var before=Funds(copy);if(!Upgrade(e,slot).Success)return null;
        int gold=before.gold-copy.gold,parts=before.parts-copy.parts;
        return new Quote{slot=slot,target=slot==3?"hero":Catalog.Items[state.items.Find(i=>i.id==state.equipped[slot]).definition].Name,
            level=slot==3?state.heroes[state.selectedHero].level:state.items.Find(i=>i.id==state.equipped[slot]).level,gold=gold,parts=parts,
            missingGold=Math.Max(0,gold-state.gold),missingParts=Math.Max(0,parts-state.parts),canAfford=gold<=state.gold&&parts<=state.parts};
    }
    static IEnumerable<Quote> OrderedQuotes(GameState s,string strategy)
    {
        return new[]{0,1,3,2}.Select(slot=>UpgradeQuote(s,slot)).Where(q=>q!=null)
            .OrderBy(q=>q.level/(strategy=="attack_first"?(q.slot==0?2f:q.slot==3?1f:.5f):1f));
    }
    internal static Preparation Prepare(Economy e,Journey j,int stage,int attempt)
    {
        bool visit=j.strategy!="passive"||e.State.totalRuns>0&&e.State.totalRuns%3==0;
        int actions=0;
        if(visit)
        {
            // Higher rarity only; equal-rarity weapon choices retain player preference.
            // No synthetic items or free levels. Transfer and equip use actual commands.
            for(int slot=0;slot<3;slot++)
            {
                var current=e.State.items.Find(i=>i.id==e.State.equipped[slot]);
                var candidate=e.State.items.Where(i=>Catalog.Items[i.definition].Slot==slot&&i.rarity>current.rarity)
                    .OrderByDescending(i=>i.rarity).ThenByDescending(i=>i.level).ThenBy(i=>i.definition).FirstOrDefault();
                if(candidate==null)continue;
                if(current.level>1&&candidate.level==1)
                    if(!Act(e,j,stage,attempt,"transfer",current.definition+"->"+candidate.definition,()=>e.Transfer(current.id,candidate.id)))throw new Exception("Legal transfer rejected");
                if(!Act(e,j,stage,attempt,"equip",candidate.definition.ToString(),()=>e.Equip(candidate.id)))throw new Exception("Owned equipment rejected");
            }
            int limit=j.strategy=="passive"?1:100;
            while(actions<limit)
            {
                var quote=OrderedQuotes(e.State,j.strategy).FirstOrDefault(q=>q.canAfford);if(quote==null)break;
                if(!Act(e,j,stage,attempt,"upgrade",quote.slot+":"+quote.target+":"+quote.level,()=>Upgrade(e,quote.slot)))throw new Exception("Quoted upgrade rejected");
                actions++;
            }
        }
        return new Preparation{visited=visit,upgradeActions=actions,nextUpgrade=OrderedQuotes(e.State,j.strategy).FirstOrDefault()};
    }
    internal static void ClaimBenefits(Economy e,Journey j,int stage,int attempt)
    {
        if(j.benefits=="campaign_only")return;
        Act(e,j,stage,attempt,"attendance","daily",()=>e.Attend());
        Act(e,j,stage,attempt,"missions","daily",()=>e.ClaimMissions());
        if(j.benefits!="daily_plus_rewarded")return;
        // A successful-view sensitivity scenario only. No SDK, ad request or paid purchase.
        // ClaimDemoReward is also the live server's eligibility probe; limits remain enforced.
        while(Act(e,j,stage,attempt,"rewarded_parts","simulated_successful_view",()=>e.ClaimDemoReward("parts","growth-parts-"+j.ledger.Count))){}
        Act(e,j,stage,attempt,"rewarded_result","simulated_successful_view",()=>e.ClaimDemoReward("result","growth-result-"+j.ledger.Count));
    }
    internal static Combat Simulate(GameState state,int stage,string id,int seed,string policy,out ReplayRecorder record)
    {
        var c=new Combat(state,stage,id,seed);record=new ReplayRecorder();bool natural=policy=="natural_legacy";
        for(int frame=0;frame<18000&&!c.Ended;frame++)
        {
            if(c.Choosing)
            {
                int skill=c.Choices[0];foreach(int preferred in new[]{1,8,0,2,3,9,4,5,10,7,11,6})if(Array.IndexOf(c.Choices,preferred)>=0){skill=preferred;break;}
                c.Choose(skill);record.Choose(skill);
            }
            if(c.CanChooseTask){c.ChooseTask(1);record.Encounter(1);}
            if(c.CanAssemble){c.Assemble(2);record.Assemble(2);}
            if(c.CanPlanRobot(5,true)){c.PlanRobot(5,true);record.RobotPlan(5,true);}
            if(c.CanActivateMachine&&(c.Enemies.Count>=8||!natural&&c.FinalBossActive)){c.ActivateMachine();record.Machine();}
            if(c.CanUltimate&&(c.Enemies.Count>=12||!natural&&c.FinalBossActive)){c.UseUltimate();record.Ultimate();if(c.Ended)break;}
            c.Tick(ReplayRecorder.Step,record.Move(natural?BalanceAudit.NaturalMove(c):BalanceAudit.Move(c,policy=="controlled_avoid")));
        }
        if(!c.Ended)throw new Exception("Simulation budget exhausted; no reward may be credited");
        return c;
    }
    internal static Journey Play(int index,int seed,string strategy,string benefits,string policy,int maxStage,int maxAttempts)
    {
        var state=GameState.New();var now=Start;var e=new Economy(state,()=>now);
        var j=new Journey{index=index,seed=seed,strategy=strategy,benefits=benefits,policy=policy,maxStage=maxStage,maxAttempts=maxAttempts};
        for(int stage=0;stage<maxStage;stage++)
        {
            bool cleared=false;
            for(int attempt=1;attempt<=maxAttempts;attempt++)
            {
                ClaimBenefits(e,j,stage,attempt);var preparation=Prepare(e,j,stage,attempt);
                var before=Capture(state);int combatSeed=seed+stage*7919+(attempt-1)*104729;
                string id="growth-"+index+"-"+stage+"-"+attempt;
                if(!Act(e,j,stage,attempt,"battle_start",id,()=>e.BeginRun(id,stage)))throw new Exception("Unreached stage attempted");
                var c=Simulate(state,stage,id,combatSeed,policy,out var record);
                // The core never mutates account stats while simulating. The result is settled once.
                if(!Act(e,j,stage,attempt,"battle_finish",id,()=>e.FinishCampaign(c)))throw new Exception("Simulation settlement rejected");
                var receipt=state.receipts.Find(x=>x.id==id);
                j.attempts.Add(new Attempt{sequence=j.attempts.Count+1,stage=stage+1,attempt=attempt,seed=combatSeed,mission=c.Definition.Mission.ToString(),
                    before=before,after=Capture(state),preparation=preparation,won=c.Won,bossReached=c.BossArenaActive,seconds=c.Elapsed,hp=c.Hp,
                    bossSeconds=c.BossCombatSeconds,bossRemaining=c.Report().bossRemaining,cause=((DamageCause)c.Report().cause).ToString(),
                    damageReceived=c.DamageReceived,healing=c.HealingReceived,bossContactDamage=c.BossContactDamage,bossWarningDamage=c.BossWarningDamage,
                    kills=c.Kills,level=c.Level,machines=c.MachinesBuilt,peakBossWarnings=c.PeakBossWarnings,
                    reward=new Wallet{gold=receipt.gold,parts=receipt.parts,gems=receipt.gems},fieldGold=c.TaskBonusGold,fieldParts=c.TaskBonusParts});
                j.completedRuns++;j.simulatedPlaySeconds+=c.Elapsed;now=now.AddSeconds(c.Elapsed+30);
                if(c.Won){cleared=true;j.clearedThrough=stage+1;break;}
            }
            if(!cleared){j.status="retry_limit";j.stoppedAt=stage+1;break;}
        }
        if(j.status==null)j.status="target_cleared";
        j.final=Capture(state);ValidateLedger(j);return j;
    }
    internal static void ValidateLedger(Journey j)
    {
        var initial=Funds(GameState.New());int gold=initial.gold,parts=initial.parts,gems=initial.gems;
        foreach(var row in j.ledger)
        {
            if(row.before.gold!=gold||row.before.parts!=parts||row.before.gems!=gems)throw new Exception("Unlogged economic mutation");
            gold+=row.delta.gold;parts+=row.delta.parts;gems+=row.delta.gems;
            if(gold!=row.after.gold||parts!=row.after.parts||gems!=row.after.gems||gold<0||parts<0||gems<0)throw new Exception("Ledger imbalance");
        }
        if(gold!=j.final.wallet.gold||parts!=j.final.wallet.parts||gems!=j.final.wallet.gems)throw new Exception("Final wallet mismatch");
        for(int i=0;i<j.attempts.Count;i++)
        {
            var run=j.attempts[i];
            if(run.before.clearedStages.Contains(run.stage)||run.before.unlockedStage!=run.stage)throw new Exception("Future rewards leaked into entry loadout");
            if(i>0){var previous=j.attempts[i-1];if(run.stage!=(previous.won?previous.stage+1:previous.stage))throw new Exception("Skipped failed stage");}
        }
    }
    public static int Run(string output,int seedCount=5,int maxStage=13,int maxAttempts=4)
    {
        if(seedCount<1||seedCount>30||maxStage<1||maxStage>Campaign.Count||maxAttempts<1||maxAttempts>10)throw new ArgumentOutOfRangeException("audit bounds");
        var jobs=new List<(int index,int seed,string strategy,string benefits,string policy)>();int[] seeds={17,41,71,91,123};
        foreach(string strategy in Strategies)foreach(string benefits in Benefits)foreach(string policy in Policies)for(int i=0;i<seedCount;i++)
            jobs.Add((jobs.Count,i<5?seeds[i]:127+(i-5)*37,strategy,benefits,policy));
        var journeys=new List<Journey>();
        Parallel.ForEach(jobs,new ParallelOptions{MaxDegreeOfParallelism=2},job=>
        {
            var j=Play(job.index,job.seed,job.strategy,job.benefits,job.policy,maxStage,maxAttempts);
            lock(journeys)journeys.Add(j);
            Console.WriteLine(job.strategy+" "+job.benefits+" "+job.policy+" seed="+job.seed+" cleared="+j.clearedThrough+" runs="+j.completedRuns+" status="+j.status);
        });
        var result=new {kind="economic_trajectory_simulation_not_human",schema=1,build=AppRelease.Build,rules=Combat.RulesVersion,seedCount,maxStage,maxAttempts,
            robotTarget=5,clockStart=Start,betweenRunSeconds=30,
            assumptions=new[]{"Continuous session; clock advances by combat duration plus 30 seconds, with no artificial day reset or overnight farming.",
                "No previous stage is granted as a win. Only real Combat outcomes settled with Economy.FinishCampaign unlock progress.",
                "Passive visits equipment every third completed run and buys at most one upgrade. Other strategies spend affordable funds between runs.",
                "Balanced picks lowest level across weapon, armor, hero, module. Attack-first weights those levels 2, 0.5, 1, 0.5.",
                "On visits all strategies equip higher-rarity earned items and use legal level transfer; equal-rarity weapon type remains unchanged.",
                "campaign_only excludes optional claims. daily_claims adds eligible attendance and mission claims. daily_plus_rewarded additionally assumes successful rewarded views within actual daily limits.",
                "No sales, shop purchases, premium spending, guild support, paid items, revived runs, fabricated participants or estimated human averages.",
                "Reached-only equipment summaries have survivor bias; unreached stages must remain null. Seeds and retry indices are paired across cohorts, not all combat paths."},
            journeys=journeys.OrderBy(j=>j.index).ToArray()};
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        File.WriteAllText(output,JsonSerializer.Serialize(result,Json));return 0;
    }
}
