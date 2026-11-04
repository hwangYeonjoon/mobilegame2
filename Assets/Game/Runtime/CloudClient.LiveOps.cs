using System;
using System.Collections;
using System.Text;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.Networking;
namespace ScrapGuard
{
    public sealed partial class CloudClient
    {
        public ContentCatalog Content {get;private set;}
        string contentEndpoint;float contentReceived,contentAttempt=-100;bool contentLoading;
        void ObserveContent(ContentCatalog catalog)
        {if(catalog==null)return;Content=catalog;contentEndpoint=Session.endpoint;contentReceived=Time.realtimeSinceStartup;}
        public bool ContentTime(out DateTimeOffset now)
        {
            now=default(DateTimeOffset);DateTimeOffset baseline;
            if(Content==null||contentEndpoint!=Session.endpoint||Time.realtimeSinceStartup-contentReceived>300||!DateTimeOffset.TryParse(Content.serverTime,out baseline))return false;
            now=baseline.AddSeconds(Math.Max(0,Time.realtimeSinceStartup-contentReceived));return true;
        }
        UnityWebRequest MetricsRequest(string path,string method,string body)
        {
            var r=new UnityWebRequest(Session.endpoint+path,method){downloadHandler=new DownloadHandlerBuffer(),timeout=8};
            r.SetRequestHeader(AppRelease.BuildHeader,AppRelease.Build.ToString());r.SetRequestHeader(AppRelease.RulesHeader,Combat.RulesVersion.ToString());
            r.SetRequestHeader("Authorization","Bearer "+Session.secret);
            if(body!=null){r.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));r.SetRequestHeader("Content-Type","application/json");}return r;
        }
        public IEnumerator RefreshContent(Action refreshed)
        {
            if(contentLoading||Time.realtimeSinceStartup-contentAttempt<30||!Session.enabled||!ValidEndpoint(Session.endpoint))yield break;
            contentLoading=true;contentAttempt=Time.realtimeSinceStartup;string endpoint=Session.endpoint,secret=Session.secret;
            using(var req=MetricsRequest("/v1/content","GET",null))
            {
                yield return req.SendWebRequest();contentLoading=false;
                if(req.responseCode!=200||endpoint!=Session.endpoint||secret!=Session.secret||!Session.enabled)yield break;
                ContentCatalog catalog=null;try{catalog=JsonUtility.FromJson<ContentCatalog>(req.downloadHandler.text);}catch(ArgumentException){}
                if(catalog!=null){ObserveContent(catalog);refreshed?.Invoke();}
            }
        }
        public IEnumerator SendOutfitPreview(int outfit)
        {
            if(!Session.enabled||Latest==null||!Latest.metricsConsent||!ValidEndpoint(Session.endpoint))yield break;
            // Best effort only; no durable journal, currency mutation or gameplay Busy lock.
            using(var req=MetricsRequest("/v1/metrics/preview","POST",JsonUtility.ToJson(new GameCommand("preview"){skin=outfit})))yield return req.SendWebRequest();
        }
    }
}
