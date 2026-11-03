using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
using ScrapGuard.Server;
static class CommerceChecks
{
    sealed class Provider: IPlayPurchases
    {
        public bool Enabled{get;set;}=true;
        public string Binding;public bool Purchased=true,Canceled,Test=true;public int Quantity=1;
        public Task<VerifiedPurchase> Verify(PurchaseRequest p,CancellationToken ct)=>Task.FromResult(new VerifiedPurchase(p.purchaseToken,p.productId,Binding,Purchased,Canceled,Test,Quantity));
    }
    public static void Run(string folder,Action<bool,string> check)
    {
        string path=Path.Combine(folder,"commerce.db"),key=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
        var now=DateTimeOffset.UtcNow;var repo=new GameRepository(path,()=>now);var initial=repo.Register(key);var provider=new Provider{Binding=repo.Commerce(key,true).accountBinding};
        ServerReply Buy(string token,string product="gems_600")=>repo.VerifyPurchase(key,new PurchaseRequest{productId=product,purchaseToken=token},provider).GetAwaiter().GetResult();
        void Fails(Action a,int status,string name){try{a();throw new Exception(name);}catch(ApiFault e){check(e.Status==status,name);}}
        provider.Enabled=false;Fails(()=>Buy("disabled"),503,"disabled commerce cannot grant");provider.Enabled=true;
        provider.Purchased=false;Fails(()=>Buy("pending"),409,"pending purchase grants nothing");provider.Purchased=true;
        provider.Test=false;Fails(()=>Buy("real"),400,"license-test server rejects real payment");provider.Test=true;
        provider.Binding="other";Fails(()=>Buy("other"),400,"purchase must bind to authenticated account");provider.Binding=repo.Commerce(key,true).accountBinding;
        provider.Quantity=2;Fails(()=>Buy("multi"),400,"unsupported multi-quantity order rejected");provider.Quantity=1;
        var purchased=Buy("one");check(purchased.state.gems==600&&purchased.state.paidGems==600&&purchased.state.testProgress,"verified test credit has paid provenance and test marker");
        check(Buy("one").state.gems==600,"repeat token grants once");
        ServerReply Apply(GameCommand c){c.revision=repo.Bootstrap(key).revision;return repo.Apply(key,c);}
        Apply(new GameCommand("attendance"));
        now=now.AddDays(1);Apply(new GameCommand("attendance"));now=now.AddDays(1);Apply(new GameCommand("attendance"));
        var spent=Apply(new GameCommand("buy"){value="parts_gems"});check(spent.state.gems==550&&spent.state.paidGems==550,"free gems consumed before purchase balance");
        repo.RevokePurchase("one");var refunded=repo.Bootstrap(key);check(refunded.state.gems==0&&refunded.state.paidGems==0&&refunded.state.gemDebt==50,"refund removes remaining purchase and tracks spent debt");
        repo.RevokePurchase("one");check(repo.Bootstrap(key).state.gemDebt==50,"refund callback is idempotent");
        Fails(()=>Buy("one"),409,"revoked purchase cannot restore credit");
        var paidDebt=Buy("two");check(paidDebt.state.gems==550&&paidDebt.state.gemDebt==0,"next credit settles refund adjustment without negative balance");
        Buy("skip","ad_skip_lifetime");check(repo.Bootstrap(key).state.adSkip,"verified permanent right granted");
        var reward=Apply(new GameCommand("ad_skip_reward",Guid.NewGuid().ToString("N")){value="parts"});check(reward.state.dailyRewardParts==1,"verified ad skip uses common daily quota");
        repo.RevokePurchase("skip");check(!repo.Bootstrap(key).state.adSkip,"refund removes permanent right");
        repo.RevokePurchase("arrived_first");Fails(()=>Buy("arrived_first"),409,"refund before grant leaves tombstone");
        repo=new GameRepository(path,()=>now);check(Buy("two").state.gems==550,"purchase provenance survives database restart");
        // Isolated fixture for ranked test; commerce test accounts must never rank.
        var s=GameState.New();s.publicProfile=true;s.clearedStages.AddRange(new[]{0,1,2,3,4});s.heroes[0].level=30;foreach(var item in s.items)item.level=20;
        using(var db=new SqliteConnection("Data Source="+path)){db.Open();using var q=db.CreateCommand();q.CommandText="UPDATE accounts SET state=$state WHERE id=$id";q.Parameters.AddWithValue("$state",JsonSerializer.Serialize(s,GameRepository.Json));q.Parameters.AddWithValue("$id",initial.playerId);q.ExecuteNonQuery();}
        var begun=Apply(new GameCommand("battle_start"){mode=SkillTrial.Mode});var c=new Combat(begun.state,4,begun.ticket.id,begun.ticket.seed,false,SkillTrial.Mode);var recorder=new ReplayRecorder();
        while(c.Elapsed<32&&!c.Ended){if(c.Choosing){int skill=c.Choices[0];c.Choose(skill);recorder.Choose(skill);}var target=new V2(270+(float)Math.Cos(c.Elapsed*.4f)*180,490+(float)Math.Sin(c.Elapsed*.4f)*230);
            V2 away=new V2();foreach(var enemy in c.Enemies){var d=c.Position-enemy.position;if(d.Length<130)away+=d.Normal*((130-d.Length)/60);}
            c.Tick(ReplayRecorder.Step,recorder.Move(((target-c.Position).Normal+away).Normal));}
        now=now.AddMinutes(2);var finish=new GameCommand("battle_finish",begun.ticket.id){replay=recorder.Steps};var done=Apply(finish);
        check(done.state.skillWeekly.Count==1&&done.state.weekly.Count==0&&done.state.gems==0,"skill trial cannot earn growth rewards");
        check(repo.Weekly(key,true).entries.Count==1&&repo.Weekly(key).entries.Count==0,"skill leaderboard is separate and server derived");
        repo.Apply(key,finish);check(repo.Bootstrap(key).state.skillWeekly[0].attempts==1,"skill settlement retry is idempotent");
    }
}
