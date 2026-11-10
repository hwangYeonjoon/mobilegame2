using System;
namespace ScrapGuard.Core
{
    public sealed partial class Economy
    {
        public CommandResult BeginChallenge(string id,string mode)
        {
            RefreshDay();
            if(!Endgame.IsChallenge(mode))return CommandResult.Fail("없는 도전이에요.");
            if((mode==Endgame.WeeklyMode || mode==SkillTrial.Mode) && !Endgame.Unlocked(State))return CommandResult.Fail("작전 05를 완료하면 성장 도전과 실력전이 열려요.");
            if(string.IsNullOrEmpty(id) || !string.IsNullOrEmpty(State.activeRun) || State.receipts.Exists(r=>r.id==id))return CommandResult.Fail("진행 중이거나 정산된 전투예요.");
            State.activeRun=id;State.activeStage=4;State.activeMode=mode;State.activeWeek=Endgame.Week(clock());
            return CommandResult.Ok(mode==Endgame.PracticeMode ? "보상 없는 도전 연습을 시작해요." : "이번 주 코어에 도전하세요!");
        }
        // Called only with a locally simulated result or the server's verified replay.
        public CommandResult FinishChallenge(Combat combat)
        {
            RefreshDay();
            if(combat==null || !combat.Ended || !Endgame.IsChallenge(State.activeMode) || combat.Mode!=State.activeMode || combat.RunId!=State.activeRun || State.receipts.Exists(r=>r.id==combat.RunId))return CommandResult.Fail("유효하지 않은 도전 결과예요.");
            bool skill=State.activeMode==SkillTrial.Mode;
            bool eligible=(State.activeMode==Endgame.WeeklyMode || skill) && combat.Elapsed>=30;
            string week=State.activeWeek;
            if(eligible)
            {
                var history=skill?State.skillWeekly:State.weekly;
                var progress=history.Find(w=>w.week==week);
                if(progress==null) { progress=new WeeklyProgress {week=week};history.Add(progress); }
                if(!skill&&combat.Elapsed>=60){var built=combat.RobotReport().Find(r=>r.rank>0&&r.builds>0&&RobotAssembly.Frame(r.design)==RobotWeek.Frame(week));if(built!=null){progress.robotContract=true;progress.robotDesign=built.design;}}
                if(!skill)RobotAssembly.Record(State,combat.RobotReport(),-1);
                progress.attempts++;progress.bestScore=Math.Max(progress.bestScore,combat.CoreScore);
                if(!skill){State.totalRuns++;State.totalKills+=combat.Kills;State.dailyRuns++;State.dailyKills+=combat.Kills;}
            }
            State.receipts.Add(new Receipt {id=combat.RunId,kind=State.activeMode,score=combat.CoreScore,week=week,seconds=combat.Elapsed,kills=combat.Kills,won=combat.Won,report=combat.Report()});
            State.activeRun=State.activeMode=State.activeWeek="";
            State.lastResultGold=0;State.lastResultBonus=true;
            return CommandResult.Ok(eligible ? skill?"동일 조건 실력 기록 저장 · 재화 보상 없음":"주간 기록 저장 · 달성 보상을 확인하세요." : "연습 또는 30초 미만 기록은 보상·순위에 반영되지 않아요.");
        }
        public CommandResult ExchangeCore(string offer)
        {
            int price=offer=="title_rift"?12:offer=="frame_core"?20:-1;
            if(price<0)return CommandResult.Fail("등록되지 않은 균열 교환품이에요.");
            if(State.ownedCosmetics==null)State.ownedCosmetics=new System.Collections.Generic.List<string>();
            if(State.ownedCosmetics.Contains(offer))return CommandResult.Fail("이미 가지고 있는 교환품이에요.");
            if(State.coreShards<price)return CommandResult.Fail("코어 파편 "+price+"개가 필요해요.");
            State.coreShards-=price;State.ownedCosmetics.Add(offer);
            return CommandResult.Ok("균열 교환 완료 · 전시실에서 적용하세요.");
        }
        public CommandResult ClaimWeekly(string week,int tier)
        {
            var progress=Endgame.Find(State,week);
            if(tier<0 || tier>=Endgame.Goals.Length || progress==null || progress.attempts<1 || progress.bestScore<Endgame.Goals[tier])return CommandResult.Fail("아직 달성하지 못한 보상이에요.");
            int bit=1<<tier;if((progress.claimed&bit)!=0)return CommandResult.Fail("이미 받은 주간 보상이에요.");
            progress.claimed|=bit;State.coreShards+=Endgame.Shards[tier];State.gold+=Endgame.Gold[tier];State.parts+=Endgame.Parts[tier];State.gems+=Endgame.Gems[tier];
            return CommandResult.Ok("주간 보상 · 골드 +"+Endgame.Gold[tier]+" · 부품 +"+Endgame.Parts[tier]+(Endgame.Gems[tier]>0 ? " · 보석 +"+Endgame.Gems[tier] : ""));
        }
    }
}
