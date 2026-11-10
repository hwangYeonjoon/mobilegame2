using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ScrapGuard.Core;
static class LongHorizonAudit
{
    internal sealed class Run
    {
        public int day,slot,stage,seed;public string mode;public bool won,repeat;public float seconds;
        public int machines,score;public GrowthAudit.Snapshot before,after;
    }
    internal sealed class Day
    {
        public int day,cleared,paid,paidSpent,freeSpent;public GrowthAudit.Snapshot state;
    }
    internal sealed class Journey
    {
        public int seed;public string cohort,policy;public int first42Day, firstCampaignDay,paidGranted,paidSpent,paidLeft,proposedWon,freeSpent,freeEarned;
        public float minutes;public List<Run> runs=new List<Run>();public List<Day> days=new List<Day>();public List<GrowthAudit.Entry> ledger=new List<GrowthAudit.Entry>();
    }
    static Journey Play(int seed,string cohort,string policy,int days,int slots)
    {
        var now=GrowthAudit.Start;var s=GameState.New();var e=new Economy(s,()=>now);var j=new Journey{seed=seed,cohort=cohort,policy=policy};
        var w=new GrowthAudit.Journey{strategy="balanced",benefits="daily_claims",ledger=j.ledger};
        var funding=new PurchaseAudit.Journey{cohort=cohort=="both_paid"?"both_paid":"kit_free",ledger=j.ledger};
        var attempts=new Dictionary<string,int>();var attemptedWeeks=new HashSet<string>();
        void Act(string action,string target,Func<CommandResult> f){if(!GrowthAudit.Act(e,w,s.unlockedStage,1,action,target,f))throw new Exception(action+" rejected");}
        void Claims()
        {
            PurchaseAudit.Claims(e,w,s.unlockedStage,1);
            if(!s.salvageClaimed&&s.dailySupplySeconds>=300)Act("salvage_claim","",e.ClaimSalvage);
            foreach(var week in s.weekly)
            {
                for(int tier=0;tier<4;tier++)if(week.attempts>0&&week.bestScore>=Endgame.Goals[tier]&&(week.claimed&(1<<tier))==0){int t=tier;Act("weekly_claim",week.week+":"+t,()=>e.ClaimWeekly(week.week,t));}
                if(week.robotContract&&(week.claimed&16)==0)Act("robot_week_claim",week.week,()=>e.ClaimRobotWeek(week.week));
            }
        }
        for(int day=1;day<=days;day++)
        {
            now=GrowthAudit.Start.AddDays(day-1);e.RefreshDay();
            for(int slot=1;slot<=slots;slot++)
            {
                Claims();
                if(cohort=="cosmetic_free"&&!Outfits.Owned(s,1)&&s.gems>=600)Act("cosmetic_buy",Outfits.Key,()=>Social.Buy(s,Outfits.Key));
                if((cohort=="kit_free"||cohort=="both_paid")&&Growth.Steps(s).Count>0)PurchaseAudit.Purchases(e,w,funding,day,s.unlockedStage,1);
                Claims();GrowthAudit.Prepare(e,w,s.unlockedStage,1);
                string week=Endgame.Week(now);bool weekly=Endgame.Unlocked(s)&&!attemptedWeeks.Contains(week);
                bool repeat=!weekly&&s.clearedStages.Count==Campaign.Count&&s.dailySupplySeconds<300;
                if(!weekly&&!repeat&&s.clearedStages.Count==Campaign.Count)break;
                int stage=weekly?4:repeat?Growth.SalvageStage(s):s.unlockedStage;string mode=weekly?Endgame.WeeklyMode:"";
                if(weekly)attemptedWeeks.Add(week);
                string key=mode+":"+stage;int attempt=attempts.TryGetValue(key,out var n)?n+1:1;attempts[key]=attempt;
                int combatSeed=weekly?Endgame.Seed(week):PurchaseAudit.Seed(seed,stage,attempt,repeat);
                string id="long-"+seed+"-"+j.runs.Count;var before=GrowthAudit.Capture(s);
                Act("battle_start",id,()=>weekly?e.BeginChallenge(id,mode):e.BeginRun(id,stage));
                var c=GrowthAudit.Simulate(s,stage,id,combatSeed,policy,out var recorder,mode);
                now=now.AddSeconds(c.Elapsed+30);j.minutes+=(c.Elapsed+30)/60;
                Act("battle_finish",id,()=>weekly?e.FinishChallenge(c):e.FinishCampaign(c));
                j.runs.Add(new Run{day=day,slot=slot,stage=stage+1,seed=combatSeed,mode=mode,won=c.Won,repeat=repeat,seconds=c.Elapsed,machines=c.MachinesBuilt,score=c.CoreScore,before=before,after=GrowthAudit.Capture(s)});
                if(s.clearedStages.Count>=42&&j.first42Day==0)j.first42Day=day;
                if(s.clearedStages.Count==Campaign.Count&&j.firstCampaignDay==0)j.firstCampaignDay=day;
                Claims();
            }
            PurchaseAudit.Totals(funding);j.days.Add(new Day{day=day,cleared=s.clearedStages.Count,paid=s.paidGems,paidSpent=funding.paidSpent,freeSpent=funding.freeSpent,state=GrowthAudit.Capture(s)});
        }
        PurchaseAudit.Totals(funding);j.paidGranted=funding.paidGranted;j.paidSpent=funding.paidSpent;j.paidLeft=s.paidGems;j.proposedWon=funding.proposedWon;j.freeEarned=funding.freeEarned;j.freeSpent=funding.freeSpent;
        return j;
    }
    public static int Execute(string output,int seeds=10,int days=28,int slots=6)
    {
        if(seeds<1||seeds>30||days<1||days>90||slots<1||slots>12)throw new ArgumentOutOfRangeException();
        string[] cohorts={"reserve","cosmetic_free","kit_free","both_paid"},policies={"natural_legacy","controlled_avoid"};
        var jobs=new List<(int seed,string cohort,string policy)>();foreach(string c in cohorts)foreach(string p in policies)for(int i=0;i<seeds;i++)jobs.Add((17+i*37,c,p));
        var results=new List<Journey>();Parallel.ForEach(jobs,new ParallelOptions{MaxDegreeOfParallelism=4},x=>{var j=Play(x.seed,x.cohort,x.policy,days,slots);lock(results)results.Add(j);Console.WriteLine(x.cohort+" "+x.policy+" seed="+x.seed+" clear="+j.days.Last().cleared+" day42="+j.first42Day+" runs="+j.runs.Count);});
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        File.WriteAllText(output,JsonSerializer.Serialize(new{kind="long_horizon_not_human",schema=2,campaignCount=Campaign.Count,build=AppRelease.Build,rules=Combat.RulesVersion,seeds,days,slots,cohorts,policies,
            assumptions=new[]{"All cohorts attempt the weekly challenge, then advance the campaign. Ordinary verified battles totaling 300 seconds (minimum 60 per run) earn field supply without repeat-only obligations. After the campaign only, repeat until the field supply is earned.",
                "All use actual earned attendance, missions, free pass, salvage and weekly rewards. No ad income. Cosmetic cohort buys the 600-gem heavy engineer outfit once and never buys growth. Reserve keeps gems. Paid uses free gems first and catalog proposal prices.",
                "Continue through the packaged campaign to the calendar horizon. Stop a day after campaign completion and field supply. Stop buying growth if all equipped growth steps are capped. first42Day remains a historical milestone; firstCampaignDay records the current finale.",
                "Paired stage/attempt seeds within policy; weekly seed shared by calendar. Natural and avoid are not movement-matched controls. All snapshots contain only actually earned gear. No injected progression or real payments."},journeys=results.OrderBy(x=>x.cohort).ThenBy(x=>x.policy).ThenBy(x=>x.seed).ToArray()},GrowthAudit.Json));return 0;
    }
}
