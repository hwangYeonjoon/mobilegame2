using System;
using UnityEngine;
namespace ScrapGuard
{
    [Serializable] public sealed class ServiceSettings
    {
        public bool adsEnabled,productionAds;
        public string adMobAppId="",rewardedUnitId="",privacyUrl="",supportUrl="",deletionUrl="";
        public string[] testDeviceIds=new string[0];
        public static ServiceSettings Load()
        {
            var text=Resources.Load<TextAsset>("ServiceSettings");
            try{return text==null?new ServiceSettings():JsonUtility.FromJson<ServiceSettings>(text.text)??new ServiceSettings();}
            catch(ArgumentException){return new ServiceSettings();}
        }
        public static bool PublicUrl(string value)
        {Uri uri;return Uri.TryCreate(value,UriKind.Absolute,out uri)&&uri.Scheme=="https"&&uri.UserInfo.Length==0&&!uri.Host.EndsWith("example.com",StringComparison.OrdinalIgnoreCase);}
    }
}
