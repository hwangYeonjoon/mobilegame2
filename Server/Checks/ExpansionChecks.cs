using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
using ScrapGuard.Server;

static class ExpansionChecks
{
    public static void Run(string folder,Action<bool,string> check)
    {
        string path=Path.Combine(folder,"expansion.db");var now=DateTimeOffset.UtcNow;var repo=new GameRepository(path,()=>now);
        string Key()=>Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
        var a=Key();var b=Key();var aa=repo.Register(a);var bb=repo.Register(b);
        ServerReply Apply(string key,GameCommand c){c.revision=repo.Bootstrap(key).revision;return repo.Apply(key,c);}
        void Fails(Action action,string label){try{action();throw new Exception("Expected rejection: "+label);}catch(ApiFault f){check(f.Status==400,label);}}
        void Fixture(string id,GameState state)
        {
            using var db=new SqliteConnection("Data Source="+path);db.Open();using var c=db.CreateCommand();
            c.CommandText="UPDATE accounts SET state=$state WHERE id=$id";c.Parameters.AddWithValue("$id",id);c.Parameters.AddWithValue("$state",JsonSerializer.Serialize(state,GameRepository.Json));c.ExecuteNonQuery();
        }
        Fails(()=>Apply(a,new GameCommand("hero_recruit"){hero=3,stage=11,won=true}),"client cannot forge recruitment clear");
        Fails(()=>Apply(a,new GameCommand("hero_select"){hero=4}),"locked new hero cannot be selected");
        var old=aa.state;old.heroes.RemoveRange(3,2);old.heroes[1].unlocked=true;old.heroes[1].level=13;old.gems=900;old.unlockedStage=29;
        for(int i=0;i<30;i++)old.clearedStages.Add(i);Fixture(aa.playerId,old);
        var migrated=repo.Bootstrap(a).state;
        check(migrated.heroes.Count==5 && migrated.heroes[1].level==13 && migrated.gems==900 && migrated.unlockedStage==30,"server preserves v0.6 heroes and unlocks stage 31");
        foreach(int hero in new[]{3,4})
        {
            var recruit=new GameCommand("hero_recruit"){hero=hero};var granted=Apply(a,recruit);var retry=repo.Apply(a,recruit);
            check(granted.state.heroes[hero].unlocked && retry.state.gems==900,"server recruits new hero once without charging "+hero);
            Fails(()=>Apply(a,new GameCommand("hero_recruit"){hero=hero}),"duplicate new recruit rejected "+hero);
        }
        var helper=repo.Bootstrap(b).state;helper.clearedStages.AddRange(new[]{11,23});Fixture(bb.playerId,helper);
        foreach(int hero in new[]{3,4})Apply(b,new GameCommand("hero_recruit"){hero=hero});
        Apply(a,new GameCommand("guild_create"){value="확장 수호대"});Apply(b,new GameCommand("guild_join"){value=repo.Guild(a).code});
        var state=repo.Bootstrap(a).state;state.unlockedStage=41;state.tutorialCompleted=true;foreach(var h in state.heroes)h.level=30;foreach(var item in state.items)item.level=20;Fixture(aa.playerId,state);
        foreach(int hero in new[]{3,4})
        {
            Apply(a,new GameCommand("hero_select"){hero=hero});Apply(a,new GameCommand("customize"){hero=hero});
            Apply(b,new GameCommand("guild_support"){hero=hero,enabled=true});
            check(repo.Guild(a).members.Find(m=>m.id==bb.playerId).hero==hero,"new support appears in guild roster "+hero);
            int stage=hero==3?30:36;var start=Apply(a,new GameCommand("battle_start"){stage=stage,supportId=bb.playerId});
            check(start.ticket.support.hero==hero && start.ticket.support.power>0,"new support is reserved authoritatively "+hero);
            var c=new Combat(start.state,stage,start.ticket.id,start.ticket.seed,false,"",start.ticket.support);var recorder=new ReplayRecorder();
            for(int i=0;i<1500 && !c.Ended;i++)
            {
                if(c.Choosing){recorder.Choose(c.Choices[0]);c.Choose(c.Choices[0]);}
                if(c.CanUltimate){recorder.Ultimate();c.UseUltimate();}
                if(c.CanSupport){recorder.Support();c.UseSupport();}
                c.Tick(ReplayRecorder.Step,recorder.Move(new V2((float)Math.Cos(i*.02),(float)Math.Sin(i*.02))));
            }
            c.Abandon();now=now.AddMinutes(2);
            var finish=new GameCommand("battle_finish",start.ticket.id){replay=recorder.Steps,won=true,kills=999999};var result=Apply(a,finish);
            var receipt=result.state.receipts.Find(r=>r.id==start.ticket.id);
            check(c.SupportUsed && c.UltimateUses>0 && receipt.kills==c.Kills && !receipt.won && receipt.blueprints==0,"new hero/support/terrain replay ignores forged victory "+hero);
            check(receipt.report.damage.SequenceEqual(c.Report().damage),"expanded damage sources match server replay "+hero);
            check(repo.Apply(a,finish).state.gold==result.state.gold,"expanded settlement retry is idempotent "+hero);
            state=repo.Bootstrap(a).state;int region=hero+2;Workshop.Region(state,region).blueprints=8;Fixture(aa.playerId,state);
            Apply(a,new GameCommand("region_craft"){stage=region});Apply(a,new GameCommand("customize"){hero=hero,skin=region+3});
            check(Social.Build(repo.Bootstrap(a).state).skin==region+3,"new region cosmetic can be showcased "+region);
        }
        repo=new GameRepository(path,()=>now);state=repo.Bootstrap(a).state;
        check(state.selectedHero==4 && state.heroes[3].unlocked && state.heroes[4].unlocked && Social.HasSkin(state,9),"expanded ownership and selection survive restart");
    }
}
