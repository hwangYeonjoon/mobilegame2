using System;
namespace ScrapGuard.Core
{
    public static class AppRelease
    {
        public const int Build=15, MinimumBuild=14;
        public const string Version="0.15.0", Package="com.scrapguard.mobilegame2.prototype";
        public const string BuildHeader="X-Scrap-Build", RulesHeader="X-Scrap-Rules";
    }
    [Serializable] public class ReleasePolicy
    {
        public int schema=1, latestBuild, minimumBuild, combatRules;
        public string latestVersion, packageName, notes;
        public bool playAvailable;
    }
    public enum UpdateRequirement { Invalid, Current, Recommended, Required, ServerMismatch }
    public static class ReleaseRules
    {
        public static bool Valid(ReleasePolicy p)
        {
            Version version;
            return p!=null && p.schema==1 && p.minimumBuild>0 && p.latestBuild>=p.minimumBuild && p.combatRules>0 &&
                p.packageName==AppRelease.Package && !string.IsNullOrEmpty(p.latestVersion) && p.latestVersion.Length<=32 && Version.TryParse(p.latestVersion,out version) &&
                (p.notes==null || p.notes.Length<=500);
        }
        public static UpdateRequirement Evaluate(ReleasePolicy p,int build,int rules)
        {
            if(!Valid(p) || build<=0 || rules<=0)return UpdateRequirement.Invalid;
            if(build<p.minimumBuild)return UpdateRequirement.Required;
            if(rules!=p.combatRules)return build<p.latestBuild?UpdateRequirement.Required:UpdateRequirement.ServerMismatch;
            return build<p.latestBuild?UpdateRequirement.Recommended:UpdateRequirement.Current;
        }
        public static bool Blocks(UpdateRequirement requirement){return requirement==UpdateRequirement.Required || requirement==UpdateRequirement.ServerMismatch;}
        public static string StoreUrl(ReleasePolicy p){return Valid(p) && p.playAvailable?"https://play.google.com/store/apps/details?id="+AppRelease.Package:"";}
        public static bool CompletePending(bool success,long status)
        {return success || status>=400 && status<500 && status!=408 && status!=426 && status!=429;}
    }
}
