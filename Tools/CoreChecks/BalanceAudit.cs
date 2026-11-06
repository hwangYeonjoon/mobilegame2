using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ScrapGuard.Core;

// Deterministic bot comparison, not human difficulty/performance evidence.
static class BalanceAudit
{
    public static int Run(string output)
    {
        var rows=new List<object>();
        foreach(int stage in new[]{0,5,17,29,41})foreach(bool grown in new[]{false,true})
        foreach(bool avoid in new[]{false,true})foreach(int seed in new[]{17,41,71,91,123})
        {
            var state=GameState.New();state.tutorialCompleted=stage>0;
            if(grown)
            {
                state.heroes[0].level=Math.Min(30,1+stage/2);
                foreach(var item in state.items){item.level=Math.Min(20,1+stage/3);item.rarity=Math.Min(2,stage/12);}
            }
            var c=new Combat(state,stage,"audit-"+stage+"-"+seed,seed);var record=new ReplayRecorder();
            float firstMachine=-1;int peak=0;
            for(int frame=0;frame<18000&&!c.Ended;frame++)
            {
                if(c.Choosing){int choice=c.Choices[0];c.Choose(choice);record.Choose(choice);}
                if(c.CanChooseTask){c.ChooseTask(1);record.Encounter(1);}
                if(c.CanAssemble){c.Assemble(2);record.Assemble(2);}
                if(c.CanActivateMachine&&(c.Enemies.Count>=8||c.FinalBossActive))
                {c.ActivateMachine();record.Machine();if(firstMachine<0)firstMachine=c.Elapsed;}
                if(c.CanUltimate&&(c.Enemies.Count>=12||c.FinalBossActive)){c.UseUltimate();record.Ultimate();if(c.Ended)break;}
                c.Tick(ReplayRecorder.Step,record.Move(Move(c,avoid)));peak=Math.Max(peak,c.Enemies.Count);
            }
            bool exhausted=!c.Ended;if(exhausted)c.Abandon();
            var row=new {stage=stage+1,seed,loadout=grown?"assumed_progression":"fresh",policy=avoid?"avoid_hazards":"ignore_hazards",
                attack=Catalog.Attack(state),health=Catalog.Health(state),seconds=c.Elapsed,won=c.Won,budget_exhausted=exhausted,
                kills=c.Kills,level=c.Level,peakEnemies=peak,hp=c.Hp,maxHp=c.MaxHp,minHp=c.MinimumHp,
                damageReceived=c.DamageReceived,healing=c.HealingReceived,hazardHits=c.HazardHits,hazardDamage=c.HazardDamageReceived,
                machines=c.MachinesBuilt,firstMachine,machineSeconds=c.MachineActiveSeconds,
                machineUptime=c.MachineActiveSeconds/Math.Max(1,c.Elapsed),machineDamageShare=c.MachineDamage/Math.Max(1,c.DamageDealt),
                bossSeconds=c.BossCombatSeconds,firstEvolution=c.FirstEvolutionSeconds,evolvedPlaySeconds=c.FirstEvolutionSeconds<0?0:c.Elapsed-c.FirstEvolutionSeconds,
                evolutions=c.EvolutionMask,gunEvolution=c.WeaponEvolution,cause=(int)c.Report().cause};
            rows.Add(row);
            Console.WriteLine("stage="+(stage+1)+" "+row.loadout+" "+row.policy+" seed="+seed+" win="+c.Won+" time="+c.Elapsed.ToString("F1")+" machine="+row.machineUptime.ToString("P0"));
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        File.WriteAllText(output,JsonSerializer.Serialize(new {kind="automated_simulation_not_human",rules=Combat.RulesVersion,
            notes="Five fixed seeds per cell. Assumed progression is a sensitivity profile, not a measured player economy. Same loadout and seeds for both movement policies. No HP, enemies, clock or skills injected during runs.",rows},new JsonSerializerOptions{WriteIndented=true}));
        return 0;
    }
    static V2 Move(Combat c,bool avoid)
    {
        V2 target=c.BossArenaActive?c.BossArenaCenter:new V2(270+(float)Math.Cos(c.Elapsed*.055f)*240,510+(float)Math.Sin(c.Elapsed*.055f)*280);
        var boss=c.Enemies.Find(e=>e.finalBoss&&e.hp>0);
        if(boss!=null)target=boss.position+(c.Position-boss.position).Normal*120;
        else
        {
            float nearest=300;foreach(var p in c.Pickups){float d=V2.Distance(p.position,c.Position);if(d<nearest){nearest=d;target=p.position;}}
        }
        V2 away=new V2();foreach(var e in c.Enemies){var delta=c.Position-e.position;float d=delta.Length;if(d<90)away+=delta.Normal*((90-d)/55);}
        var desired=((target-c.Position).Normal+away).Normal;V2 best=desired;float bestScore=float.NegativeInfinity;
        for(int i=0;i<16;i++)
        {
            var direction=V2.Angle(i*(float)Math.PI/8);var point=c.Position+direction*48;
            float score=direction.x*desired.x+direction.y*desired.y;
            if(c.IsBlocked(point,18))score-=10;
            if(V2.Distance(point,c.ClampBossPoint(point,26))>1)score-=10;
            if(avoid)
            {
                foreach(var h in c.Hazards)
                {
                    bool inside=h.width>0?Math.Abs(point.x-h.position.x)<h.width/2+26&&Math.Abs(point.y-h.position.y)<h.height/2+26:V2.Distance(point,h.position)<h.radius+26;
                    if(inside)score-=4/(.25f+h.delay);
                }
                if(!c.BossArenaActive&&c.Definition.Region==2)foreach(var z in c.Zones)if(z.Contains(point,20))score-=4;
            }
            if(score>bestScore){bestScore=score;best=direction;}
        }
        return best;
    }
}
