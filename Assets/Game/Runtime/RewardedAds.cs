using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    // The SDK callback never awards currency. Only an authenticated server reply does.
    public sealed class RewardedAds
    {
        readonly ServiceSettings settings;
        RewardedAd ad;
        bool initialized,consentStarted,consentFinished,initializing;
        int generation;
        public bool Available=>settings.adsEnabled&&Application.platform==RuntimePlatform.Android;
        public bool PrivacyRequired=>Available&&ConsentInformation.PrivacyOptionsRequirementStatus==PrivacyOptionsRequirementStatus.Required;
        public RewardedAds(ServiceSettings settings){this.settings=settings;}
        static void Main(Action action)=>MobileAdsEventExecutor.ExecuteInUpdate(action);
        public void GatherConsent()
        {
            if(!Available||consentStarted)return;consentStarted=true;
            ConsentInformation.Update(new ConsentRequestParameters(),error=>Main(()=>{
                if(error!=null){consentFinished=true;return;}
                ConsentForm.LoadAndShowConsentFormIfRequired(formError=>Main(()=>consentFinished=true));
            }));
        }
        public void Privacy(Action done)
        {
            if(!PrivacyRequired){done();return;}
            ConsentForm.ShowPrivacyOptionsForm(error=>Main(done));
        }
        public bool ConsentReady=>consentFinished&&ConsentInformation.CanRequestAds();
        public void Show(AdClaim claim,Action<string> done)
        {
            if(!Available||!ConsentReady||claim.unitId!=settings.rewardedUnitId||claim.test==settings.productionAds||claim.test&&!claim.unitId.Contains("3940256099942544")&&(settings.testDeviceIds==null||settings.testDeviceIds.Length==0)){done("unavailable");return;}
            int current=++generation;
            void Finish(string result){if(current!=generation)return;generation++;ad?.Destroy();ad=null;done(result);}
            void Load()
            {
                if(current!=generation)return;
                RewardedAd.Load(claim.unitId,new AdRequest(),(loaded,error)=>Main(()=>{
                    if(current!=generation){loaded?.Destroy();return;}
                    if(error!=null||loaded==null){Finish("load_failed");return;}
                    ad=loaded;
                    ad.SetServerSideVerificationOptions(new ServerSideVerificationOptions{CustomData=claim.id,UserId=claim.userId});
                    ad.OnAdFullScreenContentClosed+=()=>Main(()=>Finish("closed"));
                    ad.OnAdFullScreenContentFailed+=failure=>Main(()=>Finish("show_failed"));
                    if(!ad.CanShowAd()||!ConsentInformation.CanRequestAds()){Finish("unavailable");return;}
                    ad.Show(reward=>{});
                }));
            }
            if(initialized){Load();return;}
            if(initializing){Finish("unavailable");return;}
            initializing=true;
            MobileAds.SetRequestConfiguration(new RequestConfiguration{TestDeviceIds=new List<string>(settings.testDeviceIds??new string[0])});
            MobileAds.Initialize(status=>Main(()=>{initializing=false;initialized=status!=null;if(!initialized){Finish("unavailable");return;}Load();}));
        }
        public void Cancel(){generation++;ad?.Destroy();ad=null;}
    }
}
