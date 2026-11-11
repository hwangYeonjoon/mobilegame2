using System;
using ScrapGuard.Core;
namespace ScrapGuard.Tests
{
    public static partial class CoreSpecification
    {
        public static void FactoryMinionsAndFinaleScope()
        {
            var s=GameState.New();s.tutorialCompleted=true;var c=new Combat(s,24,"factory",17);c.Elapsed=30;c.Position=new V2(270,600);
            for(int i=0;i<20;i++)c.Enemies.Add(new Enemy{id=9000+i,role=EnemyRole.Summoner,position=new V2(800,800),hp=10000,maxHp=10000});
            c.Tick(.05f,new V2());
            int minions=c.Enemies.FindAll(x=>x.summoned&&x.hp>0).Count;
            Check(minions>0&&minions<=8,"factory spawned shields cannot accumulate beyond eight");
            foreach(var e in c.Enemies)if(e.summoned)Check(e.role==EnemyRole.Shield&&e.hp<=50*c.Definition.Difficulty,"factory retains weak shield identity");
            var field=typeof(Combat).GetProperty("FinalePressure",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            float Pressure(int stage,string mode="")=>(float)field.GetValue(new Combat(s,stage,"scope",17,false,mode));
            Check(Pressure(30)==0&&Pressure(33)==0&&Pressure(34)==0,"ordinary late missions keep growth payoff");
            Check(Pressure(35)>0&&Pressure(41)>Pressure(35)&&Pressure(59)>Pressure(54),"finales ramp gradually");
            Check(Pressure(59,Apex.Mode)==0&&Pressure(4,SkillTrial.Mode)==0,"existing apex and fair trial excluded");
        }
        public static void SurvivalPracticeIsolation()
        {
            var e=New();var s=e.State;s.unlockedStage=14;s.tutorialCompleted=true;s.heroes[0].level=20;foreach(var item in s.items)item.level=18;
            Check(!e.BeginSurvivalPractice("locked",20).Success&&!e.BeginSurvivalPractice("boss",13).Success,"only unlocked survival operations");
            Check(e.BeginSurvivalPractice("survival-drill",14).Success,"free survival drill starts");
            int gold=s.gold,parts=s.parts,gems=s.gems,runs=s.totalRuns,daily=s.dailyRuns,supply=s.dailySupplySeconds;
            var c=new Combat(s,14,"survival-drill",17,false,SurvivalTraining.Mode);var r=new ReplayRecorder();
            Check(c.Practice&&c.SurvivalPractice&&!c.BossPractice&&c.Duration==60&&!c.CanChooseTask&&!c.RobotEnabled&&!c.Enemies.Exists(x=>x.finalBoss),"60 second regional drill, no boss/tasks/robots");
            for(int i=0;i<1900&&!c.Ended;i++)
            {
                if(c.Choosing){int pick=c.Choices[0];c.Choose(pick);r.Choose(pick);}
                c.Tick(ReplayRecorder.Step,r.Move(V2.Angle(i*.013)));
            }
            Check(c.Ended&&c.Elapsed<=60.1f&&!c.CanRevive,"drill ends within sixty seconds and cannot revive");
            var replay=ReplayRecorder.Verify(s,14,c.RunId,17,r.Steps,65,SurvivalTraining.Mode);
            Check(replay.Hp==c.Hp&&replay.Kills==c.Kills&&replay.Won==c.Won,"drill replay agrees");
            Check(!e.FinishCampaign(c).Success&&!e.FinishBossPractice(c).Success&&!e.FinishRun(c.RunId,14,true,999,60).Success,"cannot settle as campaign or boss practice");
            Check(e.FinishPractice(replay).Success&&!e.FinishPractice(replay).Success,"practice settles once");
            Check(s.gold==gold&&s.parts==parts&&s.gems==gems&&s.totalRuns==runs&&s.dailyRuns==daily&&s.dailySupplySeconds==supply&&s.clearedStages.Count==0&&s.robotRecords.Count==0,"no currency, daily, collection or progression credit");
        }
        public static void RecentDamageSourcesAndCoaching()
        {
            var s=GameState.New();s.tutorialCompleted=true;var c=new Combat(s,14,"recent",17);
            for(int i=0;i<30;i++)
            {
                c.Enemies.Clear();c.Hazards.Clear();c.Elapsed=30+i;c.Hp=c.MaxHp;c.Hazards.Add(new Hazard{position=c.Position,radius=100,damage=10,delay=0,source=DamageSource.EnemyAttack,cause=DamageCause.Poison});c.Tick(.05f,new V2());
            }
            var report=c.Report();Check(report.recentHits.Count>0&&report.recentHits.Count<=32,"recent damage bounded");
            foreach(var hit in report.recentHits)Check(hit.seconds>=c.Elapsed-8&&hit.source==(int)DamageSource.EnemyAttack,"recent window preserves enemy source");
            Check(BossTraining.Advice(report).Contains("적")&&!BossTraining.Advice(report).Contains("지대"),"enemy poison is not mislabeled as floor damage");
            c.Elapsed+=9;Check(c.Report().recentHits.Count==0,"old hits expire without another hit");
            Check(BossTraining.Advice(new BattleReport{lastSource=(int)DamageSource.Terrain,cause=(int)DamageCause.Poison}).Contains("지대"),"terrain poison advice preserved");
        }
        public static void BossPracticeIsolation()
        {
            var e=New();var s=e.State;
            Check(!e.BeginBossPractice("future",17).Success,"locked boss training rejected");
            s.unlockedStage=3;Check(!e.BeginBossPractice("survival",2).Success,"survival mission not a boss");
            Check(e.BeginBossPractice("training",1).Success,"unlocked boss can be practiced");
            var c=new Combat(s,1,"training",41,false,BossTraining.Mode);var record=new ReplayRecorder();
            Check(c.BossArenaActive&&c.Enemies.Exists(x=>x.finalBoss)&&!c.CanChooseTask&&!c.CanActivateMachine,"practice starts at boss with explicit preset, no robot or tasks");
            for(int i=0;i<1500&&!c.Ended;i++){if(c.Choosing){c.Choose(c.Choices[0]);record.Choose(c.Choices[0]);}c.Tick(ReplayRecorder.Step,record.Move(new V2(1,0)));}
            if(!c.Ended)c.Abandon();var replay=ReplayRecorder.Verify(s,1,"training",41,record.Steps,100,BossTraining.Mode);
            Check(c.Hp==replay.Hp&&c.Kills==replay.Kills&&c.Won==replay.Won,"boss practice replay reproduces");
            int gold=s.gold,parts=s.parts,gems=s.gems;
            Check(!e.FinishCampaign(c).Success&&e.FinishBossPractice(replay).Success,"practice cannot enter campaign settlement");
            Check(s.gold==gold&&s.parts==parts&&s.gems==gems&&s.totalRuns==0&&s.dailyRuns==0&&s.clearedStages.Count==0&&s.robotRecords.Count==0,"practice grants no progression, currency, or robot rights");
            Check(!e.FinishBossPractice(replay).Success&&!c.CanRevive,"practice settlement once, no ad revive");
        }
        public static void SalvageSupplyGuards()
        {
            var now=Now;var s=GameState.New();var e=new Economy(s,()=>now);
            Check(!e.ClaimSalvage().Success,"no unearned salvage");
            e.BeginRun("first",0);e.FinishRun("first",0,true,100,180);Check(s.dailySupplySeconds==180,"first clear contributes without repeating");
            e.BeginRun("short",0);e.FinishRun("short",0,true,100,40);Check(s.dailySupplySeconds==180,"short battle excluded");
            e.BeginRun("loss",0);e.FinishRun("loss",0,false,100,180);Check(s.dailySupplySeconds==300,"defeat contributes and daily time caps");
            for(int i=0;i<3;i++){string id="repeat"+i;e.BeginRun(id,0);Check(e.FinishRun(id,0,true,100,180).Success,"repeat settles");Check(!e.FinishRun(id,0,true,100,180).Success,"repeat receipt cannot count twice");}
            int gold=s.gold,parts=s.parts,gems=s.gems;
            Check(e.ClaimSalvage().Success&&s.gold==gold+4500&&s.parts==parts+40&&s.gems==gems,"same materials as kit without gems");
            Check(!e.ClaimSalvage().Success,"daily claim capped");now=now.AddDays(1);e.RefreshDay();Check(s.dailySupplySeconds==0&&!s.salvageClaimed&&!e.ClaimSalvage().Success,"next day must earn again");
            now=now.AddDays(-1);e.RefreshDay();Check(!e.ClaimSalvage().Success,"clock rollback cannot reopen reward");
        }
        public static void WeeklyRobotReplayAndRewards()
        {
            var e=New();var s=e.State;s.clearedStages.Add(4);s.unlockedStage=5;s.heroes[0].level=20;foreach(var item in s.items){item.level=20;item.rarity=2;}
            Check(e.BeginChallenge("robotweek",Endgame.WeeklyMode).Success,"weekly start");
            int seed=Endgame.Seed(s.activeWeek),design=RobotWeek.Frame(seed)*4+1;
            var c=new Combat(s,4,"robotweek",seed,false,Endgame.WeeklyMode);var r=new ReplayRecorder();
            Check(c.PlanRobot(design,true)&&c.ActivateMachine(),"weekly starter parts build selected specialty");r.RobotPlan(design,true);r.Machine();
            for(int i=0;i<2400&&!c.Ended;i++)
            {
                if(c.Choosing){int pick=c.Choices[0];c.Choose(pick);r.Choose(pick);}
                if(c.CanUltimate){c.UseUltimate();r.Ultimate();}
                var target=new V2(270+(float)Math.Cos(c.Elapsed*.25)*180,500+(float)Math.Sin(c.Elapsed*.25)*230);
                c.Tick(ReplayRecorder.Step,r.Move((target-c.Position).Normal));
            }
            if(!c.Ended)c.Abandon();var replay=ReplayRecorder.Verify(s,4,c.RunId,seed,r.Steps,100,Endgame.WeeklyMode);
            Check(replay.CoreScore==c.CoreScore&&replay.MachineDamage==c.MachineDamage&&c.MachineDamage>0,"robot damage contributes and replays");
            Check(e.FinishChallenge(replay).Success,"weekly verified settlement");var w=Endgame.Find(s,Endgame.Week(Now));
            Check(w!=null&&c.Elapsed>=60,"test pilot survived to contract threshold");
            Check(w.robotContract==(c.Elapsed>=60),"contract requires sixty seconds and specified specialty");
            if(w.robotContract){int g=s.gold,p=s.parts;Check(e.ClaimRobotWeek(w.week).Success&&s.gold==g+3000&&s.parts==p+30,"weekly materials");Check(!e.ClaimRobotWeek(w.week).Success,"weekly duplicate rejected");}
            Check(s.robotRecords.Exists(x=>x.design==design)&&RobotAssembly.Showcase(s,design).Success,"verified weekly robot can be displayed");
            Check(!e.ClaimRobotWeek("2099-01-01").Success,"unearned week rejected");
            var trial=new Combat(s,4,"trial",seed,false,SkillTrial.Mode);Check(!trial.RobotEnabled&&!trial.PlanRobot(design,true),"fixed trial unchanged");
            Check(RobotWeek.Frame("2026-11-02")!=RobotWeek.Frame("2026-11-09")&&RobotWeek.Frame("2026-11-02")==RobotWeek.Frame("2026-11-23"),"three week rotation");
        }
        public static void LastDamageCoaching()
        {
            var report=new BattleReport{lastCause=(int)DamageCause.Contact,lastSource=(int)DamageSource.Boss};
            Check(BossTraining.Advice(report).Contains("몸체"),"boss contact distinct from warnings");report.lastCause=(int)DamageCause.Charge;Check(BossTraining.Advice(report).Contains("옆으로"),"charge escape hint");
            report.lastCause=(int)DamageCause.Boss;Check(BossTraining.Advice(report).Contains("십자"),"warning advice distinct");
        }
    }
}
