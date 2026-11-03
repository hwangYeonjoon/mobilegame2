using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator ExpansionSmokeRoutine()
        {
            DismissTitleScreen();Show("heroes");
            yield return null;screen.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=0;
            yield return Capture("01-locked-recruits");
            // Local fixtures exercise late-game UI; never imported into server accounts.
            Store.Change(e=>
            {
                e.State.testProgress=true;e.State.tutorialCompleted=true;
                for(int i=0;i<30;i++)if(!e.State.clearedStages.Contains(i))e.State.clearedStages.Add(i);
                e.State.unlockedStage=41;foreach(var h in e.State.heroes)h.level=30;foreach(var item in e.State.items){item.level=20;item.rarity=2;}
                return CommandResult.Ok("Expansion UI fixture");
            });
            for(int hero=3;hero<5;hero++)
            {
                Show("heroes");yield return null;screen.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=0;
                ClickVisibleButton("무료 모집 · 작전 보상");
                if(!State.heroes[hero].unlocked)smokeErrors.Add("Recruitment UI failed "+hero);
            }
            Show("heroes");yield return null;screen.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=0;
            yield return Capture("02-recruited-heroes");
            Wardrobe(1,3);yield return Capture("03-spark-amber");Wardrobe(2,4);yield return Capture("04-iris-frost");
            Change(new GameCommand("customize"){hero=4});CustomizeProfile();yield return Capture("05-five-hero-showcase");
            SupportRegistration();yield return Capture("06-five-hero-support");CloseModal();
            for(int region=5;region<7;region++)
            {
                CampaignMap(region);yield return Capture("map-"+region);
                Store.Change(e=>{Workshop.Region(e.State,region).blueprints=8;return CommandResult.Ok("Collection fixture");});
                Change(new GameCommand("region_craft"){stage=region});RegionCollection(region);yield return Capture("collection-"+region);CloseModal();
                Change(new GameCommand("hero_select"){hero=region-2});
                OpenBattle(Campaign.FinalStage(region).Id,"boss-preview-"+region,17,false);battlePaused=true;
                combat.Enemies.Clear();var boss=new Enemy{id=9999,kind=3,finalBoss=true,role=Campaign.BossRole(region),position=new V2(270,350),hp=50000,maxHp=50000};combat.Enemies.Add(boss);
                for(int pose=0;pose<4;pose++)
                {
                    boss.windup=pose==1?1:0;boss.attackFlash=pose==2?1:0;boss.flash=pose==3?1:0;RenderBattle();yield return Capture("boss-"+region+"-pose-"+pose);
                }
                combat=null;Show("home");
            }
            for(int hero=3;hero<5;hero++)
            {
                Change(new GameCommand("hero_select"){hero=hero});StartBattle(hero==3?30:36);
                float until=Time.realtimeSinceStartup+45;bool captured=false;
                while(!combat.Ended && combat.Elapsed<28 && Time.realtimeSinceStartup<until)
                {
                    if(combat.Choosing)ChooseBattleSkill(combat.Choices[0]);
                    if(combat.CanUltimate && !captured){TriggerUltimate();yield return Capture("ultimate-"+hero);captured=true;}
                    yield return null;
                }
                if(!captured)smokeErrors.Add("New ultimate not exercised "+hero);
                combat.Abandon();FinishBattle();BattleAnalysis();yield return Capture("analysis-"+hero);combat=null;CloseModal();Show("home");
            }
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Expansion runtime errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("EXPANSION_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
