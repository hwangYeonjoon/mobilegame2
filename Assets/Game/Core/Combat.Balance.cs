using System;
using System.Collections.Generic;
namespace ScrapGuard.Core
{
    public sealed partial class Combat
    {
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
        void Heal(float amount)
        {
            float actual=Math.Max(0,Math.Min(MaxHp-Hp,amount));HealingReceived+=actual;Hp+=actual;
        }
        static readonly int[] EvolutionAttackIds={0,1,2,3,8,9,10};
        static int EvolutionSupport(int attack){return attack==0||attack==8?4:attack==1||attack==10?7:attack==2?6:attack==3?5:11;}
        int FocusedGrowth(List<int> pool)
        {
            if(Challenge)return -1;
            // Encourage completing a chosen attack, without selecting it for the player.
            int best=-1,level=0;
            foreach(int attack in EvolutionAttackIds)
            {
                int support=EvolutionSupport(attack);
                if(Skills[attack]>level&&(Skills[attack]<4||Skills[support]<2)){best=attack;level=Skills[attack];}
            }
            if(best>=0){int option=Skills[best]<4?best:EvolutionSupport(best);if(pool.Contains(option))return option;}
            return -1;
        }
    }
}
