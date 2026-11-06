using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace ScrapGuard.Editor
{
    public sealed class BuildFootprint : IPostprocessBuildWithReport
    {
        public int callbackOrder {get{return 9999;}}
        [Serializable] sealed class Entry {public string path;public long bytes;}
        [Serializable] sealed class Footprint {public string target,options,output;public long unityReportedBytes,artifactBytes,musicBytes;public Entry[] largestAssets;}
        public void OnPostprocessBuild(BuildReport report)
        {
            var entries=report.packedAssets.SelectMany(p=>p.contents).GroupBy(p=>p.sourceAssetPath)
                .Select(g=>new Entry{path=g.Key??"",bytes=g.Sum(x=>(long)x.packedSize)}).ToArray();
            var result=new Footprint{target=report.summary.platform.ToString(),options=report.summary.options.ToString(),output=report.summary.outputPath,
                unityReportedBytes=(long)report.summary.totalSize,artifactBytes=File.Exists(report.summary.outputPath)?new FileInfo(report.summary.outputPath).Length:0,musicBytes=entries.Where(e=>e.path.StartsWith("Assets/Resources/Audio/Licensed/")&&e.path.EndsWith(".ogg")).Sum(e=>e.bytes),
                largestAssets=entries.OrderByDescending(e=>e.bytes).Take(30).ToArray()};
            Directory.CreateDirectory("TestResults/BuildSize");File.WriteAllText("TestResults/BuildSize/"+Path.GetFileNameWithoutExtension(report.summary.outputPath)+(report.summary.platform==BuildTarget.Android&&EditorUserBuildSettings.buildAppBundle?(Environment.GetEnvironmentVariable("SCRAP_AUDIO_BASELINE")=="1"?"-baseline":"-optimized"):"")+".json",JsonUtility.ToJson(result,true));
        }
        static void ReimportMusic()
        {foreach(string name in new[]{"Factory","Overdrive","Boss"})AssetDatabase.ImportAsset("Assets/Resources/Audio/Licensed/"+name+".ogg",ImportAssetOptions.ForceUpdate);}
        public static void BuildBaseline()
        {
            var previous=Environment.GetEnvironmentVariable("SCRAP_AUDIO_BASELINE");
            try{Environment.SetEnvironmentVariable("SCRAP_AUDIO_BASELINE","1");ReimportMusic();ProjectBuilder.BuildAndroidBundle();}
            finally{Environment.SetEnvironmentVariable("SCRAP_AUDIO_BASELINE",previous);ReimportMusic();}
        }
        public static void BuildOptimized(){ReimportMusic();ProjectBuilder.BuildAndroidBundle();}
    }
}
