using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;

namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator CampaignSmokeRoutine()
        {
            DismissTitleScreen();
            CampaignMap(4);yield return Capture("01-locked-region");
            CampaignDetails(29);yield return Capture("02-final-operation-rewards");
            CloseModal();
            Store.Change(e=>{e.State.testProgress=true;e.State.unlockedStage=Campaign.Count-1;e.State.heroes[0].level=30;foreach(var item in e.State.items)item.level=20;return CommandResult.Ok("Isolated campaign fixture");});
            for(int region=0;region<Campaign.Regions.Length;region++)
            {
                selectedStage=region*6;
                CampaignMap(region);yield return Capture("region-"+region+"-map");CloseModal();
                StartBattle(selectedStage);
                float deadline=Time.realtimeSinceStartup+30;
                while(!combat.Ended && combat.Elapsed<10 && Time.realtimeSinceStartup<deadline)
                {
                    if(combat.Choosing)ChooseBattleSkill(combat.Choices[0]);
                    yield return null;
                }
                if(combat.Elapsed<8)smokeErrors.Add("Campaign did not advance in region "+region);
                yield return Capture("region-"+region+"-battle");
                // Showcase the role tell in this isolated local profile; this is not a gameplay reward fixture.
                combat.Enemies.Add(new Enemy {id=90000,kind=3,finalBoss=true,role=region==0?EnemyRole.Charger:region==1?EnemyRole.Artillery:region==2?EnemyRole.Bomber:region==3?EnemyRole.Shield:EnemyRole.Summoner,position=new V2(270,310),hp=10000,maxHp=10000,speed=0,windup=.8f});
                yield return null;yield return Capture("region-"+region+"-boss");
                combat.Abandon();FinishBattle();yield return null;combat=null;CloseModal();
            }
            selectedStage=29;Show("home");yield return Capture("03-expanded-home");
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Campaign runtime errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("CAMPAIGN_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
