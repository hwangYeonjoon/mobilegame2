using System;

namespace ScrapGuard.Core
{
    public static class FieldEngineering
    {
        public static readonly string[] Names={"미장착","현장 포탑","자력 절단기","응급 수리기"};
        public static string Name(int module,int hero)
        { return module==1 && hero==0?"루크 · 쌍둥이 포탑":module==2 && hero==1?"레이 · 질주 레일건":Names[module]; }
        public static string Detail(int module,int hero)
        {
            if(module==1)return hero==0?"서로 반대편의 포탑 2기가 자동 사격합니다.":"주위를 도는 포탑이 자동 사격합니다.";
            if(module==2)return hero==1?"이동 거리 180마다 가까운 적에게 관통탄 발사\n톱날 Lv.1과 주변 적 끌어당기기":"톱날 Lv.1과 주변 적 끌어당기기";
            return "10초마다 최대 체력 3% 수리 · 보호막 8% 보충";
        }
        public static string TrialTitle(string mode)
        { return mode==SkillTrial.Mode?"동일 조건 실력전":mode==Endgame.PracticeMode?"성장 도전 연습":"주간 성장 도전"; }
        public static string TrialObjective(string mode)
        { return mode==SkillTrial.Mode?"개인 최고 기록에 도전 · 재화 보상 없음":mode==Endgame.PracticeMode?"연습 중 · 기록과 보상은 저장되지 않아요":"최고 단계 목표 · 코어 피해 30,000점"; }
    }
    public sealed partial class Combat
    {
        public readonly int FieldVariant;
        public readonly string GuildAtStart;
        public int FieldModule { get; private set; }
        public int RailShots { get; private set; }
        float railDistance;
        public bool CanAssemble { get { return TaskCompleted && FieldModule==0 && !Ended && !Choosing && !Challenge; } }
        public bool Assemble(int module)
        {
            if(!CanAssemble || module<1 || module>3)return false;
            FieldModule=module;
            if(module==2)Skills[1]=Math.Max(1,Skills[1]);
            Effects.Add(new BattleEffect{position=Position,kind=10,radius=85,text=FieldEngineering.Name(module,Hero)+" 장착"});
            return true;
        }
        int FieldMods { get { return FieldModule==1?1<<2:FieldModule==2?1:FieldModule==3?1<<4:0; } }
        void UpdateEngineering(float dt)
        {
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
        public static int Level(int points){int level=0;for(int i=1;i<Goals.Length;i++)if(points>=Goals[i])level=i;return level;}
    }
}
