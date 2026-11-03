using System;
using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;

namespace ScrapGuard
{
    public partial class GameApp
    {
        CloudClient cloud;
        ProfileStore localStore;
        string cloudCachePath;
        bool networkBusy, cloudReady;
        RectTransform busyRoot;
        bool Online { get { return cloud != null && cloud.Session.enabled; } }
        void SetupCloud(string profile)
        {
            string folder = Path.GetDirectoryName(profile);
            localStore = Store; cloudCachePath = Path.Combine(folder, "cloud-cache.json");
            cloud = new CloudClient(folder);
            if (Online) Store = new ProfileStore(cloudCachePath);
        }
        void SetNetworkBusy(bool busy)
        {
            networkBusy = busy;
            if (busyRoot != null) { Destroy(busyRoot.gameObject); busyRoot = null; }
            if (!busy) return;
            busyRoot = Ui.Box(Root, 0, 0, 540, 960, new Color(0,0,0,.55f), false).rectTransform;
            busyRoot.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            Ui.Box(busyRoot, 90, 420, 360, 100, Ui.Panel);
            Ui.Label(busyRoot, "서버와 동기화 중…", 90, 420, 360, 100, 22, Ui.Mint, UnityEngine.TextAnchor.MiddleCenter);
        }
        void ApplyRemote(ServerReply reply)
        {
            if (reply.state == null) return;
            if (Store == localStore) Store = new ProfileStore(cloudCachePath);
            Store.ReplaceRemote(reply.state); selectedStage = Math.Min(State.unlockedStage, selectedStage);
            cloudReady = true;
        }
        IEnumerator SubmitCloud(GameCommand command, Action<ServerReply> done)
        {
            if (!cloudReady) { done(new ServerReply { message = "먼저 계정 화면에서 서버에 다시 연결해 주세요." }); yield break; }
            SetNetworkBusy(true);
            ServerReply reply = null;
            yield return cloud.Execute(command, value => reply = value);
            ApplyRemote(reply); SetNetworkBusy(false); done(reply);
        }
        IEnumerator ReconnectCloud()
        {
            if (networkBusy) yield break;
            SetNetworkBusy(true); cloudReady = false;
            ServerReply reply = null;
            yield return cloud.Connect(cloud.Session.endpoint, cloud.Session.secret, false, value => reply = value);
            if (reply.success)
            {
                ApplyRemote(reply);
                if (cloud.Pending != null) { yield return cloud.Execute(cloud.Pending, value => reply = value); ApplyRemote(reply); }
            }
            SetNetworkBusy(false); Show(page); Toast(reply.message);
            if (!reply.success || !string.IsNullOrEmpty(State.activeRun)) AccountPanel();
            else if(titleScreen==null && !smokeActive && !State.guideIntroSeen)Change(new GameCommand("guide_seen"),GuidePanel);
        }
        void AccountPanel()
        {
            if(adBusy){Toast("광고 보상 확인을 마친 뒤 계정을 변경해 주세요.");return;}
            var card = Dialog("계정과 저장", Online ? "서버 계정 · ID " + cloud.Session.playerId : "로컬 체험과 서버 계정은 별도로 성장합니다.", 770);
            if (!Online)
            {
                Ui.Label(card, "서버 주소", 24, 132, 444, 28, 15, Ui.Muted);
                var endpoint = Ui.Input(card, cloud.Session.endpoint, "https://게임서버주소", 24, 166, 444, 200);
                Ui.Label(card, "복구 코드 (새 계정은 비워 두세요)", 24, 236, 444, 28, 15, Ui.Muted);
                var secret = Ui.Input(card, cloud.Session.secret, "개인 복구 코드 43자", 24, 270, 444, 43);
                secret.contentType = UnityEngine.UI.InputField.ContentType.Password;
                Ui.Label(card,"베타 초대 코드 (새 계정 생성 시)",24,334,444,28,15,Ui.Muted);
                var invite = Ui.Input(card,"","초대 서버에서만 필요해요",24,370,444,80);
                Ui.Button(card, "서버 계정 연결 / 복구", 24, 446, 444, 57, () => StartCoroutine(ConnectAccount(endpoint.text, secret.text, invite.text)));
                Ui.Label(card, "처음 연결하면 새 수호대가 시작돼요.\n서버 주소는 운영자가 제공해야 합니다.\n기존 로컬 재화는 서버로 옮겨지지 않아요.\n\n개발 테스트: PC는 127.0.0.1:5187\nAndroid는 adb reverse 후 같은 주소 사용", 24, 526, 444, 166, 15, Ui.Muted);
                Ui.Button(card,"Google 계정으로 복구",24,704,444,42,()=>GoogleRecoveryPanel(endpoint.text),Ui.Raised,true,16);
                return;
            }
            Ui.Label(card, cloudReady ? "연결됨 · 서버 저장 사용" : "연결 확인 필요 · 저장된 화면만 표시 중", 24, 137, 444, 40, 17, cloudReady ? Ui.Mint : Ui.Gold);
            Ui.Button(card, cloud.Pending == null ? "최신 상태 동기화" : "미확인 요청 다시 동기화", 24, 191, 444, 53, () => StartCoroutine(ReconnectCloud()), Ui.Raised);
            Ui.Button(card, "내 공개 ID 복사", 24, 258, 444, 53, () => { GUIUtility.systemCopyBuffer = cloud.Session.playerId; Toast("공개 ID를 복사했어요."); }, Ui.Raised);
            Ui.Button(card, "개인 복구 코드 보관", 24, 325, 444, 53, RecoveryPanel, Ui.Raised);
            Ui.Button(card, "중단된 작전 종료 (보상 없음)", 24, 392, 444, 53, () => Change(new GameCommand("battle_cancel"), AccountPanel), Ui.Raised, cloudReady && !string.IsNullOrEmpty(State.activeRun) && cloud.Pending == null, 16);
            Ui.Button(card, "로컬 체험으로 전환", 24, 459, 444, 53, () => { cloud.Disconnect(); selectedGuildSupport=null; cloudReady = false; Store = localStore; selectedStage = State.unlockedStage; Show("home"); }, Ui.Raised);
            Ui.Button(card, "서버 계정 삭제", 24, 553, 444, 53, DeleteAccountPanel, Ui.Raised, cloudReady && cloud.Pending == null);
            Ui.Button(card,"현재 수호대에 Google 계정 연결",24,623,444,47,()=>StartGoogleIdentity(cloud.Session.endpoint,true),Ui.Mint,true,16);
            Ui.Label(card,"복구 코드는 비밀로 보관하세요.\nGoogle 연결 후 다른 기기에서 복구할 수 있어요.",24,692,444,52,14,Ui.Muted);
        }
        IEnumerator ConnectAccount(string endpoint, string secret, string inviteCode = "")
        {
            if (networkBusy) yield break;
            bool create = string.IsNullOrWhiteSpace(secret);
            if (create) secret = CloudClient.NewSecret();
            SetNetworkBusy(true); ServerReply reply = null;
            yield return cloud.Connect(endpoint, secret.Trim(), create, value => reply = value, inviteCode.Trim());
            if (reply.success) ApplyRemote(reply);
            SetNetworkBusy(false); Show("showcase"); Toast(reply.message); AccountPanel();
        }
        void RecoveryPanel()
        {
            var card = Dialog("개인 복구 코드", "다른 기기에서 같은 서버 계정을 복구할 때 필요해요.", 410);
            Ui.Label(card, "코드를 아는 사람은 계정에 접근할 수 있어요.\n비밀번호 보관함 등에 안전하게 저장하세요.\n전시 카드에는 이 코드가 들어가지 않습니다.", 24, 142, 444, 108, 17, Ui.Muted);
            Ui.Button(card, "개인 복구 코드 복사", 24, 294, 444, 61, () => { GUIUtility.systemCopyBuffer = cloud.Session.secret; Toast("개인 복구 코드를 복사했어요."); });
        }
        void DeleteAccountPanel()
        {
            var card = Dialog("서버 계정을 삭제할까요?", "성장, 재화, 공개 프로필과 응원이 삭제됩니다.\n삭제 후 복구할 수 없습니다.", 390);
            Ui.Button(card, "계속 사용하기", 24, 180, 444, 54, AccountPanel);
            Ui.Button(card, "계정 영구 삭제", 24, 256, 444, 54, () => StartCoroutine(DeleteAccount()), Ui.Raised);
        }
        IEnumerator DeleteAccount()
        {
            SetNetworkBusy(true); bool success = false; string message = "";
            yield return cloud.Send("/v1/account", "DELETE", null, (body,status) => { var r = cloud.Parse(body,status); success = r.success; message = r.message; });
            if (success) { cloud.Forget(); cloudReady = false; Store = localStore; selectedStage = State.unlockedStage; }
            SetNetworkBusy(false); Show("home"); Toast(message);
        }
    }
}
