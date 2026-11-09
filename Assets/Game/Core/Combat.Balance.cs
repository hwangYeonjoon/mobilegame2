using System;
using System.Collections.Generic;
namespace ScrapGuard.Core
{
    public enum DamageSource { Unspecified, Contact, EnemyAttack, Terrain, Condition, Boss }
    public enum HealingSource { Pickup, RepairSkill, FieldTask, RepairMod, Machine, MaxedBuild, Support }
    public sealed partial class Combat
    {
        readonly float[] damageBySource=new float[6];
        readonly float[] healingBySource=new float[7];
        public readonly bool IntroBattle;
        // Campaign operations 2/4/6 extend the introduction without changing ranked rules.
        // Stage is zero-based; the first-clear-only introduction remains separate.
        public int BossLearningTier {get{return Challenge?0:IntroBattle?1:Stage>=1&&Stage<=5?(Stage<=1?1:Stage<=3?2:3):0;}}
        float BossBaseHealth {get{return IntroBattle?18000:BossLearningTier==1?24000:BossLearningTier==2?32000:BossLearningTier==3?36000:50000;}}
        float BossPatternInterval {get{return BossLearningTier==1?5f:BossLearningTier==2?4.6f:BossLearningTier==3?4.2f:3.8f;}}
        float BossHitRecovery {get{return BossLearningTier==1?.8f:BossLearningTier==2?.7f:BossLearningTier==3?.7f:.45f;}}
        float BossDamageScale {get{return BossLearningTier==1?.45f:BossLearningTier==2?.52f:BossLearningTier==3?.56f:.6f+.4f*Math.Min(1,Stage/18f);}}
        float BossChargeSpeed {get{return BossLearningTier==1?145:BossLearningTier==2?160:BossLearningTier==3?170:190;}}
        float BossChargeWindup {get{return BossLearningTier==1?1.3f:BossLearningTier==2?1.15f:BossLearningTier==3?1f:.8f;}}
        float BossChargeRecovery {get{return BossLearningTier==1?6f:BossLearningTier==2?5.5f:BossLearningTier==3?5f:4.5f;}}
        public float RegionEntryThreat {get{return Challenge?1:.75f+.25f*Math.Min(1,(Stage%6)/3f);}}
        public float LowHealthSeconds {get;private set;}
        public float[] HealingBySource {get{return (float[])healingBySource.Clone();}}
        float areaHitRecovery,bossHitRecovery,enemyWarningRecovery;
        public float[] DamageBySource {get{return (float[])damageBySource.Clone();}}
        public float DamageDuringMachine {get;private set;}
        public int KillsDuringMachine {get;private set;}
        public int MachineKills {get;private set;}
        public int MachineIntercepts {get;private set;}
        public int BossPatterns {get;private set;}
        public float BossContactDamage {get;private set;}
        public float BossWarningDamage {get;private set;}
        public int PeakEnemyWarnings {get;private set;}
        static DamageSource ResolveDamageSource(DamageSource source,DamageCause cause)
        {
            if(source!=DamageSource.Unspecified)return source;
            if(cause==DamageCause.Boss)return DamageSource.Boss;
            if(cause==DamageCause.Contact||cause==DamageCause.Charge)return DamageSource.Contact;
            if(cause==DamageCause.Poison||cause==DamageCause.Rail||cause==DamageCause.Laser||cause==DamageCause.Heat)return DamageSource.Terrain;
            if(cause==DamageCause.Electricity)return DamageSource.Condition;
            return DamageSource.EnemyAttack;
        }
        EnemyRole OpeningEnemyRole(EnemyRole role)
        {
            if(Elapsed<12)return EnemyRole.Pursuer;
            if(role==EnemyRole.Artillery||role==EnemyRole.Bomber)
            {
                int limit=Elapsed<25?1:3,count=0;
                foreach(var e in Enemies)if(e.hp>0&&!e.finalBoss&&e.role==role)count++;
                if(count>=limit)return EnemyRole.Pursuer;
            }
            return role;
        }
        public float MachineRecovery {get;private set;}
        public float MachineActiveSeconds {get;private set;}
        public float DamageDealt {get;private set;}
        public float MachineDamage {get;private set;}
        public float DamageReceived {get;private set;}
        public float HealingReceived {get;private set;}
        public float MinimumHp {get;private set;}
        public float HazardDamageReceived {get;private set;}
        int experienceCarry;
        void GainExperience(int amount)
        {
            if(Challenge){Xp+=amount;return;}
            int scale=10240/(10+Definition.Region*2);experienceCarry+=amount*scale;Xp+=experienceCarry/1024;experienceCarry%=1024;
        }
        public int HazardHits {get;private set;}
        public float FirstEvolutionSeconds {get;private set;}=-1;
        public float BossCombatSeconds {get;private set;}
        public bool BossArenaActive {get;private set;}
        public V2 BossArenaCenter {get;private set;}
        public float BossArenaWarning {get{return !Challenge&&!BossArenaActive&&Definition.Mission==Mission.Eliminate&&Elapsed>=Duration-33?Math.Max(0,Duration-30-Elapsed):0;}}
        public V2 ClampBossPoint(V2 point,float radius)
        {
            if(!BossArenaActive)return point;
            return new V2(Math.Max(BossArenaCenter.x-235+radius,Math.Min(BossArenaCenter.x+235-radius,point.x)),
                Math.Max(BossArenaCenter.y-300+radius,Math.Min(BossArenaCenter.y+300-radius,point.y)));
        }
        void UpdateBossArena()
        {
            if(Challenge||BossArenaActive||Definition.Mission!=Mission.Eliminate||Elapsed<Duration-30)return;
            // The warning precedes this transition. Center on the player's current position,
            // then clear regional geometry rather than teleporting into an obstacle.
            BossArenaCenter=new V2(Math.Max(WorldLeft+235,Math.Min(WorldRight-235,Position.x)),Math.Max(WorldTop+300,Math.Min(WorldBottom-300,Position.y)));
            BossArenaActive=true;Position=ClampBossPoint(Position,26);Hazards.Clear();WaveWarning=0;reviveGuard=Math.Max(reviveGuard,2);
            foreach(var enemy in Enemies)enemy.position=ClampBossPoint(enemy.position,enemy.Radius);
            Effects.Add(new BattleEffect{position=Position,kind=10,text="보스 결전 · 전투 구역 고정"});
        }
        void Heal(float amount,HealingSource source)
        {
            float actual=Math.Max(0,Math.Min(MaxHp-Hp,amount));HealingReceived+=actual;healingBySource[(int)source]+=actual;Hp+=actual;
        }
        static readonly int[] EvolutionAttackIds={0,1,2,3,8,9,10};
        static int EvolutionSupport(int attack){return attack==0||attack==8?4:attack==1||attack==10?7:attack==2?6:attack==3?5:11;}
        int FocusedGrowth(List<int> pool)
        {
            if(Challenge||Level%2!=0)return -1;
            // Encourage completing a chosen attack, without selecting it for the player.
            int best=-1,level=0;
            foreach(int attack in EvolutionAttackIds)
            {
                int support=EvolutionSupport(attack);
                if(Skills[attack]>level&&(Skills[attack]<5||Skills[support]<3)){best=attack;level=Skills[attack];}
            }
            if(best>=0){int option=Skills[best]<5?best:EvolutionSupport(best);if(pool.Contains(option))return option;}
            return -1;
        }
    }
}
