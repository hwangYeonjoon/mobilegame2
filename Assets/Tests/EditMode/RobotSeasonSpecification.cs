using System;
using ScrapGuard.Core;
namespace ScrapGuard.Tests
{
    public static partial class CoreSpecification
    {
        public static void RobotSeasonCommerce()
        {
            var e=New();var s=e.State;s.gold=16000;s.parts=160;s.gems=1200;
            Check(!e.UnlockSeeker().Success&&s.gold==16000,"locked blueprint does not spend");
            s.clearedStages.Add(4);Check(e.UnlockSeeker().Success&&s.gold==10000&&s.parts==100,"exact permanent blueprint debit");
            Check(!e.UnlockSeeker().Success&&s.gold==10000,"blueprint duplicate blocked");
            Check(!e.CraftRescueStyle(0).Success&&!e.CraftRescueStyle(9).Success,"mastery and frame guards");
            s.robotRecords.Add(new RobotRecord{design=10,kills=200,builds=2,rank=1});
            Check(e.CraftRescueStyle(0).Success&&s.gold==0&&s.parts==0&&RobotStyles.Active(s,0)==3,"earned style exact debit and auto equip");
            Check(!e.CraftRescueStyle(0).Success,"duplicate free style blocked");
            int attack=Catalog.Attack(s),hp=Catalog.Health(s);s.gemDebt=1;
            Check(!e.BuyStormStyle().Success&&s.gems==1200,"debt blocks optional purchase");s.gemDebt=0;
            Check(e.BuyStormStyle().Success&&s.gems==600&&s.gold==0&&s.parts==0,"pack never grants growth");
            for(int i=0;i<3;i++)Check(RobotStyles.Active(s,i)==4,"pack applies all chassis");
            Check(!e.BuyStormStyle().Success&&s.gems==600&&Catalog.Attack(s)==attack&&Catalog.Health(s)==hp,"duplicates and cosmetic stat parity");
            Check(RobotStyles.Equip(s,0,3).Success&&!RobotStyles.Equip(s,1,3).Success,"per frame free ownership retained");
            s.robotSeasonWeeks=null;s.robotSeasonBest=null;RobotSeason.Normalize(s);RobotSeason.Normalize(s);
            Check(s.robotSeasonBest.Count==3&&s.robotSeasonWeeks.Count==0&&RobotSeason.HasBlueprint(s),"idempotent old-save migration");
        }
        public static Combat PlayRobotSeason(GameState s,string mode,int seed,int design,out ReplayRecorder recorder,bool avoid=true,string runId=null)
        {
            var c=new Combat(s,4,runId??"season-"+mode,seed,false,mode);recorder=new ReplayRecorder();
            for(int i=0;i<2800&&!c.Ended;i++)
            {
                if(c.Choosing){int pick=c.Choices[0];c.Choose(pick);recorder.Choose(pick);}
                if(c.CanPlanRobot(design,true)){c.PlanRobot(design,true);recorder.RobotPlan(design,true);}
                if(c.CanActivateMachine){c.ActivateMachine();recorder.Machine();}
                if(c.CanUltimate){c.UseUltimate();recorder.Ultimate();if(c.Ended)break;}
                V2 away=new V2();if(avoid)foreach(var enemy in c.Enemies){float d=V2.Distance(enemy.position,c.Position);if(d<120)away+=(c.Position-enemy.position).Normal*(120-d)/60;}
                if(avoid)foreach(var hazard in c.Hazards){float d=V2.Distance(hazard.position,c.Position);if(d<120)away+=(c.Position-hazard.position).Normal*2;}
                var target=new V2(270+(float)Math.Cos(c.Elapsed*.25)*180,500+(float)Math.Sin(c.Elapsed*.25)*230);
                c.Tick(ReplayRecorder.Step,recorder.Move(((target-c.Position).Normal+away).Normal));
            }
            if(!c.Ended)c.Abandon();return c;
        }
        public static void RobotSeasonReplayAndRewards()
        {
            var locked=GameState.New();Check(!new Combat(locked,4,"locked").RobotDesignUnlocked(10),"new design locked in campaign");
            var oldTrial=new Combat(locked,4,"skill",17,false,SkillTrial.Mode);Check(!oldTrial.RobotEnabled,"existing skill trial unchanged");
            var e=New();e.State.clearedStages.Add(4);string week=Endgame.Week(Now);int featured=RobotSeason.Featured(week);
            foreach(var mode in new[]{RobotSeason.Modes[0],RobotSeason.Modes[1],RobotSeason.Modes[2],RobotSeason.Preview})
            {
                string id="season-"+mode;Check(e.BeginChallenge(id,mode).Success,"season ticket");
                int gold=e.State.gold,parts=e.State.parts,gems=e.State.gems,records=e.State.robotRecords.Count;
                var c=PlayRobotSeason(e.State,mode,Endgame.Seed(week),10,out var input);
                var replay=ReplayRecorder.Verify(e.State,4,id,Endgame.Seed(week),input.Steps,100,mode);
                Check(c.Hp==replay.Hp&&c.Kills==replay.Kills&&c.MachineDamage==replay.MachineDamage,"seeker/role replay exact "+mode);
                Check(c.PeakEnemyWarnings<=3,"bounded theme warning load");
                Check(e.FinishChallenge(replay).Success&&!e.FinishChallenge(replay).Success,"settlement exactly once");
                Check(e.State.gold==gold&&e.State.parts==parts&&e.State.gems==gems&&e.State.weekly.Count==0&&e.State.skillWeekly.Count==0,"no campaign or ranking rewards leak");
                if(mode==RobotSeason.Preview)Check(e.State.robotRecords.Count==records&&e.State.receipts[e.State.receipts.Count-1].shards==0,"preview never grants mastery or currency");
            }
            var weak=new Combat(GameState.New(),4,"same",17,false,RobotSeason.Modes[0]);
            var rich=GameState.New();rich.heroes[0].level=30;foreach(var item in rich.items){item.level=20;item.rarity=2;}
            var strong=new Combat(rich,4,"same",17,false,RobotSeason.Modes[0]);
            Check(weak.Damage==strong.Damage&&weak.MaxHp==strong.MaxHp&&weak.SeekerUnlocked&&strong.SeekerUnlocked,"loan combat ignores paid growth and ownership");
        }
        public static void RobotSeasonRewardsOnce()
        {
            var day=Now;var e=new Economy(GameState.New(),()=>day);e.State.clearedStages.Add(4);
            for(int attempt=0;attempt<3;attempt++)
            {
                if(attempt==2)day=day.AddDays(7);
                string week=Endgame.Week(day),mode=RobotSeason.Modes[RobotSeason.Featured(week)],id="reward-"+attempt;
                Check(e.BeginChallenge(id,mode).Success,"reward ticket");
                var c=PlayRobotSeason(e.State,mode,Endgame.Seed(week),1,out var input,true,id);
                var replay=ReplayRecorder.Verify(e.State,4,id,Endgame.Seed(week),input.Steps,100,mode);
                int before=e.State.coreShards;bool previously=(e.State.robotSeasonClears&(1<<RobotSeason.Contract(mode)))!=0;
                Check(c.Won&&c.RobotReport().Exists(r=>r.kills>=15),"real completed contract fixture");
                Check(e.FinishChallenge(replay).Success,"reward settles");
                int expected=attempt==1?0:(previously?0:2)+3;
                Check(e.State.coreShards-before==expected,"first clear and weekly bonus cannot repeat");
            }
            Check(e.State.robotSeasonWeeks.Count==2,"weekly rotation preserves archive progress");
        }
        public static void SeekerTradeoff()
        {
            float[] damage=new float[2];int[] hits=new int[2];
            for(int n=0;n<2;n++)
            {
                var c=new Combat(GameState.New(),4,"salvo",17,false,RobotSeason.Preview);
                c.Enemies.Clear();for(int i=0;i<5;i++)c.Enemies.Add(new Enemy{id=100+i,hp=100000,maxHp=100000,position=c.Position+new V2(90+i*5,0),role=EnemyRole.Pursuer});
                int design=n==0?1:10;Check(c.PlanRobot(design,true)&&c.ActivateMachine(),"salvo fixture deploy");
                c.Tick(ReplayRecorder.Step,new V2());damage[n]=c.MachineDamage;
                foreach(var enemy in c.Enemies)if(enemy.killedByMachine)hits[n]++;
            }
            Check(hits[0]==5&&hits[1]==1&&damage[0]>damage[1],"seeker is concentrated fire, not a straight area upgrade");
        }
    }
}
