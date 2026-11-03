using Microsoft.Extensions.Configuration;
using ScrapGuard.Core;
using ScrapGuard.Server;
static class ReleaseChecks
{
    public static void Run(Action<bool,string> check)
    {
        ClientRelease Make(params (string,string)[] values)=>new(new ConfigurationBuilder().AddInMemoryCollection(values.Select(v=>new KeyValuePair<string,string?>(v.Item1,v.Item2))).Build());
        var release=Make();check(release.Policy.latestBuild==AppRelease.Build && release.Policy.minimumBuild==AppRelease.Build && !release.Policy.playAvailable,"release defaults match packaged app and unpublished Play state");
        check(release.Accepts(AppRelease.Build.ToString(),Combat.RulesVersion.ToString()),"matching build and rules accepted");
        foreach(string build in new[]{"", "-1", "0", "13", "14,15", "2147483648"})check(!release.Accepts(build,Combat.RulesVersion.ToString()),"legacy or invalid build header rejected "+build);
        check(!release.Accepts("14","10"),"incompatible rules rejected before account mutation");
        var optional=Make(("CLIENT_LATEST_BUILD","15"),("CLIENT_MIN_BUILD","14"),("CLIENT_LATEST_VERSION","0.15.0"),("PLAY_UPDATE_AVAILABLE","true"));
        check(optional.Accepts("14",Combat.RulesVersion.ToString()) && ReleaseRules.Evaluate(optional.Policy,14,Combat.RulesVersion)==UpdateRequirement.Recommended,"rollout can recommend while retaining minimum");
        var required=Make(("CLIENT_LATEST_BUILD","15"),("CLIENT_MIN_BUILD","15"),("CLIENT_LATEST_VERSION","0.15.0"));
        check(!required.Accepts("14",Combat.RulesVersion.ToString()) && required.Rejection().release.minimumBuild==15 && required.Rejection().code=="client_update_required","typed 426 response describes required update");
        bool invalid=false;try{Make(("CLIENT_MIN_BUILD","99"));}catch(InvalidOperationException){invalid=true;}check(invalid,"invalid rollout configuration fails server startup");
        check(ClientRelease.Exempt("/v1/release","GET") && ClientRelease.Exempt("/health/ready","GET") && ClientRelease.Exempt("/v1/account","DELETE"),"release health and account deletion remain reachable");
        check(!ClientRelease.Exempt("/v1/commands","POST") && !ClientRelease.Exempt("/v1/auth/guest","POST") && !ClientRelease.Exempt("/v1/commerce/verify","POST"),"commands registration and commerce cannot bypass version gate");
    }
}
