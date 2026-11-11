using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator SurvivalSmokeRoutine()
        {
            CloseModal();soakDriving=true;
            // Isolated loss UI fixture. Not included in balance or replay evidence.
            StartBattle(14);battlePaused=true;combat.Elapsed=190;combat.Hp=1;
            combat.Hazards.Add(new Hazard{position=combat.Position,radius=100,damage=1000,delay=0,cause=DamageCause.Poison,source=DamageSource.EnemyAttack});
            combat.Tick(ReplayRecorder.Step,new V2());FinishBattle();yield return Capture("45-survival-loss");
            BattleAnalysis();yield return Capture("46-recent-damage");CloseModal();SurvivalPracticeOptions(14);yield return Capture("46b-failure-options");CloseModal();ShowResult();
            foreach(int stage in new[]{14,26,56})
            {
                int gold=State.gold,parts=State.parts,gems=State.gems,runs=State.totalRuns,supply=State.dailySupplySeconds;
                var snapshot=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(State));
                if(stage==14)
                {
                    // Exercise the same button offered by the loss screen.
                    ClickVisibleButton("실패 구간 60초 무료 연습");
                }
                else StartBattle(stage,false,SurvivalTraining.Mode);
                if(combat==null||!combat.SurvivalPractice){smokeErrors.Add("Survival practice route failed "+stage);yield break;}
                gold=State.gold;parts=State.parts;gems=State.gems;runs=State.totalRuns;supply=State.dailySupplySeconds;
                snapshot=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(State));
                battlePaused=true;RenderBattle();yield return Capture("47-survival-start-"+stage);battlePaused=false;
                for(int frame=0;frame<1900&&!combat.Ended;frame++)
                {
                    if(combat.Choosing){int pick=combat.Choices[0];combat.Choose(pick);recorder.Choose(pick);}
                    combat.Tick(ReplayRecorder.Step,recorder.Move(NaturalMovement()));RenderBattle();
                    yield return new WaitForSecondsRealtime(ReplayRecorder.Step);
                }
                if(!combat.Ended)smokeErrors.Add("Practice exceeded sixty seconds "+stage);
                var probe=new AssaultReplayProbe{state=snapshot,steps=recorder.Steps,id=combat.RunId,mode=combat.Mode,stage=stage,seed=replaySeed,kills=combat.Kills,machines=combat.MachinesBuilt,waves=combat.WaveNumber,hp=combat.Hp,seconds=combat.Elapsed,won=combat.Won};
                File.WriteAllText(Path.Combine(smokeOutput,"runtime-replay-survival-"+stage+".json"),JsonUtility.ToJson(probe));
                FinishBattle();yield return Capture("48-survival-result-"+stage);
                if(State.gold!=gold||State.parts!=parts||State.gems!=gems||State.totalRuns!=runs||State.dailySupplySeconds!=supply)smokeErrors.Add("Practice leaked progression "+stage);
                CloseModal();combat=null;Show("home");
            }
            soakDriving=false;
        }
    }
}
