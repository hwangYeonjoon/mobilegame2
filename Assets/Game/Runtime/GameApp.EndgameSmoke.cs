using System;
using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
using Social=ScrapGuard.Core.Social;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator EndgameSmokeRoutine()
        {
            DismissTitleScreen();
            // Isolated test profile only, explicitly marked as local test progress.
            Store.Change(e=>{e.State.testProgress=true;e.State.clearedStages=new System.Collections.Generic.List<int>{0,1,2,3,4};e.State.unlockedStage=4;e.State.heroes[0].level=30;foreach(var item in e.State.items)item.level=20;return CommandResult.Ok("Test fixture");});
            Show("home");yield return Capture("01-expanded-home");
            Show("weekly");yield return Capture("02-core-rift");
            CoreExchange();yield return Capture("03-exclusive-exchange");
            OpenGuild();yield return Capture("04-guild-benefits");
            WeeklyRewards(CurrentWeek());yield return Capture("05-reward-tiers");CloseModal();
            StartBattle(4,false,Endgame.WeeklyMode);
            float until=Time.realtimeSinceStartup+100;bool captured=false;
            while(!combat.Ended && combat.Elapsed<36 && Time.realtimeSinceStartup<until)
            {
                if(combat.Choosing)ChooseBattleSkill(combat.Choices[0]);
                if(combat.CanUltimate)TriggerUltimate();
                if(combat.Elapsed>12 && !captured){yield return Capture("06-rift-combat");captured=true;}
                yield return null;
            }
            if(!combat.Ended)combat.Abandon();FinishBattle();yield return Capture("07-rift-result");
            if(combat.Elapsed<30 || State.weekly.Count!=1 || State.weekly[0].bestScore!=combat.CoreScore)smokeErrors.Add("Local weekly settlement failed");
            combat=null;Show("weekly");
            Change(new GameCommand("weekly_claim"){value=CurrentWeek(),stage=0});
            if(State.coreShards!=1)smokeErrors.Add("Participation shards not awarded once");
            WeeklyRewards(CurrentWeek());yield return Capture("08-claimed-reward");
            // Enough shards to inspect both cosmetic exchange paths; never touches server data.
            Store.Change(e=>{e.State.coreShards=32;return CommandResult.Ok("Cosmetic UI fixture");});
            ConfirmCoreExchange("frame_core","코어 궤도",20);yield return Capture("09-confirm-exchange");ClickVisibleButton("교환 확정");
            ConfirmCoreExchange("title_rift","균열의 개척자",12);ClickVisibleButton("교환 확정");
            if(State.coreShards!=0 || !Social.HasFrame(State,4) || !Social.HasTitle(State,5))smokeErrors.Add("Exclusive exchange UI failed");
            Change(new GameCommand("customize"){hero=0,title=5,frame=4,skin=0});Show("showcase");yield return Capture("10-rift-showcase");
            TrophyRoom(Social.Build(State));yield return Capture("11-trophy-room");
            CustomizeProfile();yield return Capture("12-expanded-customization");
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Endgame runtime errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("ENDGAME_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
