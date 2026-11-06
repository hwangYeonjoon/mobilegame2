using System;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard.Editor
{
    public static class CombatRuntimeAudit
    {
        [Serializable] sealed class Probe
        {
            public GameState state;public System.Collections.Generic.List<ReplayStep> steps;
            public string id,mode;public int stage,seed,kills,machines,waves;public float hp,seconds;public bool won;
        }
        public static void CaptureAndBuildMac(){Capture();ProjectBuilder.BuildMac();}
        public static void Capture()
        {
            Directory.CreateDirectory("TestResults/RuntimeAuditFinal");
            foreach(int stage in new[]{0,1,7,31,4})foreach(int seed in new[]{1,17,71})
            {
                var state=GameState.New();state.heroes[0].level=30;foreach(var item in state.items){item.level=20;item.rarity=2;}
                var c=new Combat(state,stage,"editor-probe-"+stage+"-"+seed,seed,false,stage==4?SkillTrial.Mode:"");var input=new ReplayRecorder();
                for(int frame=0;frame<6000&&!c.Ended;frame++)
                {
                    if(c.Choosing){int skill=c.Choices[0];foreach(int candidate in c.Choices)if(candidate>=8){skill=candidate;break;}c.Choose(skill);input.Choose(skill);}
                    if(c.CanUltimate){c.UseUltimate();input.Ultimate();if(c.Ended)break;}
                    var target=new V2(270+Mathf.Cos(c.Elapsed*.06f)*450,490+Mathf.Sin(c.Elapsed*.06f)*600);
                    c.Tick(ReplayRecorder.Step,input.Move((target-c.Position).Normal));
                }
                c.Abandon();
                File.WriteAllText("TestResults/RuntimeAuditFinal/probe-"+stage+"-"+seed+".json",JsonUtility.ToJson(new Probe{state=state,steps=input.Steps,id=c.RunId,mode=c.Mode,stage=stage,seed=seed,kills=c.Kills,machines=c.MachinesBuilt,waves=c.WaveNumber,hp=c.Hp,seconds=c.Elapsed,won=c.Won}));
            }
        }
    }
}
