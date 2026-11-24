using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator NavigationSmoke()
        {
            int oldGoal=PlayerPrefs.GetInt(RobotGoalKey,0);PlayerPrefs.DeleteKey(RobotGoalKey);
            DismissTitleScreen();CloseModal();Show("home");yield return Capture("01-home-first");
            ClickVisibleButton("강화");if(page!="growth")smokeErrors.Add("Bottom upgrade route failed");
            yield return Capture("02-upgrade");
            string weapon=State.equipped[0];int level=State.items.Find(i=>i.id==weapon).level;
            ClickVisibleButton("강화 +1");
            if(State.items.Find(i=>i.id==weapon).level!=level+1)smokeErrors.Add("Equipped weapon upgrade failed");
            yield return Capture("03-upgrade-applied");ClickVisibleButton("상점");yield return Capture("04-shop");
            ClickVisibleButton("로봇");yield return Capture("05-codex");
            string before=JsonUtility.ToJson(State);
            foreach(int id in new[]{1,5,9,10})foreach(int rank in new[]{1,2}){RobotCodexDetail(id,rank);yield return Capture("06-robot-"+id+"-"+rank);}
            for(int p=0;p<3;p++){RobotPartSources(p==0?1:p==1?5:9,p,1);yield return Capture("07-source-"+p);}
            if(before!=JsonUtility.ToJson(State))smokeErrors.Add("Codex preview changed wallet, unlocks or progress");
            SetRobotGoal(10);if(SavedRobotGoal!=0)smokeErrors.Add("Locked blueprint became a selectable goal");
            SetRobotGoal(9);if(SavedRobotGoal!=9)smokeErrors.Add("Goal was not saved");
            yield return Capture("08-home-goal");
            OpenBattle(0,"navigation-goal-fixture",17,false);battlePaused=true;
            if(robotTargetDesign!=9||combat.RobotPlansBuilt!=0||combat.RobotPartsSpent!=0||combat.MachineDesign!=RobotAssembly.DefaultDesign)smokeErrors.Add("Goal either ignored or changed battle inventory/design");
            RobotAssembler(robotTargetDesign);yield return Capture("09-goal-in-battle");
            CloseModal();combat.Abandon();ShowResult();yield return Capture("10-defeat-result");
            if(ResultDestination(false,0)!=0)smokeErrors.Add("Defeat advanced stage");
            // Isolated UI receipt fixture: not a recorded gameplay win.
            Store.Change(e=>{e.State.testProgress=true;e.State.unlockedStage=1;e.State.clearedStages.Add(0);e.State.receipts.Add(new Receipt{id=combat.RunId,won=true,gold=800,parts=8});return CommandResult.Ok("QA result route fixture");});
            ShowResult();yield return Capture("11-victory-next-stage");
            ClickVisibleButton("다음 작전 02 출격 →");
            if(combat==null||combat.Stage!=1||selectedStage!=1)smokeErrors.Add("Win CTA did not start next stage");
            combat=null;battlePaused=false;Show("home");yield return Capture("12-home-progress");
            Store.Change(e=>{e.State.unlockedStage=59;return CommandResult.Ok("QA boundary fixture");});
            if(ResultDestination(true,58)!=59||ResultDestination(true,59)!=59||ResultDestination(true,3)!=4)smokeErrors.Add("Campaign destination boundary/replay mismatch");
            OpenBattle(59,"navigation-final-fixture",17,false);battlePaused=true;combat.Abandon();
            Store.Change(e=>{e.State.receipts.Add(new Receipt{id=combat.RunId,won=true});return CommandResult.Ok("QA final result fixture");});
            ShowResult();yield return Capture("13-final-result");ClickVisibleButton("주간 로봇 도전 →");
            if(page!="weekly"||combat!=null)smokeErrors.Add("Final win did not route to weekly");
            Show("more");yield return Capture("14-activities");
            Store.Change(e=>{for(int i=0;i<Campaign.Count;i++)if(!e.State.clearedStages.Contains(i))e.State.clearedStages.Add(i);return CommandResult.Ok("QA completed campaign fixture");});
            selectedStage=59;Show("home");yield return Capture("15-home-completed");ClickVisibleButton("주간 로봇 도전 →");
            if(page!="weekly")smokeErrors.Add("Completed campaign home did not expose weekly challenge");
            if(oldGoal>0)PlayerPrefs.SetInt(RobotGoalKey,oldGoal);else PlayerPrefs.DeleteKey(RobotGoalKey);PlayerPrefs.Save();
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Navigation UI errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("NAVIGATION_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
