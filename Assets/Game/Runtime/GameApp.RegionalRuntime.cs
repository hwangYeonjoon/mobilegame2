using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        [Serializable] sealed class RegionalFrameEvidence
        {
            public string kind="synthetic_render_load_not_balance_or_physical_device",device,gpu,phase;
            public int build,rules,stage,frames,peakEnemies,regionalActions,kills;
            public float wallSeconds,simulationSeconds,p50Ms,p95Ms,p99Ms;
            public long unityBytes;
        }
        // Drives the actual Android scene, unlike the paused artwork fixture. Never settles rewards.
        // Frame deltas include Update/render work; screenshot I/O is outside measured intervals.
        IEnumerator RegionalRuntimeRoutine()
        {
            CloseModal();soakDriving=true;
            State.tutorialCompleted=true;State.heroes[0].level=20;
            foreach(var item in State.items){item.level=10;item.rarity=2;}
            foreach(int stage in new[]{18,36,54})
            {
                OpenBattle(stage,"regional-runtime-"+stage,71,false);battlePaused=false;
                foreach(string phase in new[]{"opening","crowded"})
                {
                    if(phase=="crowded")
                    {
                        combat.Elapsed=90;
                        for(int skill=0;skill<combat.Skills.Length;skill++)combat.Skills[skill]=3;
                    }
                    var samples=new List<float>();float start=Time.realtimeSinceStartup,clock=combat.Elapsed,accumulator=0,nextProgress=0;
                    int serial=100000,peak=0,startActions=combat.RegionalActions,startKills=combat.Kills;
                    yield return null;
                    while(Time.realtimeSinceStartup-start<60)
                    {
                        float delta=Time.unscaledDeltaTime;samples.Add(delta*1000);accumulator+=Mathf.Min(.1f,delta);
                        if(combat.Ended){smokeErrors.Add("Render fixture unexpectedly ended at "+stage);break;}
                        // Invulnerability and injected crowd are load fixtures, never gameplay evidence.
                        combat.Hp=combat.MaxHp;
                        if(phase=="crowded"&&combat.Enemies.Count<180)
                        {
                            for(int n=combat.Enemies.Count;n<210;n++)
                                combat.Enemies.Add(new Enemy{id=serial++,role=Campaign.Get(stage).Enemies[n%Campaign.Get(stage).Enemies.Length],
                                    position=combat.Position+V2.Angle(n*.8f)*(90+n%30*9),hp=1200,maxHp=1200});
                        }
                        while(accumulator>=ReplayRecorder.Step&&!combat.Ended)
                        {
                            accumulator-=ReplayRecorder.Step;
                            if(combat.Choosing)combat.Choose(combat.Choices[0]);
                            if(combat.CanChooseTask)combat.ChooseTask(1);
                            if(combat.CanAssemble)combat.Assemble(2);
                            if(combat.CanPlanRobot(5,true))combat.PlanRobot(5,true);
                            if(combat.CanActivateMachine)combat.ActivateMachine();
                            if(combat.CanUltimate)combat.UseUltimate();
                            combat.Tick(ReplayRecorder.Step,NaturalMovement());
                        }
                        peak=Math.Max(peak,combat.Enemies.Count);RenderBattle();
                        if(Time.realtimeSinceStartup-start>=nextProgress)
                        {
                            nextProgress+=10;File.WriteAllText(Path.Combine(smokeOutput,"progress.txt"),"stage="+(stage+1)+" phase="+phase+" seconds="+(Time.realtimeSinceStartup-start)+" enemies="+combat.Enemies.Count);
                        }
                        yield return null;
                    }
                    samples.Sort();
                    if(samples.Count<30)smokeErrors.Add("Insufficient rendered frames "+stage+" "+phase);
                    else
                    {
                        var result=new RegionalFrameEvidence{device=SystemInfo.deviceModel,gpu=SystemInfo.graphicsDeviceName,phase=phase,build=AppRelease.Build,rules=Combat.RulesVersion,
                            stage=stage+1,frames=samples.Count,peakEnemies=peak,regionalActions=combat.RegionalActions-startActions,kills=combat.Kills-startKills,
                            wallSeconds=Time.realtimeSinceStartup-start,simulationSeconds=combat.Elapsed-clock,
                            p50Ms=samples[samples.Count/2],p95Ms=samples[(int)(samples.Count*.95f)],p99Ms=samples[(int)(samples.Count*.99f)],
                            unityBytes=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()};
                        File.WriteAllText(Path.Combine(smokeOutput,"frames-"+(stage+1)+"-"+phase+".json"),JsonUtility.ToJson(result,true));
                    }
                    yield return Capture("runtime-"+(stage+1)+"-"+phase);
                }
                combat=null;Show("home");
            }
        }
    }
}
