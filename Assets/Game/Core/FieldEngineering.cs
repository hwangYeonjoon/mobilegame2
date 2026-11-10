using System;

namespace ScrapGuard.Core
{
    public static class FieldEngineering
    {
        public static readonly string[] Names={"미장착","현장 포탑","자력 절단기","응급 수리기","질주 관통기"};
        public static string Name(int module,int hero)
        { return module==1 && hero==0?"루크 · 쌍둥이 포탑":module==2 && hero==1?"레이 · 질주 레일건":module==3 && hero==2?"볼트 · 방호 충격기":module==1 && hero==3?"스파크 · 축전 포탑":module==2 && hero==4?"아이리스 · 서리 자력장":Names[module]; }
        public static string Detail(int module,int hero)
        {
            if(module==4)return "이동 거리 150 충전 후 관통탄 발사 · 최대 7회 적중\n적을 일렬로 유도하세요";
            if(module==3 && hero==2)return "6초마다 보호막 12% 보충 · 근처 적 밀어내기\n기본 수리 효과도 유지";
            if(module==1 && hero==3)return "포탑 사격 + 2초간 멈추면 연쇄 전격\n다시 발사하려면 이동 후 재충전";
            if(module==2 && hero==4)return "5초마다 주변 일반 적 1초 동결 · 보스 둔화\n톱날 Lv.1과 주변 적 끌어당기기";
            if(module==1)return hero==0?"거점 포탑 2기 · 원 안에서 사격 강화\n150 이상 떨어져 1초 멈추면 거점 이동":"거점 포탑 · 원 안에서 사격 강화\n150 이상 떨어져 1초 멈추면 거점 이동";
            if(module==2)return hero==1?"이동 거리 180마다 가까운 적에게 관통탄 발사\n톱날 Lv.1과 주변 적 끌어당기기":"톱날 Lv.1과 주변 적 끌어당기기";
            return "10초마다 최대 체력 3% 수리 · 보호막 8% 보충";
        }
        public static string TrialTitle(string mode)
        { return mode==SkillTrial.Mode?"동일 조건 실력전":mode==Endgame.PracticeMode?"성장 도전 연습":"주간 로봇 도전"; }
        public static string TrialObjective(string mode)
        { return mode==SkillTrial.Mode?"개인 최고 기록에 도전 · 재화 보상 없음":mode==Endgame.PracticeMode?"연습 중 · 기록과 보상은 저장되지 않아요":"최고 단계 목표 · 코어 피해 30,000점"; }
    }
    public sealed partial class Combat
    {
        public readonly int FieldVariant;
        public readonly string GuildAtStart;
        public int FieldModule { get; private set; }
        public int RailShots { get; private set; }
        float railDistance,engineeringTimer,stationaryCharge;
        bool stationaryDischarged;
        public int EngineeringPulses { get; private set; }
        public bool CanAssemble { get { return TaskCompleted && FieldModule==0 && !Ended && !Choosing && !Challenge; } }
        public bool Assemble(int module)
        {
            if(!CanAssemble || module<1 || module>4)return false;
            FieldModule=module;InitializeBuild();
            if(module==2)Skills[1]=Math.Max(1,Skills[1]);
            Effects.Add(new BattleEffect{position=Position,kind=10,radius=85,text=FieldEngineering.Name(module,Hero)+" 장착"});
            return true;
        }
        int FieldMods { get { return FieldModule==1?1<<2:FieldModule==2?1:FieldModule==3?1<<4:0; } }
        void UpdateEngineering(float dt)
        {
            if(FieldModule==0)return;
            engineeringTimer-=dt;
            if(Hero==2 && FieldModule==3 && engineeringTimer<=0)
            {
                engineeringTimer=6;EngineeringPulses++;Shield=Math.Max(Shield,MaxHp*.12f);
                foreach(var e in Enemies)if(e.hp>0 && !e.core && !e.finalBoss && V2.Distance(e.position,Position)<140)
                    e.position=Slide(e.position,(e.position-Position).Normal*65,Math.Min(e.Radius,20));
                Effects.Add(new BattleEffect{position=Position,kind=6,radius=140,text="방호 충격 · 보호막"});
            }
            if(Hero==3 && FieldModule==1)
            {
                if(LastMovement.Length>.1f){stationaryCharge=0;stationaryDischarged=false;}
                else if(!stationaryDischarged)
                {
                    stationaryCharge+=dt;
                    if(stationaryCharge>=2 && Nearest(Position)!=null){ChainLightning();EngineeringPulses++;stationaryDischarged=true;Effects.Add(new BattleEffect{position=Position,kind=7,text="축전 방출 · 이동 후 재충전"});}
                }
            }
            if(Hero==4 && FieldModule==2 && engineeringTimer<=0)
            {
                engineeringTimer=5;EngineeringPulses++;
                foreach(var e in Enemies)if(e.hp>0 && V2.Distance(e.position,Position)<150)
                {if(!e.core && !e.finalBoss)e.freezeTime=Math.Max(e.freezeTime,1);e.slowTime=Math.Max(e.slowTime,2);}
                Effects.Add(new BattleEffect{position=Position,kind=6,radius=150,text="서리 자력장"});
            }
            if(FieldModule!=2 || Hero!=1)return;
            if(DistanceMoved-railDistance<180)return;
            var target=Nearest(Position);if(target==null)return;
            railDistance=DistanceMoved;RailShots++;
            Bullets.Add(new Bullet{id=nextId++,position=Position,velocity=(target.position-Position).Normal*560,damage=Damage*2,life=1.3f,pierce=4,mod=true});
            Effects.Add(new BattleEffect{position=Position,end=target.position,kind=8});
        }
    }
    public static class GuildRestoration
    {
        public const int DailyLimit=3;
        public static readonly int[] Goals={0,6,15,30};
        public static readonly string[] Names={"복구 대기","전력 복구","작업대 복구","수호대 전시관 완성"};
        public static readonly int[] HonorGoals={30,60,120};
        public static readonly string[] HonorNames={"복구 기념장","협력 기념장","개척 기념장"};
        public static int Honor(int points){return points>=120?3:points>=60?2:points>=30?1:0;}
        public static int NextGoal(int points){return points<30?30:points<60?60:points<120?120:0;}
        public static int Level(int points){int level=0;for(int i=1;i<Goals.Length;i++)if(points>=Goals[i])level=i;return level;}
    }
}
