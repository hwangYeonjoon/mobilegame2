using System;
using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;

namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator CloudSmokeRoutine()
        {
            DismissTitleScreen();
            yield return new WaitForSecondsRealtime(.5f);
            string endpoint = "http://127.0.0.1:5187", invite = "";
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-server") endpoint = args[i+1];
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-invite") invite = args[i+1];
            yield return ConnectAccount(endpoint, "", invite);
            if (!Online || !cloudReady) { smokeErrors.Add("Server connection failed"); FinishCloudSmoke(); yield break; }
            CloseModal();
            Attendance(); ClickVisibleButton("오늘의 보상 받기");
            while (networkBusy) yield return null;
            if (cloud.Pending != null)
            {
                Debug.Log("CLOUD_SMOKE_RETRY_PENDING");
                yield return ReconnectCloud();
                if (cloud.Pending != null) smokeErrors.Add("Pending journal did not clear after retry");
            }
            if (State.attendanceCount != 1) smokeErrors.Add("Remote attendance button failed");
            yield return SubmitCloud(new GameCommand("rename") { value = "구름의 수호자" }, r => { if (!r.success) smokeErrors.Add(r.message); });
            yield return SubmitCloud(new GameCommand("visibility") { enabled = true }, r => { if (!r.success) smokeErrors.Add(r.message); });
            var other = new CloudClient(Path.Combine(Path.GetDirectoryName(SmokeProfilePath()),"other"));
            yield return other.Connect(endpoint,CloudClient.NewSecret(),true,r => { if (!r.success) smokeErrors.Add(r.message); },invite);
            yield return other.Execute(new GameCommand("rename") { value = "테스트 수호대" }, r => { if (!r.success) smokeErrors.Add(r.message); });
            yield return other.Execute(new GameCommand("visibility") { enabled = true }, r => { if (!r.success) smokeErrors.Add(r.message); });
            Show("showcase"); yield return Capture("01-server-showcase");
            yield return ExportCard(showcaseCard);
            if (File.Exists(lastSharedPath)) File.Copy(lastSharedPath,Path.Combine(smokeOutput,"02-server-share.png"),true);
            yield return LoadGallery(""); yield return Capture("03-gallery");
            yield return LoadPublic(other.Session.playerId); yield return Capture("04-public-profile");
            yield return SocialAction(other.Session.playerId,"like",new GameCommand { enabled = true },null);
            yield return cloud.Get("/v1/profiles/" + other.Session.playerId,(body,status) => { if (status != 200 || JsonUtility.FromJson<PublicProfile>(body).likes != 1) smokeErrors.Add("Server like failed"); });
            yield return SocialAction(other.Session.playerId,"block",new GameCommand { enabled = true },null);
            yield return cloud.Get("/v1/profiles/" + other.Session.playerId,(body,status) => { if (status != 404) smokeErrors.Add("Block did not hide profile"); });
            yield return LoadBlocks(); yield return Capture("05-blocks");
            yield return SocialAction(other.Session.playerId,"block",new GameCommand { enabled = false },null);
            yield return SubmitCloud(new GameCommand("guild_create"){value="구름 수호대"},r=>{if(!r.success)smokeErrors.Add(r.message);});
            GuildView guild=null;
            yield return cloud.Get("/v1/guild",(body,status)=>{if(status==200)guild=JsonUtility.FromJson<GuildView>(body);});
            if(guild==null){smokeErrors.Add("Guild creation failed");FinishCloudSmoke();yield break;}
            yield return other.Execute(new GameCommand("guild_join"){value=guild.code},r=>{if(!r.success)smokeErrors.Add(r.message);});
            yield return other.Execute(new GameCommand("guild_support"){hero=0,enabled=true},r=>{if(!r.success)smokeErrors.Add(r.message);});
            yield return LoadGuild();yield return Capture("08-guild-roster");
            StartBattle(0,false,"",other.Session.playerId);
            while (networkBusy) yield return null;
            if (combat == null) { smokeErrors.Add("Server battle failed to start"); FinishCloudSmoke(); yield break; }
            float until = Time.realtimeSinceStartup + 65;
            while (!combat.Ended && combat.Elapsed < 40 && Time.realtimeSinceStartup < until)
            {
                if (combat.Choosing) ChooseBattleSkill(combat.Choices[0]);
                if (combat.CanUltimate) TriggerUltimate();
                if (combat.CanSupport) TriggerSupport();
                yield return null;
            }
            if (!combat.SupportUsed) smokeErrors.Add("Guild support not exercised");
            if (combat.UltimateUses == 0) smokeErrors.Add("Cloud replay did not exercise ultimate");
            Debug.Log("CLOUD_ULTIMATE_USES="+combat.UltimateUses);
            if (!combat.Ended) combat.Abandon();
            FinishBattle(); while (networkBusy) yield return null;
            var receipt = State.receipts.Find(r => r.id == combat.RunId);
            if (receipt == null || receipt.kills != combat.Kills || Mathf.Abs(receipt.seconds-combat.Elapsed) > .05f) smokeErrors.Add("Unity/server replay mismatch or missing reward");
            yield return Capture("06-server-result");
            combat=null;Show("weekly");yield return Capture("09-cloud-rift-hub");
            StartBattle(4,false,Endgame.PracticeMode);while(networkBusy)yield return null;
            if(combat==null){smokeErrors.Add("Cloud practice failed to start");FinishCloudSmoke();yield break;}
            until=Time.realtimeSinceStartup+65;
            while(!combat.Ended && combat.Elapsed<32 && Time.realtimeSinceStartup<until)
            {
                if(combat.Choosing)ChooseBattleSkill(combat.Choices[0]);if(combat.CanUltimate)TriggerUltimate();yield return null;
            }
            if(!combat.Ended)combat.Abandon();FinishBattle();while(networkBusy)yield return null;
            receipt=State.receipts.Find(r=>r.id==combat.RunId);
            if(receipt==null || receipt.score!=combat.CoreScore || State.weekly.Count!=0 || State.coreShards!=0)smokeErrors.Add("Cloud challenge replay or practice isolation mismatch");
            yield return Capture("10-cloud-rift-result");combat=null;Show("weekly");
            yield return LoadWeeklyBoard();yield return Capture("11-cloud-ranking");
            var restored = new CloudClient(Path.Combine(Path.GetDirectoryName(SmokeProfilePath()),"recovery"));
            yield return restored.Connect(endpoint,cloud.Session.secret,false,r => { if (!r.success || r.state.gold != State.gold || r.state.totalKills != State.totalKills) smokeErrors.Add("Cross-device recovery mismatch"); });
            yield return other.Send("/v1/account","DELETE",null,(body,status) => { if (status != 200) smokeErrors.Add("Secondary account cleanup failed"); });
            combat = null; Show("showcase"); AccountPanel(); yield return Capture("07-server-account");
            FinishCloudSmoke();
        }
        void FinishCloudSmoke()
        {
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Cloud runtime errors: " + smokeErrors.Count + "\n" + string.Join("\n",smokeErrors) + "\n");
            Debug.Log("CLOUD_SMOKE_COMPLETE errors=" + smokeErrors.Count);
            Application.Quit(smokeErrors.Count == 0 ? 0 : 1);
        }
    }
}
