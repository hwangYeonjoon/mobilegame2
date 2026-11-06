using System;
using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        [Serializable] sealed class AssaultReplayProbe
        {
            public GameState state;public System.Collections.Generic.List<ReplayStep> steps;
            public string id,mode;public int stage,seed,kills,machines,waves;public float hp,seconds;public bool won;
        }
        IEnumerator AssaultReplayCapture()
        {
            foreach(int stage in new[]{0,31})
            {
                var snapshot=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(State));snapshot.heroes[snapshot.selectedHero].level=30;
                foreach(var item in snapshot.items){item.level=20;item.rarity=2;}
                var run=new Combat(snapshot,stage,"cross-runtime-"+stage,71);var input=new ReplayRecorder();
                for(int frame=0;frame<6000&&!run.Ended;frame++)
                {
                    if(run.Choosing){int skill=run.Choices[0];foreach(int candidate in run.Choices)if(candidate>=8){skill=candidate;break;}run.Choose(skill);input.Choose(skill);}
                    if(run.CanUltimate){run.UseUltimate();input.Ultimate();if(run.Ended)break;}
                    var target=new V2(270+Mathf.Cos(run.Elapsed*.06f)*450,490+Mathf.Sin(run.Elapsed*.06f)*600);
                    run.Tick(ReplayRecorder.Step,input.Move((target-run.Position).Normal));
                    if(frame%120==0)yield return null;
                }
                run.Abandon();
                var probe=new AssaultReplayProbe{state=snapshot,steps=input.Steps,id=run.RunId,mode=run.Mode,stage=stage,seed=71,kills=run.Kills,machines=run.MachinesBuilt,waves=run.WaveNumber,hp=run.Hp,seconds=run.Elapsed,won=run.Won};
                File.WriteAllText(Path.Combine(smokeOutput,"runtime-replay-"+stage+".json"),JsonUtility.ToJson(probe));
            }
        }
        IEnumerator AssaultSmokeRoutine()
        {
            DismissTitleScreen();CloseModal();soakDriving=true;
            Store.Change(e=>{e.State.testProgress=true;e.State.unlockedStage=41;return CommandResult.Ok("Isolated assault QA");});
            Show("home");yield return Capture("01-home");HomeActivities();yield return Capture("02-activities");
            AudioOptions();yield return Capture("03-audio");CloseModal();
            OpenBattle(0,"assault-qa",17,false);battlePaused=true;
            combat.Tick(.05f,new V2());RenderBattle();yield return Capture("04-first-battle");
            combat.Position=new V2(810,1100);RenderBattle();yield return Capture("05-follow-camera");
            if(Mathf.Abs(arena.anchoredPosition.x)<100)smokeErrors.Add("Camera did not follow world position");
            combat.Enemies.Add(new Enemy{id=50000,hp=10000,maxHp=10000,kind=3,finalBoss=true,position=new V2(-400,0)});
            RenderBattle();yield return Capture("05b-offscreen-boss-bearing");
            if(!bossText.text.Contains("보스 위치"))smokeErrors.Add("Missing offscreen boss guidance");
            combat.Enemies.RemoveAll(e=>e.id==50000);
            combat.Position=new V2(270,510);combat.Elapsed=22;combat.Tick(.05f,new V2());
            if(combat.Choosing)combat.Choose(combat.Choices[0]);RenderBattle();yield return Capture("06-wave-warning");
            for(int i=0;i<210;i++)
            {
                var p=combat.Position+V2.Angle(i*2.39996f)*(115+i%9*19);
                combat.Enemies.Add(new Enemy{id=10000+i,hp=10000,maxHp=10000,kind=i%12==0?1:0,role=(EnemyRole)(i%6),position=p});
            }
            RenderBattle();yield return Capture("07-horde-210");
            combat.Pickups.Add(new Pickup{id=30000,xp=40,position=combat.Position});
            for(int i=0;i<20;i++){if(combat.Choosing)combat.Choose(combat.Choices[0]);combat.Tick(.05f,new V2());}
            combat.Hazards.Clear();RenderBattle();yield return Capture("08-salvage-machine");
            if(combat.MachinesBuilt<1)smokeErrors.Add("Machine assembly did not trigger from collected salvage");
            combat.Skills[8]=combat.Skills[9]=combat.Skills[10]=5;combat.Tick(.05f,new V2());RenderBattle();yield return Capture("09-new-arsenal");
            combat.Choosing=true;combat.Skills[8]=combat.Skills[9]=2;combat.Choices=new[]{8,9,11};ShowChoices();yield return Capture("10-new-skill-choices");CloseModal();combat.Choosing=false;
            combat.Skills[0]=5;combat.Skills[4]=3;combat.Tick(.05f,new V2());RenderBattle();yield return Capture("11-evolution");
            DrawReviveChoice();yield return Capture("12-revive-choice");CloseModal();
            if(Resources.Load<AudioClip>("Audio/SalvageDrive")==null)smokeErrors.Add("Missing combat music asset");
            combat=null;Show("home");yield return Capture("13-home-return");
            yield return AssaultReplayCapture();
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Assault UI errors: "+smokeErrors.Count+"\n"+Screen.width+"x"+Screen.height+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("ASSAULT_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
