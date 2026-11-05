using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator ShopPolishSmokeRoutine()
        {
            DismissTitleScreen();CloseModal();
            Store.Change(e=>{e.State.testProgress=true;e.State.gold=6200;e.State.gems=900;e.State.parts=100;return CommandResult.Ok("Isolated shop QA fixture");});
            Show("home");yield return Capture("01-wallet-home");
            for(int i=0;i<3;i++)if(CurrencyArt.Icon(i)==null)smokeErrors.Add("Missing currency icon "+i);
            GuidePanel();yield return Capture("02-next-guide");CloseModal();
            shopTab=0;featuredOutfit=1;Show("shop");yield return Capture("03-style-shop");
            ClickVisibleButton("성장 · 장비");yield return Capture("04-growth-shop");
            ClickVisibleButton("2,000 골드");yield return Capture("05-balance-before-purchase");
            ClickVisibleButton("구매하기");yield return Capture("06-parts-acquired");
            if(State.gold!=4200||State.parts!=120)smokeErrors.Add("Parts purchase balance mismatch");
            ClickVisibleButton("상점으로 돌아가기");yield return null;ClickVisibleButton("확정 장비");yield return Capture("07-equipment-shop");
            if(!shopGear)smokeErrors.Add("Equipment category did not open");
            var scroll=screen.GetComponentInChildren<ScrollRect>();scroll.verticalNormalizedPosition=.35f;
            yield return Capture("08-equipment-scroll");float offset=scroll.content.anchoredPosition.y;
            if(offset<100)smokeErrors.Add("Equipment scroll did not move");
            ClickVisibleButton("보석 · 광고");yield return Capture("09-gems-and-free-reward");
            ClickVisibleButton("성장 · 장비");yield return null;
            if(Mathf.Abs(screen.GetComponentInChildren<ScrollRect>().content.anchoredPosition.y-offset)>3)smokeErrors.Add("Shop scroll position lost");
            shopTab=2;Show("shop");StoreOfferPanel(CommerceCatalog.Offers[0]);yield return Capture("10-play-offer");CloseModal();
            OutfitRoom(1);string before=JsonUtility.ToJson(State);
            ClickVisibleButton("전투 크기로 보기");var preview=modal.GetComponentInChildren<OutfitPreview>();preview.Fire();yield return Capture("11-battle-size-preview");
            if(JsonUtility.ToJson(State)!=before)smokeErrors.Add("Preview changed owned state");
            OutfitPurchase(1);yield return Capture("12-outfit-balance");ClickVisibleButton("보석 600개로 구매");yield return Capture("13-unlocked-outfit");
            if(!Outfits.Owned(State,1)||State.gems!=300)smokeErrors.Add("Outfit purchase mismatch");
            ClickVisibleButton("이 모습으로 출격 준비");yield return Capture("14-ready-to-deploy");
            if(Outfits.Active(State,0)!=1||State.selectedHero!=0)smokeErrors.Add("Equip and hero selection failed");
            OutfitAcquired(1);ClickVisibleButton("이 모습으로 대표 대원 전시");yield return Capture("15-showcase-equipped");
            PurchaseOutfit(1);if(State.gems!=300)smokeErrors.Add("Owned outfit charged again");CloseModal();
            Store.Change(e=>{e.State.gold=100;return CommandResult.Ok("Insufficient balance fixture");});
            ConfirmBuy("normal:1",Catalog.Items[1].Name,"골드 3,000");yield return Capture("16-insufficient-gold");
            foreach(var button in modal.GetComponentsInChildren<Button>())if(button.name=="재화가 부족해요"&&button.interactable)smokeErrors.Add("Insufficient balance purchase enabled");
            CloseModal();StartBattle(0);battlePaused=true;
            combat.Hazards.Add(new Hazard{id=9999,position=combat.Position,radius=80,delay=1.4f});RenderBattle();
            yield return Capture("17-warning-readability");
            if(string.IsNullOrEmpty(dangerHint.text))smokeErrors.Add("Danger prompt missing");
            combat.Hazards.Clear();RenderBattle();if(dangerHint.text!="")smokeErrors.Add("Danger prompt remained outside warning");
            while(!combat.CanUltimate&&combat.Elapsed<40)
            {combat.Enemies.Clear();combat.Tick(.05f,new V2());if(combat.Choosing)combat.Choose(combat.Choices[0]);}
            battlePaused=false;TriggerUltimate();battlePaused=true;
            yield return Capture("18-ultimate-visibility");
            // Presentation-only result fixture; not evidence of a server-verified win.
            combat.Ended=combat.Won=true;FinishBattle();yield return Capture("19-reward-icons");
            if(!State.receipts.Exists(r=>r.id==combat.RunId))smokeErrors.Add("Presentation result fixture was not saved");
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Shop polish runtime errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("SHOP_POLISH_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
