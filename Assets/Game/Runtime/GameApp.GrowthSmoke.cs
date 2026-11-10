using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator GrowthSmokeRoutine()
        {
            DismissTitleScreen();CloseModal();
            Store.Change(e=>{e.State.gems=2000;e.State.gold=1800;e.State.parts=30;e.State.testProgress=true;return CommandResult.Ok("Isolated UI fixture, not human or balance evidence");});
            Show("home");yield return Capture("01-home-growth-ready");
            ClickVisibleButton("강화 가능 ●");yield return Capture("02-upgrade-before");
            int attack=Catalog.Attack(State);ClickVisibleButton("강화");yield return Capture("03-upgrade-after");
            if(Catalog.Attack(State)<=attack||State.gold!=1500)smokeErrors.Add("Upgrade preview/action mismatch");
            string before=JsonUtility.ToJson(State);GrowthKitPanel();yield return Capture("04-kit-preview");
            if(before!=JsonUtility.ToJson(State))smokeErrors.Add("Kit preview mutated wallet");
            var kit=Growth.KitQuote(State,Growth.CurrentKit(State).id,0);int kitGold=State.gold,kitParts=State.parts;
            GrowthKitConfirm(kit.offer.id,0);yield return Capture("04b-kit-confirm");
            ClickVisibleButton("100 보석 구매 · "+kit.after.count+"단계 강화");yield return Capture("05-kit-use-now");
            if(State.gold!=kitGold+kit.offer.gold-kit.after.gold||State.parts!=kitParts+kit.offer.parts-kit.after.parts||State.gems!=1900||Catalog.Attack(State)!=kit.after.attack)smokeErrors.Add("Kit atomic upgrade mismatch");
            ClickVisibleButton("이 장비로 1작전 출격");yield return null;
            if(combat==null||combat.Stage!=0||combat.Damage!=Catalog.Attack(State))smokeErrors.Add("Purchased stats not used in retry");
            battlePaused=true;RenderBattle();yield return Capture("05b-kit-retry");combat.Abandon();FinishBattle();combat=null;
            GrowthPassPanel();yield return Capture("06-pass-free-and-premium");
            ClickVisibleButton("추가 트랙 900 보석 · 구성 확인");yield return Capture("07-pass-confirmation");
            ClickVisibleButton("900 보석으로 해금");yield return Capture("08-pass-owned");
            if(!Growth.PassOwned(State)||State.gems!=1000)smokeErrors.Add("Pass purchase mismatch");
            Store.Change(e=>{for(int i=0;i<12;i++)if(!e.State.clearedStages.Contains(i))e.State.clearedStages.Add(i);e.State.unlockedStage=12;return CommandResult.Ok("Earned milestones UI fixture only");});
            GrowthPassPanel();yield return null;modal.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=0;yield return Capture("09-pass-final-tier");
            ClaimGrowthReward(3,true);yield return Capture("10-pass-outfit-acquired");
            if(!Outfits.Owned(State,1)||State.growthPassPremium!=8)smokeErrors.Add("Final tier missing outfit/receipt");
            CloseModal();shopTab=1;shopGear=false;Show("shop");yield return Capture("11-growth-shop");
            screen.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=0;yield return Capture("12-growth-shop-options");
            Store.Change(e=>{e.State.gold=0;e.State.parts=0;return CommandResult.Ok("Shortage fixture");});RefreshWallet();
            GrowthPanel();ClickVisibleButton("부족분");yield return Capture("13-free-and-optional-paths");
            CloseModal();shopTab=2;Show("shop");yield return Capture("14-repair-ad-offer");
            GrowthPanel();ClickVisibleButton("보스 패턴 무료 연습");yield return Capture("15-boss-selector");
            StartBattle(1,false,BossTraining.Mode);yield return null;
            if(combat==null||!combat.BossPractice||!combat.BossArenaActive||combat.Skills[4]!=3||Mathf.Abs(combat.Damage-Catalog.Attack(State)*1.6f)>.001f)smokeErrors.Add("Practice preset/start mismatch: damage="+(combat==null?"missing":combat.Damage.ToString("R"))+" expected="+(Catalog.Attack(State)*1.6f).ToString("R"));
            battlePaused=true;RenderBattle();yield return Capture("16-boss-practice");
            combat.Abandon();FinishBattle();yield return Capture("17-practice-result");combat=null;
            SalvagePanel();yield return Capture("18-free-salvage");
            Show("weekly");yield return Capture("19-weekly-hub");RobotWeekPanel(CurrentWeek());yield return Capture("20-weekly-contract");
            StartBattle(4,false,Endgame.WeeklyMode);yield return null;battlePaused=true;
            int design=combat.WeeklyFrame*4+1;RobotAssembler(design);yield return Capture("21-weekly-assembly");
            ConfirmRobotPlan(design,true);battlePaused=true;RenderBattle();yield return Capture("22-weekly-machine");
            if(combat.MachineTime<=0||combat.RobotRank(design)!=1)smokeErrors.Add("Weekly robot control failed");
            combat.Abandon();FinishBattle();combat=null;
            Store.Change(e=>{for(int i=0;i<Campaign.Count;i++)if(!e.State.clearedStages.Contains(i))e.State.clearedStages.Add(i);e.State.unlockedStage=Campaign.Count-1;return CommandResult.Ok("Completed campaign UI fixture only");});
            Show("home");yield return Capture("23-campaign-complete-home");
            GrowthKitPanel();yield return Capture("42-late-kit-comparison");
            GrowthKitCatalog();yield return Capture("43-kit-public-catalog");
            GrowthKitConfirm(Growth.CurrentKit(State).id,3);yield return Capture("44-late-kit-confirm");
            yield return Content034Smoke();
            yield return ReplayExpansionCapture();
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Growth UI errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("GROWTH_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
