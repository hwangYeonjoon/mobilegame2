using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;

namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator ArtSmokeRoutine()
        {
            DismissTitleScreen();
            Store.Change(e=>{e.State.testProgress=true;e.State.tutorialCompleted=true;e.State.unlockedStage=41;return CommandResult.Ok("Isolated art fixture");});
            for(int region=0;region<7;region++)
            {
                OpenBattle(Campaign.FinalStage(region).Id,"art-region-"+region,23,false);battlePaused=true;
                combat.Enemies.Clear();combat.Elapsed=5;
                var boss=new Enemy{id=9999,kind=3,finalBoss=true,role=Campaign.BossRole(region),position=new V2(270,350),hp=4000,maxHp=4000};
                combat.Enemies.Add(boss);
                for(int role=0;role<6;role++)combat.Enemies.Add(new Enemy{id=9900+role,kind=0,role=(EnemyRole)role,position=new V2(90+role%3*180,465+role/3*170),hp=300,maxHp=300});
                RenderBattle();yield return Capture("region-"+region+"-idle");
                boss.windup=1;combat.Elapsed+=.2f;
                combat.Hazards.Add(new Hazard{id=88,position=new V2(270,590),radius=65,delay=.6f});
                combat.Hazards.Add(new Hazard{id=89,position=new V2(270,740),width=420,height=42,delay=.8f});
                foreach(var enemy in combat.Enemies)enemy.windup=1;
                RenderBattle();yield return Capture("region-"+region+"-windup");
                foreach(var enemy in combat.Enemies){enemy.windup=0;enemy.attackFlash=1;}
                boss.hp=1900;combat.Elapsed+=.2f;RenderBattle();yield return Capture("region-"+region+"-attack-rage");
                if(hazardLayer.GetSiblingIndex()!=arena.childCount-1)smokeErrors.Add("Warnings occluded by combat art");
                foreach(var tag in scenery.GetComponentsInChildren<ArenaGeometryTag>())
                {
                    var r=(RectTransform)tag.transform;
                    if(r.anchoredPosition!=new Vector2(tag.Bounds.X,-tag.Bounds.Y)||r.sizeDelta!=new Vector2(tag.Bounds.W,tag.Bounds.H))smokeErrors.Add("Terrain footprint mismatch");
                }
                int count=particles.Count;yield return new WaitForSecondsRealtime(.2f);RenderBattle();
                if(particles.Count!=count)smokeErrors.Add("Particles advanced while combat paused");
                combat=null;Show("home");
            }
            // Actual simulation, with all evolved skill combinations and representative warnings.
            OpenBattle(24,"art-combat",73,false);combat.Skills[0]=5;combat.Skills[1]=5;combat.Skills[2]=5;combat.Skills[3]=5;combat.Skills[4]=4;combat.Skills[5]=3;combat.Skills[7]=3;
            combat.MaxHp=combat.Hp=100000;
            float until=Time.realtimeSinceStartup+40;bool ultimate=false;
            while(combat.Elapsed<24 && Time.realtimeSinceStartup<until)
            {
                if(combat.Choosing)ChooseBattleSkill(combat.Choices[0]);
                if(combat.Elapsed>10&&!ultimate && combat.CanUltimate){TriggerUltimate();RenderBattle();yield return Capture("evolved-skills-ultimate");ultimate=true;}
                yield return null;
            }
            yield return Capture("evolved-skills-combat");
            if(!ultimate)smokeErrors.Add("Ultimate not exercised");
            // Pause must freeze the simulation and presentation clock together.
            battlePaused=true;float elapsed=combat.Elapsed;int particlesBefore=particles.Count;
            yield return new WaitForSecondsRealtime(.5f);RenderBattle();
            if(combat.Elapsed!=elapsed || particles.Count!=particlesBefore)smokeErrors.Add("Pause changed presentation clock");
            combat=null;Show("home");
            for(int hero=0;hero<5;hero++)
            {
                Store.Change(e=>{e.State.heroes[hero].unlocked=true;e.State.selectedHero=hero;return CommandResult.Ok("Hero effect fixture");});
                OpenBattle(hero*6,"hero-art-"+hero,55,false);battlePaused=true;combat.MaxHp=combat.Hp=100000;
                for(int step=0;step<2400 && !combat.CanUltimate && !combat.Ended;step++)
                {
                    if(combat.Choosing)combat.Choose(combat.Choices[0]);
                    combat.Tick(ReplayRecorder.Step,new V2(0,0));
                }
                RenderBattle();battlePaused=false;TriggerUltimate();battlePaused=true;RenderBattle();
                if(combat.UltimateUses==0)smokeErrors.Add("Hero ultimate unavailable "+hero);
                yield return Capture("hero-"+hero+"-ultimate");combat=null;Show("home");
            }
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Battle art runtime errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("ART_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
