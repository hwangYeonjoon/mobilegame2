using System;
namespace ScrapGuard.Core
{
    public static class RobotWeek
    {
        public static int Frame(string week){return Frame(Endgame.Seed(week));}
        public static int Frame(int seed){return (int)(((long)seed-40000)/7%3+3)%3;}
        public static string Brief(string week){return RobotAssembly.Frames[Frame(week)]+" 특화형으로 탑승하고 60초 생존";}
        public const int Gold=3000, Parts=30, Shards=5;
    }
    public static class SurvivalTraining
    {
        public const string Mode="survival_practice";
        public const string PressureMode="survival_pressure";
        public static bool IsMode(string mode){return mode==Mode||mode==PressureMode;}
        public static bool Available(GameState s,int stage){return stage>=0&&stage<Campaign.Count&&stage<=s.unlockedStage&&Campaign.Get(stage).Mission==Mission.Survive;}
        public static Receipt Failure(GameState s,int stage){return s.receipts.FindLast(r=>r.kind=="battle"&&!r.won&&r.seconds>=30&&r.report!=null&&r.report.operation==stage+1&&r.report.cause!=(int)DamageCause.Abandoned);}
        public static bool PressureAvailable(GameState s,int stage){return Available(s,stage)&&Failure(s,stage)!=null;}
        public static float Offset(GameState s,int stage,bool pressure)
        {
            if(!pressure)return Campaign.Get(stage).Duration*.4f;
            var failure=Failure(s,stage);if(failure==null)throw new ArgumentException("Verified failure required for pressure practice");
            return Math.Max(0,Math.Min(Campaign.Get(stage).Duration-60,failure.seconds-30));
        }
    }
    public static class CombatTraining
    {
        public static string RetryMode(GameState s,int stage){return SurvivalTraining.PressureAvailable(s,stage)?SurvivalTraining.PressureMode:ModeFor(stage);}
        public static string ModeFor(int stage){return Campaign.Get(stage).Mission==Mission.Survive?SurvivalTraining.Mode:BossTraining.Mode;}
        public static bool Available(GameState s,int stage){return BossTraining.Available(s,stage)||SurvivalTraining.Available(s,stage);}
        public static string SourceName(int source){return source==(int)DamageSource.Boss?"보스":source==(int)DamageSource.EnemyAttack?"일반 적 공격":source==(int)DamageSource.Terrain?"지형":source==(int)DamageSource.Contact?"적 접촉":source==(int)DamageSource.Condition?"지역 조건":"기타";}
    }
    public static class BossTraining
    {
        public const string Mode="boss_practice";
        public static bool Available(GameState s,int stage){return stage>=0&&stage<Campaign.Count&&stage<=s.unlockedStage&&Campaign.Get(stage).Mission==Mission.Eliminate;}
        public static string Advice(BattleReport r)
        {
            if(r==null)return "예고가 생기면 공격보다 탈출을 먼저 선택하세요.";
            if(r.lastSource==(int)DamageSource.Boss&&r.lastCause==(int)DamageCause.Contact)return "보스와 맞닿았어요. 보스 몸체에서 거리를 벌리세요.";
            if(r.lastSource==(int)DamageSource.Boss&&r.lastCause==(int)DamageCause.Charge)return "보스 돌진에 맞았어요. 진행 방향 옆으로 피하세요.";
            if(r.lastSource==(int)DamageSource.Boss)return "보스 예고 공격에 맞았어요. 원 밖이나 십자의 빈 모서리로 이동하세요.";
            if(r.lastSource==(int)DamageSource.EnemyAttack)
            {
                if(r.lastCause==(int)DamageCause.Poison||r.lastCause==(int)DamageCause.Explosion||r.lastCause==(int)DamageCause.Heat)return "폭발을 준비하는 적에게 맞았어요. 적 주변 예고 원에서 먼저 벗어나세요.";
                if(r.lastCause==(int)DamageCause.Electricity)return "적 포대의 방전에 맞았어요. 길게 표시된 예고 띠 옆으로 피하세요.";
                if(r.lastCause==(int)DamageCause.Laser)return "절단 공격에 맞았어요. 멈춰 선 적 주변의 예고를 벗어나세요.";
                return "일반 적 포격에 맞았어요. 예고 원 밖으로 이동하며 포대를 먼저 정리해 보세요.";
            }
            return Workshop.Advice[Workshop.MainCause(r)];
        }
    }
    public sealed partial class Economy
    {
        public CommandResult BeginBossPractice(string id,int stage)
        {
            if(!BossTraining.Available(State,stage))return CommandResult.Fail("열린 보스 작전만 연습할 수 있어요.");
            var result=BeginRun(id,stage);if(result.Success)State.activeMode=BossTraining.Mode;return result;
        }
        public CommandResult FinishBossPractice(Combat c)
        {
            if(c==null||!c.BossPractice)return CommandResult.Fail("유효하지 않은 보스 연습이에요.");
            return FinishPractice(c);
        }
        public CommandResult BeginSurvivalPractice(string id,int stage,bool pressure=false)
        {
            if(!SurvivalTraining.Available(State,stage)||pressure&&!SurvivalTraining.PressureAvailable(State,stage))return CommandResult.Fail("열린 생존 작전과 해당 작전의 실패 기록이 필요해요.");
            var result=BeginRun(id,stage);if(result.Success)State.activeMode=pressure?SurvivalTraining.PressureMode:SurvivalTraining.Mode;return result;
        }
        public CommandResult FinishPractice(Combat c)
        {
            if(c==null||!c.Ended||!c.Practice||State.activeMode!=c.Mode||State.activeRun!=c.RunId||State.activeStage!=c.Stage||!CombatTraining.Available(State,c.Stage)||State.receipts.Exists(r=>r.id==c.RunId))return CommandResult.Fail("유효하지 않은 연습 기록이에요.");
            State.receipts.Add(new Receipt{id=c.RunId,kind=c.Mode,seconds=c.Elapsed,kills=c.Kills,won=c.Won,report=c.Report()});
            State.activeRun=State.activeMode=State.activeWeek="";
            return CommandResult.Ok("연습 완료 · 재화·작전 진행·일일 임무에 반영되지 않아요.");
        }
        void CreditFieldSupply(float seconds)
        {if(seconds>=60&&!float.IsNaN(seconds)&&!float.IsInfinity(seconds))State.dailySupplySeconds=Math.Min(300,State.dailySupplySeconds+(int)Math.Min(seconds,300));}
        public CommandResult ClaimSalvage()
        {
            RefreshDay();if(State.salvageClaimed||State.dailySupplySeconds<300)return CommandResult.Fail("작전 또는 주간 도전의 전투 시간 5분을 채우세요. 60초 이상 전투만 합산해요.");
            State.salvageClaimed=true;State.gold+=Growth.KitGold;State.parts+=Growth.KitParts;
            return CommandResult.Ok("현장 보급 수령 · 골드 4,500 · 부품 40");
        }
        public CommandResult ClaimRobotWeek(string week)
        {
            var p=Endgame.Find(State,week);
            if(p==null||!p.robotContract||(p.claimed&16)!=0)return CommandResult.Fail("주간 로봇 과제 달성과 수령 내역을 확인하세요.");
            p.claimed|=16;State.gold+=RobotWeek.Gold;State.parts+=RobotWeek.Parts;State.coreShards+=RobotWeek.Shards;
            return CommandResult.Ok("로봇 과제 보상 · 골드 3,000 · 부품 30 · 코어 파편 5");
        }
    }
    public sealed partial class Combat
    {
        public bool BossPractice {get{return Mode==BossTraining.Mode;}}
        public bool SurvivalPractice {get{return SurvivalTraining.IsMode(Mode);}}
        public bool PressurePractice {get{return Mode==SurvivalTraining.PressureMode;}}
        public readonly float PracticeOffset;
        public bool Practice {get{return BossPractice||SurvivalPractice;}}
        public bool RobotEnabled {get{return Mode!=SkillTrial.Mode&&!Practice;}}
        public readonly int WeeklyFrame;
        int lastCause,lastSource;float lastHitTime,lastHitDamage;
        readonly System.Collections.Generic.List<DamageHit> recentHits=new System.Collections.Generic.List<DamageHit>();
        void RecordRecentHit(float damage,DamageCause cause,DamageSource source)
        {
            recentHits.RemoveAll(h=>h.seconds<Elapsed-8);
            if(recentHits.Count>=32)recentHits.RemoveAt(0);
            recentHits.Add(new DamageHit{seconds=Elapsed,damage=damage,cause=(int)cause,source=(int)source});
        }
        void InitializeSurvivalPractice(GameState state)
        {
            if(Definition.Mission!=Mission.Survive)throw new ArgumentException("Survival practice requires a survival operation");
            Skills[0]=3;Skills[1]=2;Skills[4]=1;Skills[7]=1;Level=8;reviveGuard=3;eliteSpawned=true;
            var failure=PressurePractice?SurvivalTraining.Failure(state,Stage):null;
            if(failure!=null&&failure.report.skills!=null)for(int i=0;i<Math.Min(Skills.Length,failure.report.skills.Length);i++)Skills[i]=Math.Max(0,Math.Min(5,failure.report.skills[i]));
            Skills[0]=Math.Max(1,Skills[0]);
            int count=failure==null?Definition.Enemies.Length:Math.Min(EnemyLimit,Math.Max(12,failure.report.enemies));
            for(int i=0;i<count;i++){Spawn(failure==null?1:random.NextDouble()<.18+Definition.Region*.035?1:0,false);Enemies[Enemies.Count-1].role=Definition.Enemies[i%Definition.Enemies.Length];}
        }
        void InitializeBossPractice()
        {
            // Preset build for pattern practice, not a campaign win-rate simulation.
            Skills[0]=5;Skills[4]=3;Skills[5]=3;Skills[7]=3;Level=12;
            BossArenaCenter=Position;BossArenaActive=true;bossSpawned=eliteSpawned=true;
            bossRoleLessonsRemaining=Definition.Region>0?2:0;bossRoleTurn=bossRoleLessonsRemaining>0;
            bossTimer=2;reviveGuard=2;Spawn(3,true);
            var boss=Enemies.Find(e=>e.finalBoss);boss.hp=boss.maxHp=BossBaseHealth*(Definition.Difficulty+(Definition.Duration-30)/Definition.Duration*.55f);
        }
    }
}
