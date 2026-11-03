using System.Security.Cryptography;
using ScrapGuard.Core;
using ScrapGuard.Server;
static class IdentityChecks
{
    sealed class Provider:IGoogleIdentityToken
    {
        public string ClientId=>"test-client";public string Account="subject-a";
        public Task<string> Subject(string token,string nonce)
        {if(token!="valid")throw new ApiFault(401,"invalid token");return Task.FromResult(Account);}
    }
    public static void Run(string folder,Action<bool,string> check)
    {
        string Key()=>Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
        var repo=new GameRepository(Path.Combine(folder,"identity.db"));string a=Key(),b=Key();var aa=repo.Register(a);repo.Register(b);var provider=new Provider();var flow=new GoogleIdentityFlow(provider,repo);
        void Fails(Action action,int status,string message){try{action();throw new Exception(message);}catch(ApiFault e){check(e.Status==status,message);}}
        var challenge=flow.Begin(a,new GoogleChallengeRequest{link=true});var login=new GoogleLoginRequest{link=true,nonce=challenge.nonce,idToken="invalid"};
        Fails(()=>flow.Finish(a,login).GetAwaiter().GetResult(),401,"invalid Google signature adapter cannot link");login.idToken="valid";
        check(flow.Finish(a,login).GetAwaiter().GetResult().success,"verified identity links existing account");
        Fails(()=>flow.Finish(a,login).GetAwaiter().GetResult(),401,"login challenge cannot replay");
        var other=flow.Begin(b,new GoogleChallengeRequest{link=true});Fails(()=>flow.Finish(b,new GoogleLoginRequest{link=true,nonce=other.nonce,idToken="valid"}).GetAwaiter().GetResult(),409,"same Google identity cannot merge two accounts");
        string replacement=Key();var recover=flow.Begin(null,new GoogleChallengeRequest{secret=replacement});
        Fails(()=>flow.Finish(null,new GoogleLoginRequest{nonce=recover.nonce,idToken="valid",secret=Key()}).GetAwaiter().GetResult(),401,"recovery nonce binds new secret");
        var done=flow.Finish(null,new GoogleLoginRequest{nonce=recover.nonce,idToken="valid",secret=replacement}).GetAwaiter().GetResult();
        check(done.playerId==aa.playerId,"Google recovery preserves server progression");Fails(()=>repo.Bootstrap(a),401,"Google recovery revokes old bearer");
        check(repo.Bootstrap(replacement).playerId==aa.playerId,"new secret authenticates recovered account");
        repo.Delete(replacement);Fails(()=>repo.RecoverGoogle("subject-a",Key()),404,"account deletion removes Google link");
    }
}
