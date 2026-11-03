using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator UpdateSmokeRoutine()
        {
            DismissTitleScreen();CloseModal();Show("home");
            string before=JsonUtility.ToJson(State), endpoint="http://127.0.0.1:1";
            var policy=new ReleasePolicy{latestBuild=AppRelease.Build+1,minimumBuild=AppRelease.Build,latestVersion="0.16.0",combatRules=Combat.RulesVersion,packageName=AppRelease.Package,playAvailable=true,notes="화면 검증용 업데이트 안내 · 새 콘텐츠와 안정성 개선"};
            cloud.ObserveRelease(endpoint,policy);PollUpdateNotice();yield return Capture("01-recommended");
            ClickVisibleButton("나중에 하기");if(updateOverlay!=null)smokeErrors.Add("Recommended update could not be deferred");
            policy.notes=new string('가',480);cloud.ObserveRelease(endpoint,policy);cloud.ReleaseNotice=false;ShowUpdatePanel();
            yield return Capture("01a-long-notes");
            var notesScroll=updateOverlay.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            if(notesScroll.content.rect.height<=notesScroll.viewport.rect.height)smokeErrors.Add("Long release notes cannot scroll");
            notesScroll.verticalNormalizedPosition=0;yield return Capture("01b-long-notes-bottom");
            ClickVisibleButton("나중에 하기");policy.notes="화면 검증용 업데이트 안내 · 새 콘텐츠와 안정성 개선";
            cloud.ObserveRelease(endpoint,policy);PollUpdateNotice();if(updateOverlay!=null)smokeErrors.Add("Deferred recommendation repeated");
            policy.minimumBuild=AppRelease.Build+1;policy.playAvailable=false;cloud.ObserveRelease(endpoint,policy);PollUpdateNotice();yield return Capture("02-required-unpublished");
            CloseModal();CloseUpdatePanel();StartBattle(0,true);Change(new GameCommand("demo_gems"));
            if(updateOverlay==null || combat!=null || before!=JsonUtility.ToJson(State))smokeErrors.Add("Mandatory update gate bypassed or mutated save");
            policy.playAvailable=true;cloud.ObserveRelease(endpoint,policy);PollUpdateNotice();yield return Capture("03-required-play");
            bool checkedOk=true;yield return cloud.CheckRelease(endpoint,ok=>checkedOk=ok);PollUpdateNotice();yield return Capture("04-required-network-failure");
            if(checkedOk || !cloud.UpdateBlocked || updateOverlay==null)smokeErrors.Add("Network failure bypassed cached requirement");
            policy.minimumBuild=policy.latestBuild=AppRelease.Build;policy.latestVersion=AppRelease.Version;policy.combatRules++;
            cloud.ObserveRelease(endpoint,policy);PollUpdateNotice();yield return Capture("05-server-mismatch");
            if(!cloud.UpdateBlocked)smokeErrors.Add("Incompatible server accepted");
            policy.combatRules=Combat.RulesVersion;cloud.ObserveRelease(endpoint,policy);PollUpdateNotice();
            if(updateOverlay!=null || cloud.UpdateBlocked)smokeErrors.Add("Policy rollback did not unlock app");
            ShowUpdatePanel();yield return Capture("06-current");ClickVisibleButton("닫기");
            if(before!=JsonUtility.ToJson(State))smokeErrors.Add("Update screens changed player data");
            // A mandatory gate pauses a live run; rollback restores its prior pause state.
            OpenBattle(0,"update-ui-fixture",0,false);battlePaused=false;
            policy.minimumBuild=policy.latestBuild=AppRelease.Build+1;policy.latestVersion="0.16.0";cloud.ObserveRelease(endpoint,policy);PollUpdateNotice();
            if(!battlePaused)smokeErrors.Add("Mandatory update did not pause combat");
            yield return Capture("07-paused-battle");
            policy.minimumBuild=policy.latestBuild=AppRelease.Build;policy.latestVersion=AppRelease.Version;cloud.ObserveRelease(endpoint,policy);PollUpdateNotice();
            if(battlePaused || updateOverlay!=null)smokeErrors.Add("Rollback did not restore live combat pause state");
            combat=null;Show("home");
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Update fixture runtime errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("UPDATE_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
