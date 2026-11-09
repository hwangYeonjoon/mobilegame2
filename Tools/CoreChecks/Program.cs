using System;
using ScrapGuard.Tests;
using ScrapGuard.Core;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length>=2 && (args[0]=="--balance-audit"||args[0]=="--machine-audit"||args[0]=="--onboarding-audit"||args[0]=="--robot-audit"||args[0]=="--intro-robot-audit"||args[0]=="--ramp-robot-audit"||args[0]=="--boss-ramp-audit")) return BalanceAudit.Run(args[1],args.Length>2?int.Parse(args[2]):5,args[0]=="--machine-audit",args.Length>3?Array.ConvertAll(args[3].Split(','),s=>int.Parse(s)-1):null,args[0]=="--onboarding-audit",args[0]=="--robot-audit",args[0]=="--intro-robot-audit",args[0]=="--ramp-robot-audit",args[0]=="--boss-ramp-audit");
        if (args.Length==2 && args[0]=="--case") { CoreSpecification.Run(args[1]);Console.WriteLine("PASS "+args[1]);return 0; }
        if (args.Length==2 && args[0]=="--verify-replay") return VerifyRuntimeReplay(args[1]);
        if (Array.IndexOf(args, "--simulate") >= 0) return Simulate();
        if (Array.IndexOf(args, "--campaign") >= 0) return CampaignSimulation();
        int failures = 0;
        foreach (var name in CoreSpecification.Names)
        {
            try { CoreSpecification.Run(name); Console.WriteLine("PASS " + name); }
            catch (Exception e) { failures++; Console.WriteLine("FAIL " + name + " " + (e.InnerException ?? e).Message); }
        }
        Console.WriteLine((CoreSpecification.Names.Length - failures) + "/" + CoreSpecification.Names.Length + " passed");
        return failures == 0 ? 0 : 1;
    }
    #pragma warning disable CS0649 // Deserialized diagnostic fields.
    sealed class RuntimeProbe
    {
        public GameState state;public System.Collections.Generic.List<ReplayStep> steps;
        public string id,mode;public int stage,seed,kills,machines,waves;public float hp,seconds;public bool won;
    }
    #pragma warning restore CS0649
    static int VerifyRuntimeReplay(string path)
    {
        var data=System.Text.Json.JsonSerializer.Deserialize<RuntimeProbe>(System.IO.File.ReadAllText(path),new System.Text.Json.JsonSerializerOptions{IncludeFields=true});
        var trace=new Combat(data.state,data.stage,data.id,data.seed,false,data.mode);int at=0;
        foreach(var step in data.steps)
        {
            if(step.robotPlan!=0){if(!trace.PlanRobot(step.robotPlan,step.robotUpgrade))throw new Exception("Invalid robot plan input");}
            else if(step.machine){if(!trace.ActivateMachine(step.robot))throw new Exception("Invalid machine input");}
            else if(step.encounter!=0){if(!trace.ChooseTask(step.encounter))throw new Exception("Invalid encounter");}
            else if(step.assembly!=0){if(!trace.Assemble(step.assembly))throw new Exception("Invalid assembly");}
            else if(step.skill>=0&&!trace.Choose(step.skill)){Console.WriteLine("DIVERGED step="+at+" time="+trace.Elapsed+" level="+trace.Level+" xp="+trace.Xp+" requested="+step.skill+" choosing="+trace.Choosing+" offered="+string.Join(",",trace.Choices)+" pos="+trace.Position.x+","+trace.Position.y+" kills="+trace.Kills);return 1;}
            else if(step.ultimate)trace.UseUltimate();
            else if(step.skill<0)for(int i=0;i<step.frames;i++)trace.Tick(ReplayRecorder.Step,new V2(step.x/100f,step.y/100f));
            at++;
        }
        var result=ReplayRecorder.Verify(data.state,data.stage,data.id,data.seed,data.steps,600,data.mode);
        bool match=result.Kills==data.kills && result.MachinesBuilt==data.machines && result.WaveNumber==data.waves && result.Won==data.won && Math.Abs(result.Hp-data.hp)<.02f;
        Console.WriteLine("Unity -> .NET replay: "+(match?"PASS":"FAIL")+" kills="+result.Kills+"/"+data.kills+" machines="+result.MachinesBuilt+"/"+data.machines+" hp="+result.Hp+"/"+data.hp);
        return match?0:1;
    }
    static int Simulate()
    {
        for (int seed = 0; seed < 5; seed++)
        {
            var c = new Combat(GameState.New(), 0, "sim", seed);
            for (int i = 0; i < 10000 && !c.Ended; i++)
            {
                if (c.Choosing)
                {
                    int choice = c.Choices[0]; int[] priority = { 1, 2, 0, 3, 4, 5, 6, 7 };
                    foreach (int skill in priority) if (Array.IndexOf(c.Choices, skill) >= 0) { choice = skill; break; }
                    c.Choose(choice);
                }
                V2 away = new V2();
                foreach (var e in c.Enemies) { V2 delta = c.Position - e.position; float d = delta.Length; if (d < 100) away += delta.Normal * ((100 - d) / 90); }
                V2 target = new V2(270 + (float)Math.Cos(c.Elapsed * .4f) * 160, 490 + (float)Math.Sin(c.Elapsed * .4f) * 210);
                c.Tick(.05f, ((target - c.Position).Normal + away).Normal);
            }
            Console.WriteLine("Seed=" + seed + " won=" + c.Won + " time=" + c.Elapsed.ToString("F1") + " kills=" + c.Kills + " level=" + c.Level + " hp=" + c.Hp.ToString("F0") + " skills=" + string.Join(",",c.Skills));
        }
        return 0;
    }
    static int CampaignSimulation()
    {
        int wins=0;
        for(int stage=0;stage<Campaign.Count;stage++)
        {
            var state=GameState.New();state.heroes[0].level=30;state.tutorialCompleted=true;
            foreach(var item in state.items){item.level=20;item.rarity=2;}
            var c=new Combat(state,stage,"balance",41);
            for(int i=0;i<15000 && !c.Ended;i++)
            {
                if(c.Choosing)
                {
                    int choice=c.Choices[0];foreach(int skill in new[]{0,4,1,3,2,5,6,7})if(Array.IndexOf(c.Choices,skill)>=0){choice=skill;break;}
                    c.Choose(choice);
                }
                if(c.CanUltimate)c.UseUltimate();
                V2 away=new V2();foreach(var e in c.Enemies){var delta=c.Position-e.position;float d=delta.Length;if(d<110)away+=delta.Normal*((110-d)/60);}
                foreach(var h in c.Hazards)
                {
                    bool near=h.width>0?Math.Abs(c.Position.x-h.position.x)<h.width/2+25 && Math.Abs(c.Position.y-h.position.y)<h.height/2+25:V2.Distance(c.Position,h.position)<h.radius+25;
                    if(near)away+=(c.Position-h.position).Normal*3;
                }
                var target=new V2(270+(float)Math.Cos(c.Elapsed*.4f)*180,490+(float)Math.Sin(c.Elapsed*.4f)*230);
                c.Tick(ReplayRecorder.Step,((target-c.Position).Normal+away).Normal);
            }
            if(c.Won)wins++;
            Console.WriteLine("operation="+(stage+1)+" goal="+c.Definition.Mission+" won="+c.Won+" elapsed="+c.Elapsed.ToString("F1")+" kills="+c.Kills+" hp="+c.Hp.ToString("F0"));
            if(!c.Ended)return 1;
        }
        Console.WriteLine("Max-level scripted pilot wins="+wins+"/"+Campaign.Count+" (balance observation, not a player completion guarantee)");
        return 0;
    }
}
