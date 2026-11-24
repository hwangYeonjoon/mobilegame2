using System;
using System.Collections.Generic;
namespace ScrapGuard.Core
{
    // Stable archive IDs: a featured week never removes a purchased item or a free contract.
    public static class RobotSeason
    {
        public const string Preview="storm_preview", Blueprint="robot_seeker_blueprint", Pack="robot_style_storm";
        public const int Seeker=10, BlueprintGold=6000, BlueprintParts=60, PackPrice=600;
        public static readonly string[] Modes={"storm_carriers","storm_battery","storm_swarm"};
        public static readonly string[] Titles={"운반대 추적","분산된 방어선","밀집 돌파"};
        public static readonly string[] Briefs={"빠른 운반 적 추적 · 이동 경로를 읽고 회수", "멀리 떨어진 포대 · 유도 포신과 방어를 비교", "몰려오는 근접 무리 · 범위 공격과 돌파를 비교"};
        public static bool IsMode(string mode){return mode==Preview||Array.IndexOf(Modes,mode)>=0;}
        public static int Contract(string mode){return Array.IndexOf(Modes,mode);}
        public static int Featured(string week){return RobotWeek.Frame(week);}
        public static bool HasBlueprint(GameState s){return s.ownedCosmetics!=null&&s.ownedCosmetics.Contains(Blueprint);}
        public static void Normalize(GameState s)
        {
            if(s.robotSeasonWeeks==null)s.robotSeasonWeeks=new List<string>();
            if(s.robotSeasonBest==null)s.robotSeasonBest=new List<int>();
            while(s.robotSeasonBest.Count<3)s.robotSeasonBest.Add(0);
        }
        public static int Mastery(GameState s,int frame)
        {
            int kills=0;RobotAssembly.Normalize(s);
            foreach(var r in s.robotRecords)if(r!=null&&RobotAssembly.Frame(r.design)==frame)kills+=r.kills;
            return kills;
        }
        public static string Badge(int kills){return kills>=1000?"구조대 명장":kills>=200?"구조대 정비사":"견습 정비사";}
        public static GameState Loan()
        {
            var s=SkillTrial.Loadout();s.heroes[0].level=10;
            foreach(var item in s.items){item.level=8;item.rarity=1;}
            return s;
        }
    }
    public sealed partial class Economy
    {
        public CommandResult UnlockSeeker()
        {
            if(!Endgame.Unlocked(State)||RobotSeason.HasBlueprint(State))return CommandResult.Fail("작전 05 완료 후, 아직 없는 설계만 제작할 수 있어요.");
            if(State.gold<RobotSeason.BlueprintGold||State.parts<RobotSeason.BlueprintParts)return CommandResult.Fail("골드 6,000 · 부품 60이 필요해요. 테마 작전에서는 무료로 빌려 쓸 수 있어요.");
            State.gold-=RobotSeason.BlueprintGold;State.parts-=RobotSeason.BlueprintParts;
            if(State.ownedCosmetics==null)State.ownedCosmetics=new List<string>();
            State.ownedCosmetics.Add(RobotSeason.Blueprint);
            return CommandResult.Ok("유도 포신 영구 해금 · 일반 전투에서는 회수 부품으로 조립하세요.");
        }
        public CommandResult CraftRescueStyle(int frame)
        {
            if(frame<0||frame>2||RobotStyles.Owned(State,frame,3)||RobotSeason.Mastery(State,frame)<200)return CommandResult.Fail("해당 기체로 누적 200처치 후 제작할 수 있어요.");
            if(State.gold<10000||State.parts<100)return CommandResult.Fail("골드 10,000 · 부품 100이 필요해요.");
            State.gold-=10000;State.parts-=100;
            if(State.ownedCosmetics==null)State.ownedCosmetics=new List<string>();
            State.ownedCosmetics.Add("robot_style_rescue_"+frame);
            return RobotStyles.Equip(State,frame,3);
        }
        public CommandResult BuyStormStyle()
        {
            if(RobotStyles.Owned(State,0,4))return CommandResult.Fail("이미 폭풍 구조대 팩을 보유하고 있어요.");
            if(!PremiumWallet.Spend(State,RobotSeason.PackPrice))return CommandResult.Fail("보석 600개와 환불 조정 내역을 확인하세요.");
            if(State.ownedCosmetics==null)State.ownedCosmetics=new List<string>();
            State.ownedCosmetics.Add(RobotSeason.Pack);RobotStyles.Normalize(State);
            for(int i=0;i<3;i++)State.robotStyles[i]=4;
            return CommandResult.Ok("폭풍 구조대 3기체 적용 · 영구 외형 · 능력치와 재료 변화 없음");
        }
        CommandResult FinishRobotSeason(Combat c)
        {
            RobotSeason.Normalize(State);int contract=RobotSeason.Contract(c.Mode),kills=0;
            foreach(var r in c.RobotReport())if(r.rank>0)kills+=r.kills;
            int shards=0;bool cleared=contract>=0&&c.Won&&kills>=15;
            if(contract>=0&&c.Elapsed>=30)
            {
                State.robotSeasonBest[contract]=Math.Max(State.robotSeasonBest[contract],kills);
                RobotAssembly.Record(State,c.RobotReport(),-1);
                CreditFieldSupply(c.Elapsed);
            }
            if(cleared)
            {
                int bit=1<<contract;
                if((State.robotSeasonClears&bit)==0){State.robotSeasonClears|=bit;shards+=2;}
                if(contract==RobotSeason.Featured(State.activeWeek)&&!State.robotSeasonWeeks.Contains(State.activeWeek))
                {State.robotSeasonWeeks.Add(State.activeWeek);shards+=3;}
                State.coreShards+=shards;
            }
            State.receipts.Add(new Receipt{id=c.RunId,kind=c.Mode,week=State.activeWeek,seconds=c.Elapsed,kills=c.Kills,won=c.Won,shards=shards,score=kills,report=c.Report()});
            State.activeRun=State.activeMode=State.activeWeek="";State.lastResultGold=0;State.lastResultBonus=true;
            return CommandResult.Ok(c.Mode==RobotSeason.Preview?"외형 체험 완료 · 보상과 기록은 남지 않아요.":cleared?"구조 임무 달성 · 코어 파편 +"+shards:"90초 생존 + 특화 로봇 15처치에 도전하세요.");
        }
    }
    public sealed partial class Combat
    {
        public bool RobotSeasonBattle {get{return RobotSeason.IsMode(Mode);}}
        public readonly bool SeekerUnlocked;
        public bool RobotDesignUnlocked(int id){return RobotAssembly.Valid(id)&&(id!=RobotSeason.Seeker||SeekerUnlocked);}
        void InitializeRobotSeason()
        {
            Skills[0]=3;Skills[4]=2;Level=7;
            for(int i=0;i<3;i++)robotParts[i]=12;
            // Preview really uses the normal fixed-loadout battle; only presentation is overridden.
        }
        Enemy SeekerTarget()
        {
            Enemy result=null;float best=850*850;
            foreach(var e in Enemies)if(e.hp>0&&!e.core)
            {float dx=e.position.x-Position.x,dy=e.position.y-Position.y,d=dx*dx+dy*dy;if(d<best){best=d;result=e;}}
            return result??Nearest(Position);
        }
        void FireSeeker(float quality)
        {
            var target=SeekerTarget();if(target==null)return;
            // Concentrated tracking salvo: no splash, no three-way piercing support.
            Hit(target,Damage*10*quality,true);
            if(FieldModule==1||FieldModule==4)Bullets.Add(new Bullet{id=nextId++,position=Position,velocity=(target.position-Position).Normal*600,damage=Damage*2*quality,life=1.2f,pierce=8,mod=true,machine=true,robotDesign=MachineDesign});
            Effects.Add(new BattleEffect{position=Position,end=target.position,kind=8,robot=true});
            Effects.Add(new BattleEffect{position=target.position,kind=11,radius=28,robot=true});
        }
    }
}
