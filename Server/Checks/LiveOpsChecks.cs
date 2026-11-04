using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
using ScrapGuard.Server;
static class LiveOpsChecks
{
    public static void Run(string folder,Action<bool,string> check)
    {
        var now=new DateTimeOffset(2026,11,4,0,0,0,TimeSpan.Zero);string path=Path.Combine(folder,"liveops.db");var repo=new GameRepository(path,()=>now);
        string key=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');var a=repo.Register(key);var state=a.state;state.gems=1800;
        using(var db=new SqliteConnection("Data Source="+path)){db.Open();using var q=db.CreateCommand();q.CommandText="UPDATE accounts SET state=$s";q.Parameters.AddWithValue("$s",JsonSerializer.Serialize(state,GameRepository.Json));q.ExecuteNonQuery();}
        ServerReply Apply(GameCommand c){c.revision=repo.Bootstrap(key).revision;return repo.Apply(key,c);}
        void Fails(Action action,int status,string name){try{action();throw new Exception("Expected "+name);}catch(ApiFault e){check(e.Status==status,name);}}
        ContentChange Schedule(string start,string end,bool paused=false)=>new(Guid.NewGuid().ToString("N"),"qa","release test",1,start,end,paused,AppRelease.Build);
        var schedule=Schedule(now.AddHours(1).ToString("O"),now.AddHours(2).ToString("O"));repo.ScheduleContent(schedule);repo.ScheduleContent(schedule);
        Fails(()=>repo.ScheduleContent(schedule with{Paused=true}),409,"schedule request collision rejected");
        Fails(()=>repo.ScheduleContent(Schedule("2026-11-04T01:00:00","")),400,"timezone required for schedules");
        Fails(()=>repo.ScheduleContent(Schedule(now.ToString("O"),now.ToString("O"))),400,"empty schedule interval rejected");
        Fails(()=>Apply(new GameCommand("cosmetic_buy"){value=Outfits.Product(1)}),409,"future outfit cannot charge");
        check(repo.Bootstrap(key).state.gems==1800,"scheduled rejection preserves balance");
        Fails(()=>repo.RecordOutfitPreview(key,1),403,"preview requires separate consent");
        Apply(new GameCommand("metrics_consent"){enabled=true});repo.RecordOutfitPreview(key,1);repo.RecordOutfitPreview(key,1);
        now=now.AddHours(1);var buy=new GameCommand("cosmetic_buy"){value=Outfits.Product(1)};Apply(buy);Apply(new GameCommand("outfit_equip"){skin=1});
        now=now.AddHours(1);repo.Apply(key,buy);check(repo.Bootstrap(key).state.gems==1200,"purchase retry after scheduled end does not charge again");
        repo.ScheduleContent(Schedule("","",true));Apply(new GameCommand("outfit_clear"){hero=0});Apply(new GameCommand("outfit_equip"){skin=1});
        check(Outfits.Active(repo.Bootstrap(key).state,0)==1,"paused sale keeps owned outfit usable");
        var report=JsonSerializer.Serialize(repo.MetricsDashboard(),GameRepository.Json);using var json=JsonDocument.Parse(report);var window=json.RootElement.GetProperty("windows")[0];var row=window.GetProperty("outfits")[0];
        check(row.GetProperty("previews").GetInt32()==1&&row.GetProperty("converted").GetInt32()==1&&row.GetProperty("purchases").GetInt32()==1,"preview deduplication and purchase conversion count once");
        check(row.GetProperty("owners").GetInt32()==1&&row.GetProperty("equipped").GetInt32()==1,"dashboard counts current ownership and equip");
        check(window.GetProperty("gemFlows")[0].GetProperty("spent").GetInt32()==600,"dashboard uses authoritative gem ledger");
        check(!report.Contains(key)&&!report.Contains(a.playerId)&&!report.Contains("token_cipher"),"aggregate contains no account credentials or player IDs");
        if(Environment.GetEnvironmentVariable("SCRAP_METRICS_FIXTURE") is string fixture)File.WriteAllText(fixture,report);
        Apply(new GameCommand("metrics_consent"){enabled=false});using var optedOut=JsonDocument.Parse(JsonSerializer.Serialize(repo.MetricsDashboard(),GameRepository.Json));
        check(optedOut.RootElement.GetProperty("windows")[0].GetProperty("outfits")[0].GetProperty("previews").GetInt32()==0,"opting out deletes historical previews");
        Apply(new GameCommand("metrics_consent"){enabled=true});repo.RecordOutfitPreview(key,6);now=now.AddDays(91);repo.PurgeMetrics();
        using(var db=new SqliteConnection("Data Source="+path)){db.Open();using var q=db.CreateCommand();q.CommandText="SELECT COUNT(*) FROM outfit_previews";check((long)q.ExecuteScalar()==0,"retention expires old previews without new gameplay or a report");}
        state=repo.Bootstrap(key).state;state.testProgress=true;
        using(var db=new SqliteConnection("Data Source="+path)){db.Open();using var q=db.CreateCommand();q.CommandText="UPDATE accounts SET state=$s";q.Parameters.AddWithValue("$s",JsonSerializer.Serialize(state,GameRepository.Json));q.ExecuteNonQuery();}
        using var filtered=JsonDocument.Parse(JsonSerializer.Serialize(repo.MetricsDashboard(),GameRepository.Json));check(filtered.RootElement.GetProperty("accounts").GetInt32()==0,"dashboard excludes test progress by default");
        repo.Delete(key);using var deleted=JsonDocument.Parse(JsonSerializer.Serialize(repo.MetricsDashboard(true),GameRepository.Json));check(deleted.RootElement.GetProperty("windows")[0].GetProperty("outfits")[0].GetProperty("purchases").GetInt32()==0,"account deletion cascades through metric events");
    }
}
