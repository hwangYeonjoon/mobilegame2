using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator ApexSmoke()
        {
            Show("weekly");ApexPanel();yield return Capture("37-apex-list");ApexConfirm(47);yield return Capture("38-apex-confirm");
            Store.Change(e=>{e.State.heroes[0].level=30;foreach(var item in e.State.items){item.level=20;item.rarity=2;}return CommandResult.Ok("QA max-level fixture");});
            foreach(int stage in Apex.Stages)
            {
                string id="qa-apex-"+stage;Store.Change(e=>e.BeginApex(id,stage));OpenBattle(stage,id,71,false,Apex.Mode);battlePaused=true;yield return null;
                var snapshot=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(State));bool captured=false;
                for(int i=0;i<16000&&!combat.Ended;i++)
                {
                    if(combat.Choosing){int pick=combat.Choices[0];combat.Choose(pick);recorder.Choose(pick);}
                    if(combat.CanPlanRobot(5,true)){combat.PlanRobot(5,true);recorder.RobotPlan(5,true);}
                    if(combat.CanActivateMachine){combat.ActivateMachine();recorder.Machine();}
                    if(combat.CanUltimate){combat.UseUltimate();recorder.Ultimate();if(combat.Ended)break;}
                    var target=new V2(270+Mathf.Cos(combat.Elapsed*.25f)*160,500+Mathf.Sin(combat.Elapsed*.25f)*230);
                    combat.Tick(ReplayRecorder.Step,recorder.Move((target-combat.Position).Normal));
                    if(!captured&&combat.BossArenaActive&&combat.Hazards.Exists(h=>h.source==DamageSource.Boss)){RenderBattle();yield return Capture("39-apex-boss-"+stage);captured=true;}
                    if(i%120==0)yield return null;
                }
                if(!combat.Ended)combat.Abandon();
                File.WriteAllText(Path.Combine(smokeOutput,"runtime-replay-apex-"+stage+".json"),JsonUtility.ToJson(new AssaultReplayProbe{state=snapshot,steps=recorder.Steps,id=combat.RunId,mode=Apex.Mode,stage=stage,seed=71,kills=combat.Kills,machines=combat.MachinesBuilt,waves=combat.WaveNumber,hp=combat.Hp,seconds=combat.Elapsed,won=combat.Won}));
                FinishBattle();yield return Capture("40-apex-result-"+stage);combat=null;
            }
            CustomizeProfile();yield return null;
            modal.GetComponentInChildren<UnityEngine.UI.ScrollRect>().verticalNormalizedPosition=.72f;
            yield return Capture("41-apex-title-layout");
        }
    }
}
