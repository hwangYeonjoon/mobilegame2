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
            if(c==null||!c.Ended||!c.BossPractice||State.activeMode!=BossTraining.Mode||State.activeRun!=c.RunId||State.activeStage!=c.Stage||State.receipts.Exists(r=>r.id==c.RunId))return CommandResult.Fail("유효하지 않은 연습 기록이에요.");
            State.receipts.Add(new Receipt{id=c.RunId,kind=BossTraining.Mode,seconds=c.Elapsed,kills=c.Kills,won=c.Won,report=c.Report()});
            State.activeRun=State.activeMode=State.activeWeek="";
            return CommandResult.Ok("보스 연습 완료 · 재화·작전 진행·일일 임무에 반영되지 않아요.");
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
        public bool RobotEnabled {get{return Mode!=SkillTrial.Mode&&!BossPractice;}}
        public readonly int WeeklyFrame;
        int lastCause,lastSource;float lastHitTime,lastHitDamage;
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
