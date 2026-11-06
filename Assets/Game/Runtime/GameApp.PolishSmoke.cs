using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        bool soakDriving;
        IEnumerator PolishSmokeRoutine(bool soak=false)
        {
            DismissTitleScreen();CloseModal();
            Store.Change(e=>{e.State.testProgress=true;e.State.gems=1000;e.State.unlockedStage=41;foreach(var h in e.State.heroes){h.unlocked=true;h.level=15;}return CommandResult.Ok("Isolated polish QA");});
            foreach(int theme in new[]{0,1,2,3,4}){ThemeCollection(theme);yield return Capture("01-theme-"+theme);}
            string before=JsonUtility.ToJson(State);ThemeCollection(1);if(before!=JsonUtility.ToJson(State))smokeErrors.Add("Theme preview changed save");
            ThemePurchase(1);yield return Capture("02-theme-confirm");ClickVisibleButton("보석 400개로 배경 구매");yield return Capture("03-theme-owned");
            if(State.gems!=600||!ShowcaseThemes.Owned(State,1))smokeErrors.Add("Theme purchase amount mismatch");
            ClickVisibleButton("이 배경으로 전시");yield return Capture("04-theme-showcase");
            if(State.showcaseTheme!=1)smokeErrors.Add("Theme equip failed");
            ActivityPanel(null,null);yield return Capture("05-activity-offline");
            var helper=new GuildView{id="qa",week="2026-45",featuredId="self"};helper.members.Add(new GuildMember{id="self",self=true,supportUses=3,supportWins=2,helpedMembers=2});
            ActivityPanel(new PublicProfile{likes=4},helper);yield return Capture("06-activity-fixture");
            CloseModal();yield return null;soakDriving=true;
            foreach(int stage in new[]{2,4,8})
            {
                OpenBattle(stage,"operation-qa-"+stage,7,false);battlePaused=true;combat.Elapsed=12;RenderBattle();yield return Capture("07-operation-"+stage);
                for(int i=0;i<2500&&!combat.TaskCompleted;i++)
                {
                    combat.Enemies.Clear();combat.Bullets.Clear();combat.Hazards.Clear();combat.Pickups.Clear();combat.Hp=combat.MaxHp;
                    combat.Position=combat.TaskPosition;if(combat.Choosing)combat.Choose(combat.Choices[0]);combat.Tick(.05f,new V2());
                }
                RenderBattle();FieldAssemblyPanel();yield return Capture("08-assembly-"+stage);
                modal.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=0;yield return Capture("09-assembly-bottom-"+stage);
                CloseModal();yield return null;
                if(!combat.TaskCompleted||!combat.Assemble(4))smokeErrors.Add("Operation failed to offer build "+stage);
                RenderBattle();yield return Capture("10-operation-complete-"+stage);
            }
            for(int module=1;module<=4;module++)
            {
                OpenBattle(1,"build-qa-"+module,7,false);battlePaused=true;combat.Elapsed=12;combat.Position=combat.TaskPosition;combat.ChooseTask(1);combat.Assemble(module);
                for(int i=0;i<6;i++)combat.Enemies.Add(new Enemy{id=5000+i,hp=10000,maxHp=10000,role=(EnemyRole)i,position=new V2(70+i%3*195,300+i/3*320)});
                RenderBattle();yield return Capture("11-build-"+module);
            }
            for(int region=0;region<8;region++)
            {
                var sprite=BattleArt.Role(region,2,true);if(sprite==null)smokeErrors.Add("Missing boss art "+region);
            }
            if(soak){yield return PolishSoakRoutine();yield break;}
            WritePolishReport("Polish UI");
        }
        IEnumerator PolishSoakRoutine()
        {
            CloseModal();yield return null;soakDriving=true;
            float start=Time.realtimeSinceStartup,nextReport=0,accumulator=0;int runs=0;
            var frames=new List<float>(90000);string csv=Path.Combine(smokeOutput,"soak-minutes.csv");
            File.WriteAllText(csv,"seconds,runs,managed_bytes,unity_allocated_bytes,enemies,bullets\n");
            while(Time.realtimeSinceStartup-start<1200)
            {
                if(combat==null||combat.Ended||combat.Elapsed>=85)
                {
                    State.selectedHero=runs%HeroCatalog.Count;OpenBattle(new[]{1,2,4,8,30,40}[runs%6],"soak-"+runs,71+runs,false);runs++;accumulator=0;
                }
                battlePaused=false;float delta=Time.unscaledDeltaTime;frames.Add(delta);accumulator+=Mathf.Min(.1f,delta);
                while(accumulator>=ReplayRecorder.Step&&!combat.Ended)
                {
                    accumulator-=ReplayRecorder.Step;if(combat.Choosing)combat.Choose(combat.Choices[0]);
                    if(combat.CanChooseTask)combat.ChooseTask(1);if(combat.CanAssemble)combat.Assemble(1+runs%4);
                    if(combat.CanUltimate)combat.UseUltimate();
                    var move=combat.TaskActive?(combat.TaskPosition-combat.Position).Normal:SmokeMovement();
                    if(combat.TaskActive&&V2.Distance(combat.Position,combat.TaskPosition)<20)move=new V2();
                    combat.Tick(ReplayRecorder.Step,move);
                }
                RenderBattle();float elapsed=Time.realtimeSinceStartup-start;
                if(elapsed>=nextReport)
                {
                    nextReport+=60;File.AppendAllText(csv,elapsed.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+","+runs+","+GC.GetTotalMemory(false)+","+UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()+","+combat.Enemies.Count+","+combat.Bullets.Count+"\n");
                    File.WriteAllText(Path.Combine(smokeOutput,"progress.txt"),"seconds="+elapsed+" / 1200; runs="+runs);
                }
                yield return null;
            }
            frames.Sort();File.WriteAllText(Path.Combine(smokeOutput,"frame-summary.txt"),"Device: "+SystemInfo.deviceModel+"\nDuration >=1200 seconds; runs="+runs+"\nFrames="+frames.Count+" p50_ms="+frames[frames.Count/2]*1000+" p95_ms="+frames[(int)(frames.Count*.95f)]*1000+"\nEmulator measurements do not establish physical-device heat, GPU performance or touch usability.");
            yield return Capture("12-soak-final");WritePolishReport("20 minute soak");
        }
        void WritePolishReport(string kind)
        {
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),kind+" errors: "+smokeErrors.Count+"\n"+Screen.width+"x"+Screen.height+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("POLISH_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
