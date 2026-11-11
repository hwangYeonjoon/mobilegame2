using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator RegionalSmokeRoutine()
        {
            DismissTitleScreen();CloseModal();
            for(int region=0;region<10;region++)
            {
                int stage=region*6;
                RegionalEnemyGuide(region,stage);yield return Capture("region-"+region+"-guide");CloseModal();
                OpenBattle(stage,"region-art-"+region,17,false);battlePaused=true;combat.Enemies.Clear();combat.Elapsed=30;
                for(int role=0;role<6;role++)combat.Enemies.Add(new Enemy{id=9000+role,role=(EnemyRole)role,position=combat.Position+new V2(-140+(role%3)*140,-120+(role/3)*250),hp=10000,maxHp=10000,windup=role==4&&region==3?2:0,direction=new V2(-1,0)});
                RenderBattle();yield return Capture("region-"+region+"-family");
                combat.Enemies.Clear();
                for(int n=0;n<210;n++)combat.Enemies.Add(new Enemy{id=10000+n,role=Campaign.Get(stage).Enemies[n%Campaign.Get(stage).Enemies.Length],position=combat.Position+V2.Angle(n*.8)*(65+n%30*9),hp=5000,maxHp=5000});
                RenderBattle();yield return Capture("region-"+region+"-crowd");combat=null;Show("home");
                var s=GameState.New();s.tutorialCompleted=true;s.heroes[0].level=20;foreach(var item in s.items){item.level=10;item.rarity=2;}
                var run=new Combat(s,stage,"region-runtime-"+region,71);var input=new ReplayRecorder();
                for(int frame=0;frame<2400&&!run.Ended;frame++)
                {
                    if(run.Choosing){input.Choose(run.Choices[0]);run.Choose(run.Choices[0]);}
                    if(run.CanUltimate){input.Ultimate();run.UseUltimate();}
                    run.Tick(ReplayRecorder.Step,input.Move(V2.Angle(frame*.008)));
                    if(frame%120==0)yield return null;
                }
                File.WriteAllText(Path.Combine(smokeOutput,"runtime-replay-region-"+region+".json"),JsonUtility.ToJson(new AssaultReplayProbe{state=s,steps=input.Steps,id=run.RunId,mode=run.Mode,stage=stage,seed=71,kills=run.Kills,machines=run.MachinesBuilt,waves=run.WaveNumber,hp=run.Hp,seconds=run.Elapsed,won=run.Won}));
            }
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Regional runtime errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("REGIONAL_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
