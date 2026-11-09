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
        [Serializable] sealed class NaturalRunEvidence
        {
            public string kind="automated_fresh_loadout_not_human",clip;
            public float[] damageBySource,healingBySource;public float lowHealthSeconds;public int machineKills,machineIntercepts;public float bossSeconds;public float hazardDamageReceived;public float machineActiveSeconds,machineDamage,damageDealt,damageReceived,healingReceived,minimumHp,firstEvolutionSeconds;public int hazardHits;
            public int seed,kills,peakEnemies,bestThreeSecondKills,machines,frames;
            public float seconds,firstMachine=-1,hp;
            public bool won;
        }
        V2 NaturalMovement()
        {
            V2 target=new V2(270+Mathf.Cos(combat.Elapsed*.055f)*240,510+Mathf.Sin(combat.Elapsed*.055f)*280);
            float nearest=300;
            foreach(var pickup in combat.Pickups)
            {
                float distance=V2.Distance(pickup.position,combat.Position);
                if(distance<nearest){nearest=distance;target=pickup.position;}
            }
            V2 away=new V2();
            foreach(var enemy in combat.Enemies)
            {
                var delta=combat.Position-enemy.position;float d=delta.Length;
                if(d<90)away+=delta.Normal*((90-d)/55);
            }
            return ((target-combat.Position).Normal+away).Normal;
        }
        IEnumerator NaturalCombatRoutine()
        {
            DismissTitleScreen();CloseModal();soakDriving=true;
            int runIndex=0;
            foreach(int seed in new[]{17,71,91})
            {
                runIndex++;string folder=Path.Combine(smokeOutput,"run-"+runIndex);Directory.CreateDirectory(folder);
                var snapshot=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(State));
                OpenBattle(0,"natural-"+seed,seed,false);battlePaused=false;
                var evidence=new NaturalRunEvidence{seed=seed,clip="run-"+runIndex};
                int[] recentKills=new int[90];int captureStart=-1,clipFrame=0;
                for(int frame=0;frame<9000&&!combat.Ended;frame++)
                {
                    if(combat.Choosing)
                    {
                        int choice=combat.Choices[0];
                        foreach(int preferred in new[]{1,8,0,2,3,9,4,5,10,7,11,6})
                            if(Array.IndexOf(combat.Choices,preferred)>=0){choice=preferred;break;}
                        combat.Choose(choice);recorder.Choose(choice);
                    }
                    if(combat.CanChooseTask){combat.ChooseTask(1);recorder.Encounter(1);}
                    if(combat.CanAssemble){combat.Assemble(2);recorder.Assemble(2);}
                    if(combat.CanActivateMachine&&combat.Enemies.Count>=8)
                    {
                        combat.ActivateMachine();recorder.Machine();
                        if(evidence.firstMachine<0){evidence.firstMachine=combat.Elapsed;captureStart=frame;}
                    }
                    if(combat.CanUltimate&&combat.Enemies.Count>=12){combat.UseUltimate();recorder.Ultimate();if(combat.Ended)break;}
                    combat.Tick(ReplayRecorder.Step,recorder.Move(NaturalMovement()));
                    evidence.peakEnemies=Math.Max(evidence.peakEnemies,combat.Enemies.Count);
                    evidence.bestThreeSecondKills=Math.Max(evidence.bestThreeSecondKills,combat.Kills-recentKills[frame%90]);recentKills[frame%90]=combat.Kills;
                    if(frame%3==0)
                    {
                        RenderBattle();yield return null;
                        // Genuine simulation frames: no injected enemies, health, skills or clock changes.
                        if(captureStart>=0&&frame>=captureStart&&frame<captureStart+450&&frame%6==0)
                        {
                            yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();
                            try{File.WriteAllBytes(Path.Combine(folder,"frame-"+(clipFrame++).ToString("D4")+".jpg"),texture.EncodeToJPG(88));}
                            finally{Destroy(texture);}
                        }
                    }
                    evidence.frames=frame+1;
                    if(frame%300==0)File.WriteAllText(Path.Combine(smokeOutput,"progress.txt"),"run="+runIndex+" seconds="+combat.Elapsed+" kills="+combat.Kills+" frame="+frame);
                }
                if(!combat.Ended)combat.Abandon();
                evidence.kills=combat.Kills;evidence.seconds=combat.Elapsed;evidence.machines=combat.MachinesBuilt;evidence.hp=combat.Hp;evidence.won=combat.Won;
                evidence.damageBySource=combat.DamageBySource;evidence.healingBySource=combat.HealingBySource;evidence.lowHealthSeconds=combat.LowHealthSeconds;evidence.machineKills=combat.MachineKills;evidence.machineIntercepts=combat.MachineIntercepts;evidence.bossSeconds=combat.BossCombatSeconds;evidence.hazardDamageReceived=combat.HazardDamageReceived;evidence.machineActiveSeconds=combat.MachineActiveSeconds;evidence.machineDamage=combat.MachineDamage;evidence.damageDealt=combat.DamageDealt;evidence.damageReceived=combat.DamageReceived;evidence.healingReceived=combat.HealingReceived;evidence.minimumHp=combat.MinimumHp;evidence.hazardHits=combat.HazardHits;evidence.firstEvolutionSeconds=combat.FirstEvolutionSeconds;
                File.WriteAllText(Path.Combine(folder,"evidence.json"),JsonUtility.ToJson(evidence,true));
                var probe=new AssaultReplayProbe{state=snapshot,steps=recorder.Steps,id=combat.RunId,mode=combat.Mode,stage=0,seed=seed,kills=combat.Kills,machines=combat.MachinesBuilt,waves=combat.WaveNumber,hp=combat.Hp,seconds=combat.Elapsed,won=combat.Won};
                File.WriteAllText(Path.Combine(smokeOutput,"runtime-replay-natural-"+seed+".json"),JsonUtility.ToJson(probe));
                RenderBattle();yield return Capture("run-"+runIndex+"-end");combat=null;Show("home");
            }
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Natural combat capture errors: "+smokeErrors.Count+"\nAutomated fresh-loadout runs, not human or real-device performance evidence.\n"+string.Join("\n",smokeErrors));
            Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
