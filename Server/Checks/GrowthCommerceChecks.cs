using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
using ScrapGuard.Server;
static class GrowthCommerceChecks
{
    sealed class Provider:IPlayPurchases
    {
        public bool Enabled=>true;public string Binding;
        public Task<VerifiedPurchase> Verify(PurchaseRequest p,CancellationToken ct)=>Task.FromResult(new VerifiedPurchase(p.purchaseToken,p.productId,Binding,true,false,true,1));
    }
    public static void Run(string folder,Action<bool,string> check)
    {
        string path=Path.Combine(folder,"growth-commerce.db"),key=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
        var now=DateTimeOffset.UtcNow;var repo=new GameRepository(path,()=>now);var first=repo.Register(key);
        var provider=new Provider{Binding=repo.Commerce(key,true).accountBinding};
        repo.VerifyPurchase(key,new PurchaseRequest{productId="gems_2600",purchaseToken="growth-test"},provider).GetAwaiter().GetResult();
        ServerReply Apply(GameCommand c){c.revision=repo.Bootstrap(key).revision;return repo.Apply(key,c);}
        void Reject(GameCommand c,string name){try{Apply(c);throw new Exception("Unexpected grant: "+name);}catch(ApiFault f){check(f.Status==400,name);}}
        var purchase=new GameCommand("buy"){value="growth_kit"};var result=Apply(purchase);
        check(result.state.gems==2500&&result.state.gold==6300&&result.state.parts==70,"verified purchase funds growth kit with exact resources");
        var retry=repo.Apply(key,purchase);check(retry.state.gold==6300&&retry.state.gems==2500,"network retry cannot duplicate growth kit or charge twice");
        Reject(new GameCommand("buy"){value="growth_kit"},"second daily kit rejected");
        Apply(new GameCommand("buy"){value="growth_pass"});
        Reject(new GameCommand("growth_claim"){stage=3,enabled=true},"server rejects unearned pass milestone");
        Reject(new GameCommand("buy"){value="growth_pass"},"server rejects duplicate pass purchase");
        // Isolated earned-milestone fixture, never participant/balance evidence.
        var s=repo.Bootstrap(key).state;s.clearedStages.AddRange(new[]{0,1,2,3,4,5,6,7,8,9,10,11});
        using(var db=new SqliteConnection("Data Source="+path))
        {db.Open();using var q=db.CreateCommand();q.CommandText="UPDATE accounts SET state=$s WHERE id=$id";q.Parameters.AddWithValue("$s",JsonSerializer.Serialize(s,GameRepository.Json));q.Parameters.AddWithValue("$id",first.playerId);q.ExecuteNonQuery();}
        var claim=new GameCommand("growth_claim"){stage=3,enabled=true};var claimed=Apply(claim);int gold=claimed.state.gold;
        check(Outfits.Owned(claimed.state,1)&&claimed.state.growthPassPremium==8,"server grants earned final pass outfit");
        check(repo.Apply(key,claim).state.gold==gold,"retry cannot duplicate premium milestone payout");
        repo=new GameRepository(path,()=>now);check(Growth.PassOwned(repo.Bootstrap(key).state)&&repo.Bootstrap(key).state.growthPassPremium==8,"pass ownership and claims survive restart");
        repo.RevokePurchase("growth-test");var refunded=repo.Bootstrap(key).state;
        check(refunded.gems==0&&refunded.gemDebt==1000,"spent kit and pass participate in existing refund debt accounting");
        now=now.AddDays(1);Reject(new GameCommand("buy"){value="growth_kit"},"refund debt cannot fund another growth purchase");
        string secondKey=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
        repo.Register(secondKey);provider.Binding=repo.Commerce(secondKey,true).accountBinding;
        repo.VerifyPurchase(secondKey,new PurchaseRequest{productId="gems_1200",purchaseToken="robot-test"},provider).GetAwaiter().GetResult();
        ServerReply StyleApply(GameCommand c){c.revision=repo.Bootstrap(secondKey).revision;return repo.Apply(secondKey,c);}
        var pack=new GameCommand("buy"){value="robot_astral_pack"};var robot=StyleApply(pack);
        check(robot.state.gems==300&&robot.state.gold==19800&&RobotStyles.Active(robot.state,2)==1,"server pack exact grant and immediate visual equip");
        check(repo.Apply(secondKey,pack).state.gold==19800,"pack retry does not duplicate grant");
        var quote=GrowthPaths.Quote(robot.state,0);var batch=new GameCommand("upgrade_path",quote.id){stage=0,count=quote.count,level=quote.from};
        var upgraded=StyleApply(batch);check(Catalog.Attack(upgraded.state)==quote.attack&&upgraded.state.gold==19800-quote.gold,"server batch matches displayed stats and cost");
        check(repo.Apply(secondKey,batch).state.gold==upgraded.state.gold,"batch request retry is idempotent");
        repo=new GameRepository(path,()=>now);check(RobotStyles.Active(repo.Bootstrap(secondKey).state,0)==1,"robot style survives server restart");
        repo.RevokePurchase("robot-test");var robotRefund=repo.Bootstrap(secondKey).state;
        check(robotRefund.gems==0&&robotRefund.gemDebt==900,"robot pack uses existing refund debt accounting");
        var old=new Combat(GameState.New(),4,"same",17,false,SkillTrial.Mode);var paid=new Combat(refunded,4,"same",17,false,SkillTrial.Mode);
        check(old.Damage==paid.Damage&&old.MaxHp==paid.MaxHp,"verified purchases and pass cannot alter fixed trial power");
    }
}
