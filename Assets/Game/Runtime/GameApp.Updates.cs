using System;
using System.Collections;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    [Serializable] public class ClientConnection { public string releaseEndpoint=""; }
    public partial class GameApp
    {
        RectTransform updateOverlay;
        bool checkingRelease, updatePausedBattle, priorBattlePause;
        Combat updateCombat;
        int dismissedRelease;
        float lastReleaseCheck=-100;
        string configuredReleaseEndpoint="";
        string updateStoreError="";
        bool UpdateBlocksPlay { get { return cloud!=null && cloud.UpdateBlocked; } }
        void SetupUpdates()
        {
#if SCRAP_QA_SOAK
            // Isolated offline QA builds must not query or block on production release rules.
            return;
#else
            var asset=Resources.Load<TextAsset>("ClientConnection");
            try{if(asset!=null)configuredReleaseEndpoint=JsonUtility.FromJson<ClientConnection>(asset.text)?.releaseEndpoint??"";}catch(ArgumentException){}
            if(!smokeActive && !Online && CloudClient.ValidEndpoint(configuredReleaseEndpoint))StartCoroutine(CheckAppUpdate(false));
#endif
        }
        string UpdateEndpoint()
        {
            if(Online)return cloud.Session.endpoint;
            if(CloudClient.ValidEndpoint(configuredReleaseEndpoint))return configuredReleaseEndpoint;
            return cloud.Release!=null?cloud.ReleaseEndpoint:null;
        }
        void PollUpdateNotice()
        {
            if(cloud==null || !cloud.ReleaseNotice || networkBusy || cloud.Busy || checkingRelease)return;
            var state=cloud.UpdateState;
            if(state==UpdateRequirement.Recommended && combat!=null && !combat.Ended)return;
            cloud.ReleaseNotice=false;
            if(ReleaseRules.Blocks(state) || state==UpdateRequirement.Recommended && cloud.Release.latestBuild!=dismissedRelease)ShowUpdatePanel();
            else if(updateOverlay!=null && !ReleaseRules.Blocks(state))CloseUpdatePanel();
        }
        IEnumerator CheckAppUpdate(bool manual)
        {
            if(checkingRelease || networkBusy || cloud.Busy){if(manual)Toast("진행 중인 요청이 끝난 뒤 다시 확인해 주세요.");yield break;}
            string endpoint=UpdateEndpoint();
            if(!CloudClient.ValidEndpoint(endpoint))
            {
                if(manual)Info("앱 버전 "+AppRelease.Version,"서버에 연결하면 업데이트를 확인할 수 있어요.\n현재는 이 기기에서 로컬 체험 중입니다.");
                yield break;
            }
            checkingRelease=true;lastReleaseCheck=Time.unscaledTime;
            if(updateOverlay!=null)ShowUpdatePanel();
            bool ok=false;yield return cloud.CheckRelease(endpoint,value=>ok=value);
            checkingRelease=false;
            if(manual || updateOverlay!=null || UpdateBlocksPlay){cloud.ReleaseNotice=false;ShowUpdatePanel();}
            else if(!ok)Toast(cloud.ReleaseError);
        }
        void ShowUpdatePanel()
        {
            if(updateOverlay!=null){updateOverlay.gameObject.SetActive(false);Destroy(updateOverlay.gameObject);}
            bool blocked=UpdateBlocksPlay;
            if(combat!=null && !combat.Ended && !updatePausedBattle)
            {updatePausedBattle=true;updateCombat=combat;priorBattlePause=battlePaused;battlePaused=true;dragging=false;}
            updateOverlay=Ui.Rect(Root,"AppUpdateGate",0,0,540,960);updateOverlay.SetAsLastSibling();
            Ui.Box(updateOverlay,0,0,540,960,new Color(0,0,0,.88f)).raycastTarget=true;
            var card=Ui.Box(updateOverlay,24,153,492,654,Ui.Panel).transform;
            var state=cloud.UpdateState;var policy=cloud.Release;
            string title=state==UpdateRequirement.Required?"업데이트가 필요해요":state==UpdateRequirement.ServerMismatch?"서버 연결 준비 중이에요":state==UpdateRequirement.Recommended?"새 버전이 나왔어요":state==UpdateRequirement.Current?"최신 버전이에요":"버전 정보를 확인해 주세요";
            if(!blocked && !string.IsNullOrEmpty(cloud.ReleaseError))title="버전 확인에 실패했어요";
            Ui.Label(card,title,24,24,444,48,27,Ui.Mint);
            Ui.Label(card,"설치된 앱  "+AppRelease.Version+(policy!=null?"\n안내 버전  "+policy.latestVersion:""),24,87,444,62,18,Ui.Ink);
            string detail=state==UpdateRequirement.ServerMismatch?"현재 앱과 서버의 연결 준비가 맞지 않아요.\n서버 준비가 끝난 뒤 다시 확인해 주세요.":state==UpdateRequirement.Required?"계속하려면 호환되는 새 앱이 필요해요.\n계정과 미확인 거래 기록은 보관됩니다.":state==UpdateRequirement.Recommended?"새 콘텐츠와 개선 사항이 준비됐어요.\n지금 업데이트하거나 나중에 진행할 수 있어요.":state==UpdateRequirement.Current?"현재 버전으로 이용할 수 있어요.":"연결 상태를 확인한 뒤 다시 시도해 주세요.";
            if(!blocked && !string.IsNullOrEmpty(cloud.ReleaseError))detail="마지막으로 확인한 버전 정보만 표시하고 있어요.\n연결 상태를 확인한 뒤 다시 시도해 주세요.";
            Ui.Label(card,detail,24,168,444,73,18,Ui.Ink);
            var notesArea=Ui.Scroll(card,24,255,444,74,74);
            var notes=Ui.Label(notesArea,policy?.notes??"",0,0,444,74,15,Ui.Muted,TextAnchor.UpperLeft);
            notes.supportRichText=false;
            float notesHeight=Mathf.Max(74,notes.preferredHeight);
            notes.rectTransform.sizeDelta=new Vector2(444,notesHeight);notesArea.sizeDelta=new Vector2(444,notesHeight);
            bool storeReady=state!=UpdateRequirement.ServerMismatch && state!=UpdateRequirement.Current && ReleaseRules.StoreUrl(policy)!="";
            string storeLabel=storeReady?"Google Play에서 업데이트":state==UpdateRequirement.Current?"현재 버전 이용 가능":"새 버전 배포 준비 중";
            Ui.Button(card,storeLabel,24,345,444,55,OpenUpdateStore,Ui.Gold,storeReady&&!checkingRelease,18);
            string status=checkingRelease?"업데이트 정보를 확인하고 있어요…":!string.IsNullOrEmpty(cloud.ReleaseError)?"연결 확인 실패 · 마지막으로 확인한 안내를 유지해요.":storeReady?"스토어에서 설치한 뒤 돌아와 다시 확인해 주세요.":"테스트 APK는 안내받은 새 파일로 설치해 주세요.";
            if(!checkingRelease && string.IsNullOrEmpty(cloud.ReleaseError))
            {
                if(state==UpdateRequirement.Current)status="현재 앱으로 계속 플레이할 수 있어요.";
                else if(state==UpdateRequirement.ServerMismatch)status="잠시 후 업데이트 정보를 다시 확인해 주세요.";
            }
            if(!string.IsNullOrEmpty(updateStoreError))status=updateStoreError;
            Ui.Label(card,status,24,410,444,46,14,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,checkingRelease?"확인 중…":"업데이트 다시 확인",24,471,444,48,()=>StartCoroutine(CheckAppUpdate(true)),Ui.Raised,!checkingRelease,17);
            Ui.Button(card,blocked?"게임 종료":state==UpdateRequirement.Recommended?"나중에 하기":"닫기",24,541,444,48,()=>{if(blocked)Application.Quit();else {if(policy!=null)dismissedRelease=policy.latestBuild;CloseUpdatePanel();}},Ui.Raised,!checkingRelease,17);
            Ui.Label(card,"진행 상황을 지우거나 앱을 삭제할 필요가 없어요.",24,606,444,25,12,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void CloseUpdatePanel()
        {
            if(UpdateBlocksPlay)return;
            if(updateOverlay!=null){updateOverlay.gameObject.SetActive(false);Destroy(updateOverlay.gameObject);updateOverlay=null;}
            if(updatePausedBattle && combat==updateCombat)battlePaused=priorBattlePause;
            updatePausedBattle=false;updateCombat=null;
        }
        void OpenUpdateStore()
        {
            string url=ReleaseRules.StoreUrl(cloud.Release);if(url=="" || checkingRelease)return;
            updateStoreError="";
            if(smokeActive){Toast("화면 테스트 · 스토어 이동 생략");return;}
#if UNITY_ANDROID && !UNITY_EDITOR
            try{using(var bridge=new AndroidJavaClass("com.scrapguard.mobile.UpdateBridge"))bridge.CallStatic("openStore",gameObject.name);}
            catch(AndroidJavaException){Application.OpenURL(url);}
#else
            Toast("Android에서 Google Play로 업데이트할 수 있어요.");
#endif
        }
        public void OnUpdateStoreError(string unused){updateStoreError="스토어를 열지 못했어요. Google Play에서 고철 수호대를 찾아주세요.";ShowUpdatePanel();}
    }
}
