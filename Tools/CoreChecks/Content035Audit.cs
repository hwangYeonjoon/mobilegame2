using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using ScrapGuard.Core;
static class Content035Audit
{
    static GameState Restore(GrowthAudit.Snapshot x,bool cap)
    {
        var s=GameState.New();s.tutorialCompleted=true;s.heroes[0].level=cap?30:x.heroLevel;
        s.unlockedStage=59;s.clearedStages=Enumerable.Range(0,60).ToList();
        foreach(var g in x.equipped){var item=s.items.Find(i=>i.id==s.equipped[g.slot]);item.definition=g.definition;item.rarity=g.rarity;item.level=cap?20:g.level;}
        return s;
    }
    public static int Run(string input,string output,bool hard)
    {
        var profiles=JsonSerializer.Deserialize<Content034Audit.Input>(File.ReadAllText(input),GrowthAudit.Json).profiles;
        var rows=new List<object>();
        foreach(var p in profiles)foreach(bool cap in new[]{false,true})foreach(int stage in new[]{47,53,59})
        {
            var s=Restore(p.late,cap);int seed=PurchaseAudit.Seed(p.seed,stage,1,false);
            var c=GrowthAudit.Simulate(s,stage,"035",seed,p.policy,out var replay);
            rows.Add(Row(p.seed,p.policy,cap?"cap":"earned","campaign",stage,s,c));
            if(cap&&hard){var h=GrowthAudit.Simulate(s,stage,"035-hard",seed,p.policy,out replay,"apex");rows.Add(Row(p.seed,p.policy,"cap","apex",stage,s,h));}
            File.WriteAllText(output,JsonSerializer.Serialize(new{schema=1,rules=Combat.RulesVersion,build=AppRelease.Build,source=input,kind="matched_bot_diagnostic_not_human",rows},GrowthAudit.Json));
        }
        Console.WriteLine("035 audit battles="+rows.Count);return 0;
    }
    static object Row(int seed,string policy,string gear,string mode,int stage,GameState s,Combat c)=>new{seed,policy,gear,mode,stage=stage+1,attack=Catalog.Attack(s),health=Catalog.Health(s),c.Won,c.Elapsed,c.Hp,c.MaxHp,c.MinimumHp,c.DamageReceived,c.HealingReceived,c.BossCombatSeconds,c.BossPatterns,c.PeakBossWarnings,c.LowHealthSeconds,c.MachineActiveSeconds,c.MachinesBuilt};
}
