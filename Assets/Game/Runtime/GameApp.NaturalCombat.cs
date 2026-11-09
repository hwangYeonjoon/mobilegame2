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
            public string kind="automated_fresh_loadout_not_human",clip,policy="natural_legacy";
            public int stage,build,rules,peakBossWarnings,bossRoleAttacks,bossAttackGroups;public float bossContactDamage,bossWarningDamage;
            public float[] damageBySource,healingBySource;public float lowHealthSeconds;public int machineKills,machineIntercepts;public float bossSeconds;public float hazardDamageReceived;public float machineActiveSeconds,machineDamage,damageDealt,damageReceived,healingReceived,minimumHp,firstEvolutionSeconds;public int hazardHits;
            public int robotDesign,robotRank,plansBuilt,partsSpent;public float firstSpecialized=-1;public int seed,kills,peakEnemies,bestThreeSecondKills,machines,frames;
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
        V2 BossProbeMovement()
        {
            var c=combat;
            V2 target=c.BossArenaActive?c.BossArenaCenter:new V2(270+(float)Math.Cos(c.Elapsed*.055f)*240,510+(float)Math.Sin(c.Elapsed*.055f)*280);
            var boss=c.Enemies.Find(e=>e.finalBoss&&e.hp>0);
            if(boss!=null)target=boss.position+(c.Position-boss.position).Normal*120;
            else{float nearest=300;foreach(var p in c.Pickups){float d=V2.Distance(p.position,c.Position);if(d<nearest){nearest=d;target=p.position;}}}
            V2 away=new V2();foreach(var e in c.Enemies){var delta=c.Position-e.position;float d=delta.Length;if(d<90)away+=delta.Normal*((90-d)/55);}
            var desired=((target-c.Position).Normal+away).Normal;V2 best=desired;float bestScore=float.NegativeInfinity;
            for(int i=0;i<16;i++)
            {
                var direction=V2.Angle(i*(float)Math.PI/8);var point=c.Position+direction*48;
                float score=direction.x*desired.x+direction.y*desired.y;
                if(c.IsBlocked(point,18)||V2.Distance(point,c.ClampBossPoint(point,26))>1)score-=10;
                foreach(var h in c.Hazards)
                {
                    bool inside=h.width>0?Math.Abs(point.x-h.position.x)<h.width/2+26&&Math.Abs(point.y-h.position.y)<h.height/2+26:V2.Distance(point,h.position)<h.radius+26;
                    if(inside)score-=4/(.25f+h.delay);
                }
                if(!c.BossArenaActive&&c.Definition.Region==2)foreach(var z in c.Zones)if(z.Contains(point,20))score-=4;
                if(score>bestScore){bestScore=score;best=direction;}
            }
            return best;
        }
        IEnumerator NaturalCombatRoutine()
        {
            DismissTitleScreen();CloseModal();soakDriving=true;
            int runIndex=0;
            foreach(int seed in new[]{17,71,91})
            {
                runIndex++;int stage=smokeBossProgression?new[]{1,6,12}[runIndex-1]:0;bool controlled=smokeBossProgression&&runIndex>1;
                if(smokeBossProgression)State.tutorialCompleted=true;
                string folder=Path.Combine(smokeOutput,"run-"+runIndex);Directory.CreateDirectory(folder);
                var snapshot=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(State));
                OpenBattle(stage,"natural-"+seed,seed,false);battlePaused=false;
                var evidence=new NaturalRunEvidence{seed=seed,stage=stage+1,build=AppRelease.Build,rules=Combat.RulesVersion,policy=controlled?"controlled_avoid":"natural_legacy",clip="run-"+runIndex};
                int[] recentKills=new int[90];int captureStart=-1,clipFrame=0,capturedBossGroups=0;
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
                    int planned=1+(runIndex-1)*4;if(combat.CanPlanRobot(planned,true)){combat.PlanRobot(planned,true);recorder.RobotPlan(planned,true);}
                    if(combat.CanActivateMachine&&(combat.Enemies.Count>=8||controlled&&combat.FinalBossActive))
                    {
                        combat.ActivateMachine();recorder.Machine();evidence.robotDesign=combat.MachineDesign;
                        if(evidence.firstMachine<0)evidence.firstMachine=combat.Elapsed;
                        if(evidence.firstSpecialized<0&&combat.RobotRank(combat.MachineDesign)>0){evidence.firstSpecialized=combat.Elapsed;captureStart=frame;}
                    }
                    if(combat.CanUltimate&&(combat.Enemies.Count>=12||controlled&&combat.FinalBossActive)){combat.UseUltimate();recorder.Ultimate();if(combat.Ended)break;}
                    combat.Tick(ReplayRecorder.Step,recorder.Move(controlled?BossProbeMovement():NaturalMovement()));
                    evidence.peakEnemies=Math.Max(evidence.peakEnemies,combat.Enemies.Count);
                    evidence.bestThreeSecondKills=Math.Max(evidence.bestThreeSecondKills,combat.Kills-recentKills[frame%90]);recentKills[frame%90]=combat.Kills;
                    if(frame%3==0)
                    {
                        RenderBattle();yield return null;
                        if(smokeBossProgression&&combat.BossAttackGroups>capturedBossGroups&&combat.Hazards.Count>0)
                        {capturedBossGroups=combat.BossAttackGroups;yield return Capture("run-"+runIndex+"-boss-group-"+capturedBossGroups);}
                        // Genuine simulation frames: no injected enemies, health, skills or clock changes.
                        if(!smokeBossProgression&&captureStart>=0&&frame>=captureStart&&frame<captureStart+450&&frame%6==0)
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
                evidence.robotRank=combat.RobotRank(evidence.robotDesign);evidence.plansBuilt=combat.RobotPlansBuilt;evidence.partsSpent=combat.RobotPartsSpent;
                evidence.kills=combat.Kills;evidence.seconds=combat.Elapsed;evidence.machines=combat.MachinesBuilt;evidence.hp=combat.Hp;evidence.won=combat.Won;
                evidence.peakBossWarnings=combat.PeakBossWarnings;evidence.bossRoleAttacks=combat.BossRoleAttacks;evidence.bossAttackGroups=combat.BossAttackGroups;evidence.bossContactDamage=combat.BossContactDamage;evidence.bossWarningDamage=combat.BossWarningDamage;
                evidence.damageBySource=combat.DamageBySource;evidence.healingBySource=combat.HealingBySource;evidence.lowHealthSeconds=combat.LowHealthSeconds;evidence.machineKills=combat.MachineKills;evidence.machineIntercepts=combat.MachineIntercepts;evidence.bossSeconds=combat.BossCombatSeconds;evidence.hazardDamageReceived=combat.HazardDamageReceived;evidence.machineActiveSeconds=combat.MachineActiveSeconds;evidence.machineDamage=combat.MachineDamage;evidence.damageDealt=combat.DamageDealt;evidence.damageReceived=combat.DamageReceived;evidence.healingReceived=combat.HealingReceived;evidence.minimumHp=combat.MinimumHp;evidence.hazardHits=combat.HazardHits;evidence.firstEvolutionSeconds=combat.FirstEvolutionSeconds;
                File.WriteAllText(Path.Combine(folder,"evidence.json"),JsonUtility.ToJson(evidence,true));
                var probe=new AssaultReplayProbe{state=snapshot,steps=recorder.Steps,id=combat.RunId,mode=combat.Mode,stage=stage,seed=seed,kills=combat.Kills,machines=combat.MachinesBuilt,waves=combat.WaveNumber,hp=combat.Hp,seconds=combat.Elapsed,won=combat.Won};
                File.WriteAllText(Path.Combine(smokeOutput,"runtime-replay-natural-"+seed+".json"),JsonUtility.ToJson(probe));
                RenderBattle();yield return Capture("run-"+runIndex+"-end");combat=null;Show("home");
            }
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Natural combat capture errors: "+smokeErrors.Count+"\nAutomated fresh-loadout runs, not human or real-device performance evidence.\n"+string.Join("\n",smokeErrors));
            Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
