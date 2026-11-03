using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
using ScrapGuard.Server;

static class ExperienceChecks
{
    public static void Run(string folder,Action<bool,string> check)
    {
        string path=Path.Combine(folder,"experience.db");var now=new DateTimeOffset(2026,11,3,4,0,0,TimeSpan.Zero);var repo=new GameRepository(path,()=>now);
        string Key()=>Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
        var a=Key();var b=Key();var aa=repo.Register(a);var bb=repo.Register(b);
        ServerReply Apply(string token,GameCommand command){command.revision=repo.Bootstrap(token).revision;return repo.Apply(token,command);}
        void Fixture(string owner,GameState state)
        {
            using var db=new SqliteConnection("Data Source="+path);db.Open();using var query=db.CreateCommand();
            query.CommandText="UPDATE accounts SET state=$state WHERE id=$id";query.Parameters.AddWithValue("$id",owner);query.Parameters.AddWithValue("$state",JsonSerializer.Serialize(state,GameRepository.Json));query.ExecuteNonQuery();
        }
        var strong=aa.state;strong.heroes[0].level=30;strong.unlockedStage=5;strong.tutorialCompleted=true;
        foreach(var item in strong.items){item.level=20;item.rarity=2;}Fixture(aa.playerId,strong);
        Apply(b,new GameCommand("guild_create"){value="현장 수호대"});Apply(b,new GameCommand("guild_support"){enabled=true,hero=0});
        Apply(a,new GameCommand("guild_join"){value=repo.Guild(b).code});
        var begun=Apply(a,new GameCommand("battle_start"){stage=1,supportId=bb.playerId});
        check(begun.ticket.support.guild==repo.Guild(b).id,"support snapshot binds originating guild");
        check(repo.Guild(b).members.Find(m=>m.self).supportUses==0,"reservation alone does not earn support credit");
        var c=new Combat(begun.state,1,begun.ticket.id,begun.ticket.seed,false,"",begun.ticket.GetSupport());var recorder=new ReplayRecorder();
        for(int frame=0;frame<14500 && !c.Ended;frame++)
        {
            if(c.Choosing){int skill=c.Choices[0];foreach(int desired in new[]{0,4,1,3,2,5,6,7})if(Array.IndexOf(c.Choices,desired)>=0){skill=desired;break;}c.Choose(skill);recorder.Choose(skill);}
            if(c.CanChooseTask){c.ChooseTask(1);recorder.Encounter(1);}
            if(c.CanSupport){c.UseSupport();recorder.Support();}
            if(c.CanUltimate){c.UseUltimate();recorder.Ultimate();if(c.Ended)break;}
            var target=c.Elapsed<15?new V2(270,420):new V2(270+(float)Math.Cos(c.Elapsed*.4f)*150,460+(float)Math.Sin(c.Elapsed*.4f)*180);
            c.Tick(ReplayRecorder.Step,recorder.Move((target-c.Position).Normal));
        }
        check(c.Won && c.TaskCompleted && c.SupportUsed,"fixture wins with actual field recovery and support call");
        now=now.AddMinutes(9);
        var finish=new GameCommand("battle_finish",begun.ticket.id){replay=recorder.Steps,won=false,kills=999999};
        var settled=Apply(a,finish);var receipt=settled.state.receipts.Find(x=>x.id==begun.ticket.id);
        check(receipt.won && receipt.report.taskCompleted && receipt.gold==Campaign.Get(1).Gold+200,"server derives field rewards from replay, ignores reported outcome");
        repo.Apply(a,finish);
        var self=repo.Guild(b).members.Find(m=>m.self);
        check(self.supportUses==1 && self.supportWins==1 && self.helpedMembers==1,"verified clear credits provider exactly once on retry");
        check(repo.Guild(b).featuredId==bb.playerId,"weekly featured guardian comes from verified helped members");
        repo=new GameRepository(path,()=>now);check(repo.Guild(b).members.Find(m=>m.self).supportWins==1,"support hall survives restart and schema reinitialization");
        var repeat=Apply(a,new GameCommand("battle_start"){stage=1,supportId=bb.playerId});
        // Isolated deterministic seed fixture: replay the same successful inputs on a new run.
        using(var db=new SqliteConnection("Data Source="+path))
        {
            db.Open();using var query=db.CreateCommand();query.CommandText="UPDATE accounts SET run_seed=$seed WHERE id=$id";
            query.Parameters.AddWithValue("$seed",begun.ticket.seed);query.Parameters.AddWithValue("$id",aa.playerId);query.ExecuteNonQuery();
        }
        now=now.AddMinutes(9);Apply(a,new GameCommand("battle_finish",repeat.ticket.id){replay=recorder.Steps});
        self=repo.Guild(b).members.Find(m=>m.self);
        check(self.supportUses==2 && self.supportWins==2 && self.helpedMembers==1,"repeat clear with same borrower never inflates helped-member count");
        // Starting another ticket or declaring a win cannot create a contribution without a call.
        var empty=Apply(a,new GameCommand("battle_start"){stage=1,supportId=bb.playerId});now=now.AddSeconds(40);
        Apply(a,new GameCommand("battle_finish",empty.ticket.id){replay=new(),won=true,kills=500});
        check(repo.Guild(b).members.Find(m=>m.self).supportUses==2,"uncalled support and client win flag earn no credit");
        repo.Block(b,aa.playerId,true);
        check(repo.Guild(b).members.Find(m=>m.self).supportUses==0 && repo.Guild(b).featuredId==null,"blocked relationship excluded from support hall");
        repo.Block(b,aa.playerId,false);
        check(repo.Guild(b).members.Find(m=>m.self).supportUses==2,"unblock restores existing verified counts without duplicating them");
        now=now.AddDays(7);check(repo.Guild(b).members.Find(m=>m.self).supportUses==0 && repo.Guild(b).featuredId==null,"new week has no fabricated featured guardian");
    }
}
