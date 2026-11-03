using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator OutfitSmokeRoutine()
        {
            DismissTitleScreen();GuidePanel();yield return Capture("01-guide");CloseModal();
            shopSupplies=false;Show("shop");yield return Capture("00-outfit-shop");
            string before=JsonUtility.ToJson(State);ClickVisibleButton("움직여 보기  ·  ◆ 600");yield return Capture("02-outfit-room");
            var preview=modal.GetComponentInChildren<OutfitPreview>();
            var rect=(RectTransform)preview.transform;
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector3(330,-235,0)))};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(hits.Count==0 || hits[0].gameObject!=preview.gameObject)smokeErrors.Add("Preview drag target is blocked");
            var actor=preview.transform.Find("GuardianArt") as RectTransform;Vector2 original=actor.anchoredPosition;
            preview.OnPointerDown(pointer);yield return new WaitForSecondsRealtime(.5f);preview.OnPointerUp(pointer);
            if(Vector2.Distance(original,actor.anchoredPosition)<20)smokeErrors.Add("Preview did not move after drag");
            preview.Fire();yield return Capture("03-preview-fire");preview.Ultimate();yield return Capture("04-preview-ultimate");preview.Support();yield return Capture("05-preview-support");
            if(before!=JsonUtility.ToJson(State))smokeErrors.Add("Preview mutated profile");
            Store.Change(e=>{e.State.gems=900;e.State.testProgress=true;return CommandResult.Ok("Isolated outfit fixture");});
            OutfitRoom();ClickVisibleButton("구성 확인하고 구매 · ◆ 600");yield return Capture("06-confirm");ClickVisibleButton("보석 600개로 구매");
            if(!Outfits.Owned(State) || State.gems!=300)smokeErrors.Add("Purchase failed");
            ClickVisibleButton("전용 대원에 의상 장착");if(Outfits.Active(State,0)!=1)smokeErrors.Add("Equip failed");
            CloseModal();Show("home");yield return Capture("07-equipped-home");Show("showcase");yield return Capture("08-equipped-showcase");
            StartBattle(0);float until=Time.realtimeSinceStartup+60;bool ultimate=false;
            while(!combat.Ended && combat.Elapsed<35 && Time.realtimeSinceStartup<until)
            {
                if(combat.Choosing)ChooseBattleSkill(combat.Choices[0]);
                if(combat.CanUltimate && !ultimate){TriggerUltimate();yield return Capture("09-equipped-ultimate");ultimate=true;}
                yield return null;
            }
            if(!ultimate)smokeErrors.Add("Ultimate not exercised");
            combat.Abandon();FinishBattle();yield return Capture("10-guide-result");combat=null;Show("home");
            if(!State.guideMoved || !State.guideUltimate)smokeErrors.Add("Guide proof not saved");
            Change(new GameCommand("upgrade",State.equipped[0]));GuidePanel();yield return Capture("11-guide-progress");CloseModal();
            var support=new SupportSnapshot{hero=0,outfit=1,power=150,name="중장비 동료",showIdentity=true};
            OpenBattle(0,"outfit-support",12,false,"",support);combat.Elapsed=8;TriggerSupport();yield return Capture("12-support-arrival");combat=null;Show("home");
            OutfitRoom();ClickVisibleButton("의상 해제 · 기존 도색 표시");if(State.selectedOutfit!=0)smokeErrors.Add("Unequip failed");
            Show("home");yield return Capture("13-original-restored");
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Outfit/guide runtime errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("OUTFIT_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
