using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
using ScrapGuard.Server;

static class RestorationChecks
{
    public static void Run(string folder,Action<bool,string> check)
    {
        string path=Path.Combine(folder,"restoration.db");var now=new DateTimeOffset(2026,11,3,4,0,0,TimeSpan.Zero);var repo=new GameRepository(path,()=>now);
        string Key()=>Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
        string a=Key(),b=Key();var aa=repo.Register(a);var bb=repo.Register(b);
        ServerReply Apply(string key,GameCommand c){c.revision=repo.Bootstrap(key).revision;return repo.Apply(key,c);}
        void Fixture(GameState state)
        {
            using var db=new SqliteConnection("Data Source="+path);db.Open();using var q=db.CreateCommand();q.CommandText="UPDATE accounts SET state=$state WHERE id=$id";
            q.Parameters.AddWithValue("$state",JsonSerializer.Serialize(state,GameRepository.Json));q.Parameters.AddWithValue("$id",aa.playerId);q.ExecuteNonQuery();
        }
        var state=aa.state;state.unlockedStage=5;state.tutorialCompleted=true;state.heroes[0].level=30;
        foreach(var item in state.items){item.level=20;item.rarity=2;}Fixture(state);
        Apply(b,new GameCommand("guild_create"){value="복구 길드"});Apply(a,new GameCommand("guild_join"){value=repo.Guild(b).code});
        ServerReply Start()
        {
            var reply=Apply(a,new GameCommand("battle_start"){stage=1});
            using var db=new SqliteConnection("Data Source="+path);db.Open();using var q=db.CreateCommand();q.CommandText="UPDATE accounts SET run_seed=71 WHERE id=$id";q.Parameters.AddWithValue("$id",aa.playerId);q.ExecuteNonQuery();
            reply.ticket.seed=71;return reply;
        }
        var first=Start();var c=new Combat(first.state,1,first.ticket.id,71);var r=new ReplayRecorder();
        for(int frame=0;frame<14500&&!c.Ended;frame++)
        {
            if(c.Choosing){int skill=c.Choices[0];foreach(int desired in new[]{0,4,1,3,2,5,6,7})if(Array.IndexOf(c.Choices,desired)>=0){skill=desired;break;}c.Choose(skill);r.Choose(skill);}
            if(c.CanChooseTask){c.ChooseTask(1);r.Encounter(1);}
            if(c.CanAssemble){c.Assemble(1);r.Assemble(1);}
            if(c.CanUltimate){c.UseUltimate();r.Ultimate();if(c.Ended)break;}
            var target=c.Elapsed<15?c.TaskPosition:new V2(270+(float)Math.Cos(c.Elapsed*.4)*150,460+(float)Math.Sin(c.Elapsed*.4)*180);
            c.Tick(ReplayRecorder.Step,r.Move((target-c.Position).Normal));
        }
        check(c.Won && c.FieldModule==1,"verified assembly bot completes campaign");
        ServerReply Finish(ServerReply begun){now=now.AddMinutes(9);return Apply(a,new GameCommand("battle_finish",begun.ticket.id){replay=r.Steps});}
        now=now.AddMinutes(9);var finish=new GameCommand("battle_finish",first.ticket.id){replay=r.Steps};Apply(a,finish);repo.Apply(a,finish);
        check(repo.Guild(a).restorationPoints==1 && repo.Guild(a).restorationToday==1,"restoration transaction and retry award once");
        check(repo.Bootstrap(a).state.receipts.Last().report.fieldModule==1,"server report preserves verified field assembly");
        Finish(Start());Finish(Start());Finish(Start());
        check(repo.Guild(a).restorationPoints==3 && repo.Guild(a).restorationToday==3,"daily contribution cap across repeated clears");
        now=now.AddDays(1);check(repo.Guild(a).restorationToday==0 && repo.Guild(a).restorationPoints==3,"daily reset preserves weekly construction");
        var interrupted=Start();Apply(a,new GameCommand("guild_leave"));Apply(a,new GameCommand("guild_create"){value="다른 복구 길드"});Finish(interrupted);
        check(repo.Guild(a).restorationPoints==0,"mid-battle guild switch cannot move contribution");
        Apply(a,new GameCommand("guild_leave"));Apply(a,new GameCommand("guild_join"){value=repo.Guild(b).code});
        state=repo.Bootstrap(a).state;state.testProgress=true;Fixture(state);Finish(Start());
        check(repo.Guild(a).restorationPoints==3,"test progress cannot build official guild facility");
        state=repo.Bootstrap(a).state;state.testProgress=false;Fixture(state);
        var abandoned=Start();now=now.AddMinutes(1);Apply(a,new GameCommand("battle_finish",abandoned.ticket.id){replay=new(),won=true,kills=9999});
        check(repo.Guild(a).restorationPoints==3,"forged win with no replay earns no construction");
        Apply(a,new GameCommand("visibility"){enabled=true});
        check(repo.Gallery(b,"").featuredId==aa.playerId,"weekly discovery includes ordinary public player");
        repo.Block(b,aa.playerId,true);
        check(repo.Gallery(b,"").featuredId=="" && repo.Guild(b).members.All(m=>m.id!=aa.playerId),"blocking suppresses discovery and contributor identity");
        repo.Block(b,aa.playerId,false);Apply(a,new GameCommand("visibility"){enabled=false});check(repo.Gallery(b,"").featuredId=="","private profile cannot be featured");
        repo=new GameRepository(path,()=>now);check(repo.Guild(a).restorationPoints==3,"restoration survives restart");
        now=now.AddDays(7);check(repo.Guild(a).restorationPoints==0,"new week starts fresh construction");
        Apply(a,new GameCommand("region_target"){stage=6});check(repo.Bootstrap(a).state.collectionTarget==6,"server persists collection target");
    }
}
