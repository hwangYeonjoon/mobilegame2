using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
using ScrapGuard.Server;
static class RobotSeasonChecks
{
    public static void Run(string folder,Action<bool,string> check)
    {
        string path=Path.Combine(folder,"robot-season.db"),key=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
        var now=new DateTimeOffset(2026,11,11,0,0,0,TimeSpan.Zero);var repo=new GameRepository(path,()=>now);var first=repo.Register(key);
        ServerReply Apply(GameCommand c){c.revision=repo.Bootstrap(key).revision;return repo.Apply(key,c);}
        void Reject(GameCommand c,string name){try{Apply(c);throw new Exception("Unexpected success "+name);}catch(ApiFault ex){check(ex.Status==400||ex.Status==409,name);}}
        void Fixture(Action<GameState> f)
        {
            var s=repo.Bootstrap(key).state;f(s);using var db=new SqliteConnection("Data Source="+path);db.Open();using var q=db.CreateCommand();
            q.CommandText="UPDATE accounts SET state=$s WHERE id=$id";q.Parameters.AddWithValue("$s",JsonSerializer.Serialize(s,GameRepository.Json));q.Parameters.AddWithValue("$id",first.playerId);q.ExecuteNonQuery();
        }
        Reject(new GameCommand("battle_start"){mode=RobotSeason.Modes[0]},"locked theme rejected");
        Reject(new GameCommand("robot_seeker_unlock"),"locked permanent design rejected");
        Fixture(s=>{s.clearedStages.Add(4);s.gold=16000;s.parts=160;s.gems=600;});
        var unlock=new GameCommand("robot_seeker_unlock");var design=Apply(unlock);
        check(design.state.gold==10000&&design.state.parts==100&&RobotSeason.HasBlueprint(design.state),"server blueprint grant/debit");
        check(repo.Apply(key,unlock).state.gold==10000,"blueprint request retry exactly once");
        Reject(new GameCommand("robot_seeker_unlock"),"second blueprint purchase rejected");
        var buy=new GameCommand("robot_storm_buy");var bought=Apply(buy);
        check(bought.state.gems==0&&bought.state.robotStyles.All(s=>s==4)&&bought.state.gold==10000,"server pack is cosmetic and immediately equipped");
        check(repo.Apply(key,buy).state.gems==0,"pack retry exactly once");
        Reject(new GameCommand("robot_storm_buy"),"duplicate pack rejected");
        Reject(new GameCommand("robot_rescue_craft"){frame=0},"unearned mastery craft rejected");
        foreach(string mode in new[]{RobotSeason.Modes[0],RobotSeason.Modes[1],RobotSeason.Modes[2],RobotSeason.Preview})
        {
            var before=repo.Bootstrap(key).state;var reply=Apply(new GameCommand("battle_start"){mode=mode});var t=reply.ticket;
            var c=new Combat(reply.state,t.stage,t.id,t.seed,false,t.mode);var input=new ReplayRecorder();
            for(int i=0;i<2800&&!c.Ended;i++)
            {
                if(c.Choosing){int skill=c.Choices[0];c.Choose(skill);input.Choose(skill);}
                if(c.CanPlanRobot(10,true)){c.PlanRobot(10,true);input.RobotPlan(10,true);}
                if(c.CanActivateMachine){c.ActivateMachine();input.Machine();}
                if(c.CanUltimate){c.UseUltimate();input.Ultimate();if(c.Ended)break;}
                V2 away=new V2();foreach(var enemy in c.Enemies){float d=V2.Distance(enemy.position,c.Position);if(d<120)away+=(c.Position-enemy.position).Normal*(120-d)/60;}
                foreach(var hazard in c.Hazards)if(V2.Distance(hazard.position,c.Position)<120)away+=(c.Position-hazard.position).Normal*2;
                var target=new V2(270+(float)Math.Cos(c.Elapsed*.25)*180,500+(float)Math.Sin(c.Elapsed*.25)*230);
                c.Tick(ReplayRecorder.Step,input.Move(((target-c.Position).Normal+away).Normal));
            }
            if(!c.Ended)c.Abandon();now=now.AddSeconds(c.Elapsed+5);
            var finish=new GameCommand("battle_finish",t.id){replay=input.Steps};var done=Apply(finish);
            check(done.state.receipts.Last().won==c.Won&&done.state.receipts.Last().kills==c.Kills,"verified theme settlement "+mode);
            check(repo.Apply(key,finish).state.coreShards==done.state.coreShards,"theme reward retry exactly once "+mode);
            check(done.state.gold==before.gold&&done.state.parts==before.parts&&done.state.gems==before.gems&&done.state.weekly.Count==0&&done.state.skillWeekly.Count==0,"theme does not pay growth or ranking "+mode);
            if(mode==RobotSeason.Preview)check(done.state.coreShards==before.coreShards&&JsonSerializer.Serialize(done.state.robotRecords,GameRepository.Json)==JsonSerializer.Serialize(before.robotRecords,GameRepository.Json),"preview never pays mastery/shards");
        }
        repo=new GameRepository(path,()=>now);check(RobotStyles.Owned(repo.Bootstrap(key).state,0,4),"pack survives server reload");
        Reject(new GameCommand("robot_style_equip"){frame=1,skin=3},"unowned free chassis cannot be equipped");
    }
}
