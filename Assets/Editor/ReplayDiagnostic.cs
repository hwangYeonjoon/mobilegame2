using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ScrapGuard.Core;
namespace ScrapGuard.Editor
{
    public static class ReplayDiagnostic
    {
        static string F(float f)=>Math.Round(f,2).ToString(CultureInfo.InvariantCulture);
        static string P(V2 v)=>((int)Math.Round(v.x*4096))+","+((int)Math.Round(v.y*4096));
        public static void Trace(GameState state,int stage,string id,int seed,string mode,List<ReplayStep> steps,string path)
        {
            var c=new Combat(state,stage,id,seed,false,mode);int at=0;
            using(var w=new StreamWriter(path))foreach(var s in steps)
            {
                if(s.robotPlan!=0)c.PlanRobot(s.robotPlan,s.robotUpgrade);
                else if(s.machine)c.ActivateMachine(s.robot);
                else if(s.encounter!=0)c.ChooseTask(s.encounter);
                else if(s.assembly!=0)c.Assemble(s.assembly);
                else if(s.skill>=0)c.Choose(s.skill);
                else if(s.ultimate)c.UseUltimate();
                else for(int i=0;i<s.frames;i++)c.Tick(ReplayRecorder.Step,new V2(s.x/100f,s.y/100f));
                var b=new StringBuilder();b.Append(at++).Append('|').Append(F(c.Elapsed)).Append('|').Append(P(c.Position)).Append('|').Append(c.Kills).Append('|').Append(c.Level).Append('|').Append(c.Xp).Append('|').Append(F(c.Hp)).Append('|').Append(c.Salvage).Append('|').Append(c.CanActivateMachine).Append('|');
                foreach(var e in c.Enemies)b.Append(e.id).Append(':').Append(P(e.position)).Append(':').Append(F(e.hp)).Append(';');b.Append('|');
                foreach(var p in c.Pickups)b.Append(p.id).Append(':').Append(P(p.position)).Append(':').Append(p.xp).Append(';');b.Append('|');
                foreach(var p in c.Bullets)b.Append(p.id).Append(':').Append(P(p.position)).Append(':').Append(F(p.damage)).Append(';');
                w.WriteLine(b.ToString());
            }
        }
#if UNITY_EDITOR
        [Serializable] sealed class Probe {public GameState state;public int stage,seed;public string id,mode;public List<ReplayStep> steps;}
        public static void Capture()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-traceReplay");
            if(at<0||at+1>=args.Length)throw new ArgumentException("Supply -traceReplay with a local replay fixture path.");
            string path=args[at+1];
            var d=UnityEngine.JsonUtility.FromJson<Probe>(File.ReadAllText(path));
            Trace(d.state,d.stage,d.id,d.seed,d.mode,d.steps,path+".unity.trace");
        }
#endif
    }
}
