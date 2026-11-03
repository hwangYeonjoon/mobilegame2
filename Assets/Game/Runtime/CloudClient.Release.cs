using System;
using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    [Serializable] public class ReleaseCache { public string endpoint; public ReleasePolicy policy; }
    public sealed partial class CloudClient
    {
        string releasePath;
        public ReleasePolicy Release { get; private set; }
        public string ReleaseEndpoint { get; private set; }
        public string ReleaseError { get; private set; }
        public bool ReleaseNotice { get; set; }
        public UpdateRequirement UpdateState { get { return ReleaseRules.Evaluate(Release,AppRelease.Build,Combat.RulesVersion); } }
        public bool UpdateBlocked { get { return ReleaseRules.Blocks(UpdateState); } }
        void LoadRelease(string endpoint)
        {
            if(endpoint==ReleaseEndpoint)return;
            Release=null;ReleaseEndpoint=endpoint;ReleaseError="";
            var cache=Read<ReleaseCache>(releasePath);
            if(cache!=null && cache.endpoint==endpoint && ReleaseRules.Valid(cache.policy)){Release=cache.policy;ReleaseNotice=true;}
        }
        public bool ObserveRelease(string endpoint,ReleasePolicy policy)
        {
            if(!ValidEndpoint(endpoint) || !ReleaseRules.Valid(policy))return false;
            Release=JsonUtility.FromJson<ReleasePolicy>(JsonUtility.ToJson(policy));ReleaseEndpoint=endpoint;ReleaseError="";ReleaseNotice=true;
            Persist(()=>Atomic(releasePath,JsonUtility.ToJson(new ReleaseCache{endpoint=endpoint,policy=Release})));
            return true;
        }
        public IEnumerator CheckRelease(string endpoint,Action<bool> done)
        {
            endpoint=(endpoint??"").Trim().TrimEnd('/');
            if(Busy || !ValidEndpoint(endpoint)){done(false);yield break;}
            LoadRelease(endpoint);bool success=false;
            yield return Request("/v1/release","GET",null,(body,status)=>{
                if(status==200)
                {try{success=ObserveRelease(endpoint,JsonUtility.FromJson<ReleasePolicy>(body));}catch(ArgumentException){}}
                if(!success){ReleaseError="업데이트 정보를 확인하지 못했어요. 연결 상태를 확인하고 다시 시도해 주세요.";if(UpdateBlocked)ReleaseNotice=true;}
            },endpoint);
            done(success);
        }
        public ServerReply UpdateFailure()=>new ServerReply{code="client_update_required",message=UpdateBlocked?"앱 업데이트 안내를 확인해 주세요.":ReleaseError??"업데이트 정보를 먼저 확인해 주세요.",release=Release};
    }
}
