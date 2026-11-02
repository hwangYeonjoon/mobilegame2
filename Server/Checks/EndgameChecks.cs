using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
using ScrapGuard.Server;

static class EndgameChecks
{
    public static void Run(string folder,Action<bool,string> check)
    {
        string path=Path.Combine(folder,"endgame.db");var now=new DateTimeOffset(2026,11,2,0,0,0,TimeSpan.Zero);
        var repo=new GameRepository(path,()=>now);
        string Key()=>Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
        var a=Key();var b=Key();var c=Key();var aa=repo.Register(a);var bb=repo.Register(b);repo.Register(c);
        ServerReply Apply(string key,GameCommand command){command.revision=repo.Bootstrap(key).revision;return repo.Apply(key,command);}
        void Fails(Action action,int status,string name){try{action();throw new Exception("Expected failure "+name);}catch(ApiFault e){check(e.Status==status,name);}}
        void Mutate(string id,Action<GameState> edit)
        {
            using var db=new SqliteConnection("Data Source="+path);db.Open();using var q=db.CreateCommand();q.CommandText="SELECT state FROM accounts WHERE id=$id";q.Parameters.AddWithValue("$id",id);
            var state=JsonSerializer.Deserialize<GameState>((string)q.ExecuteScalar(),GameRepository.Json);edit(state);
            q.CommandText="UPDATE accounts SET state=$state WHERE id=$id";q.Parameters.AddWithValue("$state",JsonSerializer.Serialize(state,GameRepository.Json));q.ExecuteNonQuery();
        }
        (Combat combat,ReplayRecorder recorder) Replay(ServerReply reply,int maxFrames=1200)
        {
            var t=reply.ticket;var sim=new Combat(reply.state,t.stage,t.id,t.seed,false,t.mode,t.support);var recorder=new ReplayRecorder();
            for(int f=0;f<maxFrames && !sim.Ended;f++)
            {
                if(sim.Choosing){int skill=sim.Choices[0];sim.Choose(skill);recorder.Choose(skill);}
                if(sim.CanUltimate){sim.UseUltimate();recorder.Ultimate();}
                if(sim.CanSupport){sim.UseSupport();recorder.Support();}
                sim.Tick(ReplayRecorder.Step,recorder.Move(new V2((float)Math.Sin(f*.013),(float)Math.Cos(f*.013))));
            }
            if(!sim.Ended)sim.Abandon();return(sim,recorder);
        }
        Fails(()=>Apply(a,new GameCommand("battle_start"){mode="forged"}),400,"unknown challenge mode rejected");
        Fails(()=>Apply(a,new GameCommand("battle_start"){mode=Endgame.WeeklyMode}),400,"server enforces fifth-stage unlock");
        var practice=Apply(a,new GameCommand("battle_start"){mode=Endgame.PracticeMode});var practiceRun=Replay(practice);now=now.AddMinutes(2);
        Apply(a,new GameCommand("battle_finish",practice.ticket.id){replay=practiceRun.recorder.Steps});
        check(repo.Bootstrap(a).state.weekly.Count==0 && repo.Weekly(a).entries.Count==0,"practice cannot create ranked or rewarded progress");
        var create=new GameCommand("guild_create"){value="코어 수호대"};Apply(a,create);repo.Apply(a,create);
        var guild=repo.Guild(a);check(guild.id!=null && guild.code.Length==16 && guild.members.Count==1,"idempotent guild creation");
        Fails(()=>Apply(c,new GameCommand("guild_join"){value="INVALID"}),400,"unknown guild code rejected");
        Apply(b,new GameCommand("guild_join"){value=guild.code});check(repo.Guild(b).members.Count==2,"invited member joins guild");
        Fails(()=>Apply(a,new GameCommand("guild_join"){value=guild.code}),400,"single membership enforced");
        Fails(()=>Apply(b,new GameCommand("guild_support"){enabled=true,hero=1}),400,"locked hero cannot be registered");
        Apply(b,new GameCommand("guild_support"){enabled=true,hero=0});
        Fails(()=>Apply(c,new GameCommand("battle_start"){supportId=bb.playerId}),400,"outsider cannot borrow guild support");
        Fails(()=>Apply(a,new GameCommand("battle_start"){supportId=aa.playerId}),400,"self support rejected");
        repo.Block(a,bb.playerId,true);check(repo.Guild(a).members.Count==1,"blocked member omitted from support roster");
        Fails(()=>Apply(a,new GameCommand("battle_start"){supportId=bb.playerId}),400,"blocked supporter cannot be borrowed");repo.Block(a,bb.playerId,false);
        var start=Apply(a,new GameCommand("battle_start"){supportId=bb.playerId});
        check(start.ticket.support.hero==0 && start.ticket.support.owner==bb.playerId && start.state.dailySupportStarts==1,"server snapshots supporter and reserves daily allowance");
        Apply(b,new GameCommand("guild_support"){enabled=false});Apply(b,new GameCommand("guild_leave"));
        var supported=Replay(start);now=now.AddMinutes(2);
        var finish=Apply(a,new GameCommand("battle_finish",start.ticket.id){replay=supported.recorder.Steps});
        check(supported.combat.SupportUsed && finish.state.receipts.Last().kills==supported.combat.Kills,"issued support remains deterministic after donor leaves");
        Apply(b,new GameCommand("guild_join"){value=guild.code});Apply(b,new GameCommand("guild_support"){enabled=true,hero=0});
        for(int i=0;i<2;i++){Apply(a,new GameCommand("battle_start"){supportId=bb.playerId});Apply(a,new GameCommand("battle_cancel"));}
        Fails(()=>Apply(a,new GameCommand("battle_start"){supportId=bb.playerId}),400,"three daily support starts maximum including abandoned runs");
        now=now.AddDays(1);check(repo.Guild(a).remaining==3,"support allowance resets at service day");
        Apply(a,new GameCommand("guild_leave"));check(repo.Guild(b).leader,"departing leader transfers ownership");
        repo.Delete(b);Fails(()=>Apply(c,new GameCommand("guild_join"){value=guild.code}),400,"last member deletion disbands guild");
        Mutate(aa.playerId,s=>{s.clearedStages.AddRange(new[]{0,1,2,3,4});s.unlockedStage=4;s.heroes[0].level=30;foreach(var item in s.items)item.level=20;});
        var weekly=Apply(a,new GameCommand("battle_start"){mode=Endgame.WeeklyMode});
        Fails(()=>Apply(a,new GameCommand("battle_finish",weekly.ticket.id){replay=new(){new ReplayStep{support=true}}}),400,"weekly support event rejected by replay");
        var run=Replay(weekly,2700);now=now.AddMinutes(3);
        check(run.combat.Elapsed>=30 && run.combat.CoreScore>0,"ranked fixture survives and damages core");
        var result=Apply(a,new GameCommand("battle_finish",weekly.ticket.id){replay=run.recorder.Steps,kills=int.MaxValue,seconds=float.MaxValue,won=true});
        var w=result.state.weekly.Single();check(w.bestScore==run.combat.CoreScore && w.attempts==1,"weekly score comes only from verified simulation");
        check(repo.Weekly(a).entries.Count==0,"private profile excluded from leaderboard");
        Apply(a,new GameCommand("visibility"){enabled=true});var board=repo.Weekly(a);
        check(board.entries.Count==1 && board.myRank==1 && board.entries[0].score==run.combat.CoreScore,"opt-in score and own rank visible");
        repo.Block(c,aa.playerId,true);check(repo.Weekly(c).entries.Count==0,"leaderboard respects mutual blocking");repo.Block(c,aa.playerId,false);
        var claim=new GameCommand("weekly_claim"){value=w.week,stage=0};var claimed=Apply(a,claim);repo.Apply(a,claim);
        check(claimed.state.coreShards==1 && repo.Bootstrap(a).state.coreShards==1,"shard claim idempotent across request retries");
        Fails(()=>Apply(a,new GameCommand("weekly_claim"){value=w.week,stage=0}),400,"new request cannot claim same tier twice");
        Fails(()=>Apply(a,new GameCommand("buy"){value="core_shards"}),400,"shards cannot be purchased through ordinary shop");
        Fails(()=>Apply(a,new GameCommand("core_exchange"){value="frame_core"}),400,"insufficient shard exchange rejected");
        Mutate(aa.playerId,s=>{s.weekly[0].bestScore=30000;});
        for(int tier=1;tier<4;tier++)Apply(a,new GameCommand("weekly_claim"){value=w.week,stage=tier});
        var exchange=Apply(a,new GameCommand("core_exchange"){value="title_rift"});
        check(exchange.state.coreShards==5 && Social.HasTitle(exchange.state,5),"exclusive title spends only earned shard balance");
        Apply(a,new GameCommand("customize"){title=5});
        check(repo.GetProfile(c,aa.playerId).trophy==3 && repo.GetProfile(c,aa.playerId).title==5,"public exhibition includes earned trophy and exclusive title");
        var nextWeek=now.AddDays(7);now=nextWeek;check(repo.Weekly(a).entries.Count==0,"weekly board resets without deleting permanent trophy");
        check(repo.GetProfile(c,aa.playerId).trophy==3,"earned trophy survives week rollover");
        repo.Delete(a);check(repo.Weekly(c).entries.Count==0,"account deletion removes ranking identity");
        using(var db=new SqliteConnection("Data Source="+path))
        {
            db.Open();using var q=db.CreateCommand();q.CommandText="SELECT COUNT(*) FROM weekly_records";
            check((long)q.ExecuteScalar()==0,"weekly score row cascades on deletion");
        }
    }
}
