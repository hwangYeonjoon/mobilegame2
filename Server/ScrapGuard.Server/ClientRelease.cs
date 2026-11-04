using System.Globalization;
using ScrapGuard.Core;
namespace ScrapGuard.Server;
public sealed class ClientRelease
{
    public ReleasePolicy Policy { get; }
    public ClientRelease(IConfiguration config)
    {
        Policy=new ReleasePolicy{
            latestBuild=config.GetValue("CLIENT_LATEST_BUILD",AppRelease.Build),
            minimumBuild=config.GetValue("CLIENT_MIN_BUILD",AppRelease.MinimumBuild),
            latestVersion=config["CLIENT_LATEST_VERSION"]??AppRelease.Version,
            combatRules=Combat.RulesVersion,packageName=AppRelease.Package,
            playAvailable=config.GetValue("PLAY_UPDATE_AVAILABLE",false),
            notes=config["CLIENT_UPDATE_NOTES"]??"새로운 수호대 콘텐츠와 안정성 개선을 확인하세요."
        };
        if(!ReleaseRules.Valid(Policy) || Policy.latestBuild<AppRelease.Build)
            throw new InvalidOperationException("Invalid client release configuration: minimum <= latest, valid version and latest >= server build are required.");
    }
    public bool Accepts(string buildHeader,string rulesHeader)
    {
        return int.TryParse(buildHeader,NumberStyles.None,CultureInfo.InvariantCulture,out var build) && build>=Policy.minimumBuild &&
            int.TryParse(rulesHeader,NumberStyles.None,CultureInfo.InvariantCulture,out var rules) && rules==Policy.combatRules;
    }
    public static bool Exempt(string path,string method)
    {return !path.StartsWith("/v1/",StringComparison.OrdinalIgnoreCase) || path.Equals("/v1/release",StringComparison.OrdinalIgnoreCase) || path=="/v1/ads/ssv" || path=="/v1/commerce/rtdn" || method=="DELETE"&&path.Equals("/v1/account",StringComparison.OrdinalIgnoreCase);}
    public ServerReply Rejection()=>new ServerReply{success=false,code="client_update_required",message=Policy.playAvailable?"새 버전으로 업데이트한 뒤 다시 연결해 주세요.":"호환되는 앱이 필요해요. 배포 상태를 확인한 뒤 다시 연결해 주세요.",release=Policy};
}
