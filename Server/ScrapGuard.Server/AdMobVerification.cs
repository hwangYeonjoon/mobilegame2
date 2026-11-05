using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
namespace ScrapGuard.Server;

public sealed class AdPolicy
{
    public string Mode {get;}
    public bool Enabled => Mode != "disabled";
    public string UnitId {get;}
    public string CallbackUnit => UnitId.Split('/').Last();
    public const string TestUnit = "ca-app-pub-3940256099942544/5224354917";
    // The AdMob console sends this placeholder unit even when editing a real unit.
    // A signed, explicitly named configuration probe never grants a game reward.
    public bool IsConsoleProbe(VerifiedAd ad,DateTimeOffset now) =>
        Mode=="test"&&ad.Unit=="1234567890"&&ad.User=="scrapguard-ssv-probe"&&ad.Claim=="configuration-check"
        &&ad.Amount==1&&ad.Reward=="reward"
        &&ad.Timestamp>=now.AddMinutes(-5).ToUnixTimeMilliseconds()&&ad.Timestamp<=now.AddMinutes(1).ToUnixTimeMilliseconds();
    public AdPolicy(IConfiguration c)
    {
        Mode=c["ADS_MODE"]??"disabled";UnitId=c["ADMOB_REWARDED_UNIT_ID"]??"";
        if(Mode!="disabled"&&Mode!="test"&&Mode!="production")throw new InvalidOperationException("Unknown ADS_MODE.");
        if(Enabled&&!System.Text.RegularExpressions.Regex.IsMatch(UnitId,@"^ca-app-pub-\d{16}/\d{10}$"))throw new InvalidOperationException("A rewarded unit ID is required.");
        if(Mode=="production"&&(!c.GetValue("ADS_RELEASE_READY",false)||UnitId.Contains("3940256099942544")))throw new InvalidOperationException("Production ads require reviewed settings and a game-specific unit.");
    }
}
public sealed record VerifiedAd(string Claim,string User,string Unit,string Transaction,long Timestamp,string Reward,int Amount);
public sealed class AdMobVerifier
{
    readonly HttpClient http=new(){Timeout=TimeSpan.FromSeconds(10)};
    readonly SemaphoreSlim mutex=new(1,1);
    Dictionary<string,string> keys=new();
    DateTimeOffset refreshed=DateTimeOffset.MinValue;
    public async Task<VerifiedAd> Verify(string query,CancellationToken ct)
    {
        var parts=Split(query);
        await mutex.WaitAsync(ct);
        string pem;
        try
        {
            var age=DateTimeOffset.UtcNow-refreshed;
            if(age>TimeSpan.FromHours(12)||(!keys.ContainsKey(parts.key)&&age>TimeSpan.FromMinutes(1)))
            {
                try
                {
                    using var json=JsonDocument.Parse(await http.GetStringAsync("https://www.gstatic.com/admob/reward/verifier-keys.json",ct));
                    var updated=new Dictionary<string,string>();
                    foreach(var key in json.RootElement.GetProperty("keys").EnumerateArray())updated.Add(key.GetProperty("keyId").ToString(),key.GetProperty("pem").GetString()!);
                    keys=updated;refreshed=DateTimeOffset.UtcNow;
                }
                catch(Exception ex) when(ex is HttpRequestException or JsonException or TaskCanceledException)
                {throw new ApiFault(503,"광고 서명 키를 확인하지 못했어요.");}
            }
            if(!keys.TryGetValue(parts.key,out pem))throw new ApiFault(400,"알 수 없는 광고 서명 키예요.");
        }
        finally{mutex.Release();}
        return VerifyWithKey(query,pem);
    }
    static (string data,string signature,string key) Split(string query)
    {
        string raw=(query??"").TrimStart('?');int marker=raw.IndexOf("&signature=",StringComparison.Ordinal);
        if(raw.Length>8192||marker<=0)throw new ApiFault(400,"광고 서명이 없어요.");
        var tail=raw[(marker+11)..].Split('&');
        if(tail.Length!=2||!tail[1].StartsWith("key_id=",StringComparison.Ordinal))throw new ApiFault(400,"광고 서명 형식이 올바르지 않아요.");
        return(raw[..marker],Uri.UnescapeDataString(tail[0]),tail[1][7..]);
    }
    public static VerifiedAd VerifyWithKey(string query,string pem)
    {
        var part=Split(query);
        try
        {
            using var key=ECDsa.Create();key.ImportFromPem(pem);
            byte[] sig=WebEncoders.Base64UrlDecode(part.signature);
            if(!key.VerifyData(Encoding.UTF8.GetBytes(part.data),sig,HashAlgorithmName.SHA256,DSASignatureFormat.Rfc3279DerSequence))throw new ApiFault(400,"광고 서명이 일치하지 않아요.");
            var values=QueryHelpers.ParseQuery(part.data);
            if(values.Any(p=>p.Value.Count!=1)||values.ContainsKey("signature")||values.ContainsKey("key_id"))throw new ApiFault(400,"광고 값이 중복됐어요.");
            string Value(string name)=>values.TryGetValue(name,out var v)&&v.ToString().Length is >0 and <=256?v.ToString():throw new ApiFault(400,"필수 광고 값이 없어요.");
            if(!long.TryParse(Value("timestamp"),out long stamp)||!int.TryParse(Value("reward_amount"),out int amount))throw new ApiFault(400,"잘못된 광고 보상 값이에요.");
            return new(Value("custom_data"),Value("user_id"),Value("ad_unit"),Value("transaction_id"),stamp,Value("reward_item"),amount);
        }
        catch(Exception ex)when(ex is FormatException or CryptographicException or ArgumentException){throw new ApiFault(400,"광고 서명을 확인할 수 없어요.");}
    }
}
