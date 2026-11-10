using System;
using System.Collections.Generic;
namespace ScrapGuard.Core
{
    [Serializable] public class ApexRecord { public int stage,attempts,wins,robotDesign; public float bestSeconds; }
    public static class Apex
    {
        public const string Mode="apex";
        public const int FirstShards=6;
        public static readonly int[] Stages={47,53,59};
        public static void Normalize(GameState s){if(s.apexRecords==null)s.apexRecords=new List<ApexRecord>();}
        public static bool Available(GameState s,int stage){return Array.IndexOf(Stages,stage)>=0&&s.clearedStages.Contains(stage);}
        public static ApexRecord Find(GameState s,int stage){return s.apexRecords==null?null:s.apexRecords.Find(r=>r.stage==stage);}
        public static bool Mastered(GameState s){foreach(int stage in Stages)if((Find(s,stage)?.wins??0)==0)return false;return true;}
    }
    public sealed partial class Economy
    {
        public CommandResult BeginApex(string id,int stage)
        {
            if(!Apex.Available(State,stage))return CommandResult.Fail("일반 48·54·60작전을 각각 완료하면 해당 극한 작전이 열려요.");
            var result=BeginRun(id,stage);if(result.Success)State.activeMode=Apex.Mode;return result;
        }
        public CommandResult FinishApex(Combat c)
        {
            RefreshDay();
            if(c==null||!c.Ended||!c.ApexBattle||State.activeMode!=Apex.Mode||State.activeRun!=c.RunId||State.activeStage!=c.Stage||!Apex.Available(State,c.Stage)||State.receipts.Exists(r=>r.id==c.RunId))return CommandResult.Fail("유효하지 않은 극한 작전 기록이에요.");
            Apex.Normalize(State);var p=Apex.Find(State,c.Stage);if(p==null){p=new ApexRecord{stage=c.Stage};State.apexRecords.Add(p);}p.attempts++;
            int shards=0;
            if(c.Won)
            {
                if(p.wins==0){shards=Apex.FirstShards;State.coreShards+=shards;}
                p.wins++;if(p.bestSeconds<=0||c.Elapsed<p.bestSeconds){p.bestSeconds=c.Elapsed;p.robotDesign=c.RobotReport().FindLast(r=>r.builds>0)?.design??0;}
            }
            RobotAssembly.Record(State,c.RobotReport(),-1);CreditFieldSupply(c.Elapsed);
            State.receipts.Add(new Receipt{id=c.RunId,kind=Apex.Mode,shards=shards,seconds=c.Elapsed,kills=c.Kills,won=c.Won,report=c.Report()});
            State.activeRun=State.activeMode=State.activeWeek="";State.lastResultGold=0;State.lastResultBonus=true;
            return CommandResult.Ok(shards>0?"극한 첫 완료 · 코어 파편 +6":c.Won?"극한 완료 · 최고 시간 기록 저장":"극한 도전 기록 저장 · 일반 작전 진행은 그대로예요.");
        }
    }
    public sealed partial class Combat { public bool ApexBattle {get{return Mode==Apex.Mode;}} }
}
