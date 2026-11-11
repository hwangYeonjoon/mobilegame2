using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ScrapGuard.Core;
static class RegionalAudit
{
    public static int Run(string input,string output)
    {
        var profiles=JsonSerializer.Deserialize<GrowthValueAudit.Input>(File.ReadAllText(input),GrowthAudit.Json).profiles;
        var rows=new List<object>();
        Parallel.ForEach(profiles,new ParallelOptions{MaxDegreeOfParallelism=2},p=>
        {
            var x=p.snapshot;var s=GameState.New();s.tutorialCompleted=p.stage>1||x.clearedStages.Length>0;s.heroes[0].level=x.heroLevel;
            s.unlockedStage=x.unlockedStage-1;s.clearedStages=x.clearedStages.Select(v=>v-1).ToList();
            foreach(var g in x.equipped){var item=s.items.Find(i=>i.id==s.equipped[g.slot]);item.definition=g.definition;item.rarity=g.rarity;item.level=g.level;}
            var c=GrowthAudit.Simulate(s,p.stage-1,"regional",PurchaseAudit.Seed(p.seed,p.stage-1,1,false),p.policy,out var replay);
            var row=new{p.seed,p.stage,p.policy,c.Won,c.Elapsed,c.Hp,c.MaxHp,c.DamageReceived,c.PeakEnemyWarnings,c.BossCombatSeconds,c.Kills,damage=c.DamageBySource};
            lock(rows){rows.Add(row);Console.WriteLine("regional "+p.policy+" "+p.stage+" "+p.seed+" won="+c.Won);}
        });
        File.WriteAllText(output,JsonSerializer.Serialize(new{build=AppRelease.Build,rules=Combat.RulesVersion,kind="matched_regional_regression_not_human",source=input,rows},GrowthAudit.Json));return 0;
    }
}
