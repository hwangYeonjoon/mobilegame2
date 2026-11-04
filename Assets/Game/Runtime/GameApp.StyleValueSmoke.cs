using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator StyleValueSmokeRoutine()
        {
            DismissTitleScreen();CloseModal();
            int fixtureGems=Outfits.Count*Outfits.Price+2400;
            Store.Change(e=>{e.State.testProgress=true;e.State.gems=fixtureGems;e.State.gold=30000;e.State.parts=1000;return CommandResult.Ok("Isolated presentation fixtures");});
            Show("shop");PurchaseSupply("hero","돌격수 레이");yield return Capture("01-recruited");
            if(!State.heroes[1].unlocked || State.gems!=fixtureGems-1200)smokeErrors.Add("Recruit acquisition receipt mismatch");
            ClickVisibleButton("레이를 출격 대원으로 선택");HeroUpgrade();ClickVisibleButton("레벨업");yield return Capture("02-growth-result");
            if(State.heroes[1].level!=2)smokeErrors.Add("Growth feedback changed upgrade flow");
            PurchaseSupply("rare:2","희귀 충격 발사기");yield return Capture("03-equipment-received");ClickVisibleButton("장비 확인 · 장착");yield return Capture("04-item-details");
            string weapon=State.equipped[0];ItemDetail(weapon);ClickVisibleButton("장비 강화  +1");yield return Capture("05-upgrade-result");
            Store.Change(e=>{foreach(var hero in e.State.heroes)hero.unlocked=true;return CommandResult.Ok("Unlocked roster fixture");});
            int savedGems=State.gems;Store.Change(e=>{e.State.gems=0;return CommandResult.Ok("Failed purchase fixture");});
            PurchaseOutfit(1);yield return Capture("05a-insufficient-funds");
            if(Outfits.Owned(State,1)||State.gems!=0)smokeErrors.Add("Failed purchase granted outfit");
            foreach(var text in Root.GetComponentsInChildren<Text>())if(text.text=="새 의상을 수집했어요")smokeErrors.Add("Failed purchase celebrated success");
            Store.Change(e=>{e.State.gems=savedGems;return CommandResult.Ok("Restore fixture wallet");});
            for(int id=1;id<=Outfits.Count;id++)
            {
                int gems=State.gems;PurchaseOutfit(id);yield return Capture("06-acquired-"+id);
                if(!Outfits.Owned(State,id)||State.gems!=gems-600)smokeErrors.Add("Acquisition charged incorrectly "+id);
                ClickVisibleButton("전용 대원에 의상 장착");yield return Capture("07-equipped-"+id);
                ClickVisibleButton("이 모습으로 대표 대원 전시");yield return Capture("08-showcase-"+id);
                if(State.showcaseHero!=Outfits.Hero(id)||Outfits.Active(State,State.showcaseHero)!=id)smokeErrors.Add("Showcase not wearing acquired outfit");
                string before=JsonUtility.ToJson(State);
                OutfitRoom(id);ClickVisibleButton("전투 크기로 보기");ClickVisibleButton("사격");yield return Capture("09-battle-size-"+id);
                ClickVisibleButton("필살기");yield return Capture("09a-ultimate-"+id);
                ClickVisibleButton("기본 외형과 비교");yield return Capture("10-base-size-"+id);
                var profile=ScrapGuard.Core.Social.Build(State,"",true);GuardianGallery(profile,()=>Show("showcase"));ClickVisibleButton("사격");yield return Capture("11-gallery-shot-"+id);
                ClickVisibleButton("지원");yield return Capture("12-gallery-support-"+id);
                if(before!=JsonUtility.ToJson(State))smokeErrors.Add("Preview changed save "+id);
                CloseModal();Store.Change(e=>{e.State.selectedHero=Outfits.Hero(id);return CommandResult.Ok("Selected hero fixture");});
                var helper=new SupportSnapshot{hero=Outfits.Hero(id),outfit=id,showIdentity=true,name="동료 수호자",power=50};
                OpenBattle(1,"style-fixture-"+id,6,false,"",helper);battlePaused=true;combat.Elapsed=9;
                for(int j=0;j<4;j++)combat.Bullets.Add(new Bullet{id=900+j,position=new V2(170+j*65,440+j*16),velocity=new V2(1,0),life=1,damage=1,chilling=id==4});
                combat.Hazards.Add(new Hazard{id=950,position=combat.Position,radius=62,delay=1.5f});
                RenderBattle();yield return Capture("13-live-battle-"+id);
                battlePaused=false;TriggerSupport();battlePaused=true;
                // Advance the presentation clock enough to reveal the entrance, keeping combat paused.
                combat.Elapsed+=.3f;RenderBattle();yield return Capture("14-live-support-"+id);
                if(hazardLayer.GetSiblingIndex()<=effectLayer.GetSiblingIndex())smokeErrors.Add("Warning layer obscured by cosmetic effects");
                combat=null;Show("home");
            }
            int balance=State.gems;PurchaseOutfit(4);if(State.gems!=balance)smokeErrors.Add("Repeated purchase deducted currency");CloseModal();
            var privateHelper=new SupportSnapshot{hero=4,outfit=4,skin=2,name="MUST_NOT_LEAK",showIdentity=false,power=50};
            OpenBattle(1,"private-helper",6,false,"",privateHelper);combat.Elapsed=9;TriggerSupport();battlePaused=true;combat.Elapsed+=.3f;RenderBattle();yield return Capture("15-private-support");
            foreach(var label in screen.GetComponentsInChildren<Text>())if(label.text.Contains("MUST_NOT_LEAK"))smokeErrors.Add("Private support identity leaked");
            if(supportArt.GetComponent<OutfitAttachment>()!=null)smokeErrors.Add("Private support outfit leaked");
            combat=null;Show("home");
            var gallery=new ProfileList();for(int i=1;i<=4;i++)gallery.profiles.Add(new PublicProfile{id="FIXTURE"+i,name="전시 테스트 "+i,hero=Outfits.Hero(i),outfit=i,heroLevel=10,test=true});
            gallery.featuredId=gallery.profiles[0].id;Gallery(gallery);yield return Capture("16-discovery-gallery");
            CloseModal();Show("showcase");yield return ExportCard(showcaseCard);
            if(string.IsNullOrEmpty(lastSharedPath)||!File.Exists(lastSharedPath))smokeErrors.Add("Missing updated export card");
            else File.Copy(lastSharedPath,Path.Combine(smokeOutput,"17-export-card.png"),true);
            Screen.SetResolution(360,800,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.7f);
            shopSupplies=false;Show("shop");yield return Capture("18-small-screen-shop");
            ClickVisibleButton("다음");yield return Capture("18a-next-outfit");
            OutfitCollection();ClickVisibleButton(Outfits.Collections[Outfits.LatestCollection]);yield return Capture("18b-new-collection");
            OutfitRoom(Outfits.Count);yield return Capture("18c-small-screen-outfit");
            GuardianGallery(ScrapGuard.Core.Social.Build(State,"",true),()=>Show("showcase"));ClickVisibleButton("전투 크기로 보기");yield return Capture("19-small-screen-gallery");
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Style value fixture runtime errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("STYLE_VALUE_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
