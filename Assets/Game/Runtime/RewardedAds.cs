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
        static bool IsEmulator
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                using(var build=new AndroidJavaClass("android.os.Build"))
                {string model=build.GetStatic<string>("MODEL")??"",fingerprint=build.GetStatic<string>("FINGERPRINT")??"";return model.Contains("sdk_gphone")||fingerprint.StartsWith("generic",StringComparison.Ordinal);}
#else
                return false;
#endif
            }
        }
        public bool PrivacyRequired=>Available&&ConsentInformation.PrivacyOptionsRequirementStatus==PrivacyOptionsRequirementStatus.Required;
        public RewardedAds(ServiceSettings settings)
        {
            this.settings=settings;
            // UMP completes before MobileAds.Initialize. Its callbacks already use this
            // queue, so create the Unity-thread dispatcher now without starting ads.
            if(settings.adsEnabled)MobileAdsEventExecutor.Initialize();
        }
        static void Main(Action action)=>MobileAdsEventExecutor.ExecuteInUpdate(action);
        public void GatherConsent()
        {
            if(!Available||consentStarted)return;consentStarted=true;
            // Until an age-selection flow is verified, apply the conservative consent treatment to all testers.
            ConsentInformation.Update(new ConsentRequestParameters{TagForUnderAgeOfConsent=true},error=>Main(()=>{
                if(error!=null){FinishConsent(error);return;}
                ConsentForm.LoadAndShowConsentFormIfRequired(formError=>Main(()=>FinishConsent(formError)));
            }));
        }
        void FinishConsent(FormError error)
        {
            consentFinished=true;
            // Log only status/error codes; never consent payloads or device identifiers.
            Debug.Log("SCRAP_AD_CONSENT ready="+ConsentInformation.CanRequestAds()+" error="+(error==null?"none":error.ErrorCode.ToString()));
        }
        public void Privacy(Action done)
        {
            if(!PrivacyRequired){done();return;}
            ConsentForm.ShowPrivacyOptionsForm(error=>Main(done));
        }
        public bool ConsentReady=>consentFinished&&ConsentInformation.CanRequestAds();
        public void Show(AdClaim claim,Action<string> done)
        {
            if(!Available||!ConsentReady||claim.unitId!=settings.rewardedUnitId||claim.test==settings.productionAds||claim.test&&!claim.unitId.Contains("3940256099942544")&&(settings.testDeviceIds==null||settings.testDeviceIds.Length==0)&&!IsEmulator){done("unavailable");return;}
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
            var testDevices=new List<string>(settings.testDeviceIds??new string[0]);
            if(!settings.productionAds)testDevices.Add(AdRequest.TestDeviceSimulator);
            MobileAds.SetRequestConfiguration(new RequestConfiguration{TestDeviceIds=testDevices,TagForUnderAgeOfConsent=TagForUnderAgeOfConsent.True,MaxAdContentRating=MaxAdContentRating.G});
            MobileAds.Initialize(status=>Main(()=>{initializing=false;initialized=status!=null;if(!initialized){Finish("unavailable");return;}Load();}));
        }
        public void Cancel(){generation++;ad?.Destroy();ad=null;}
    }
}
