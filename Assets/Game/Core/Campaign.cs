using System;
using System.Collections.Generic;

namespace ScrapGuard.Core
{
    public enum EnemyRole { Pursuer, Charger, Artillery, Bomber, Shield, Summoner }
    public enum Mission { Eliminate, Survive }
    public sealed class StageDefinition
    {
        public int Id, Region, Layout, Gold, Parts, FirstItem, FirstRarity;
        public string Name, Description, Boss;
        public float Duration, Difficulty, SpawnInterval;
        public Mission Mission;
        public EnemyRole[] Enemies;
        public string Objective { get { return Mission == Mission.Survive ? "제한 시간 생존" : "최종 보스 처치"; } }
    }
    public struct MapRect
    {
        public float X, Y, W, H;
        public MapRect(float x, float y, float w, float h) { X=x;Y=y;W=w;H=h; }
        public bool Contains(V2 p, float padding=0) { return p.x>X-padding && p.x<X+W+padding && p.y>Y-padding && p.y<Y+H+padding; }
        public V2 Center { get { return new V2(X+W/2,Y+H/2); } }
    }
    // Shared packaged content: add a region/operation here, then ship the same rules to app and server.
    public static class Campaign
    {
        public static readonly string[] Regions = { "고철 정비소", "폐철도", "오염 처리장", "냉각 시설", "중앙 공장", "고압 발전소", "심층 용광로" };
        public static readonly string[] Mechanics = { "작업대를 돌아 이동", "철로 포격 예고를 피하세요", "오염 웅덩이를 피하세요", "냉각 바닥에서는 관성 이동", "컨베이어 이동 · 레이저 예고", "번갈아 켜지는 전기 격자를 피하세요", "분출 예고 후 용광로 통로를 벗어나세요" };
        public static readonly StageDefinition[] Stages = Build();
        public static int Count { get { return Stages.Length; } }
        public static StageDefinition Get(int id) { if(id<0 || id>=Count)throw new ArgumentOutOfRangeException("id");return Stages[id]; }
        static StageDefinition[] Build()
        {
            string[][] names = {
                new[]{"버려진 정비소","부품 회수로","거대 로봇 해체","용접 작업장","정비소 전력 복구","정비소 탈환"},
                new[]{"폐역 진입","철길의 불청객","수송체 호송","포격 승강장","끊어진 철교","철도 파괴자"},
                new[]{"폐기물 처리장","독성 배수로","오염 봉쇄선","분해 작업장","폐수 저장고","오염의 심장"},
                new[]{"냉각실 입구","얼어붙은 배관","냉기 경보","빙결 저장소","차가운 추격","극저온 수호기"},
                new[]{"멈춰버린 공장","조립 컨베이어","생산 라인 봉쇄","레이저 격자","코어 접근로","중앙 제어 코어"},
                new[]{"변전소 진입","축전기 통로","전류 봉쇄선","송전 격자","과부하 경보","뇌전 제어기"},
                new[]{"제련로 입구","열교환 통로","화염 봉쇄선","용융 운반로","압력 폭주","심층 주조왕"}
            };
            string[] bosses={"정비소 분쇄기","철도 파괴자","폐수 폭파기","빙결 방패기","중앙 제어 코어","뇌전 제어기","심층 주조왕"};
            EnemyRole[][] pools={
                new[]{EnemyRole.Pursuer,EnemyRole.Charger},
                new[]{EnemyRole.Charger,EnemyRole.Artillery},
                new[]{EnemyRole.Pursuer,EnemyRole.Bomber,EnemyRole.Artillery},
                new[]{EnemyRole.Shield,EnemyRole.Charger,EnemyRole.Artillery},
                new[]{EnemyRole.Shield,EnemyRole.Summoner,EnemyRole.Artillery,EnemyRole.Bomber},
                new[]{EnemyRole.Charger,EnemyRole.Artillery,EnemyRole.Summoner},
                new[]{EnemyRole.Bomber,EnemyRole.Shield,EnemyRole.Charger,EnemyRole.Summoner}
            };
            var stages=new List<StageDefinition>();
            for(int region=0;region<names.Length;region++)for(int local=0;local<names[region].Length;local++)
            {
                int id=stages.Count;var mission=local==2 || local==4 ? Mission.Survive : Mission.Eliminate;
                stages.Add(new StageDefinition {Id=id,Region=region,Layout=local,Name=names[region][local],Boss=bosses[region],Mission=mission,
                    Description=(id==2?"부품 3곳 회수 · ":id==4?"전원 3곳 복구 · ":id==8?"수송체 호송 · ":"")+(mission==Mission.Survive?"생존":"보스 토벌")+" · "+Mechanics[region],
                    Duration=210+local*12, Difficulty=1+id*.075f, SpawnInterval=Math.Max(.48f,.9f-region*.055f),
                    Gold=1500+id*90,Parts=10+id/2,FirstItem=id<5?new[]{1,7,10,2,5}[id]:new[]{3,8,11,4,2,5}[local],
                    FirstRarity=id<2?1:2,Enemies=pools[region]});
            }
            return stages.ToArray();
        }
        public static StageDefinition FinalStage(int region) { return Array.FindLast(Stages,s=>s.Region==region); }
        public static bool IsRegionFinal(StageDefinition stage) { return FinalStage(stage.Region).Id==stage.Id; }
        public static EnemyRole BossRole(int region) { return new[]{EnemyRole.Charger,EnemyRole.Artillery,EnemyRole.Bomber,EnemyRole.Shield,EnemyRole.Summoner,EnemyRole.Artillery,EnemyRole.Charger}[region]; }
        public static void Normalize(GameState state)
        {
            if(state.clearedStages==null)state.clearedStages=new List<int>();
            // Keep existing IDs/rewards. A completed old final stage now unlocks operation 6.
            int next=0;while(next<Count-1 && state.clearedStages.Contains(next))next++;
            state.unlockedStage=Math.Max(next,Math.Max(0,Math.Min(Count-1,state.unlockedStage)));
        }
        public static MapRect[] Blocks(StageDefinition stage)
        {
            var blocks=new System.Collections.Generic.List<MapRect>(SmallBlocks(stage));
            blocks.Add(new MapRect(208,710,124,68));return blocks.ToArray();
        }
        static MapRect[] SmallBlocks(StageDefinition stage)
        {
            float offset=(stage.Layout%3)*25;
            switch(stage.Region)
            {
                case 0:return new[]{new MapRect(82,320+offset,100,44),new MapRect(356,612-offset,100,44)};
                case 1:return new[]{new MapRect(28,352+offset,82,95),new MapRect(430,560-offset,82,95)};
                case 2:return new[]{new MapRect(63,290,65,65),new MapRect(410,675,65,65)};
                case 3:return new[]{new MapRect(74,380+offset,88,38),new MapRect(378,590-offset,88,38)};
                case 5:return new[]{new MapRect(84,355,68,68),new MapRect(388,585,68,68)};
                case 6:return new[]{new MapRect(65,302,94,42),new MapRect(381,674,94,42)};
                default:return new[]{new MapRect(35,330+offset,100,50),new MapRect(405,620-offset,100,50)};
            }
        }
        public static MapRect[] Zones(StageDefinition stage)
        {
            float offset=stage.Layout%3*22;
            switch(stage.Region)
            {
                case 1:return new[]{new MapRect(155,190,66,600),new MapRect(319,190,66,600)};
                case 2:return new[]{new MapRect(72,430+offset,128,100),new MapRect(340,560-offset,128,100)};
                case 3:return new[]{new MapRect(174,286,192,410)};
                case 4:return new[]{new MapRect(153,220,76,550),new MapRect(311,220,76,550)};
                case 5:return new[]{new MapRect(190,195,46,610),new MapRect(304,195,46,610)};
                case 6:return new[]{new MapRect(28,380+offset,484,52),new MapRect(28,580-offset,484,52)};
                default:return new MapRect[0];
            }
        }
    }
}
