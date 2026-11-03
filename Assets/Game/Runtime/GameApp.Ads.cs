using System;
using System.Collections;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        ServiceSettings services;
        RewardedAds rewardedAds;
        bool adBusy;
        [Serializable] sealed class AdHistory { public AdClaim[] claims; }
        void SetupServices(){services=ServiceSettings.Load();rewardedAds=new RewardedAds(services);rewardedAds.GatherConsent();}
        bool SameAdAccount(string owner,string endpoint)=>Online&&cloud.Session.playerId==owner&&cloud.Session.endpoint==endpoint;
        void ApplyAd(AdClaim claim)
        {
            if(cloud.AcceptAd(claim)){ApplyRemote(claim.reply);RefreshWallet();}
        }
        IEnumerator RequestOnlineReward(string placement)
        {
            if(adBusy||networkBusy||!cloudReady||cloud.Pending!=null){Toast("진행 중인 요청을 먼저 동기화해 주세요.");yield break;}
            if(!State.adSkip&&(!rewardedAds.Available||!rewardedAds.ConsentReady)){Info("광고 보상", "현재 광고를 이용할 수 없어요.\n기본 전투 보상은 그대로 유지됩니다.");yield break;}
            adBusy=true;SetNetworkBusy(true);string owner=cloud.Session.playerId,endpoint=cloud.Session.endpoint;
            AdClaim claim=null;
            yield return cloud.AdRequest("/v1/ads/claims","POST",new GameCommand("ad_request"){value=placement},c=>claim=c);
            SetNetworkBusy(false);ApplyAd(claim);
            if(claim==null||!SameAdAccount(owner,endpoint)){adBusy=false;Toast("요청을 확인하지 못했어요. 설정의 광고 보상 내역에서 확인해 주세요.");yield break;}
            if(claim.status=="granted"){adBusy=false;RewardDone(claim);yield break;}
            string result=null;
            rewardedAds.Show(claim,r=>result=r);
            float deadline=Time.realtimeSinceStartup+180;
            while(result==null&&Time.realtimeSinceStartup<deadline)yield return null;
            if(result==null){rewardedAds.Cancel();result="timeout";}
            if(!SameAdAccount(owner,endpoint)){adBusy=false;yield break;}
            if(result=="load_failed"||result=="show_failed"||result=="unavailable")
            {
                SetNetworkBusy(true);yield return cloud.AdRequest("/v1/ads/claims/"+claim.id,"DELETE",null,ApplyAd);SetNetworkBusy(false);
                adBusy=false;Toast("광고를 표시하지 못했어요. 보상 횟수는 차감하지 않았어요.");yield break;
            }
            Toast("광고 보상을 확인하고 있어요.");
            for(int i=0;i<10;i++)
            {
                if(!SameAdAccount(owner,endpoint)){adBusy=false;yield break;}
                while(networkBusy||cloud.Busy)yield return null;
                if(!SameAdAccount(owner,endpoint)){adBusy=false;yield break;}
                SetNetworkBusy(true);yield return cloud.AdRequest("/v1/ads/claims/"+claim.id,"GET",null,c=>{if(c!=null)claim=c;});SetNetworkBusy(false);ApplyAd(claim);
                if(claim.status!="pending")break;
                yield return new WaitForSecondsRealtime(2);
            }
            adBusy=false;
            if(claim.status=="granted")RewardDone(claim);else Info("광고 보상 확인 중","서버 확인 후 지급됩니다.\n설정 → 광고 보상·개인정보에서 내역을 확인하세요.");
        }
        void RewardDone(AdClaim claim)
        {
            Toast(claim.gold>0?"추가 골드 +"+claim.gold:"부품 +"+claim.parts);
            if(combat!=null&&combat.Ended)ShowResult();else{CloseModal();Show(page);}
        }
        void ServicePanel()
        {
            var card=Dialog("광고 보상 · 개인정보","광고 지급 내역과 개인정보 설정을 확인하세요.",680);
            Ui.Button(card,"광고 보상 내역 · 다시 확인",24,142,444,54,()=>StartCoroutine(AdHistoryPanel()),Ui.Raised,Online&&!networkBusy&&!adBusy,17);
            Ui.Button(card,"광고 개인정보 선택 변경",24,212,444,54,()=>rewardedAds.Privacy(ServicePanel),Ui.Raised,rewardedAds.PrivacyRequired&&!adBusy,17);
            Ui.Button(card,"개인정보처리방침",24,282,444,54,()=>Application.OpenURL(services.privacyUrl),Ui.Raised,ServiceSettings.PublicUrl(services.privacyUrl),17);
            Ui.Button(card,"고객지원",24,352,444,54,()=>Application.OpenURL(services.supportUrl),Ui.Raised,ServiceSettings.PublicUrl(services.supportUrl),17);
            Ui.Button(card,"계정 삭제 안내",24,422,444,54,()=>Application.OpenURL(services.deletionUrl),Ui.Raised,ServiceSettings.PublicUrl(services.deletionUrl),17);
            Ui.Label(card,"기본 보상은 광고 시청 여부와 무관하게 지급됩니다.\n추가 보상은 서버가 시청을 확인한 후 지급해요.\n광고가 열리지 않으면 보상 횟수를 차감하지 않아요.",24,514,444,110,16,Ui.Muted);
        }
        IEnumerator AdHistoryPanel()
        {
            if(!Online||networkBusy||adBusy)yield break;
            SetNetworkBusy(true);AdHistory history=null;
            yield return cloud.Get("/v1/ads/claims",(body,code)=>{if(code==200)history=JsonUtility.FromJson<AdHistory>(body);});SetNetworkBusy(false);
            if(history==null){Toast("보상 내역을 불러오지 못했어요.");yield break;}
            DrawAdHistory(history);
        }
        void DrawAdHistory(AdHistory history)
        {
            var card=Dialog("광고 보상 내역","최근 10건 · 지급 완료는 지갑에 반영됩니다.",740);
            var list=Ui.Scroll(card,24,135,444,550,Math.Max(500,(history.claims==null?0:history.claims.Length)*110));
            if(history.claims==null||history.claims.Length==0){Ui.Label(list,"최근 광고 보상 요청이 없어요.",0,0,430,60,18,Ui.Muted);return;}
            for(int i=0;i<history.claims.Length;i++)
            {
                var claim=history.claims[i];ApplyAd(claim);float y=i*110;
                string state=claim.status=="granted"?"지급 완료":claim.status=="pending"?"확인 중":claim.status=="canceled"?"취소됨":"시간 만료";
                Ui.Label(list,(claim.gold>0?"골드 "+claim.gold:"부품 "+claim.parts)+" · "+state,0,y,430,38,18);
                if(claim.status=="pending")Ui.Button(list,"광고가 열리지 않은 요청 취소",0,y+44,430,46,()=>ConfirmCancelAd(claim.id),Ui.Raised,true,15);
            }
        }
        void ConfirmCancelAd(string id)
        {
            var card=Dialog("광고 요청을 취소할까요?","광고가 열리지 않은 경우에만 취소해 주세요.",440);
            Ui.Label(card,"취소하면 늦게 도착하는 보상도 지급되지 않아요.\n이미 시청했다면 기다린 뒤 내역을 다시 확인하세요.",24,142,444,96,18,Ui.Ink);
            Ui.Button(card,"계속 기다리기",24,268,444,52,()=>StartCoroutine(AdHistoryPanel()),Ui.Mint,true,17);
            Ui.Button(card,"미시청 요청 취소",24,337,444,52,()=>StartCoroutine(CancelAdClaim(id)),Ui.Raised,true,17);
        }
        IEnumerator CancelAdClaim(string id)
        {
            if(networkBusy||adBusy)yield break;SetNetworkBusy(true);
            yield return cloud.AdRequest("/v1/ads/claims/"+id,"DELETE",null,ApplyAd);SetNetworkBusy(false);yield return AdHistoryPanel();
        }
    }
}
