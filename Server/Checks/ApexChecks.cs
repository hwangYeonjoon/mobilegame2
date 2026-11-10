using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
using ScrapGuard.Server;
static class ApexChecks
{
    public static void Run(string folder,Action<bool,string> check)
    {
        string path=Path.Combine(folder,"apex.db"),key=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
        var now=DateTimeOffset.UtcNow;var repo=new GameRepository(path,()=>now);var registered=repo.Register(key);
        ServerReply Apply(GameCommand c){c.revision=repo.Bootstrap(key).revision;return repo.Apply(key,c);}
        void Reject(GameCommand c,string label){try{Apply(c);throw new Exception("Unexpected success: "+label);}catch(ApiFault f){check(f.Status==400||f.Status==409,label);}}
        Reject(new GameCommand("battle_start"){stage=47,mode=Apex.Mode},"server rejects locked apex");
        var s=repo.Bootstrap(key).state;s.tutorialCompleted=true;s.unlockedStage=59;s.clearedStages=Enumerable.Range(0,60).ToList();s.heroes[0].level=30;foreach(var item in s.items){item.level=20;item.rarity=2;}
        using(var db=new SqliteConnection("Data Source="+path)){db.Open();using var q=db.CreateCommand();q.CommandText="UPDATE accounts SET state=$s WHERE id=$id";q.Parameters.AddWithValue("$s",JsonSerializer.Serialize(s,GameRepository.Json));q.Parameters.AddWithValue("$id",registered.playerId);q.ExecuteNonQuery();}
        Reject(new GameCommand("battle_start"){stage=46,mode=Apex.Mode},"server refuses non-finale apex");
        Reject(new GameCommand("battle_start"){stage=47,mode=Apex.Mode,supportId="other"},"server rejects apex support");
        var before=repo.Bootstrap(key).state;var begun=Apply(new GameCommand("battle_start"){stage=47,mode=Apex.Mode});var t=begun.ticket;
        check(t.mode==Apex.Mode&&t.stage==47,"server issues distinct apex ticket");
        var c=new Combat(begun.state,t.stage,t.id,t.seed,false,t.mode);var r=new ReplayRecorder();
        for(int i=0;i<2400&&!c.Ended;i++)
        {
            if(c.Choosing){int skill=c.Choices[0];c.Choose(skill);r.Choose(skill);}
            if(c.CanPlanRobot(5,true)){c.PlanRobot(5,true);r.RobotPlan(5,true);}
            if(c.CanActivateMachine){c.ActivateMachine();r.Machine();}
            c.Tick(ReplayRecorder.Step,r.Move(new V2((float)Math.Cos(i*.02),(float)Math.Sin(i*.02))));
        }
        if(!c.Ended)c.Abandon();now=now.AddSeconds(c.Elapsed+5);
        var finish=new GameCommand("battle_finish",t.id){replay=r.Steps,won=true,kills=int.MaxValue,seconds=9999};var done=Apply(finish);
        check(done.state.receipts.Last().won==c.Won&&done.state.receipts.Last().kind==Apex.Mode,"forged victory cannot override apex replay");
        check(done.state.coreShards==before.coreShards&&done.state.gold==before.gold&&done.state.parts==before.parts&&done.state.gems==before.gems&&done.state.clearedStages.Count==before.clearedStages.Count,"abandoned apex has no first clear or campaign payout");
        check(done.state.apexRecords.Single().attempts==1&&repo.Apply(key,finish).state.apexRecords.Single().attempts==1,"apex finish retry is idempotent");
        repo=new GameRepository(path,()=>now);check(repo.Bootstrap(key).state.apexRecords.Single().attempts==1,"apex record survives restart");
        // Replay captured from the Unity player. Isolated fixture installs its earned
        // loadout and fixed seed; rewards still require full authoritative replay.
        using var compressed=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"apex-win-fixture.json.gz"));
        using var unzip=new System.IO.Compression.GZipStream(compressed,System.IO.Compression.CompressionMode.Decompress);
        var probe=JsonSerializer.Deserialize<WinProbe>(unzip,GameRepository.Json);
        var earned=probe.state;earned.activeRun=earned.activeMode=earned.activeWeek="";earned.apexRecords=new();earned.receipts=new();
        using(var db=new SqliteConnection("Data Source="+path)){db.Open();using var q=db.CreateCommand();q.CommandText="UPDATE accounts SET state=$s WHERE id=$id";q.Parameters.AddWithValue("$s",JsonSerializer.Serialize(earned,GameRepository.Json));q.Parameters.AddWithValue("$id",registered.playerId);q.ExecuteNonQuery();}
        int startingShards=earned.coreShards;
        for(int attempt=0;attempt<2;attempt++)
        {
            var start=Apply(new GameCommand("battle_start"){stage=probe.stage,mode=Apex.Mode});
            using(var db=new SqliteConnection("Data Source="+path)){db.Open();using var q=db.CreateCommand();q.CommandText="UPDATE accounts SET run_seed=$seed WHERE id=$id";q.Parameters.AddWithValue("$seed",probe.seed);q.Parameters.AddWithValue("$id",registered.playerId);q.ExecuteNonQuery();}
            now=now.AddSeconds(probe.seconds+5);
            var command=new GameCommand("battle_finish",start.ticket.id){replay=probe.steps,won=false};var result=Apply(command);
            check(result.state.receipts.Last().won&&result.state.coreShards==startingShards+6,"verified Unity win pays first apex reward exactly once "+attempt);
            check(repo.Apply(key,command).state.coreShards==startingShards+6,"verified win retry cannot duplicate apex shards "+attempt);
            repo=new GameRepository(path,()=>now);
        }
        check(Apex.Find(repo.Bootstrap(key).state,probe.stage).wins==2,"repeated verified victories survive restart");
    }
    #pragma warning disable CS0649
    sealed class WinProbe {public GameState state;public List<ReplayStep> steps;public int stage,seed;public float seconds;}
    #pragma warning restore CS0649
}
