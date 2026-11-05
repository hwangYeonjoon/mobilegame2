using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using GoogleMobileAds.Common;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScrapGuard.Tests
{
    public class AdConsentDispatchTests
    {
        [UnityTest]
        public IEnumerator ConsentCallbackRunsBeforeAdvertisingSdkInitialization()
        {
            if(MobileAdsEventExecutor.instance!=null)
                Object.DestroyImmediate(MobileAdsEventExecutor.instance.gameObject);
            int mainThread=Thread.CurrentThread.ManagedThreadId,callbackThread=0,calls=0;
            var ads=new RewardedAds(new ServiceSettings{adsEnabled=true});
            // Simulate UMP's background completion without initializing MobileAds.
            var callback=Task.Run(()=>MobileAdsEventExecutor.ExecuteInUpdate(()=>{
                callbackThread=Thread.CurrentThread.ManagedThreadId;calls++;
            }));
            float deadline=Time.realtimeSinceStartup+5;
            while((!callback.IsCompleted||calls==0)&&Time.realtimeSinceStartup<deadline)yield return null;
            try
            {
                Assert.IsTrue(callback.IsCompleted,"Background completion did not finish.");
                Assert.IsFalse(callback.IsFaulted);
                Assert.AreEqual(1,calls,"Consent callback must run before the ad SDK starts.");
                Assert.AreEqual(mainThread,callbackThread);
                yield return null;
                Assert.AreEqual(1,calls,"Consent completion must not run twice.");
            }
            finally
            {
                ads.Cancel();
                if(MobileAdsEventExecutor.instance!=null)
                    Object.DestroyImmediate(MobileAdsEventExecutor.instance.gameObject);
            }
        }
    }
}
