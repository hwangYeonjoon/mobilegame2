using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ScrapGuard.Core;
using ScrapGuard.Tests;
static class RobotSeasonAudit
{
    public static int Run(string path)
    {
        var rows=new List<object>();
        foreach(bool avoid in new[]{false,true})foreach(string mode in RobotSeason.Modes)foreach(int design in new[]{1,5,9,10})
        {
            int wins=0,goals=0;double kills=0,damage=0;
            for(int seed=0;seed<10;seed++)
            {
                int random=Endgame.Seed("2026-11-09")+seed;
                var c=CoreSpecification.PlayRobotSeason(GameState.New(),mode,random,design,out var input,avoid);
                int machineKills=0;foreach(var r in c.RobotReport())machineKills+=r.kills;
                if(c.Won)wins++;if(c.Won&&machineKills>=15)goals++;kills+=machineKills;damage+=c.DamageReceived;
                rows.Add(new{mode,design,avoid,seed=random,won=c.Won,goal=c.Won&&machineKills>=15,machineKills,seconds=c.Elapsed,hp=c.Hp,damage=c.DamageReceived,machineDamage=c.MachineDamage,peakWarnings=c.PeakEnemyWarnings});
            }
            Console.WriteLine(mode+" design="+design+" avoid="+avoid+" wins="+wins+" goals="+goals+" meanRobotKills="+(kills/10).ToString("F1")+" meanDamage="+(damage/10).ToString("F1"));
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonSerializer.Serialize(new{rules=Combat.RulesVersion,rows},new JsonSerializerOptions{WriteIndented=true}));return 0;
    }
}
