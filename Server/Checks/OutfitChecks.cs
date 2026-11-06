using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
using ScrapGuard.Server;
static class OutfitChecks
{
    public static void Run(string folder,Action<bool,string> check)
    {
        string path=Path.Combine(folder,"outfits.db");var now=new DateTimeOffset(2026,11,2,0,0,0,TimeSpan.Zero);
        var repo=new GameRepository(path,()=>now);
        string Key()=>Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
        string a=Key(),b=Key();var aa=repo.Register(a);var bb=repo.Register(b);
        ServerReply Apply(string key,GameCommand c){c.revision=repo.Bootstrap(key).revision;return repo.Apply(key,c);}
        void Reject(Action action,string name){try{action();throw new Exception("Expected rejection "+name);}catch(ApiFault f){check(f.Status==400,name);}}
        void Fixture(string id,Action<GameState> action)
        {
            using var db=new SqliteConnection("Data Source="+path);db.Open();using var q=db.CreateCommand();q.CommandText="SELECT state FROM accounts WHERE id=$id";q.Parameters.AddWithValue("$id",id);
            var state=JsonSerializer.Deserialize<GameState>((string)q.ExecuteScalar(),GameRepository.Json);action(state);
            q.CommandText="UPDATE accounts SET state=$state WHERE id=$id";q.Parameters.AddWithValue("$state",JsonSerializer.Serialize(state,GameRepository.Json));q.ExecuteNonQuery();
        }
        Reject(()=>Apply(a,new GameCommand("outfit_equip"){skin=1}),"unowned outfit rejected by server");
        Reject(()=>Apply(a,new GameCommand("cosmetic_buy"){value=Outfits.Key}),"server requires full outfit price");
        Fixture(aa.playerId,s=>s.gems=800);
        var themePurchase=new GameCommand("cosmetic_buy"){value="theme_orbit"};
        var themeResult=Apply(a,themePurchase);var themeRetry=repo.Apply(a,themePurchase);
        check(themeResult.state.gems==400&&themeRetry.state.gems==400,"theme retry debits once");
        Apply(a,new GameCommand("showcase_theme"){skin=1});
        check(repo.GetProfile(a,aa.playerId).theme==1,"owned theme survives server storage and DTO");
        using(var metrics=JsonDocument.Parse(JsonSerializer.Serialize(repo.MetricsDashboard(),GameRepository.Json)))
        {
            var theme=metrics.RootElement.GetProperty("themes")[0];
            check(theme.GetProperty("owners").GetInt32()==1&&theme.GetProperty("equipped").GetInt32()==1,"theme dashboard counts authoritative ownership and application once");
            check(metrics.RootElement.GetProperty("themes")[2].GetProperty("owners").GetInt32()==0,"unearned memorial is absent from ownership metrics");
        }
        Reject(()=>Apply(a,new GameCommand("showcase_theme"){skin=4}),"unearned theme cannot be forged");
        Fixture(aa.playerId,s=>s.gems=900);
        var purchase=new GameCommand("cosmetic_buy"){value=Outfits.Key};var bought=Apply(a,purchase);var retry=repo.Apply(a,purchase);
        check(bought.state.gems==300 && retry.state.gems==300 && Outfits.Owned(retry.state),"purchase retry grants and charges once");
        Reject(()=>Apply(a,new GameCommand("cosmetic_buy"){value=Outfits.Key}),"second purchase request rejected");
        Apply(a,new GameCommand("outfit_equip"){skin=1});Apply(a,new GameCommand("visibility"){enabled=true});
        check(repo.GetProfile(b,aa.playerId).outfit==1,"public profile displays equipped outfit");
        using(var db=new SqliteConnection("Data Source="+path))
        {
            db.Open();using var q=db.CreateCommand();q.CommandText="SELECT COUNT(*) FROM ledger WHERE currency='gems' AND delta=-600";
            check((long)q.ExecuteScalar()==1,"outfit debit audited once");
        }
        Apply(a,new GameCommand("guild_create"){value="중장비 길드"});Apply(b,new GameCommand("guild_join"){value=repo.Guild(a).code});Apply(a,new GameCommand("guild_support"){hero=0,enabled=true});
        check(repo.Guild(b).members.Find(m=>m.id==aa.playerId).outfit==1,"guild roster shows outfit");
        var start=Apply(b,new GameCommand("battle_start"){supportId=aa.playerId});check(start.ticket.support.outfit==1,"public support snapshots outfit");
        Apply(a,new GameCommand("outfit_equip"){skin=0});check(repo.Bootstrap(b).ticket.support.outfit==1,"reserved support does not change with owner equipment");
        Apply(b,new GameCommand("battle_cancel"));Apply(a,new GameCommand("outfit_equip"){skin=1});Apply(a,new GameCommand("visibility"){enabled=false});
        start=Apply(b,new GameCommand("battle_start"){supportId=aa.playerId});check(start.ticket.support.outfit==0 && !start.ticket.support.showIdentity,"private support hides premium outfit");Apply(b,new GameCommand("battle_cancel"));
        Fixture(aa.playerId,s=>{s.publicProfile=true;s.clearedStages.Add(4);s.heroes[0].level=30;foreach(var item in s.items)item.level=20;});
        start=Apply(a,new GameCommand("battle_start"){mode=Endgame.WeeklyMode});var t=start.ticket;
        var c=new Combat(start.state,t.stage,t.id,t.seed,false,t.mode);var recorder=new ReplayRecorder();
        for(int i=0;i<1200 && !c.Ended;i++)
        {
            if(c.Choosing){recorder.Choose(c.Choices[0]);c.Choose(c.Choices[0]);}
            if(c.CanUltimate){recorder.Ultimate();c.UseUltimate();}
            c.Tick(ReplayRecorder.Step,recorder.Move(new V2((float)Math.Cos(i*.02),(float)Math.Sin(i*.02))));
        }
        now=now.AddMinutes(2);Apply(a,new GameCommand("battle_finish",t.id){replay=recorder.Steps});
        check(repo.Weekly(b).entries.Exists(e=>e.id==aa.playerId && e.outfit==1),"weekly leaderboard stores outfit at run start");
        Apply(a,new GameCommand("outfit_equip"){skin=0});check(repo.Weekly(b).entries.Exists(e=>e.id==aa.playerId && e.outfit==1),"record costume survives later unequip");
        var seen=Apply(b,new GameCommand("guide_seen"){won=true,kills=999999});check(!seen.state.guideMoved && !seen.state.guideUltimate,"opening guide cannot forge achievements");
        repo=new GameRepository(path,()=>now);check(Outfits.Owned(repo.Bootstrap(a).state) && repo.Weekly(b).entries[0].outfit==1,"outfit ownership and score survive restart");
        // Reopen a genuine older schema containing a score, rather than just testing an empty database.
        using(var db=new SqliteConnection("Data Source="+path))
        {db.Open();using var q=db.CreateCommand();q.CommandText="ALTER TABLE weekly_records DROP COLUMN outfit";q.ExecuteNonQuery();}
        repo=new GameRepository(path,()=>now);check(repo.Weekly(b).entries.Count==1 && repo.Weekly(b).entries[0].outfit==0,"legacy weekly rows migrate without deleting scores or inventing outfits");
    }
}
