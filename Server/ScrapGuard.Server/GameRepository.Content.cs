using System.Globalization;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
namespace ScrapGuard.Server;

public sealed record ContentChange(string RequestId,string Operator,string Reason,int Outfit,string Starts,string Ends,bool Paused,int MinimumBuild);
public sealed partial class GameRepository
{
    void InitializeContent(SqliteConnection db)
    {
        Execute(db,null,"CREATE TABLE IF NOT EXISTS outfit_releases(outfit INTEGER PRIMARY KEY,starts TEXT,ends TEXT,paused INTEGER NOT NULL,minimum_build INTEGER NOT NULL)");
    }
    static OutfitRelease Release(SqliteConnection db,SqliteTransaction tx,int id)
    {
        using var q=Sql(db,tx,"SELECT starts,ends,paused,minimum_build FROM outfit_releases WHERE outfit=$id",("$id",id));using var r=q.ExecuteReader();
        return r.Read()?new OutfitRelease{id=id,startsAt=r.IsDBNull(0)?"":r.GetString(0),endsAt=r.IsDBNull(1)?"":r.GetString(1),paused=r.GetInt32(2)!=0,minimumBuild=r.GetInt32(3)}:new OutfitRelease{id=id,minimumBuild=AppRelease.MinimumBuild};
    }
    public ContentCatalog Content()
    {
        using var db=Open();using var tx=db.BeginTransaction(deferred:true);var result=new ContentCatalog{serverTime=clock().ToUniversalTime().ToString("O")};
        for(int i=1;i<=Outfits.Count;i++)result.outfits.Add(Release(db,tx,i));tx.Commit();return result;
    }
    void RequireContentSale(SqliteConnection db,SqliteTransaction tx,GameCommand command)
    {
        int id=command.type=="cosmetic_buy"?Outfits.Find(command.value):0;
        if(id>0&&!Release(db,tx,id).Available(clock(),AppRelease.Build))throw new ApiFault(409,"아직 공개되지 않았거나 판매가 중지된 의상이에요. 상점을 새로 확인해 주세요.");
    }
    public ContentCatalog ScheduleContent(ContentChange c)
    {
        if(c==null||!System.Text.RegularExpressions.Regex.IsMatch(c.RequestId??"","^[a-f0-9]{32}$")||string.IsNullOrWhiteSpace(c.Operator)||c.Operator.Length>80||string.IsNullOrWhiteSpace(c.Reason)||c.Reason.Length>500||!Outfits.Valid(c.Outfit)||c.MinimumBuild<AppRelease.MinimumBuild||c.MinimumBuild>AppRelease.Build)throw new ApiFault(400,"운영자·사유·의상·앱 버전을 확인하세요.");
        string Time(string text)
        {
            if(string.IsNullOrEmpty(text))return null;
            if(!System.Text.RegularExpressions.Regex.IsMatch(text,@"(Z|[+-]\d{2}:\d{2})$")||!DateTimeOffset.TryParse(text,CultureInfo.InvariantCulture,DateTimeStyles.None,out var time))throw new ApiFault(400,"시간대가 있는 ISO 날짜가 필요해요.");
            return time.ToUniversalTime().ToString("O");
        }
        string starts=Time(c.Starts),ends=Time(c.Ends);if(starts!=null&&ends!=null&&string.CompareOrdinal(starts,ends)>=0)throw new ApiFault(400,"종료는 시작 이후여야 해요.");
        string digest=Hash(Encode(c));
        lock(gate)
        {
            using var db=Open();using var tx=db.BeginTransaction();
            using(var q=Sql(db,tx,"SELECT payload_hash FROM operator_audit WHERE request_id=$id",("$id",c.RequestId)))
            {var previous=q.ExecuteScalar() as string;if(previous!=null){if(previous!=digest)throw new ApiFault(409,"운영 요청 ID가 충돌했어요.");return Content();}}
            Execute(db,tx,"INSERT INTO outfit_releases VALUES($id,$start,$end,$paused,$build) ON CONFLICT(outfit) DO UPDATE SET starts=$start,ends=$end,paused=$paused,minimum_build=$build",("$id",c.Outfit),("$start",starts),("$end",ends),("$paused",c.Paused?1:0),("$build",c.MinimumBuild));
            Execute(db,tx,"INSERT INTO operator_audit(request_id,actor,action,reason,payload_hash,created) VALUES($id,$actor,'content_schedule',$reason,$hash,$now)",("$id",c.RequestId),("$actor",c.Operator),("$reason",c.Reason),("$hash",digest),("$now",clock().ToUniversalTime().ToString("O")));
            tx.Commit();return Content();
        }
    }
}
