using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator ServiceSmokeRoutine()
        {
            DismissTitleScreen();string before=JsonUtility.ToJson(State);
            if(services.adsEnabled||rewardedAds.Available)smokeErrors.Add("Unconfigured build unexpectedly enables ads");
            Settings();yield return Capture("01-settings");
            ClickVisibleButton("광고 보상 · 개인정보 · 고객지원");yield return Capture("02-services");
            foreach(var offer in CommerceCatalog.Offers){StoreOfferPanel(offer);yield return Capture("03-offer-"+offer.Id);}
            // UI fixtures only: no SDK ad impression, payment or currency grant.
            DrawAdHistory(new AdHistory{claims=new[]{new AdClaim{id="fixture-paid",status="granted",parts=10},new AdClaim{id="fixture-pending",status="pending",gold=125},new AdClaim{id="fixture-cancel",status="canceled",parts=10},new AdClaim{id="fixture-expired",status="expired",gold=125}}});
            yield return Capture("04-ad-history-fixture");
            ClickVisibleButton("광고가 열리지 않은 요청 취소");yield return Capture("04b-ad-cancel-confirmation");
            Screen.SetResolution(360,800,false);yield return new WaitForSecondsRealtime(.5f);
            Settings();yield return Capture("05-settings-small");ServicePanel();yield return Capture("06-services-small");
            if(before!=JsonUtility.ToJson(State))smokeErrors.Add("Service information screens mutated progress");
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Service preparation UI fixture errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
