using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator UiCompleteSmokeRoutine()
        {
            DismissTitleScreen();CloseModal();
            Store.Change(e=>{e.State.testProgress=true;e.State.gold=10000;e.State.gems=900;e.State.parts=100;return CommandResult.Ok("Isolated UI QA");});
            for(int i=0;i<12;i++)if(InventoryArt.Equipment(i)==null)smokeErrors.Add("Missing equipment art "+i);
            for(int i=0;i<8;i++)if(InventoryArt.Skill(i)==null)smokeErrors.Add("Missing skill art "+i);
            for(int i=0;i<5;i++)if(InventoryArt.Pack(i)==null)smokeErrors.Add("Missing pack art "+i);
            Show("home");yield return Capture("01-workshop-home");
            foreach(var button in screen.GetComponentsInChildren<Button>())if(button.name=="출격하기  →")
            {
                var bounds=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(bounds);
                if(bounds[0].y<Screen.safeArea.yMin||bounds[1].y>Screen.safeArea.yMax||button.GetComponentInParent<ScrollRect>()!=null)smokeErrors.Add("Launch action must stay fully visible outside scrolling content");
            }
            var corners=new Vector3[4];screen.GetWorldCorners(corners);
            if(Mathf.Abs(corners[0].y-Screen.safeArea.yMin)>2||Mathf.Abs(corners[1].y-Screen.safeArea.yMax)>2)smokeErrors.Add("Menu did not fill safe height");
            Show("gear");yield return Capture("02-equipment-art");
            ItemDetail(State.equipped[0]);yield return Capture("03-item-detail");
            shopTab=1;shopGear=true;Show("shop");yield return Capture("04-gear-shop");
            string before=JsonUtility.ToJson(State);
            ConfirmBuy("normal:1",Catalog.Items[1].Name,"골드 3,000");yield return Capture("05-comparison-weapon");
            if(before!=JsonUtility.ToJson(State))smokeErrors.Add("Comparison changed inventory or currency");
            ClickVisibleButton("구매하기");yield return Capture("06-purchased-gear");
            if(State.gold!=7000||!State.items.Exists(i=>i.definition==1))smokeErrors.Add("Gear purchase failed");
            ConfirmBuy("normal:8",Catalog.Items[8].Name,"골드 3,000");yield return Capture("07-comparison-armor");
            ConfirmBuy("normal:10",Catalog.Items[10].Name,"골드 3,000");yield return Capture("08-comparison-module");
            shopTab=2;Show("shop");yield return Capture("09-gem-packs-top");
            screen.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=0;yield return Capture("10-gem-packs-bottom");
            StoreOfferPanel(CommerceCatalog.Offers[3]);yield return Capture("11-pack-detail");
            shopTab=0;featuredOutfit=1;Show("shop");yield return Capture("12-style-thumbnails");
            screen.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=0;yield return Capture("13-style-earned-rewards");
            outfitHeroFilter=-1;outfitOwnedFilter=0;OutfitCollection();yield return Capture("14-collection-grid");
            if(FilteredOutfits(-1).Count!=Outfits.Count)smokeErrors.Add("All outfit filter omitted items");
            outfitHeroFilter=0;OutfitCollection();yield return Capture("15-hero-filter");
            foreach(int id in FilteredOutfits(-1))if(Outfits.Hero(id)!=0)smokeErrors.Add("Hero filter mismatch");
            outfitOwnedFilter=1;OutfitCollection();yield return Capture("16-empty-owned-filter");
            if(FilteredOutfits(-1).Count!=0)smokeErrors.Add("Unowned item in owned filter");
            outfitHeroFilter=-1;outfitOwnedFilter=0;
            OutfitRoom(1);before=JsonUtility.ToJson(State);ClickVisibleButton("전투 크기로 보기");
            modal.GetComponentInChildren<OutfitPreview>().Fire();yield return Capture("17-compact-battle-preview");
            if(before!=JsonUtility.ToJson(State))smokeErrors.Add("Preview changed progression");
            OutfitPurchase(1);ClickVisibleButton("보석 600개로 구매");yield return Capture("18-outfit-acquired");
            if(!Outfits.Owned(State,1)||State.gems!=300)smokeErrors.Add("Outfit purchase balance mismatch");
            ClickVisibleButton("이 모습으로 출격 준비");yield return Capture("19-outfit-ready");
            outfitOwnedFilter=2;OutfitCollection();yield return Capture("20-equippable-filter");
            if(FilteredOutfits(-1).Count!=1||FilteredOutfits(-1)[0]!=1)smokeErrors.Add("Equippable filter mismatch");
            outfitOwnedFilter=0;OutfitAcquired(1);ClickVisibleButton("이 모습으로 대표 대원 전시");yield return Capture("21-showcase");
            var scroller=screen.GetComponentInChildren<ScrollRect>();scroller.verticalNormalizedPosition=0;yield return Capture("22-showcase-actions");
            yield return ExportCard(showcaseCard);
            if(string.IsNullOrEmpty(lastSharedPath)||!File.Exists(lastSharedPath))smokeErrors.Add("Card export missing");
            else File.Copy(lastSharedPath,Path.Combine(smokeOutput,"23-export-full-card.png"),true);
            ProfileEquipment(ScrapGuard.Core.Social.Build(State,"",true));yield return Capture("24-profile-equipment");
            PublicPanel(ScrapGuard.Core.Social.Build(State,"UIQA00000001",true));yield return Capture("24b-public-profile");
            ConfirmBuy("rare:2",Catalog.Items[2].Name,"보석 600");yield return Capture("24c-insufficient-gems");
            bool blocked=false;
            foreach(var button in modal.GetComponentsInChildren<Button>())if(button.name=="재화가 부족해요")blocked=!button.interactable;
            if(!blocked||State.gems!=300)smokeErrors.Add("Insufficient gem purchase was not blocked");
            ModBook();yield return Capture("25-skill-recipe-icons");CloseModal();yield return null;
            StartBattle(0);battlePaused=true;combat.Skills[0]=2;combat.Skills[1]=3;combat.Skills[5]=1;
            for(int i=0;i<6;i++)combat.Enemies.Add(new Enemy{id=8000+i,kind=0,role=(EnemyRole)i,hp=100,maxHp=100,position=new V2(105+i%3*165,350+i/3*280),windup=i==1?.5f:0});
            combat.Hazards.Add(new Hazard{id=9999,position=combat.Position,radius=80,delay=1.4f});RenderBattle();yield return Capture("26-battle-hud-warning");
            if(string.IsNullOrEmpty(dangerHint.text)||skillHudLevels[1].text!="3")smokeErrors.Add("Battle warning or HUD level missing");
            combat.Choices=new[]{0,1,5};ShowChoices();yield return Capture("27-skill-selection");CloseModal();yield return null;
            for(int region=1;region<Campaign.Regions.Length;region++)
            {
                Destroy(scenery.gameObject);scenery=ArenaScenery.Build(arena,region*6,false);scenery.transform.SetAsFirstSibling();
                scenery.Render(10);yield return Capture("28-region-"+region);
            }
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"UI complete runtime errors: "+smokeErrors.Count+"\nResolution: "+Screen.width+"x"+Screen.height+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("UI_COMPLETE_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
