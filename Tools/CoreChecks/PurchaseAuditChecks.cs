using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using ScrapGuard.Core;
static class PurchaseAuditChecks
{
    static int count;
    static void Check(bool b,string text){if(!b)throw new Exception(text);count++;Console.WriteLine("PASS "+text);}
    static (GameState,Economy,GrowthAudit.Journey,PurchaseAudit.Journey) Fixture(string cohort,Func<DateTimeOffset> clock)
    {
        var s=GameState.New();var e=new Economy(s,clock);var j=new PurchaseAudit.Journey{cohort=cohort};
        return (s,e,new GrowthAudit.Journey{strategy="balanced",benefits="daily_claims",ledger=j.ledger},j);
    }
    public static int Run()
    {
        var now=GrowthAudit.Start;var (s,e,w,j)=Fixture("none",()=>now);
        PurchaseAudit.Purchases(e,w,j,1,0,1);Check(s.gems==0&&s.gold==1800&&j.ledger.Count==0,"no purchase control gets no fabricated funds");
        (s,e,w,j)=Fixture("kit_free",()=>now);PurchaseAudit.Purchases(e,w,j,1,0,1);
        Check(s.dailyGrowthKits==0&&j.decisions.Single().outcome=="kit_unaffordable","free path waits rather than receiving imaginary gems");
        GrowthAudit.Act(e,w,0,1,"test_fixture_free_gems","unit-only",()=>{s.gems+=100;return CommandResult.Ok("unit fixture");});
        PurchaseAudit.Purchases(e,w,j,1,0,1);PurchaseAudit.Totals(j);
        Check(s.gold==6300&&s.parts==70&&s.gems==0&&s.paidGems==0&&j.freeEarned==100&&j.freeSpent==100&&j.paidGranted==0,"earned gems buy actual kit without becoming paid gems");
        (s,e,w,j)=Fixture("kit_paid",()=>now);PurchaseAudit.Purchases(e,w,j,1,0,1);PurchaseAudit.Totals(j);
        Check(j.paidGranted==300&&j.paidSpent==100&&s.paidGems==200&&j.proposedWon==2200,"paid path uses catalog pack and retains excess gems");
        int entries=j.ledger.Count;PurchaseAudit.Purchases(e,w,j,1,0,2);Check(j.ledger.Count==entries&&s.dailyGrowthKits==1,"same day cannot repurchase or unnecessarily top up");
        now=now.AddDays(1);e.RefreshDay();PurchaseAudit.Purchases(e,w,j,2,0,3);PurchaseAudit.Totals(j);
        Check(j.paidGranted==300&&j.paidSpent==200&&s.paidGems==100,"next day spends remaining paid balance before another top up");
        GrowthAudit.Act(e,w,0,4,"test_fixture_free_gems","unit-only",()=>{s.gems+=50;return CommandResult.Ok("unit fixture");});
        now=now.AddDays(1);e.RefreshDay();PurchaseAudit.Purchases(e,w,j,3,0,4);PurchaseAudit.Totals(j);
        Check(j.freeSpent==50&&j.paidSpent==250&&s.paidGems==50,"free-first spending preserves correct paid/free provenance");
        (s,e,w,j)=Fixture("both_paid",()=>now);PurchaseAudit.Purchases(e,w,j,1,0,1);PurchaseAudit.Totals(j);
        Check(Growth.PassOwned(s)&&s.dailyGrowthKits==1&&j.paidGranted==1200&&j.paidSpent==1000&&j.proposedWon==7500,"combined purchase funds planned total with smallest sufficient default pack");
        Check(!Growth.PassReady(s,0,true),"buying a pass does not grant unearned milestones");
        (s,e,w,j)=Fixture("both_free",()=>now);
        GrowthAudit.Act(e,w,0,1,"test_fixture_free_gems","unit-only",()=>{s.gems=800;return CommandResult.Ok("unit fixture");});
        PurchaseAudit.Purchases(e,w,j,1,0,1);Check(s.gems==800&&s.dailyGrowthKits==0&&!Growth.PassOwned(s)&&j.decisions.Last().outcome=="reserve_for_pass","both-free reservation policy is explicit");
        var (ks,ke,kw,kj)=Fixture(PurchaseAudit.KitFirst,()=>now);ks.gems=800;PurchaseAudit.Purchases(ke,kw,kj,1,0,1);
        Check(ks.gems==700&&ks.dailyGrowthKits==1&&!Growth.PassOwned(ks)&&ks.paidGems==0,"kit-first sensitivity spends earned gems on kit before saving for pass");
        // Unit fixture only, not a campaign victory or an audit journey.
        s.clearedStages.Add(2);PurchaseAudit.Claims(e,w,2,1);int gold=s.gold;PurchaseAudit.Claims(e,w,2,1);
        Check(s.growthPassFree==1&&s.growthPassPremium==0&&s.gold==gold&&j.ledger.Count(x=>x.action=="pass_free_claim")==1,"earned free pass is available without purchase and claims once");
        Check(PurchaseAudit.Seed(17,11,3,false)==PurchaseAudit.Seed(17,11,3,false)&&PurchaseAudit.Seed(17,11,3,false)!=PurchaseAudit.Seed(17,11,3,true),"progression seeds pair independently of cohort and farm uses separate stream");
        (s,e,w,j)=Fixture("kit_paid",()=>now);PurchaseAudit.Purchases(e,w,j,1,0,1);j.final=GrowthAudit.Capture(s);j.paidGems=s.paidGems;j.days.Add(new PurchaseAudit.Day());
        PurchaseAudit.Validate(j,1,1,1);Check(true,"purchase ledger reconciles total and paid balances");
        j.ledger[0].paidAfter++;bool rejected=false;try{PurchaseAudit.Validate(j,1,1,1);}catch{rejected=true;}Check(rejected,"corrupt paid provenance rejected");
        string folder=Path.Combine(Path.GetTempPath(),"scrap-purchase-study-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            string input=Path.Combine(folder,"qa-profile.json"),output=Path.Combine(folder,"pair");
            s=GameState.New();s.testProgress=true;s.gems=120;s.paidGems=20;
            string original=JsonSerializer.Serialize(s,GrowthAudit.Json);File.WriteAllText(input,original);PurchaseAudit.StudyClone(input,output);
            var funded=JsonSerializer.Deserialize<GameState>(File.ReadAllText(Path.Combine(output,"funded-qa-profile.json")),GrowthAudit.Json);
            Check(funded.gems==1120&&funded.paidGems==20&&File.ReadAllText(input)==original,"study adds artificial free gems and preserves source and paid balance");
            funded.gems-=1000;Check(JsonSerializer.Serialize(funded,GrowthAudit.Json)==original,"study leaves all progression and consent fields unchanged");
            rejected=false;try{PurchaseAudit.StudyClone(input,output);}catch(IOException){rejected=true;}Check(rejected,"study refuses output overwrite");
            s.testProgress=false;File.WriteAllText(input,JsonSerializer.Serialize(s,GrowthAudit.Json));rejected=false;
            try{PurchaseAudit.StudyClone(input,Path.Combine(folder,"not-qa"));}catch(ArgumentException){rejected=true;}Check(rejected,"unmarked profile cannot enter study clone");
            s.testProgress=true;s.activeRun="unfinished";File.WriteAllText(input,JsonSerializer.Serialize(s,GrowthAudit.Json));rejected=false;
            try{PurchaseAudit.StudyClone(input,Path.Combine(folder,"active"));}catch(ArgumentException){rejected=true;}Check(rejected,"active battle cannot enter study clone");
        }
        finally{Directory.Delete(folder,true);}
        Console.WriteLine("Purchase audit checks: "+count+" passed");return 0;
    }
}
