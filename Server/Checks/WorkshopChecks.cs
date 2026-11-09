using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
using ScrapGuard.Server;

static class WorkshopChecks
{
    public static void Run(string folder,Action<bool,string> check)
    {
        string path=Path.Combine(folder,"workshop.db");var now=DateTimeOffset.UtcNow;var repo=new GameRepository(path,()=>now);
        string Key()=>Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
        var a=Key();var b=Key();var aa=repo.Register(a);var bb=repo.Register(b);
        ServerReply Apply(string key,GameCommand c){c.revision=repo.Bootstrap(key).revision;return repo.Apply(key,c);}
        void Fails(Action action,string label){try{action();throw new Exception("Expected rejection: "+label);}catch(ApiFault f){check(f.Status==400,label);}}
        void Fixture(string id,GameState state)
        {
            using var db=new SqliteConnection("Data Source="+path);db.Open();using var c=db.CreateCommand();
            c.CommandText="UPDATE accounts SET state=$state WHERE id=$id";c.Parameters.AddWithValue("$id",id);c.Parameters.AddWithValue("$state",JsonSerializer.Serialize(state,GameRepository.Json));c.ExecuteNonQuery();
        }
        var s=aa.state;Workshop.Region(s,0).blueprints=8;s.heroes[0].level=30;s.tutorialCompleted=true;
        foreach(var item in s.items){item.level=20;item.rarity=2;}Fixture(aa.playerId,s);
        var craft=new GameCommand("region_craft"){stage=0};var made=Apply(a,craft);var retry=repo.Apply(a,craft);
        check(Social.HasSkin(made.state,3) && Workshop.Region(retry.state,0).blueprints==0,"server craft and retry grants exactly one paint");
        Fails(()=>Apply(a,new GameCommand("region_craft"){stage=0}),"new request cannot recraft owned paint");
        Fails(()=>Apply(a,new GameCommand("region_craft"){stage=1}),"other region blueprints cannot be spent");
        Fails(()=>Apply(a,new GameCommand("cosmetic_buy"){value="region_skin:1"}),"exclusive regional paint is not a shop offer");
        using(var db=new SqliteConnection("Data Source="+path))
        {
            db.Open();using var q=db.CreateCommand();q.CommandText="SELECT COUNT(*) FROM ledger WHERE currency='blueprints:0' AND delta=-8 AND balance=0";
            check((long)q.ExecuteScalar()==1,"craft is audited once in blueprint ledger");
        }
        Fails(()=>Apply(a,new GameCommand("robot_showcase"){stage=1}),"unbuilt robot cannot be published");
        var started=Apply(a,new GameCommand("battle_start"));
        // A fixed seed in this isolated database guarantees the run actually earns a mod.
        // Production tickets keep their random seeds; the client cannot change the stored seed.
        using(var db=new SqliteConnection("Data Source="+path))
        {
            db.Open();using var q=db.CreateCommand();q.CommandText="UPDATE accounts SET run_seed=41 WHERE id=$id";q.Parameters.AddWithValue("$id",aa.playerId);q.ExecuteNonQuery();
        }
        started.ticket.seed=41;
        var combat=new Combat(started.state,0,started.ticket.id,started.ticket.seed);var recorder=new ReplayRecorder();
        for(int i=0;i<14000 && !combat.Ended;i++)
        {
            if(combat.Choosing)
            {
                int choice=combat.Choices[0];foreach(int skill in new[]{0,5,1,7,2,3,4,6})if(Array.IndexOf(combat.Choices,skill)>=0){choice=skill;break;}
                recorder.Choose(choice);combat.Choose(choice);
            }
            if(combat.CanActivateMachine){int design=1+combat.MachinesBuilt%9;combat.ActivateMachine(design);recorder.Machine(design);}
            if(combat.CanUltimate){recorder.Ultimate();combat.UseUltimate();}
            var target=new V2(270+(float)Math.Cos(combat.Elapsed*.4f)*160,490+(float)Math.Sin(combat.Elapsed*.4f)*210);
            combat.Tick(ReplayRecorder.Step,recorder.Move((target-combat.Position).Normal));
        }
        if(!combat.Ended)combat.Abandon();now=now.AddMinutes(8);
        var command=new GameCommand("battle_finish",started.ticket.id){replay=recorder.Steps,won=!combat.Won,kills=999999};
        var settled=Apply(a,command);var receipt=settled.state.receipts.Find(r=>r.id==started.ticket.id);
        check(settled.state.guideMoved && settled.state.guideUltimate && settled.state.guideMod,"guide progression comes from server-verified movement ultimate and mod");
        check(combat.Mods>0 && receipt.report.mods==combat.Mods && receipt.kills==combat.Kills,"server verifies earned mods and ignores forged client result");
        check(receipt.report.cause==combat.Report().cause && receipt.report.damage.SequenceEqual(combat.Report().damage),"report uses authoritative replay damage");
        check(receipt.blueprints==(combat.Won?1:0) && Workshop.Region(settled.state,0).blueprints==receipt.blueprints,"blueprints require verified campaign success");
        repo.Apply(a,command);check(Workshop.Region(repo.Bootstrap(a).state,0).blueprints==receipt.blueprints,"settlement retry cannot duplicate blueprints");
        var robots=settled.state.robotRecords;
        check(robots.Count>0&&robots.Sum(r=>r.builds)==combat.MachinesBuilt,"server settles replay-authorized robot collection");
        check(repo.Bootstrap(a).state.robotRecords.Sum(r=>r.builds)==combat.MachinesBuilt,"settlement retry cannot duplicate constructed robots");
        var chosen=Apply(a,new GameCommand("robot_showcase"){stage=robots[0].design});
        check(Social.Build(chosen.state).robotDesign==robots[0].design,"built robot is exposed through public profile");
        check(new GameRepository(path,()=>now).Bootstrap(a).state.showcaseRobot==robots[0].design,"robot choice survives repository restart");
        Apply(a,new GameCommand("guild_create"){value="개조 수호대"});Apply(b,new GameCommand("guild_join"){value=repo.Guild(a).code});Apply(b,new GameCommand("guild_support"){enabled=true,hero=0});
        var state=repo.Bootstrap(b).state;state.publicProfile=true;state.attendanceCount=7;state.showcaseTitle=2;state.ownedCosmetics.Add(Workshop.SkinKey(0));state.selectedSkin=3;Fixture(bb.playerId,state);
        var publicRun=Apply(a,new GameCommand("battle_start"){supportId=bb.playerId});
        check(publicRun.ticket.support.showIdentity && publicRun.ticket.support.skin==3 && publicRun.ticket.support.title==2,"public support snapshots earned appearance and title");
        Apply(a,new GameCommand("battle_cancel"));Apply(b,new GameCommand("visibility"){enabled=false});
        var privateRun=Apply(a,new GameCommand("battle_start"){supportId=bb.playerId});
        check(!privateRun.ticket.support.showIdentity && privateRun.ticket.support.skin==0 && privateRun.ticket.support.title==0 && privateRun.ticket.support.name!="새내기 정비사","private support arrival strips showcase metadata");
        Apply(a,new GameCommand("battle_cancel"));repo.Block(a,bb.playerId,true);
        Fails(()=>Apply(a,new GameCommand("battle_start"){supportId=bb.playerId}),"blocked supporter cannot issue an arrival card");
        var reopened=new GameRepository(path,()=>now);check(Social.HasSkin(reopened.Bootstrap(a).state,3),"crafted paint survives restart");
    }
}
