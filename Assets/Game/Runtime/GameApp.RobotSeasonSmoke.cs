using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator RobotSeasonSmoke()
        {
            DismissTitleScreen();CloseModal();
            Store.Change(e=>{e.State.testProgress=true;e.State.clearedStages.Add(4);e.State.gold=16000;e.State.parts=160;e.State.gems=600;return CommandResult.Ok("Isolated theme QA fixture");});
            Show("weekly");yield return Capture("01-weekly-entry");RobotSeasonHub();yield return Capture("02-theme-archive");
            SeekerBlueprintPanel();yield return Capture("03-free-blueprint");ClickVisibleButton("골드 6,000 · 부품 60 사용");yield return Capture("04-blueprint-confirm");
            ClickVisibleButton("재료를 사용해 설계 제작");yield return Capture("05-blueprint-owned");
            if(!RobotSeason.HasBlueprint(State)||State.gold!=10000||State.parts!=100)smokeErrors.Add("Blueprint UI debit mismatch");
            for(int frame=0;frame<3;frame++){StormPackPanel(frame);yield return Capture("06-premium-preview-"+frame);}
            ClickVisibleButton("600 보석 · 구매 확인");yield return Capture("07-purchase-confirm");ClickVisibleButton("600 보석 구매 · 3기체 적용");yield return Capture("08-purchase-equipped");
            if(State.gems!=0||RobotStyles.Active(State,0)!=4||State.gold!=10000||State.parts!=100)smokeErrors.Add("Theme pack changes unexpected resources");
            Store.Change(e=>{e.State.robotRecords.Add(new RobotRecord{design=10,rank=1,kills=200,builds=5});return CommandResult.Ok("Earned mastery rendering fixture, not player data");});
            RescueWorkshop();yield return Capture("09-free-mastery-style");ClickVisibleButton("재료 사용 · 제작 확인");yield return Capture("10-free-craft-confirm");
            ClickVisibleButton("재료를 사용해 제작하고 적용");yield return Capture("11-free-style-applied");
            if(State.gold!=0||State.parts!=0||RobotStyles.Active(State,0)!=3)smokeErrors.Add("Mastery craft UI debit mismatch");
            foreach(string mode in new[]{RobotSeason.Modes[0],RobotSeason.Modes[1],RobotSeason.Modes[2],RobotSeason.Preview})
            {
                string beforeRecords=JsonUtility.ToJson(new RobotSmokeRecords{records=State.robotRecords});int shards=State.coreShards;
                StartBattle(4,false,mode);yield return null;battlePaused=true;
                RobotAssembler(10);robotSupportOptions=true;RobotAssembler(10);yield return Capture("12-design-"+mode);
                ConfirmRobotPlan(10,true);battlePaused=true;
                var snapshot=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(State));
                for(int i=0;i<2800&&!combat.Ended;i++)
                {
                    if(combat.Choosing){int skill=combat.Choices[0];combat.Choose(skill);recorder.Choose(skill);}
                    if(combat.CanPlanRobot(10,true)){combat.PlanRobot(10,true);recorder.RobotPlan(10,true);}
                    if(combat.CanActivateMachine){combat.ActivateMachine();recorder.Machine();}
                    if(combat.CanUltimate){combat.UseUltimate();recorder.Ultimate();if(combat.Ended)break;}
                    V2 away=new V2();foreach(var enemy in combat.Enemies){float d=V2.Distance(enemy.position,combat.Position);if(d<120)away+=(combat.Position-enemy.position).Normal*(120-d)/60;}
                    foreach(var hazard in combat.Hazards)if(V2.Distance(hazard.position,combat.Position)<120)away+=(combat.Position-hazard.position).Normal*2;
                    var target=new V2(270+Mathf.Cos(combat.Elapsed*.25f)*180,500+Mathf.Sin(combat.Elapsed*.25f)*230);
                    combat.Tick(ReplayRecorder.Step,recorder.Move(((target-combat.Position).Normal+away).Normal));
                    if(i==40||i==160){RenderBattle();yield return Capture("13-battle-"+mode+"-"+i);}
                    if(i%90==0)yield return null;
                }
                if(!combat.Ended)combat.Abandon();
                File.WriteAllText(Path.Combine(smokeOutput,"runtime-replay-"+mode+".json"),JsonUtility.ToJson(new AssaultReplayProbe{state=snapshot,steps=recorder.Steps,id=combat.RunId,mode=mode,stage=4,seed=replaySeed,kills=combat.Kills,machines=combat.MachinesBuilt,waves=combat.WaveNumber,hp=combat.Hp,seconds=combat.Elapsed,won=combat.Won}));
                FinishBattle();yield return Capture("14-result-"+mode);combat=null;
                if(mode==RobotSeason.Preview&&(State.coreShards!=shards||beforeRecords!=JsonUtility.ToJson(new RobotSmokeRecords{records=State.robotRecords})))smokeErrors.Add("Preview leaked progress");
            }
            RobotSeasonHub();yield return Capture("15-theme-progress");RobotHangar();yield return Capture("16-gallery");
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Robot season UI errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("ROBOT_SEASON_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
        [System.Serializable] sealed class RobotSmokeRecords {public System.Collections.Generic.List<RobotRecord> records;}
    }
}
