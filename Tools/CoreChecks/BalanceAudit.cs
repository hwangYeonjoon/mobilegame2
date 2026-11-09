using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using ScrapGuard.Core;

// Deterministic bot comparison, not human difficulty/performance evidence.
static class BalanceAudit
{
    public static int Run(string output,int seedCount=5,bool machineComparison=false)
    {
        var completed=new List<(int index,object row)>();var jobs=new List<(int index,int stage,int growth,bool avoid,bool machineEnabled,int seed)>();if(seedCount<1||seedCount>100)throw new ArgumentOutOfRangeException(nameof(seedCount));
        var seeds=new List<int>();int[] original={17,41,71,91,123};for(int i=0;i<seedCount;i++)seeds.Add(i<5?original[i]:127+(i-5)*37);
        foreach(int stage in machineComparison?new[]{0,17,41}:new[]{0,5,17,29,41})foreach(int growth in machineComparison?new[]{1}:new[]{0,1,2})
        foreach(bool avoid in machineComparison?new[]{true}:new[]{false,true})foreach(bool machineEnabled in machineComparison?new[]{true,false}:new[]{true})foreach(int seed in seeds)
            jobs.Add((jobs.Count,stage,growth,avoid,machineEnabled,seed));
        Parallel.ForEach(jobs,new ParallelOptions{MaxDegreeOfParallelism=2},job=>
        {
            int stage=job.stage,growth=job.growth,seed=job.seed;bool avoid=job.avoid,machineEnabled=job.machineEnabled;
            var state=GameState.New();state.tutorialCompleted=stage>0;
            if(growth>0)
            {
                state.heroes[0].level=Math.Min(30,1+stage/2+(growth==2?10:0));
                foreach(var item in state.items){item.level=Math.Min(20,1+stage/3+(growth==2?10:0));item.rarity=Math.Min(2,stage/12+(growth==2?1:0));}
            }
            var c=new Combat(state,stage,"audit-"+stage+"-"+seed,seed);var record=new ReplayRecorder();
            float firstMachine=-1,openingDamage=0;int peak=0;
            for(int frame=0;frame<18000&&!c.Ended;frame++)
            {
                if(c.Choosing){int choice=c.Choices[0];c.Choose(choice);record.Choose(choice);}
                if(c.CanChooseTask){c.ChooseTask(1);record.Encounter(1);}
                if(c.CanAssemble){c.Assemble(2);record.Assemble(2);}
                if(machineEnabled&&c.CanActivateMachine&&(c.Enemies.Count>=8||c.FinalBossActive))
                {c.ActivateMachine();record.Machine();if(firstMachine<0)firstMachine=c.Elapsed;}
                if(c.CanUltimate&&(c.Enemies.Count>=12||c.FinalBossActive)){c.UseUltimate();record.Ultimate();if(c.Ended)break;}
                c.Tick(ReplayRecorder.Step,record.Move(Move(c,avoid)));peak=Math.Max(peak,c.Enemies.Count);if(c.Elapsed<=20)openingDamage=c.DamageReceived;
            }
            bool exhausted=!c.Ended;if(exhausted)c.Abandon();
            var row=new {stage=stage+1,seed,loadout=growth==0?"fresh":growth==1?"assumed_progression":"advanced_sensitivity",machineEnabled,policy=avoid?"avoid_hazards":"ignore_hazards",
                attack=Catalog.Attack(state),health=Catalog.Health(state),duration=c.Duration,seconds=c.Elapsed,won=c.Won,budget_exhausted=exhausted,
                kills=c.Kills,level=c.Level,peakEnemies=peak,hp=c.Hp,maxHp=c.MaxHp,minHp=c.MinimumHp,
                openingDamage,damageBySource=c.DamageBySource,damageByCause=c.Report().damage,peakEnemyWarnings=c.PeakEnemyWarnings,damageReceived=c.DamageReceived,healing=c.HealingReceived,hazardHits=c.HazardHits,hazardDamage=c.HazardDamageReceived,
                machines=c.MachinesBuilt,firstMachine,machineSeconds=c.MachineActiveSeconds,
                machineUptime=c.MachineActiveSeconds/Math.Max(1,c.Elapsed),machineDamageShare=c.MachineDamage/Math.Max(1,c.DamageDealt),
                machineKills=c.MachineKills,killsDuringMachine=c.KillsDuringMachine,damageDuringMachine=c.DamageDuringMachine,damageDealt=c.DamageDealt,machineIntercepts=c.MachineIntercepts,
                bossReached=c.BossArenaActive,bossPatterns=c.BossPatterns,bossSeconds=c.BossCombatSeconds,firstEvolution=c.FirstEvolutionSeconds,evolvedPlaySeconds=c.FirstEvolutionSeconds<0?0:c.Elapsed-c.FirstEvolutionSeconds,
                evolutions=c.EvolutionMask,gunEvolution=c.WeaponEvolution,cause=(int)c.Report().cause};
            lock(completed)completed.Add((job.index,row));
            Console.WriteLine("stage="+(stage+1)+" "+row.loadout+" "+row.policy+" seed="+seed+" win="+c.Won+" time="+c.Elapsed.ToString("F1")+" machine="+row.machineUptime.ToString("P0"));
        });
        var rows=completed.OrderBy(x=>x.index).Select(x=>x.row).ToArray();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        File.WriteAllText(output,JsonSerializer.Serialize(new {kind="automated_simulation_not_human",rules=Combat.RulesVersion,
            seedCount,machineComparison,damageSources=Enum.GetNames(typeof(DamageSource)),
            notes="Paired fixed seeds per cell. Assumed and advanced progression are sensitivity profiles, not a measured player economy. Machine on/off paths may diverge. No HP, enemies, clock or skills injected during runs.",rows},new JsonSerializerOptions{WriteIndented=true}));
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
