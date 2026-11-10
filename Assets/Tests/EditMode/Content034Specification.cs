using System;
using ScrapGuard.Core;
namespace ScrapGuard.Tests
{
    public static partial class CoreSpecification
    {
        public static void RobotStyleCommerceAndGrowth()
        {
            var e=New();var s=e.State;int attack=Catalog.Attack(s),health=Catalog.Health(s);
            Check(!e.BuyRobotStylePack().Success&&s.gold==1800,"insufficient gems cannot grant pack");
            s.gems=RobotStyles.PackPrice;s.gemDebt=1;Check(!e.BuyRobotStylePack().Success,"refund debt rejects pack");s.gemDebt=0;
            var before=GrowthPaths.Quote(s,0);
            Check(e.BuyRobotStylePack().Success&&s.gems==0&&s.gold==1800&&s.parts==30,"pure cosmetic pack leaves materials unchanged");
            for(int f=0;f<3;f++)Check(RobotStyles.Active(s,f)==1,"all chassis styled immediately");
            Check(Catalog.Attack(s)==attack&&Catalog.Health(s)==health,"cosmetic adds no power or future spending advantage");
            Check(!e.BuyRobotStylePack().Success,"duplicate cannot charge again");
            var q=GrowthPaths.Quote(s,0);Check(q.to==before.to&&q.gold==before.gold&&q.parts==before.parts,"preview equals actual spending power");
            Check(!e.UpgradePath(0,31,q.id,q.from).Success&&!e.UpgradePath(0,q.count,"wrong",q.from).Success&&!e.UpgradePath(0,q.count,q.id,q.from+1).Success&&s.gold==1800,"invalid/stale batches spend nothing");
            Check(e.UpgradePath(0,q.count,q.id,q.from).Success&&Catalog.Attack(s)==q.attack&&s.gold==1800-q.gold&&s.parts==30-q.parts,"batch exact debit and combat stats");
            Check(!e.UpgradePath(0,q.count,q.id,q.from).Success,"repeated batch cannot double apply");
            Check(!e.CraftRobotStyle(0).Success,"craft requires earned campaign milestone");s.clearedStages.Add(41);s.gold=30000;s.parts=300;s.coreShards=12;
            Check(e.CraftRobotStyle(0).Success&&s.gold==0&&s.parts==0&&s.coreShards==0&&RobotStyles.Active(s,0)==2,"surplus materials create earned visual");
            Check(!RobotStyles.Equip(s,1,2).Success&&!e.CraftRobotStyle(0).Success,"ownership and duplicate craft guards");
            s.robotStyles[1]=2;RobotStyles.Normalize(s);Check(s.robotStyles[1]==0,"forged selected style normalized");
            var a=new Combat(GameState.New(),4,"same",17,false,SkillTrial.Mode);var b=new Combat(s,4,"same",17,false,SkillTrial.Mode);Check(a.Damage==b.Damage&&a.MaxHp==b.MaxHp,"fixed trial unaffected by purchased growth");
            foreach(var item in s.items)item.level=20;s.heroes[s.selectedHero].level=30;s.gems=100;Check(!e.BuyGrowthKit().Success&&s.gems==100,"maxed active build cannot buy unusable kit");
            s.dailySalvageWins=2;s.dailySupplySeconds=0;e.RefreshDay();Check(s.dailySupplySeconds==200&&s.dailySalvageWins==0,"old earned progress carried once");e.RefreshDay();Check(s.dailySupplySeconds==200,"migration idempotent");
        }
        public static void Expansion034BossReplay()
        {
            var s=GameState.New();for(int i=0;i<42;i++)s.clearedStages.Add(i);s.unlockedStage=41;int gems=s.gems;Campaign.Normalize(s);
            Check(s.unlockedStage==42&&s.gems==gems&&Campaign.Regions.Length==10,"old finale unlocks new content without rewards");
            s.tutorialCompleted=true;s.heroes[0].level=30;foreach(var item in s.items){item.level=20;item.rarity=2;}
            foreach(int stage in new[]{42,47,48,53,54,59})
            {
                var c=new Combat(s,stage,"newboss",71,false,BossTraining.Mode);var r=new ReplayRecorder();
                for(int i=0;i<900&&!c.Ended;i++){if(c.Choosing){int skill=c.Choices[0];c.Choose(skill);r.Choose(skill);}c.Tick(ReplayRecorder.Step,r.Move(new V2((float)Math.Cos(i*.02),(float)Math.Sin(i*.02))));}
                if(!c.Ended)c.Abandon();var replay=ReplayRecorder.Verify(s,stage,"newboss",71,r.Steps,90,BossTraining.Mode);
                Check(c.BossPatterns>0&&c.PeakBossWarnings<=7,"new boss uses bounded, coordinated attack groups");
                Check(c.Kills==replay.Kills&&c.Hp==replay.Hp&&c.BossPatterns==replay.BossPatterns,"new pattern replay deterministic "+stage);
            }
        }
    }
}
