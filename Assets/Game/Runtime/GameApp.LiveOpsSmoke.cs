using System;
using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator LiveOpsSmokeRoutine()
        {
            DismissTitleScreen();yield return ConnectAccount("http://127.0.0.1:5190","","local-integration-test-invite-03");
            if(!Online||!cloudReady){smokeErrors.Add("Local online connection failed");FinishLiveOpsSmoke();yield break;}
            CloseModal();ResearchPanel();yield return Capture("01-local-research-separate");
            ClickVisibleButton("온라인 의상 체험 통계 · 별도 동의");yield return Capture("02-online-consent-off");
            ClickVisibleButton("체험 통계에 동의하고 켜기");while(networkBusy)yield return null;
            if(!cloud.Latest.metricsConsent)smokeErrors.Add("Online consent not persisted");yield return Capture("03-online-consent-on");
            yield return cloud.RefreshContent(null);OutfitRoom(1);yield return Capture("04-live-preview");
            if(!OutfitForSale(1))smokeErrors.Add("Default server catalog missing");
            // Presentation fixtures only. Server schedule boundaries are covered by server checks.
            var release=cloud.Content.outfits.Find(x=>x.id==1);release.paused=true;
            OutfitRoom(1);yield return Capture("05-paused-presentation-fixture");
            foreach(var b in Root.GetComponentsInChildren<Button>())if(b.name=="판매 준비 중 · 체험 가능"&&b.interactable)smokeErrors.Add("Paused purchase button enabled");
            release.paused=false;DateTimeOffset now;cloud.ContentTime(out now);release.startsAt=now.AddDays(3).ToString("O");
            Screen.SetResolution(360,800,false);yield return new WaitForSecondsRealtime(.5f);
            OutfitCollection();yield return Capture("06-scheduled-collection-small-fixture");OutfitRoom(1);yield return Capture("07-scheduled-room-small-fixture");
            if(OutfitForSale(1))smokeErrors.Add("Future purchase allowed in UI");release.startsAt="";
            OnlineMetricsPanel();ClickVisibleButton("체험 통계 끄기 · 기록 삭제");while(networkBusy)yield return null;yield return Capture("08-consent-withdrawn-small");
            if(cloud.Latest.metricsConsent)smokeErrors.Add("Consent withdrawal not persisted");
            yield return DeleteAccount();FinishLiveOpsSmoke();
        }
        void FinishLiveOpsSmoke()
        {
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Live operations UI errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
