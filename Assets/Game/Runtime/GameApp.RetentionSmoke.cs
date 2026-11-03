using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator RetentionSmokeRoutine()
        {
            DismissTitleScreen();CloseModal();
            Store.Change(e=>{e.State.testProgress=true;e.State.gems=2400;e.State.unlockedStage=41;return CommandResult.Ok("Isolated retention UI fixtures");});
            Show("shop");yield return Capture("01-storm-shop");featuredOutfit=4;Show("shop");yield return Capture("02-aurora-shop");
            OutfitCollection();yield return Capture("03-collection-locked");
            OutfitRoom(3);ClickVisibleButton("수집 목표로 저장 · 자동 구매 없음");
            if(State.outfitTarget!=3 || State.gems!=2400)smokeErrors.Add("Goal unexpectedly changed currency");
            yield return Capture("04-locked-goal");
            Store.Change(e=>{foreach(var hero in e.State.heroes)hero.unlocked=true;return CommandResult.Ok("Unlocked roster fixture");});
            foreach(int outfit in new[]{3,4})
            {
                OutfitRoom(outfit);yield return Capture("05-preview-"+outfit);string before=JsonUtility.ToJson(State);
                ClickVisibleButton("기본 외형과 비교");yield return Capture("06-base-"+outfit);
                ClickVisibleButton(Outfits.Label(outfit)+"로 돌아가기");ClickVisibleButton("필살기");yield return Capture("07-ultimate-"+outfit);
                ClickVisibleButton("지원 등장");yield return Capture("08-support-"+outfit);
                if(before!=JsonUtility.ToJson(State))smokeErrors.Add("Outfit preview changed save");
                OutfitPurchase(outfit);yield return Capture("09-purchase-"+outfit);ClickVisibleButton("보석 600개로 구매");ClickVisibleButton("전용 대원에 의상 장착");
                if(Outfits.Active(State,Outfits.Hero(outfit))!=outfit)smokeErrors.Add("New outfit equip failed");
            }
            if(State.gems!=1200 || Outfits.Active(State,3)!=3 || Outfits.Active(State,4)!=4 || State.outfitTarget!=0)smokeErrors.Add("Independent wardrobe purchase or goal failed");
            OutfitCollection();yield return Capture("10-collection-owned");modal.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=0;yield return Capture("11-collection-bottom");
            OutfitRoom(3);ClickVisibleButton("의상 해제 · 기존 도색 표시");if(Outfits.Active(State,3)!=0 || Outfits.Active(State,4)!=4)smokeErrors.Add("Clearing one hero changed another");
            Change(new GameCommand("outfit_equip"){skin=3});
            foreach(int hero in new[]{2,3,4})
            {
                CloseModal();Store.Change(e=>{e.State.selectedHero=hero;e.State.showcaseHero=hero;return CommandResult.Ok("Engineering fixture");});
                OpenBattle(7,"retention-"+hero,6,false);battlePaused=true;combat.Elapsed=12;combat.Position=combat.TaskPosition;combat.Hp=combat.MaxHp=100000;
                combat.ChooseTask(1);battlePaused=false;FieldAssemblyPanel();yield return Capture("12-module-choice-"+hero);
                SelectFieldModule(hero==2?3:hero==3?1:2);battlePaused=true;
                combat.Enemies.Add(new Enemy{id=999,position=combat.Position+new V2(130,0),hp=100000,maxHp=100000});
                for(int frame=0;frame<45;frame++){if(combat.Choosing)combat.Choose(combat.Choices[0]);combat.Tick(.05f,new V2());}
                RenderBattle();yield return Capture("13-module-battle-"+hero);
                if(combat.EngineeringPulses==0)smokeErrors.Add("Hero module did not activate "+hero);
                PauseBattle();yield return Capture("14-operation-condition-"+hero);CloseModal();combat=null;Show("home");
            }
            CampaignDetails(7);yield return Capture("15-operation-details");
            var guild=new GuildView{id="fixture",name="계속 고치는 길드",week="화면 테스트",restorationPoints=61,restorationToday=3,memorialWeeks=2};
            guild.memorials.Add(new GuildMemorial{week="2026-11-02",tier=2,points=61});guild.memorials.Add(new GuildMemorial{week="2026-10-26",tier=3,points=120});
            guild.members.Add(new GuildMember{id="fixture-helper",name="눈꽃 정비사",hero=4,outfit=4,level=10,restorationPoints=3});
            RestorationPanel(guild);yield return Capture("16-stretch-goal");ClickVisibleButton("영구 기념관 · 2주 달성");yield return Capture("17-memorial-history");
            guild.restorationPoints=0;RestorationPanel(guild);yield return Capture("18-new-week-keeps-history");
            CloseModal();Show("showcase");yield return Capture("19-aurora-profile");
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Retention runtime errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("RETENTION_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
