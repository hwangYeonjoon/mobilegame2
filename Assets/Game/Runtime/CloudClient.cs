using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace ScrapGuard
{
    [Serializable] public class CloudSession
    {
        public string endpoint = "https://api.scrapguard.win", secret = "", playerId = "";
        public bool enabled;
        public string secretCipher;
        public long revision;
    }
    public sealed partial class CloudClient
    {
        readonly string sessionPath, pendingPath;
        public CloudSession Session { get; private set; }
        public GameCommand Pending { get; private set; }
        public ServerReply Latest { get; private set; }
        public bool Busy { get; private set; }
        public CloudClient(string folder)
        {
            sessionPath = Path.Combine(folder, "cloud-session.json"); pendingPath = Path.Combine(folder, "cloud-pending.json");
            Session = Read<CloudSession>(sessionPath) ?? new CloudSession(); Pending = Read<GameCommand>(pendingPath);
            releasePath=Path.Combine(folder,"release-cache.json");LoadRelease(Session.endpoint);
#if UNITY_ANDROID && !UNITY_EDITOR
            if(!string.IsNullOrEmpty(Session.secretCipher))
            {try{using(var bridge=new AndroidJavaClass("com.scrapguard.mobile.GoogleIdentityBridge"))Session.secret=bridge.CallStatic<string>("decrypt",Session.secretCipher);}
            catch(AndroidJavaException){Session.secret="";Session.enabled=false;}}
            if(!string.IsNullOrEmpty(Session.secret)){try{SaveSession();}catch(AndroidJavaException){Session.enabled=false;}}
#endif
        }
        T Read<T>(string path) where T:class
        { try { return File.Exists(path) ? JsonUtility.FromJson<T>(File.ReadAllText(path)) : null; } catch { return null; } }
        static void Atomic(string path, string value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path + ".tmp", value);
            if (File.Exists(path)) File.Replace(path + ".tmp", path, null); else File.Move(path + ".tmp", path);
        }
        public void SaveSession()
        {
            var saved=JsonUtility.FromJson<CloudSession>(JsonUtility.ToJson(Session));
#if UNITY_ANDROID && !UNITY_EDITOR
            using(var bridge=new AndroidJavaClass("com.scrapguard.mobile.GoogleIdentityBridge"))saved.secretCipher=string.IsNullOrEmpty(Session.secret)?"":bridge.CallStatic<string>("encrypt",Session.secret);
            saved.secret="";
#endif
            Atomic(sessionPath,JsonUtility.ToJson(saved));
        }
        public static string NewSecret()
        {
            var bytes = new byte[32]; using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_');
        }
        public static bool ValidEndpoint(string endpoint)
        {
            Uri uri;
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out uri) || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0) return false;
            return uri.Scheme == "https" || (Debug.isDebugBuild || Application.isEditor) && uri.Scheme == "http" && (uri.Host == "127.0.0.1" || uri.Host == "localhost" || uri.Host == "10.0.2.2");
        }
        public IEnumerator Connect(string endpoint, string secret, bool create, Action<ServerReply> done, string inviteCode = "")
        {
            endpoint = (endpoint ?? "").Trim().TrimEnd('/');
            if (!ValidEndpoint(endpoint)) { done(new ServerReply { message = "HTTPS 주소 또는 로컬 테스트 주소를 입력해 주세요." }); yield break; }
            if (Pending != null && (endpoint != Session.endpoint || secret != Session.secret)) { done(new ServerReply { message = "미확인 거래를 먼저 동기화해 주세요." }); yield break; }
            Session.endpoint = endpoint; Session.secret = secret;
            if (!Persist(SaveSession)) { done(StorageFailure()); yield break; }
            bool releaseReady=false;yield return CheckRelease(endpoint,ok=>releaseReady=ok);
            if(!releaseReady || UpdateBlocked){done(UpdateFailure());yield break;}
            string path = create ? "/v1/auth/guest" : "/v1/bootstrap";
            yield return Request(path, create ? "POST" : "GET", create ? JsonUtility.ToJson(new GuestRequest { secret = secret, inviteCode = inviteCode }) : null, (body,status) =>
            {
                var reply = Parse(body, status);
                if (reply.success && !Persist(() => { Accept(reply); Session.enabled = true; SaveSession(); })) reply = StorageFailure();
                done(reply);
            });
        }
        public IEnumerator Execute(GameCommand command, Action<ServerReply> done)
        {
            if(UpdateBlocked && ReleaseEndpoint==Session.endpoint){ReleaseNotice=true;done(UpdateFailure());yield break;}
            if (Busy) { done(new ServerReply { message = "이전 요청을 처리 중이에요." }); yield break; }
            if (Pending != null && Pending.requestId != command.requestId) { done(new ServerReply { message = "미확인 요청이 있어요. 계정 화면에서 다시 동기화해 주세요." }); yield break; }
            if (Pending == null)
            {
                command.revision = Session.revision;
                if (!Persist(() => Atomic(pendingPath, JsonUtility.ToJson(command)))) { done(StorageFailure()); yield break; }
                Pending = command;
            }
            yield return Request("/v1/commands", "POST", JsonUtility.ToJson(Pending), (body,status) =>
            {
                var reply = Parse(body,status);
                if (reply.state != null && !Persist(() => Accept(reply))) { done(StorageFailure()); return; }
                // Network/5xx failures retain the exact journal for an idempotent retry.
                if (ReleaseRules.CompletePending(reply.success,status))
                {
                    if (!Persist(() => { if (File.Exists(pendingPath)) File.Delete(pendingPath); })) { done(StorageFailure()); return; }
                    Pending = null;
                }
                done(reply);
            });
        }
        public IEnumerator GoogleChallenge(string endpoint,bool link,string freshSecret,Action<string,long> done)
        {
            if(!ValidEndpoint(endpoint)||Pending!=null){done("",400);yield break;}
            Session.endpoint=endpoint.Trim().TrimEnd('/');
            bool releaseReady=false;yield return CheckRelease(Session.endpoint,ok=>releaseReady=ok);
            if(!releaseReady || UpdateBlocked){done(JsonUtility.ToJson(UpdateFailure()),426);yield break;}
            yield return Request("/v1/auth/google/challenge","POST",JsonUtility.ToJson(new GoogleChallengeRequest{link=link,secret=freshSecret}),done);
        }
        public IEnumerator GoogleFinish(GoogleLoginRequest request,Action<ServerReply> done)
        {
            if(Pending!=null){done(new ServerReply{message="미확인 거래를 먼저 동기화해 주세요."});yield break;}
            if(!request.link){Session.secret=request.secret;Session.enabled=false;if(!Persist(SaveSession)){done(StorageFailure());yield break;}}
            yield return Request("/v1/auth/google/finish","POST",JsonUtility.ToJson(request),(body,status)=>{
                var reply=Parse(body,status);
                if(reply.success&&!Persist(()=>{Accept(reply);Session.enabled=true;SaveSession();}))reply=StorageFailure();
                done(reply);
            });
        }
        public IEnumerator VerifyPurchase(PurchaseRequest purchase,Action<ServerReply> done)
        {
            if(Pending!=null){done(new ServerReply{message="미확인 게임 거래를 먼저 동기화해 주세요."});yield break;}
            yield return Request("/v1/commerce/verify","POST",JsonUtility.ToJson(purchase),(body,status)=>{var reply=Parse(body,status);if(reply.state!=null)Persist(()=>Accept(reply));done(reply);});
        }
        public bool AcceptAd(AdClaim claim)
        {
            return claim!=null&&claim.reply!=null&&claim.reply.state!=null&&claim.reply.playerId==Session.playerId&&claim.reply.revision>=Session.revision&&Persist(()=>Accept(claim.reply));
        }
        public IEnumerator AdRequest(string path,string method,GameCommand body,Action<AdClaim> done)
        {
            if(Pending!=null){done(null);yield break;}
            yield return Request(path,method,body==null?null:JsonUtility.ToJson(body),(text,status)=>{
                AdClaim claim=null;
                if(status==200){try{claim=JsonUtility.FromJson<AdClaim>(text);}catch(ArgumentException){}}
                if(claim!=null&&!AcceptAd(claim))claim=null;
                done(claim);
            });
        }
        public IEnumerator Get(string path, Action<string,long> done) { yield return Request(path, "GET", null, done); }
        public IEnumerator Send(string path, string method, GameCommand body, Action<string,long> done)
        { yield return Request(path, method, body == null ? null : JsonUtility.ToJson(body), done); }
        public ServerReply Parse(string body, long status)
        {
            try
            {
                var reply = JsonUtility.FromJson<ServerReply>(body);
                if (reply != null) { if (status < 200 || status >= 300) reply.success = false; if (string.IsNullOrEmpty(reply.message)) reply.message = "서버 응답을 확인할 수 없어요."; return reply; }
            }
            catch { }
            return new ServerReply { message = status == 429 ? "요청이 많아요. 잠시 뒤 다시 시도해 주세요." : "서버에 연결하지 못했어요. 미확인 거래는 다시 동기화할 수 있어요." };
        }
        static bool Persist(Action action)
        {
            try { action(); return true; }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (AndroidJavaException) { return false; }
        }
        static ServerReply StorageFailure() { return new ServerReply { message = "계정 요청 기록을 저장하지 못했어요. 저장 공간을 확인하고 다시 동기화해 주세요." }; }
        void Accept(ServerReply reply) { ObserveContent(reply.content); Latest = reply; Session.playerId = reply.playerId; Session.revision = reply.revision; SaveSession(); }
        IEnumerator Request(string path, string method, string body, Action<string,long> done,string endpoint=null)
        {
            if (Busy) { done("",0); yield break; }
            Busy = true;
            using (var req = new UnityWebRequest((endpoint??Session.endpoint) + path, method))
            {
                req.downloadHandler = new DownloadHandlerBuffer(); req.timeout = path=="/v1/release"?8:25;
                req.SetRequestHeader(AppRelease.BuildHeader,AppRelease.Build.ToString(System.Globalization.CultureInfo.InvariantCulture));
                req.SetRequestHeader(AppRelease.RulesHeader,Combat.RulesVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
                if (body != null) { req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)); req.SetRequestHeader("Content-Type", "application/json"); }
                if (path!="/v1/release" && endpoint==null && !string.IsNullOrEmpty(Session.secret)) req.SetRequestHeader("Authorization", "Bearer " + Session.secret);
                UnityWebRequestAsyncOperation operation = null;
                try { operation = req.SendWebRequest(); } catch (InvalidOperationException) { }
                if (operation != null) yield return operation;
                Busy = false;
                if(req.responseCode==426)
                {
                    var reply=Parse(req.downloadHandler.text,426);
                    ObserveRelease(endpoint??Session.endpoint,reply.release);
                }
                done(req.downloadHandler.text, req.responseCode);
            }
        }
        public void Disconnect() { Session.enabled = false; SaveSession(); }
        public void Forget()
        {
            Session = new CloudSession(); Pending = null; Latest = null; Content=null; Release=null;ReleaseEndpoint=null;ReleaseNotice=false; SaveSession();
            if(File.Exists(releasePath))File.Delete(releasePath);
            if (File.Exists(pendingPath)) File.Delete(pendingPath);
        }
    }
}
