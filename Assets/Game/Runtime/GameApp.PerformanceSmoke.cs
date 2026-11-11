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
        [Serializable] sealed class PairedFrameResult
        {
            public string kind="fixed_work_per_frame_not_realtime_gameplay",phase,gpu;
            public int build,rules,repeat,frames,hash,kills,peakEnemies,gc0,gc1,gc2;
            public float wallSeconds,simulationSeconds,frameP95,frameP99,coreP95,coreP99,sceneP95,sceneP99;
            public long unityBytes;
        }
        static float Percentile(List<float> values,float p){values.Sort();return values[Math.Min(values.Count-1,(int)(values.Count*p))];}
        IEnumerator PerformanceSmokeRoutine()
        {
            yield return null;DismissTitleScreen();CloseModal();soakDriving=true;
            State.tutorialCompleted=true;State.selectedHero=0;State.selectedSkin=0;State.heroes[0].level=20;
            foreach(var item in State.items){item.level=10;item.rarity=2;}
            // Identical seed, input, tick count, and fixed compact VFX. No wall-clock catch-up:
            // a slower build must not receive a different amount of simulation work.
            for(int repeat=0;repeat<3;repeat++)foreach(string phase in new[]{"opening","crowded"})
            {
                OpenBattle(54,"performance-"+repeat+phase,71,false);visualBudget.Mode=2;battlePaused=false;
                if(phase=="crowded"){combat.Elapsed=90;for(int i=0;i<combat.Skills.Length;i++)combat.Skills[i]=3;}
                File.WriteAllText(Path.Combine(smokeOutput,"progress.txt"),"repeat="+repeat+" phase="+phase);
                var frames=new List<float>();var core=new List<float>();var scene=new List<float>();
                int serial=100000,hash=17,peak=0,g0=0,g1=0,g2=0;float wall=0,clock=0;
                for(int frame=0;frame<2100;frame++)
                {
                    if(frame==300){wall=Time.realtimeSinceStartup;clock=combat.Elapsed;g0=GC.CollectionCount(0);g1=GC.CollectionCount(1);g2=GC.CollectionCount(2);}
                    combat.Hp=combat.MaxHp;
                    if(phase=="crowded"&&combat.Enemies.Count<180)
                        for(int n=combat.Enemies.Count;n<210;n++)combat.Enemies.Add(new Enemy{id=serial++,role=Campaign.Get(54).Enemies[n%Campaign.Get(54).Enemies.Length],position=combat.Position+V2.Angle(n*.8f)*(90+n%30*9),hp=1200,maxHp=1200});
                    long start=System.Diagnostics.Stopwatch.GetTimestamp();
                    if(combat.Choosing)combat.Choose(combat.Choices[0]);
                    if(combat.CanChooseTask)combat.ChooseTask(1);
                    if(combat.CanAssemble)combat.Assemble(2);
                    if(combat.CanPlanRobot(5,true))combat.PlanRobot(5,true);
                    if(combat.CanActivateMachine)combat.ActivateMachine();
                    if(combat.CanUltimate)combat.UseUltimate();
                    combat.Tick(ReplayRecorder.Step,NaturalMovement());
                    long simulated=System.Diagnostics.Stopwatch.GetTimestamp();RenderBattle();long rendered=System.Diagnostics.Stopwatch.GetTimestamp();
                    if(combat.Ended){smokeErrors.Add("Unexpected benchmark end");break;}
                    unchecked{hash=hash*31+combat.Kills;hash=hash*31+combat.Enemies.Count;hash=hash*31+(int)(combat.Position.x*100);}
                    peak=Math.Max(peak,combat.Enemies.Count);
                    yield return null;
                    if(frame>=300){frames.Add(Time.unscaledDeltaTime*1000);core.Add((float)((simulated-start)*1000.0/System.Diagnostics.Stopwatch.Frequency));scene.Add((float)((rendered-simulated)*1000.0/System.Diagnostics.Stopwatch.Frequency));}
                }
                var result=new PairedFrameResult{build=AppRelease.Build,rules=Combat.RulesVersion,repeat=repeat,phase=phase,gpu=SystemInfo.graphicsDeviceName,frames=frames.Count,hash=hash,kills=combat.Kills,peakEnemies=peak,
                    wallSeconds=Time.realtimeSinceStartup-wall,simulationSeconds=combat.Elapsed-clock,gc0=GC.CollectionCount(0)-g0,gc1=GC.CollectionCount(1)-g1,gc2=GC.CollectionCount(2)-g2,
                    frameP95=Percentile(frames,.95f),frameP99=Percentile(frames,.99f),coreP95=Percentile(core,.95f),coreP99=Percentile(core,.99f),sceneP95=Percentile(scene,.95f),sceneP99=Percentile(scene,.99f),unityBytes=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()};
                File.WriteAllText(Path.Combine(smokeOutput,"paired-"+repeat+"-"+phase+".json"),JsonUtility.ToJson(result,true));
                combat=null;Show("home");yield return new WaitForSecondsRealtime(2);
            }
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Performance errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
