using System;
using ScrapGuard.Core;
namespace ScrapGuard.Tests
{
    public static partial class CoreSpecification
    {
        public static void VectorGridPrecision()
        {
            var scaled=new V2(1042.28271484375f,0)*1.3280609846115112f;
            var divided=new V2(684.422607421875f,0)/.4917927384376526f;
            Check(scaled.x==1384.215087890625f,"scale retains precision until canonical grid rounding");
            Check(divided.x==1391.689208984375f,"normalization cannot introduce intermediate float rounding");
            Check(V2.Angle(Math.PI/2).x==0&&V2.Angle(Math.PI/2).y==1,"double angle retains canonical cardinal directions");
            // Captured from the first differing separation pair in a stage 42 replay.
            var separation=new V2(.097412109375f,-.9951171875f)*((30*.88-new V2(.65234375f,-6.65869140625f).Length)*.32);
            Check(separation.x==.614501953125f&&separation.y==-6.276123046875f,"separation keeps double intermediates at a grid boundary");
        }
        public static void GrowthPreviewAndCampaignImpact()
        {
            var e=New();var s=e.State;int gold=s.gold,parts=s.parts;
            var plans=Growth.Steps(s,Growth.KitGold,Growth.KitParts);
            Check(s.gold==gold&&s.parts==parts&&s.items[0].level==1,"preview grants or spends nothing");
            Check(plans[0].attack==140&&plans[1].health==1246,"first upgrade has visible attack or survival gain");
            Check(e.Upgrade(plans[0].id).Success&&Catalog.Attack(s)==plans[0].attack,"attack preview matches purchased upgrade");
            Check(e.Upgrade(plans[1].id).Success&&Catalog.Health(s)==plans[1].health,"health preview matches purchased upgrade");
            foreach(var item in s.items)item.level=10;
            Check(Catalog.Attack(s)>=220&&Catalog.Health(s)>=1700,"earned levels give material campaign growth");
            var practice=new Combat(s,4,"practice",17,false,Endgame.PracticeMode);
            Check(practice.Damage==Catalog.Attack(s)&&practice.MaxHp==Catalog.Health(s),"free practice uses the equipped campaign stats");
            var trial=new Combat(s,4,"skill",17,false,SkillTrial.Mode);var other=new Combat(GameState.New(),4,"skill",17,false,SkillTrial.Mode);
            Check(trial.Damage==other.Damage&&trial.MaxHp==other.MaxHp,"paid or earned growth cannot change fixed trial stats");
        }
        public static void ModuleUpgradeHasCombatEffect()
        {
            var s=GameState.New();var low=new Combat(s,1,"low",17);s.items[2].level=10;var high=new Combat(s,1,"high",17);
            low.Pickups.Add(new Pickup{id=777,position=low.Position+new V2(110,0),xp=1});
            high.Pickups.Add(new Pickup{id=777,position=high.Position+new V2(110,0),xp=1});
            low.Tick(.05f,new V2());high.Tick(.05f,new V2());
            Check(V2.Distance(high.Pickups[0].position,high.Position)<V2.Distance(low.Pickups[0].position,low.Position),"upgraded collector actually attracts distant pickups");
            Check(Growth.ModuleEffect(s.items[2])=="회수 반경 +52","module preview matches combat radius bonus");
        }
        public static void GrowthKitAndDailyLimits()
        {
            var now=Now;var s=GameState.New();s.gems=300;var e=new Economy(s,()=>now);
            Check(e.Buy("growth_kit").Success&&s.gems==200&&s.gold==6300&&s.parts==70,"kit grants exact displayed resources");
            Check(!e.Buy("growth_kit").Success&&s.gems==200,"same day kit capped without repeat debit");
            now=now.AddDays(1);Check(e.Buy("growth_kit").Success&&s.gems==100,"next service day reopens kit allowance");
            now=now.AddDays(-1);Check(!e.Buy("growth_kit").Success,"backwards clock never reopens kit");
            s.gemDebt=1;now=now.AddDays(2);Check(!e.Buy("growth_kit").Success&&s.gems==100,"refund debt blocks premium spending");
        }
        public static void GrowthKitTiersAndIncrementalValue()
        {
            var e=New();var s=e.State;s.gems=500;
            Check(Growth.CurrentKit(s).id=="growth_kit"&&!e.Buy("growth_kit_field").Success,"future tier cannot be bought");
            s.clearedStages.Add(11);Check(Growth.CurrentKit(s).gold==9000,"12 clear unlocks fixed 9000/80 pack");
            s.clearedStages.Add(29);Check(Growth.CurrentKit(s).gold==15000,"30 clear unlocks fixed 15000/120 pack");
            foreach(var item in s.items)item.level=16;s.heroes[0].level=16;s.gold=s.parts=0;
            var old=Growth.KitQuote(s,"growth_kit",0);var next=Growth.KitQuote(s,"growth_kit_frontier",0);
            Check(old.ExtraLevels==0&&next.ExtraLevels==2&&next.before.to==16&&next.after.to==18,"late pack funds real two-step growth where base funds none");
            Check(e.Buy("growth_kit_frontier").Success&&s.gold==15000&&s.parts==120&&s.gems==400,"tier has exact public price and grant");
            Check(!e.Buy("growth_kit_field").Success&&s.gems==400,"shared daily cap cannot be bypassed by tier");
            var rich=New();rich.State.gems=500;rich.State.gold=1000000;rich.State.parts=1000000;
            Check(!Growth.KitAddsGrowth(rich.State,"growth_kit")&&!rich.Buy("growth_kit").Success&&rich.State.gems==500,"cannot sell redundant materials when wallet already funds all equipped growth");
        }
        public static void GrowthKitAtomicUpgrade()
        {
            var e=New();var s=e.State;s.gems=100;var q=Growth.KitQuote(s,"growth_kit",0);int gold=s.gold,parts=s.parts;
            Check(q.ExtraLevels==q.after.to-q.before.to&&q.ExtraLevels>0,"paid delta excludes existing wallet");
            Check(!e.BuyGrowthKitAndUpgrade("growth_kit",0,q.after.count,q.after.id,q.after.from+1,0).Success&&s.gems==100&&s.gold==gold,"stale upgrade cannot debit or grant");
            Check(!e.BuyGrowthKitAndUpgrade("growth_kit",0,q.after.count+1,q.after.id,q.after.from,0).Success&&s.dailyGrowthKits==0,"forged count is atomic failure");
            Check(!e.BuyGrowthKitAndUpgrade("growth_kit",0,q.after.count,q.after.id,q.after.from,1).Success,"changed hero rejected");
            s.gemDebt=1;Check(!e.BuyGrowthKitAndUpgrade("growth_kit",0,q.after.count,q.after.id,q.after.from,0).Success&&s.gold==gold,"refund debt cannot upgrade");s.gemDebt=0;
            Check(e.BuyGrowthKitAndUpgrade("growth_kit",0,q.after.count,q.after.id,q.after.from,0).Success,"atomic purchase and upgrade");
            Check(s.gems==0&&s.gold==gold+4500-q.after.gold&&s.parts==parts+40-q.after.parts&&Catalog.Attack(s)==q.after.attack,"exact debit, material remainder, real stats");
            Check(!e.BuyGrowthKitAndUpgrade("growth_kit",0,q.after.count,q.after.id,q.after.from,0).Success,"duplicate cannot upgrade again");
            for(int slot=1;slot<4;slot++)
            {
                var other=New();other.State.gems=100;var k=Growth.KitQuote(other.State,"growth_kit",slot);
                Check(other.BuyGrowthKitAndUpgrade("growth_kit",slot,k.after.count,k.after.id,k.after.from,0).Success,"each growth route applies");
                Check(Catalog.Health(other.State)==k.after.health&&Catalog.Attack(other.State)==k.after.attack,"route quote agrees with actual stats");
            }
        }
        public static void GrowthPassRightsAndClaims()
        {
            var e=New();var s=e.State;s.gems=1000;
            Check(!e.ClaimGrowthPass(-1,false).Success&&!e.ClaimGrowthPass(4,true).Success,"pass tier bounds");
            Check(!e.ClaimGrowthPass(0,false).Success,"future milestone cannot be claimed");
            s.clearedStages.Add(2);Check(e.ClaimGrowthPass(0,false).Success&&s.gold==2800,"free milestone needs no purchase");
            Check(!e.ClaimGrowthPass(0,false).Success&&!e.ClaimGrowthPass(0,true).Success,"duplicate and unpaid premium claim rejected");
            Check(e.Buy("growth_pass").Success&&s.gems==100&&!e.Buy("growth_pass").Success,"single pass entitlement and exact debit");
            Check(e.ClaimGrowthPass(0,true).Success,"buying late unlocks already completed premium tier");
            s.clearedStages.Add(11);Check(e.ClaimGrowthPass(3,true).Success&&Outfits.Owned(s,1),"final tier grants real outfit and its existing effects");
            int gold=s.gold;Check(!e.ClaimGrowthPass(3,true).Success&&s.gold==gold,"final cosmetic and materials cannot be duplicated");
            var duplicate=New();duplicate.State.gems=900;duplicate.State.ownedCosmetics.Add(Outfits.Key);duplicate.State.clearedStages.Add(11);
            duplicate.Buy("growth_pass");duplicate.ClaimGrowthPass(3,true);
            Check(duplicate.State.gold==1800+6000+6000&&duplicate.State.parts==30+60+60,"owned outfit gets the disclosed material replacement");
        }
        public static void FreeGrowthRecovery()
        {
            var e=New();var s=e.State;s.unlockedStage=11;
            e.BeginRun("loss",11);e.FinishRun("loss",11,false,200,240);
            var receipt=s.receipts.Find(r=>r.id=="loss");
            Check(receipt.gold>1000&&receipt.gold<Campaign.Get(11).Gold&&s.gems==0&&s.unlockedStage==11,"late loss funds progress without first-clear grants or better payout than victory");
            s.clearedStages.Add(8);Check(Growth.FarmStage(s)==8,"repeat route only targets an actually cleared operation");
            int gems=s.gems,count=s.items.Count;e.BeginRun("farm",8);e.FinishRun("farm",8,true,200,240);
            Check(s.gems==gems&&s.items.Count==count,"repeat route never duplicates first clear items or gems");
            int beforeGold=s.gold,beforeParts=s.parts;int rewardGold=Growth.RepairGold(s),rewardParts=Growth.RepairParts(s);
            e.ClaimDemoReward("parts","repair");Check(s.gold==beforeGold+rewardGold&&s.parts==beforeParts+rewardParts,"optional ad supplies both bottleneck currencies");
            Check(!e.ClaimDemoReward("parts","repair").Success,"repair receipt cannot be reused");
        }
    }
}
