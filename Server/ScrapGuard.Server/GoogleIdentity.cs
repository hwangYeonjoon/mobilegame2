using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Google.Apis.Auth;
using ScrapGuard.Core;
namespace ScrapGuard.Server;
public interface IGoogleIdentityToken
{
    string ClientId {get;}
    Task<string> Subject(string token,string nonce);
}
public sealed class GoogleIdentityToken(IConfiguration configuration):IGoogleIdentityToken
{
    public string ClientId=>configuration["GOOGLE_WEB_CLIENT_ID"]??"";
    public async Task<string> Subject(string token,string nonce)
    {
        if(ClientId.Length==0)throw new ApiFault(503,"Google 로그인 설정이 필요해요.");
        if(string.IsNullOrEmpty(token)||token.Length>16000)throw new ApiFault(400,"로그인 토큰이 필요해요.");
        try
        {
            var payload=await GoogleJsonWebSignature.ValidateAsync(token,new GoogleJsonWebSignature.ValidationSettings{Audience=new[]{ClientId}});
            if(payload.Nonce!=nonce||string.IsNullOrEmpty(payload.Subject))throw new ApiFault(401,"Google 로그인 확인 정보가 일치하지 않아요.");
            return payload.Subject;
        }
        catch(InvalidJwtException){throw new ApiFault(401,"Google 로그인을 다시 시도해 주세요.");}
    }
}
public sealed class GoogleIdentityFlow(IGoogleIdentityToken verifier,GameRepository repo)
{
    sealed record Challenge(bool Link,string Binding,DateTimeOffset Expires);
    readonly ConcurrentDictionary<string,Challenge> challenges=new();
    static string Hash(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text??"")));
    public GoogleChallenge Begin(string token,GoogleChallengeRequest request)
    {
        if(verifier.ClientId.Length==0)throw new ApiFault(503,"운영자의 Google 로그인 설정이 필요해요.");
        if(request==null)throw new ApiFault(400,"로그인 요청이 필요해요.");
        foreach(var row in challenges)if(row.Value.Expires<DateTimeOffset.UtcNow)challenges.TryRemove(row.Key,out _);
        if(challenges.Count>=1000)throw new ApiFault(429,"잠시 후 다시 시도해 주세요.");
        string binding;
        if(request.link)binding=repo.Bootstrap(token).playerId;
        else {if(!System.Text.RegularExpressions.Regex.IsMatch(request.secret??"","^[A-Za-z0-9_-]{43}$"))throw new ApiFault(400,"새 복구 코드가 필요해요.");binding=Hash(request.secret);}
        string nonce=Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        challenges[nonce]=new Challenge(request.link,binding,DateTimeOffset.UtcNow.AddMinutes(5));
        return new GoogleChallenge{nonce=nonce,clientId=verifier.ClientId};
    }
    public async Task<ServerReply> Finish(string token,GoogleLoginRequest request)
    {
        if(request==null||request.nonce==null||!challenges.TryGetValue(request.nonce,out var c)||c.Expires<DateTimeOffset.UtcNow||c.Link!=request.link)throw new ApiFault(401,"로그인 요청이 만료됐어요.");
        string binding=request.link?repo.Bootstrap(token).playerId:Hash(request.secret);
        if(binding!=c.Binding)throw new ApiFault(401,"로그인 계정이 변경됐어요.");
        string subject=await verifier.Subject(request.idToken,request.nonce);
        if(!challenges.TryRemove(request.nonce,out _))throw new ApiFault(409,"이미 처리한 로그인 요청이에요.");
        return request.link?repo.LinkGoogle(token,subject):repo.RecoverGoogle(subject,request.secret);
    }
}
