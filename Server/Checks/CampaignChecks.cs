using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
using ScrapGuard.Server;

static class CampaignChecks
{
    public static void Run(string folder,Action<bool,string> check)
    {
        string path=Path.Combine(folder,"campaign.db");var now=DateTimeOffset.UtcNow;
        var repo=new GameRepository(path,()=>now);
        string key=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
        var account=repo.Register(key);
        ServerReply Apply(GameCommand command){command.revision=repo.Bootstrap(key).revision;return repo.Apply(key,command);}
        void Fixture(GameState state)
        {
            using var db=new SqliteConnection("Data Source="+path);db.Open();using var command=db.CreateCommand();
            command.CommandText="UPDATE accounts SET state=$state WHERE id=$id";
            command.Parameters.AddWithValue("$id",account.playerId);
            command.Parameters.AddWithValue("$state",JsonSerializer.Serialize(state,new JsonSerializerOptions{IncludeFields=true}));command.ExecuteNonQuery();
        }
        var state=account.state;state.clearedStages.AddRange(new[]{0,1,2,3,4});state.unlockedStage=4;state.gems=250;Fixture(state);
        var migrated=repo.Bootstrap(key);
        check(migrated.state.unlockedStage==5 && migrated.state.gems==250,"server migrates former final clear without granting gems again");
        var sixth=Apply(new GameCommand("battle_start"){stage=5});check(sixth.ticket.stage==5 && sixth.ticket.rulesVersion==Combat.RulesVersion,"server issues new sixth operation");
        Apply(new GameCommand("battle_cancel"));
        bool rejected=false;try{Apply(new GameCommand("battle_start"){stage=29});}catch(ApiFault f){rejected=f.Status==400;}
        check(rejected,"server denies locked late operation");
        state=repo.Bootstrap(key).state;state.unlockedStage=29;state.heroes[0].level=30;foreach(var item in state.items)item.level=20;Fixture(state);
        foreach(int stage in new[]{6,12,18,24,29})
        {
            var started=Apply(new GameCommand("battle_start"){stage=stage});
            var c=new Combat(started.state,stage,started.ticket.id,started.ticket.seed);var r=new ReplayRecorder();
            for(int i=0;i<1050 && !c.Ended;i++)
            {
                if(c.Choosing){r.Choose(c.Choices[0]);c.Choose(c.Choices[0]);}
                c.Tick(ReplayRecorder.Step,r.Move(new V2((float)Math.Sin(i*.02),(float)Math.Cos(i*.02))));
            }
            c.Abandon();now=now.AddSeconds(40);
            var finish=new GameCommand("battle_finish",started.ticket.id){replay=r.Steps};var result=Apply(finish);
            var receipt=result.state.receipts.Find(x=>x.id==started.ticket.id);
            check(receipt.kills==c.Kills && receipt.seconds==c.Elapsed && !receipt.won,"server terrain replay matches operation "+(stage+1));
            check(repo.Apply(key,finish).state.gold==result.state.gold,"new operation settlement retry remains idempotent "+(stage+1));
        }
    }
}
