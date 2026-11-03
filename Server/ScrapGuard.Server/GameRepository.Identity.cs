using ScrapGuard.Core;
namespace ScrapGuard.Server;
public sealed partial class GameRepository
{
    public ServerReply LinkGoogle(string token,string subject)
    {
        if(string.IsNullOrWhiteSpace(subject))throw new ApiFault(401,"Google 계정을 확인하지 못했어요.");
        lock(gate)
        {
            using var db=Open();using var tx=db.BeginTransaction();var a=Load(db,tx,token);string hash=Hash("google:"+subject);
            using(var q=Sql(db,tx,"SELECT owner,subject_hash FROM google_accounts WHERE owner=$owner OR subject_hash=$hash",("$owner",a.Id),("$hash",hash)))
            using(var r=q.ExecuteReader())if(r.Read())
            {if(r.GetString(0)!=a.Id||r.GetString(1)!=hash)throw new ApiFault(409,"이미 다른 계정에 연결되어 있어요. 진행 상황을 자동으로 합치지 않습니다.");return Reply(a,"이미 연결된 Google 계정이에요.");}
            Execute(db,tx,"INSERT INTO google_accounts VALUES($owner,$hash)",("$owner",a.Id),("$hash",hash));tx.Commit();return Reply(a,"Google 계정 연결 완료");
        }
    }
    public ServerReply RecoverGoogle(string subject,string secret)
    {
        string hash=TokenHash(secret);
        lock(gate)
        {
            using var db=Open();using var tx=db.BeginTransaction();string owner;
            using(var q=Sql(db,tx,"SELECT owner FROM google_accounts WHERE subject_hash=$hash",("$hash",Hash("google:"+subject))))owner=q.ExecuteScalar() as string;
            if(owner==null)throw new ApiFault(404,"연결된 수호대가 없어요. 서버 계정을 만든 뒤 Google 계정을 연결해 주세요.");
            using(var q=Sql(db,tx,"SELECT id FROM accounts WHERE token_hash=$hash AND id<>$owner",("$hash",hash),("$owner",owner)))if(q.ExecuteScalar()!=null)throw new ApiFault(409,"복구 코드를 다시 발급해 주세요.");
            Execute(db,tx,"UPDATE accounts SET token_hash=$hash WHERE id=$owner",("$hash",hash),("$owner",owner));var a=Load(db,tx,secret);tx.Commit();return Reply(a,"Google 복구 완료 · 이전 복구 코드와 다른 기기 연결은 해제됩니다.");
        }
    }
}
