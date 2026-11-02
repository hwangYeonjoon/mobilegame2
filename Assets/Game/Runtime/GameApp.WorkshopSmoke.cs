using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
using Social=ScrapGuard.Core.Social;

namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator WorkshopSmokeRoutine()
        {
            DismissTitleScreen();ModBook();yield return Capture("01-mod-recipes");
            RegionCollection(0);yield return Capture("02-region-goal");
            // Explicitly isolated local UI fixtures, never server progress.
            Store.Change(e=>{e.State.testProgress=true;e.State.unlockedStage=29;e.State.heroes[0].level=30;foreach(var item in e.State.items)item.level=20;Workshop.Region(e.State,0).blueprints=8;e.State.enemyDiscoveries=63;e.State.bossDiscoveries=31;return CommandResult.Ok("Local UI fixture");});
            ConfirmRegionCraft(0);yield return Capture("03-craft-confirm");ClickVisibleButton("제작 확정");
            if(!Social.HasSkin(State,3) || Workshop.Region(State,0).blueprints!=0)smokeErrors.Add("Region crafting UI failed");
            Change(new GameCommand("customize"){hero=0,title=0,frame=0,skin=3});RegionCollection(0);yield return Capture("04-crafted-paint");
            EnemyCodex();yield return Capture("05-enemy-codex");
            CustomizeProfile();yield return Capture("06-expanded-profile");CloseModal();
            StartBattle(0);battlePaused=true;combat.Enemies.Clear();
            for(int i=0;i<6;i++)combat.Enemies.Add(new Enemy {id=90000+i,kind=2,role=(EnemyRole)i,position=new V2(110+i%3*160,330+i/3*180),hp=100000,maxHp=100000});
            for(int pose=0;pose<4;pose++)
            {
                foreach(var enemy in combat.Enemies){enemy.windup=pose==1?1:0;enemy.attackFlash=pose==2?1:0;enemy.flash=pose==3?1:0;}
                RenderBattle();yield return Capture("role-pose-"+pose);
            }
            combat.Enemies.Clear();
            for(int region=0;region<5;region++)
            {
                // Separate region battle selects the correct boss portrait without claiming rewards.
                combat=new Combat(State,region*6,"boss-art",13);combat.Enemies.Clear();
                combat.Enemies.Add(new Enemy{id=99000,kind=3,finalBoss=true,position=new V2(270,355),hp=100000,maxHp=100000,attackFlash=1});
                RenderBattle();yield return Capture("boss-"+region);
            }
            Change(new GameCommand("battle_cancel"));combat=null;
            StartBattle(0);combat.Skills[0]=3;combat.Skills[5]=2;combat.Skills[1]=3;combat.Skills[7]=2;combat.Skills[2]=3;combat.Skills[3]=2;
            float until=Time.realtimeSinceStartup+25;
            while(!combat.Ended && combat.Elapsed<12 && Time.realtimeSinceStartup<until){if(combat.Choosing)ChooseBattleSkill(combat.Choices[0]);yield return null;}
            yield return Capture("07-mod-combat");combat.Abandon();FinishBattle();yield return Capture("08-result-with-report");
            BattleAnalysis();yield return Capture("09-battle-analysis");combat=null;CloseModal();
            var helper=new SupportSnapshot {hero=0,skin=3,power=180,showIdentity=true,name="동료 정비사",title=2,frame=3};
            OpenBattle(0,"arrival-preview",11,false,"",helper);combat.Elapsed=8;TriggerSupport();yield return Capture("10-support-arrival");combat=null;CloseModal();
            Store.Change(e=>{e.State.weekly.Add(new WeeklyProgress {week=CurrentWeek(),attempts=1,bestScore=23456});return CommandResult.Ok("Local record card UI fixture");});
            Show("weekly");WeeklyRecordCard();yield return Capture("11-weekly-record-card");
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Workshop runtime errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("WORKSHOP_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
