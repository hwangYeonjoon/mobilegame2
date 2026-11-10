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
        check(robot.state.gems==600&&robot.state.gold==1800&&RobotStyles.Active(robot.state,2)==1,"server pure cosmetic pack leaves materials unchanged and equips immediately");
        check(repo.Apply(secondKey,pack).state.gold==1800,"pack retry does not duplicate grant");
        var quote=GrowthPaths.Quote(robot.state,0);var batch=new GameCommand("upgrade_path",quote.id){stage=0,count=quote.count,level=quote.from};
        var upgraded=StyleApply(batch);check(Catalog.Attack(upgraded.state)==quote.attack&&upgraded.state.gold==1800-quote.gold,"server batch matches displayed stats and cost");
        check(repo.Apply(secondKey,batch).state.gold==upgraded.state.gold,"batch request retry is idempotent");
        repo=new GameRepository(path,()=>now);check(RobotStyles.Active(repo.Bootstrap(secondKey).state,0)==1,"robot style survives server restart");
        repo.RevokePurchase("robot-test");var robotRefund=repo.Bootstrap(secondKey).state;
        check(robotRefund.gems==0&&robotRefund.gemDebt==600,"robot pack uses existing refund debt accounting");
        string atomicKey=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');var atom=repo.Register(atomicKey);
        provider.Binding=repo.Commerce(atomicKey,true).accountBinding;
        repo.VerifyPurchase(atomicKey,new PurchaseRequest{productId="gems_300",purchaseToken="atomic-growth-test"},provider).GetAwaiter().GetResult();
        var atomicState=repo.Bootstrap(atomicKey).state;var aq=Growth.KitQuote(atomicState,"growth_kit",0);
        var ac=new GameCommand("growth_kit_upgrade",aq.after.id){value="growth_kit",stage=0,count=aq.after.count,level=aq.after.from,hero=0,revision=repo.Bootstrap(atomicKey).revision};
        var forged=new GameCommand("growth_kit_upgrade",aq.after.id){value="growth_kit_frontier",stage=0,count=aq.after.count,level=aq.after.from,hero=0,revision=ac.revision};
        try{repo.Apply(atomicKey,forged);throw new Exception("locked pack accepted");}catch(ApiFault f){check(f.Status==400&&repo.Bootstrap(atomicKey).state.gems==300,"locked tier cannot debit gems or grant growth");}
        var ar=repo.Apply(atomicKey,ac);check(ar.state.gems==200&&ar.state.gold==1800+4500-aq.after.gold&&Catalog.Attack(ar.state)==aq.after.attack,"atomic kit exact purchase delta and equipped stats");
        var again=repo.Apply(atomicKey,ac);check(again.state.gold==ar.state.gold&&again.state.gems==200,"atomic network retry is idempotent");
        repo=new GameRepository(path,()=>now);check(Catalog.Attack(repo.Bootstrap(atomicKey).state)==aq.after.attack,"atomic upgraded equipment survives restart");
        repo.RevokePurchase("atomic-growth-test");check(repo.Bootstrap(atomicKey).state.gemDebt==100,"atomic kit uses verified refund accounting");
        now=now.AddDays(1);var debt=repo.Bootstrap(atomicKey);var dq=Growth.KitQuote(debt.state,"growth_kit",1);
        try{repo.Apply(atomicKey,new GameCommand("growth_kit_upgrade",dq.after.id){value="growth_kit",stage=1,count=dq.after.count,level=dq.after.from,hero=0,revision=debt.revision});throw new Exception("debt purchase accepted");}catch(ApiFault f){check(f.Status==400&&Catalog.Health(repo.Bootstrap(atomicKey).state)==Catalog.Health(debt.state),"refund debt cannot gain atomic growth");}
        var old=new Combat(GameState.New(),4,"same",17,false,SkillTrial.Mode);var paid=new Combat(refunded,4,"same",17,false,SkillTrial.Mode);
        check(old.Damage==paid.Damage&&old.MaxHp==paid.MaxHp,"verified purchases and pass cannot alter fixed trial power");
    }
}
