using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator EngineeringSmokeRoutine()
        {
            DismissTitleScreen();CloseModal();
            Store.Change(e=>{e.State.testProgress=true;e.State.heroes[1].unlocked=true;e.State.gems=1800;e.State.unlockedStage=41;e.State.clearedStages.AddRange(new[]{0,1,2,3,4});return CommandResult.Ok("Isolated engineering fixtures");});
            Show("home");yield return Capture("01-workshop");
            foreach(int hero in new[]{0,1})
            {
                Store.Change(e=>{e.State.selectedHero=hero;return CommandResult.Ok("Hero fixture");});
                OpenBattle(1,"engineering-"+hero,71,false);battlePaused=true;combat.Elapsed=12;combat.Position=combat.TaskPosition;combat.Hp=combat.MaxHp=100000;
                combat.ChooseTask(1);battlePaused=false;FieldAssemblyPanel();yield return Capture("02-assembly-"+hero);
                SelectFieldModule(hero==0?1:2);battlePaused=true;
                combat.Enemies.Add(new Enemy{id=999,position=combat.Position+new V2(180,0),hp=10000,maxHp=10000});combat.Tick(.05f,new V2());RenderBattle();
                yield return Capture("03-equipped-"+hero);
                if(combat.FieldModule!=(hero==0?1:2))smokeErrors.Add("Field module choice failed");
                combat=null;Show("home");
            }
            Show("shop");yield return Capture("04-style-shop");OutfitRoom(2);yield return Capture("05-rail-preview");
            string before=JsonUtility.ToJson(State);ClickVisibleButton("기본 외형과 비교");yield return Capture("06-rail-base");ClickVisibleButton("레일 레인저로 돌아가기");ClickVisibleButton("필살기");yield return Capture("07-rail-ultimate");
            if(before!=JsonUtility.ToJson(State))smokeErrors.Add("Preview mutated progression");
            OutfitPurchase(2);yield return Capture("08-purchase");ClickVisibleButton("보석 600개로 구매");
            if(State.gems!=1200 || !Outfits.Owned(State,2))smokeErrors.Add("Rail outfit purchase did not charge exact price");
            ClickVisibleButton("전용 대원에 의상 장착");if(Outfits.Active(State,1)!=2)smokeErrors.Add("Rail equip failed");
            ClickVisibleButton("지원 등장");yield return Capture("09-support-preview");
            CloseModal();OpenBattle(1,"rail-battle",71,false);battlePaused=true;combat.Tick(.05f,new V2());RenderBattle();yield return Capture("10-rail-battle");combat=null;Show("home");
            RegionCollection(1);ClickVisibleButton("수집 목표로 등록");yield return Capture("11-collection-target");
            if(State.collectionTarget!=1)smokeErrors.Add("Collection target failed");
            GemSources(1800,()=>Show("shop"));yield return Capture("12-free-and-paid-gems");
            StoreOfferPanel(CommerceCatalog.Find("ad_skip_lifetime"));yield return Capture("13-ad-skip-scope");
            var guild=new GuildView{id="fixture",name="함께 고치는 길드",week="화면 검증 전용",remaining=3,restorationToday=2};
            guild.members.Add(new GuildMember{id="fixture-owner",name="레일 정비사",hero=1,outfit=2,restorationPoints=3,level=10});
            guild.members.Add(new GuildMember{id="fixture-self",name="나의 수호대",hero=0,restorationPoints=2,level=10,self=true});
            foreach(int points in new[]{0,6,15,30}){guild.restorationPoints=points;RestorationPanel(guild);yield return Capture("14-restoration-"+points);}
            GuildPanel(guild);yield return Capture("15-guild-entry");
            var gallery=new ProfileList{featuredId="AAAAAAAAAAAA"};var profile=ScrapGuard.Core.Social.Build(State,"AAAAAAAAAAAA",false);profile.test=false;gallery.profiles.Add(profile);Gallery(gallery);yield return Capture("16-weekly-discovery");
            CloseModal();Show("home");StartBattle(4,false,SkillTrial.Mode);battlePaused=true;yield return Capture("17-skill-copy");
            if(bossText.text.Contains("최고 보상"))smokeErrors.Add("Skill mode still promises rewards");
            combat.Hp=combat.MaxHp=100000;while(combat.Elapsed<32&&!combat.Ended){if(combat.Choosing)combat.Choose(combat.Choices[0]);combat.Tick(ReplayRecorder.Step,new V2());}
            combat.Abandon();FinishBattle();ClickVisibleButton("주간 기록 카드");yield return Capture("18-skill-share");
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Engineering runtime errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("ENGINEERING_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
