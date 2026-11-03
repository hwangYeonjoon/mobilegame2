using System;
using System.Collections;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        GoogleLoginRequest googleLogin;
        void StartGoogleIdentity(string endpoint,bool link)
        {
            if(Application.platform!=RuntimePlatform.Android){Toast("Google 계정 연결은 Android에서 사용할 수 있어요.");return;}
            if(networkBusy||cloud.Pending!=null){Toast("미확인 거래를 먼저 동기화해 주세요.");return;}
            StartCoroutine(BeginGoogleIdentity(endpoint,link));
        }
        IEnumerator BeginGoogleIdentity(string endpoint,bool link)
        {
            SetNetworkBusy(true);GoogleChallenge challenge=null;
            string secret=link?"":CloudClient.NewSecret();string error="";
            yield return cloud.GoogleChallenge(endpoint,link,secret,(body,status)=>{if(status==200)challenge=JsonUtility.FromJson<GoogleChallenge>(body);else error=cloud.Parse(body,status).message;});
            if(challenge==null){SetNetworkBusy(false);Toast(error);yield break;}
            googleLogin=new GoogleLoginRequest{nonce=challenge.nonce,link=link,secret=secret};
#if UNITY_ANDROID && !UNITY_EDITOR
            try {using(var bridge=new AndroidJavaClass("com.scrapguard.mobile.GoogleIdentityBridge"))bridge.CallStatic("signIn",challenge.clientId,challenge.nonce,gameObject.name);}
            catch(AndroidJavaException){googleLogin=null;SetNetworkBusy(false);Toast("Google 계정 선택을 시작하지 못했어요.");}
#else
            googleLogin=null;SetNetworkBusy(false);
#endif
        }
        public void OnGoogleIdentity(string json)
        {
            if(googleLogin==null)return;
            GoogleLoginRequest result=null;try{result=JsonUtility.FromJson<GoogleLoginRequest>(json);}catch(ArgumentException){}
            if(result==null||string.IsNullOrEmpty(result.idToken)){googleLogin=null;SetNetworkBusy(false);Toast("Google 계정 선택이 취소되었거나 실패했어요.");return;}
            googleLogin.idToken=result.idToken;StartCoroutine(FinishGoogleIdentity());
        }
        IEnumerator FinishGoogleIdentity()
        {
            var request=googleLogin;googleLogin=null;ServerReply reply=null;
            yield return cloud.GoogleFinish(request,r=>reply=r);if(reply.state!=null)ApplyRemote(reply);
            SetNetworkBusy(false);Show("home");Toast(reply.message);AccountPanel();
        }
        void GoogleRecoveryPanel(string endpoint)
        {
            var card=Dialog("Google 계정으로 복구","이전에 Google 계정을 연결한 수호대를 복구합니다.",430);
            Ui.Label(card,"복구하면 이전 개인 복구 코드가 바뀌고\n다른 기기의 연결이 해제됩니다.\n기존 로컬 성장과 서버 계정을 합치지 않습니다.",24,132,444,112,17,Ui.Muted);
            Ui.Button(card,"Google 계정으로 복구",24,294,444,60,()=>StartGoogleIdentity(endpoint,false),Ui.Mint,true,18);
        }
    }
}
