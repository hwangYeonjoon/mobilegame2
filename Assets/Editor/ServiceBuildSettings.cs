using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace ScrapGuard.Editor
{
    public sealed class ServiceBuildSettings : IPreprocessBuildWithReport
    {
        public int callbackOrder=>-1000;
        public void OnPreprocessBuild(BuildReport report){Apply();}
        public static void Apply()
        {
            var config=ServiceSettings.Load();
            if(!System.Text.RegularExpressions.Regex.IsMatch(config.adMobAppId??"",@"^ca-app-pub-\d{16}~\d{10}$"))throw new BuildFailedException("Set a valid AdMob app ID in ServiceSettings.json (sample ID while ads are disabled).");
            if(config.productionAds&&(config.adMobAppId.Contains("3940256099942544")||config.rewardedUnitId.Contains("3940256099942544")||!ServiceSettings.PublicUrl(config.privacyUrl)))throw new BuildFailedException("Production ads need game-specific IDs and a public privacy URL.");
            const string path="Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
            var asset=AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if(asset==null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var script=AssetDatabase.LoadAssetAtPath<MonoScript>("Packages/com.google.ads.mobile/GoogleMobileAds/Editor/GoogleMobileAdsSettings.cs");
                if(script==null)throw new BuildFailedException("Restore the pinned Google Mobile Ads package before building.");
                asset=ScriptableObject.CreateInstance(script.GetClass());AssetDatabase.CreateAsset(asset,path);
            }
            var serialized=new SerializedObject(asset);
            serialized.FindProperty("adMobAndroidAppId").stringValue=config.adMobAppId;
            serialized.FindProperty("overrideDefaultGmaAndroidSdk").boolValue=true;
            serialized.FindProperty("selectedGmaAndroidSdk").intValue=0;
            serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();
        }
    }
}
