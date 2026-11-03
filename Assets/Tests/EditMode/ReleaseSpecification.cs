using ScrapGuard.Core;
namespace ScrapGuard.Tests
{
    public static partial class CoreSpecification
    {
        public static void ReleaseCompatibilityRules()
        {
            var p=new ReleasePolicy{latestBuild=15,minimumBuild=14,latestVersion="0.15.0",combatRules=Combat.RulesVersion,packageName=AppRelease.Package};
            Check(ReleaseRules.Evaluate(p,14,Combat.RulesVersion)==UpdateRequirement.Recommended,"compatible old build only recommends");
            Check(ReleaseRules.Evaluate(p,13,Combat.RulesVersion)==UpdateRequirement.Required,"below minimum requires update");
            Check(ReleaseRules.Evaluate(p,15,Combat.RulesVersion)==UpdateRequirement.Current && ReleaseRules.Evaluate(p,16,Combat.RulesVersion)==UpdateRequirement.Current,"current or newer never downgraded");
            p.combatRules++;
            Check(ReleaseRules.Evaluate(p,14,Combat.RulesVersion)==UpdateRequirement.Required,"newer server rules require newer app");
            Check(ReleaseRules.Evaluate(p,16,Combat.RulesVersion)==UpdateRequirement.ServerMismatch,"newer app with different rules waits for server instead of downgrading");
            Check(!ReleaseRules.Blocks(UpdateRequirement.Recommended) && ReleaseRules.Blocks(UpdateRequirement.Required) && ReleaseRules.Blocks(UpdateRequirement.ServerMismatch),"only incompatible versions block gameplay");
        }
        public static void ReleaseValidationAndStoreLink()
        {
            var p=new ReleasePolicy{latestBuild=14,minimumBuild=14,latestVersion="0.14.0",combatRules=11,packageName=AppRelease.Package};
            Check(ReleaseRules.Valid(p) && ReleaseRules.StoreUrl(p)=="","unpublished update never links to nonexistent listing");
            p.playAvailable=true;Check(ReleaseRules.StoreUrl(p)=="https://play.google.com/store/apps/details?id="+AppRelease.Package,"fixed official Play listing only");
            p.packageName="evil.example";Check(!ReleaseRules.Valid(p) && ReleaseRules.StoreUrl(p)=="","server cannot redirect to another package");p.packageName=AppRelease.Package;
            p.minimumBuild=15;Check(ReleaseRules.Evaluate(p,14,11)==UpdateRequirement.Invalid,"minimum above latest invalid");p.minimumBuild=14;
            p.schema=2;Check(!ReleaseRules.Valid(p),"unknown release schema rejected");p.schema=1;
            p.latestVersion="<b>new</b>";Check(!ReleaseRules.Valid(p),"invalid version text rejected");
        }
        public static void UpdatePreservesPendingTransactions()
        {
            foreach(long status in new long[]{0,408,426,429,500,503})Check(!ReleaseRules.CompletePending(false,status),"transient or update gate keeps request "+status);
            Check(ReleaseRules.CompletePending(true,200) && ReleaseRules.CompletePending(false,400) && ReleaseRules.CompletePending(false,409),"only success or final rejection completes journal");
        }
    }
}
