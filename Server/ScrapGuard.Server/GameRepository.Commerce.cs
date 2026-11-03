using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
namespace ScrapGuard.Server;
public sealed partial class GameRepository
{
    void InitializeCommerce(SqliteConnection db)
    {
        Execute(db,null,"""
        CREATE TABLE IF NOT EXISTS purchases(
          token_hash TEXT PRIMARY KEY, owner TEXT REFERENCES accounts(id) ON DELETE SET NULL, product TEXT NOT NULL,
          granted INTEGER NOT NULL, remaining INTEGER NOT NULL, revoked INTEGER NOT NULL DEFAULT 0, created TEXT NOT NULL);
        CREATE TABLE IF NOT EXISTS google_accounts(owner TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,subject_hash TEXT NOT NULL UNIQUE);
        CREATE TABLE IF NOT EXISTS voided_tokens(token_hash TEXT PRIMARY KEY);
        CREATE INDEX IF NOT EXISTS purchase_owner ON purchases(owner,created);
        """);
    }
    public CommerceStatus Commerce(string token,CommercePolicy policy)
    {
        var status=Commerce(token,policy.LicenseTest);
        status.enabled=policy.Enabled;status.salesEnabled=policy.SalesEnabled;status.saleProducts=policy.SaleProducts;
        if(!policy.LicenseTest && Bootstrap(token).state.testProgress){status.salesEnabled=false;status.saleProducts=[];}
        return status;
    }
    public CommerceStatus Commerce(string token,bool enabled)
    {using var db=Open();var a=Load(db,null,token);return new CommerceStatus{licenseTest=enabled,enabled=enabled,accountBinding=Hash("play:"+a.Id).ToLowerInvariant()};}
    public async Task<ServerReply> VerifyPurchase(string token,PurchaseRequest request,IPlayPurchases verifier,CancellationToken ct=default)
    {
        if(!verifier.Enabled)throw new ApiFault(503,"현재 결제를 사용할 수 없어요.");
        var status=Commerce(token,true);var purchase=await verifier.Verify(request,ct);
        if(purchase.Product!=request.productId||purchase.Token!=request.purchaseToken||purchase.Binding!=status.accountBinding||purchase.Test!=verifier.LicenseTest||purchase.Quantity!=1)
            throw new ApiFault(400,"계정 또는 결제 환경이 일치하지 않아요.");
        if(purchase.Canceled){RevokePurchase(purchase.Token);throw new ApiFault(400,"취소된 구매입니다. 계정을 동기화해 주세요.");}
        if(!purchase.Purchased)throw new ApiFault(409,"결제가 보류 중이에요. 완료 후 다시 확인합니다.");
        lock(gate)
        {
            using var db=Open();using var tx=db.BeginTransaction();var a=Load(db,tx,token);string key=Hash(purchase.Token);
            if(status.accountBinding!=Hash("play:"+a.Id).ToLowerInvariant())throw new ApiFault(409,"계정이 변경됐어요.");
            using(var query=Sql(db,tx,"SELECT owner,product,revoked FROM purchases WHERE token_hash=$key",("$key",key)))
            using(var row=query.ExecuteReader())if(row.Read())
            {
                if(row.IsDBNull(0)||row.GetString(0)!=a.Id||row.GetString(1)!=purchase.Product||row.GetInt32(2)!=0)throw new ApiFault(409,"이미 처리되었거나 회수된 구매예요.");
                return Reply(a,"구매 확인 완료 · 중복 지급 없음");
            }
            using(var voided=Sql(db,tx,"SELECT COUNT(*) FROM voided_tokens WHERE token_hash=$key",("$key",key)))if((long)voided.ExecuteScalar()>0)throw new ApiFault(409,"회수된 구매예요.");
            if(!purchase.Test && a.State.testProgress)throw new ApiFault(409,"테스트 진행 계정은 일반 결제를 사용할 수 없어요.");
            if(purchase.Test)a.State.testProgress=true;
            var offer=CommerceCatalog.Find(purchase.Product);if(offer==null)throw new ApiFault(400,"지원하지 않는 상품이에요.");
            int granted=offer.Gems>0?PremiumWallet.Grant(a.State,offer.Gems):0;
            if(!offer.Consumable)a.State.adSkip=true;
            Execute(db,tx,"INSERT INTO purchases(token_hash,owner,product,granted,remaining,created) VALUES($key,$owner,$product,$granted,$remaining,$time)",("$key",key),("$owner",a.Id),("$product",offer.Id),("$granted",offer.Gems),("$remaining",granted),("$time",clock().ToString("O")));
            // Amount applied to prior refund debt is already spent and must be restored if this grant is refunded.
            SaveCommerce(db,tx,a,key,"play_purchase",granted);tx.Commit();return Reply(a,purchase.Test?"Play 테스트 구매 지급 완료":"구매한 상품을 지급했어요.");
        }
    }
    void SaveCommerce(SqliteConnection db,SqliteTransaction tx,Account a,string key,string kind,int delta)
    {
        a.Revision++;
        Execute(db,tx,"UPDATE accounts SET state=$state,revision=$revision WHERE id=$id",("$state",Encode(a.State)),("$revision",a.Revision),("$id",a.Id));
        if(delta!=0)Execute(db,tx,"INSERT INTO ledger VALUES($owner,$id,'gems',$delta,$balance,$type,$time)",("$owner",a.Id),("$id",kind+":"+key),("$delta",delta),("$balance",a.State.gems),("$type",kind),("$time",clock().ToString("O")));
    }
    static void AllocatePaidSpend(SqliteConnection db,SqliteTransaction tx,string owner,int spent)
    {
        if(spent<=0)return;var rows=new List<(string key,int amount)>();
        using(var q=Sql(db,tx,"SELECT token_hash,remaining FROM purchases WHERE owner=$owner AND revoked=0 AND remaining>0 ORDER BY created,token_hash",("$owner",owner)))
        using(var r=q.ExecuteReader())while(r.Read())rows.Add((r.GetString(0),r.GetInt32(1)));
        foreach(var row in rows){int used=Math.Min(spent,row.amount);Execute(db,tx,"UPDATE purchases SET remaining=remaining-$used WHERE token_hash=$key",("$used",used),("$key",row.key));spent-=used;if(spent==0)break;}
        if(spent!=0)throw new ApiFault(409,"유료 보석 원장을 확인해야 해요.");
    }
    public void RevokePurchase(string purchaseToken)
    {
        lock(gate)
        {
            using var db=Open();using var tx=db.BeginTransaction();string key=Hash(purchaseToken),owner,product;int granted,remaining;
            Execute(db,tx,"INSERT OR IGNORE INTO voided_tokens VALUES($key)",("$key",key));
            using(var q=Sql(db,tx,"SELECT owner,product,granted,remaining,revoked FROM purchases WHERE token_hash=$key",("$key",key)))
            using(var r=q.ExecuteReader())
            {if(!r.Read()||r.IsDBNull(0)||r.GetInt32(4)!=0){r.Close();tx.Commit();return;}owner=r.GetString(0);product=r.GetString(1);granted=r.GetInt32(2);remaining=r.GetInt32(3);}
            var a=Load(db,tx,owner,true);a.State.gems-=remaining;a.State.paidGems-=remaining;a.State.gemDebt=checked(a.State.gemDebt+granted-remaining);
            Execute(db,tx,"UPDATE purchases SET revoked=1,remaining=0 WHERE token_hash=$key",("$key",key));
            if(product=="ad_skip_lifetime")
            {using var count=Sql(db,tx,"SELECT COUNT(*) FROM purchases WHERE owner=$owner AND product='ad_skip_lifetime' AND revoked=0",("$owner",owner));a.State.adSkip=(long)count.ExecuteScalar()>0;}
            SaveCommerce(db,tx,a,key,"play_refund",-remaining);tx.Commit();
        }
    }
}
