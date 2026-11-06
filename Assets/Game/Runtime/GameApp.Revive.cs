using System.Collections;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        bool reviveDeclined,revivePrompt;
        bool OfferRevive()
        {
            if(reviveDeclined||!Online||!combat.CanRevive)return false;
            if(!revivePrompt){revivePrompt=true;DrawReviveChoice();Track("revive","offer",combat.Stage,combat.Elapsed);}
            return true;
        }
        void DrawReviveChoice()
        {
            var card=Dialog("긴급 수리로 이어갈까요?","일반 작전당 1회 · 선택하지 않아도 기본 보상은 유지돼요.",620);
            Ui.Label(card,"체력 50% 회복 · 3초간 피해 보호\n현재 스킬과 고철 합체 진행도를 유지해요.",24,147,444,86,21,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Button(card,State.adSkip?"광고 없이 긴급 수리":"광고 보고 긴급 수리",24,260,444,66,()=>StartCoroutine(RequestRevive()),Ui.Mint,!adBusy&&!networkBusy,21);
            Ui.Button(card,"시청한 광고의 지급 상태 다시 확인",24,343,444,58,()=>StartCoroutine(RecoverRevive()),Ui.Raised,!adBusy&&!networkBusy,17);
            Ui.Button(card,"그만하고 보상 받기",24,432,444,66,()=>{reviveDeclined=true;revivePrompt=false;CloseModal();FinishBattle();},Ui.Raised,!adBusy&&!networkBusy,21);
            Ui.Label(card,"서버의 시청 확인 후 이어집니다.\n부활은 이 작전에만 적용되며 도전 순위에는 사용할 수 없어요.",24,522,444,62,15,Ui.Muted);
            card.GetChild(2).gameObject.SetActive(false);
        }
        IEnumerator RequestRevive()
        {
            Track("revive","request",combat.Stage);
            yield return RequestOnlineReward("revive");
            if(combat!=null&&combat.Ended&&revivePrompt)DrawReviveChoice();
        }
        IEnumerator RecoverRevive()
        {
            if(adBusy||networkBusy)yield break;
            adBusy=true;SetNetworkBusy(true);AdHistory history=null;
            yield return cloud.Get("/v1/ads/claims",(body,code)=>{if(code==200)history=JsonUtility.FromJson<AdHistory>(body);});
            SetNetworkBusy(false);adBusy=false;
            if(history?.claims!=null)foreach(var claim in history.claims)
                if(claim.placement=="revive"&&claim.status=="granted"&&claim.runId==combat?.RunId)
                {ApplyAd(claim);AcceptRevive(claim);yield break;}
            Toast("지급 확인 중이에요. 잠시 후 다시 확인할 수 있어요.");DrawReviveChoice();
        }
        void AcceptRevive(AdClaim claim)
        {
            if(claim.status!="granted"||combat==null||claim.runId!=combat.RunId||!combat.Revive())return;
            recorder.Revive();revivePrompt=false;reviveDeclined=true;
            battleAccumulator=0;battlePaused=false;dragging=false;CloseModal();Track("revive","used",combat.Stage,combat.Elapsed);
            Toast("긴급 수리 완료 · 3초간 보호됩니다.");RenderBattle();
        }
    }
}
