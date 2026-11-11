using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ScrapGuard.Core;
using ScrapGuard.Server;
static class ReplayExpansionChecks
{
    public static void Run(string folder,Action<bool,string> check)
    {
        string path=Path.Combine(folder,"replay-expansion.db"),key=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
        var now=new DateTimeOffset(2026,11,9,0,0,0,TimeSpan.Zero);var repo=new GameRepository(path,()=>now);var first=repo.Register(key);
        ServerReply Apply(GameCommand c){c.revision=repo.Bootstrap(key).revision;return repo.Apply(key,c);}
        void Reject(GameCommand c,string name){try{Apply(c);throw new Exception("Unexpected success "+name);}catch(ApiFault ex){check(ex.Status==400||ex.Status==409,name);}}
        void Fixture(Action<GameState> f)
        {
            var s=repo.Bootstrap(key).state;f(s);using var db=new SqliteConnection("Data Source="+path);db.Open();using var q=db.CreateCommand();
            q.CommandText="UPDATE accounts SET state=$s WHERE id=$id";q.Parameters.AddWithValue("$s",JsonSerializer.Serialize(s,GameRepository.Json));q.Parameters.AddWithValue("$id",first.playerId);q.ExecuteNonQuery();
        }
        Reject(new GameCommand("salvage_claim"),"server refuses unearned salvage");
        Reject(new GameCommand("robot_week_claim"){value=Endgame.Week(now)},"server refuses unearned robot contract");
        Reject(new GameCommand("battle_start"){stage=17,mode=BossTraining.Mode},"server refuses locked boss practice");
        Fixture(s=>{s.unlockedStage=5;for(int i=0;i<5;i++)s.clearedStages.Add(i);s.heroes[0].level=20;foreach(var item in s.items){item.level=20;item.rarity=2;}});
        Reject(new GameCommand("battle_start"){stage=20,mode=SurvivalTraining.Mode},"server refuses locked survival practice");
        Reject(new GameCommand("battle_start"){stage=1,mode=SurvivalTraining.Mode},"server refuses survival practice on boss operation");
        foreach(string mode in new[]{BossTraining.Mode,SurvivalTraining.Mode,Endgame.WeeklyMode})
        {
            var before=repo.Bootstrap(key).state;var reply=Apply(new GameCommand("battle_start"){stage=mode==SurvivalTraining.Mode?2:1,mode=mode});var t=reply.ticket;
            var c=new Combat(reply.state,t.stage,t.id,t.seed,false,t.mode);var recorder=new ReplayRecorder();
            int design=c.WeeklyFrame*4+1;
            if(c.CanPlanRobot(design,true)){c.PlanRobot(design,true);recorder.RobotPlan(design,true);c.ActivateMachine();recorder.Machine();}
            for(int i=0;i<2700&&!c.Ended;i++)
            {
                if(c.Choosing){int pick=c.Choices[0];c.Choose(pick);recorder.Choose(pick);}
                if(c.CanUltimate){c.UseUltimate();recorder.Ultimate();if(c.Ended)break;}
                var target=new V2(270+(float)Math.Cos(c.Elapsed*.25)*180,500+(float)Math.Sin(c.Elapsed*.25)*230);
                c.Tick(ReplayRecorder.Step,recorder.Move((target-c.Position).Normal));
            }
            if(!c.Ended)c.Abandon();now=now.AddSeconds(c.Elapsed+5);
            var command=new GameCommand("battle_finish",t.id){replay=recorder.Steps};var done=Apply(command);
            check(done.state.receipts.Last().won==c.Won,"server replay accepts "+mode);
            check(repo.Apply(key,command).state.receipts.Count==done.state.receipts.Count,"finish retry idempotent "+mode);
            if(mode==BossTraining.Mode||mode==SurvivalTraining.Mode)
            {
                check(done.state.gold==before.gold&&done.state.gems==before.gems&&done.state.parts==before.parts&&done.state.totalRuns==before.totalRuns&&done.state.clearedStages.Count==before.clearedStages.Count&&done.state.dailySupplySeconds==before.dailySupplySeconds&&done.state.robotRecords.Count==before.robotRecords.Count,"practice cannot pay or advance campaign");
            }
            else
            {
                var w=done.state.weekly.Single();check(w.robotContract&&done.state.robotRecords.Any(r=>r.design==design),"server derives weekly robot contract and collection from replay");
                var claim=new GameCommand("robot_week_claim"){value=w.week};var paid=Apply(claim);check(paid.state.gold==done.state.gold+3000,"robot contract exact payout");
                check(repo.Apply(key,claim).state.gold==paid.state.gold,"contract retry cannot duplicate payout");Reject(new GameCommand("robot_week_claim"){value=w.week},"contract second request rejected");
            }
        }
        // Earned-counter fixture tests persistence/idempotency; not progression evidence.
        Fixture(s=>s.dailySalvageWins=3);var reward=new GameCommand("salvage_claim");var a=Apply(reward);
        check(repo.Apply(key,reward).state.parts==a.state.parts,"salvage retry idempotent");repo=new GameRepository(path,()=>now);
        Reject(new GameCommand("salvage_claim"),"salvage remains claimed after restart");now=now.AddDays(1);Reject(new GameCommand("salvage_claim"),"new day requires new verified victories");
    }
}
