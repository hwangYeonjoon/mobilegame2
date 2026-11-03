using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator ExperienceSmokeRoutine()
        {
            DismissTitleScreen();CloseModal();
            Store.Change(e=>{e.State.testProgress=true;e.State.unlockedStage=41;e.State.gems=1200;return CommandResult.Ok("Isolated experience fixtures");});
            OpenBattle(0,"experience-intro",17,false);combat.MaxHp=combat.Hp=100000;
            yield return Capture("01-first-battle-landmark");
            float deadline=Time.realtimeSinceStartup+35;
            while(!combat.Choosing && Time.realtimeSinceStartup<deadline)yield return null;
            if(!combat.Choosing)smokeErrors.Add("Early choice did not open");
            else
            {
                if(combat.Elapsed>8.3f)smokeErrors.Add("First choice too late");
                if(!choicesShown)ShowChoices();yield return Capture("02-first-choice");ChooseBattleSkill(1);
            }
            deadline=Time.realtimeSinceStartup+40;
            while(!combat.Choosing && Time.realtimeSinceStartup<deadline)yield return null;
            if(combat.Choosing){if(!choicesShown)ShowChoices();yield return Capture("03-second-choice");ChooseBattleSkill(combat.Choices[0]);}
            combat=null;CloseModal();Show("home");
            StartBattle(1);battlePaused=true;combat.MaxHp=combat.Hp=100000;
            while(combat.Elapsed<12){combat.Enemies.Clear();combat.Tick(ReplayRecorder.Step,new V2());if(combat.Choosing)combat.Choose(combat.Choices[0]);}
            combat.Position=combat.TaskPosition;RenderBattle();battlePaused=false;OpenFieldChoice();
            yield return Capture("04-salvage-choice");int beforeGold=State.gold;SelectFieldChoice(2);battlePaused=true;
            if(State.gold!=beforeGold)smokeErrors.Add("Task paid before settlement");
            while(!combat.TaskCompleted && combat.Elapsed<35)
            {combat.Enemies.Clear();combat.Tick(ReplayRecorder.Step,new V2());if(combat.Choosing)combat.Choose(combat.Choices[0]);}
            RenderBattle();yield return Capture("05-risk-recovered");
            if(!combat.TaskCompleted)smokeErrors.Add("Risk task did not complete");
            // Isolated reward UI fixture; server replay tests exercise real earned wins separately.
            combat.Ended=combat.Won=true;FinishBattle();yield return Capture("06-field-reward");
            if(State.gold-beforeGold!=Campaign.Get(1).Gold+400)smokeErrors.Add("Field reward UI fixture mismatch");
            combat=null;Show("home");
            foreach(int stage in new[]{2,3})
            {
                OpenBattle(stage,"objective-"+stage,27,false);battlePaused=true;combat.Elapsed=12;combat.MaxHp=combat.Hp=100000;
                for(int i=0;i<280;i++){combat.Enemies.Clear();combat.Bullets.Clear();combat.Position=combat.TaskPosition;combat.Tick(ReplayRecorder.Step,new V2());if(combat.Choosing)combat.Choose(combat.Choices[0]);}
                RenderBattle();yield return Capture(stage==2?"07-defense":"08-escort");combat=null;Show("home");
            }
            ModBook();yield return Capture("09-mod-book");var scroll=modal.GetComponentInChildren<ScrollRect>();scroll.verticalNormalizedPosition=0;yield return Capture("10-new-mods");CloseModal();
            OpenBattle(5,"six-mods",27,false);battlePaused=true;for(int i=0;i<8;i++)combat.Skills[i]=5;
            combat.Enemies.Add(new Enemy{id=999,kind=2,position=new V2(390,480),hp=100000,maxHp=100000});
            combat.Tick(.05f,new V2());RenderBattle();yield return Capture("11-evolved-combat");combat=null;Show("home");
            OutfitRoom();string before=JsonUtility.ToJson(State);ClickVisibleButton("기본 외형과 비교");yield return Capture("12-original-comparison");
            var preview=modal.GetComponentInChildren<OutfitPreview>();if(!preview.BaseLook)smokeErrors.Add("Outfit comparison failed");
            ClickVisibleButton("중장비 의상으로 돌아가기");if(preview.BaseLook)smokeErrors.Add("Comparison did not restore outfit");yield return Capture("13-outfit-comparison");
            if(before!=JsonUtility.ToJson(State))smokeErrors.Add("Comparison modified ownership or wallet");
            CloseModal();Store.Change(e=>{e.State.ownedCosmetics.Add(Outfits.Key);Outfits.Equip(e.State,1);return CommandResult.Ok("Gallery fixture");});
            Show("showcase");ClickVisibleButton("확대 감상");yield return Capture("14-large-guardian");
            ClickVisibleButton("이동");yield return Capture("15-walk-preview");ClickVisibleButton("필살기");yield return Capture("16-ultimate-preview");
            var guild=new GuildView{id="fixture",name="테스트 전시 길드",week="실행 검증 전용",featuredId="fixture-owner",remaining=3};
            guild.members.Add(new GuildMember{id="fixture-owner",name="중장비 정비사",hero=0,level=15,outfit=1,supportUses=5,supportWins=3,helpedMembers=2});
            guild.members.Add(new GuildMember{id="fixture-self",name="내 냉각 대원",hero=4,level=12,skin=6,self=true,supportUses=2,supportWins=1,helpedMembers=1});
            SupportHall(guild);yield return Capture("17-support-hall");ClickVisibleButton("대원 크게 보기");yield return Capture("18-guild-portrait");
            foreach(var button in modal.GetComponentsInChildren<Button>())if(button.name=="사진 카드 공유" && button.interactable)smokeErrors.Add("Guild-only portrait export enabled");
            guild.featuredId=null;foreach(var member in guild.members)member.supportUses=member.supportWins=member.helpedMembers=0;
            SupportHall(guild);yield return Capture("19-empty-support-hall");
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Experience runtime errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("EXPERIENCE_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
