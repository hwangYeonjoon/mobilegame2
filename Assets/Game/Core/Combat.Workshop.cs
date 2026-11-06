using System;

namespace ScrapGuard.Core
{
    public sealed partial class Combat
    {
        readonly float[] damageTaken=new float[13];
        DamageCause endCause;
        float turretTimer,repairTimer;
        int announcedMods;
        public int DefeatedRoles { get; private set; }
        public int Mods { get { return Workshop.ModMask(Skills)|FieldMods; } }
        public bool HasMod(int id){return (Mods&(1<<id))!=0;}
        public V2 TurretPosition { get { return FieldModule==1?FieldAnchor:Position+V2.Angle(Elapsed*1.8f)*46; } }
        void UpdateMods(float dt)
        {
            int unlocked=Mods&~announcedMods;
            for(int i=0;i<Workshop.ModNames.Length;i++)if((unlocked&(1<<i))!=0)Effects.Add(new BattleEffect {position=Position,kind=7,text=Workshop.ModNames[i]+" 개조 완성!"});
            announcedMods|=Mods;
            if(HasMod(0))foreach(var enemy in Enemies)
            {
                float distance=V2.Distance(Position,enemy.position);
                if(!enemy.core && !enemy.finalBoss && distance>45 && distance<150)enemy.position=Slide(enemy.position,(Position-enemy.position).Normal*(95*dt),Math.Min(enemy.Radius,20));
            }
            repairTimer-=dt;
            if(HasMod(4) && repairTimer<=0)
            {
                repairTimer=10;Heal(MaxHp*.03f);Shield=Math.Max(Shield,MaxHp*.08f);
                Effects.Add(new BattleEffect{position=DronePosition,kind=6,text="수리 드론 +3%"});
            }
            turretTimer-=dt;
            if(HasMod(2) && turretTimer<=0)
            {
                var origin=TurretPosition;var target=Nearest(origin);
                if(target!=null)
                {
                    turretTimer=InTurretZone?.20f:.28f;
                    if(FieldModule==1 && Hero==0)Bullets.Add(new Bullet {id=nextId++,position=origin+new V2(40,0),velocity=(target.position-(origin+new V2(40,0))).Normal*440,damage=Damage*.65f,life=1.7f,pierce=1,mod=true});
                    Bullets.Add(new Bullet {id=nextId++,position=origin,velocity=(target.position-origin).Normal*440,damage=Damage*.65f,life=1.7f,pierce=1,mod=true});
                }
            }
        }
        void ChainLightning()
        {
            V2 origin=DronePosition;var target=Nearest(origin);var struck=new System.Collections.Generic.HashSet<int>();
            for(int hop=0;hop<4 && target!=null;hop++)
            {
                Hit(target,Damage*(1.4f-hop*.2f));
                Effects.Add(new BattleEffect {position=origin,end=target.position,kind=8,radius=0});
                struck.Add(target.id);origin=target.position;target=null;float nearest=200;
                foreach(var enemy in Enemies)
                {
                    if(enemy.hp<=0 || struck.Contains(enemy.id))continue;
                    float distance=V2.Distance(origin,enemy.position);if(distance<nearest){nearest=distance;target=enemy;}
                }
            }
        }
        public BattleReport Report()
        {
            var result=new BattleReport {cause=(int)endCause,mods=Mods,task=(int)Task,taskChoice=TaskChoice,taskCompleted=TaskCompleted,fieldModule=FieldModule,hero=Hero,machines=MachinesBuilt,waves=WaveNumber,evolution=WeaponEvolution,skillEvolutions=EvolutionMask,revived=Revived,skills=(int[])Skills.Clone(),damage=(float[])damageTaken.Clone()};
            if(bossSpawned && !Challenge)
            {
                result.bossRemaining=Won?0:-1;
                foreach(var enemy in Enemies)if(enemy.finalBoss)result.bossRemaining=Math.Max(0,enemy.hp)/enemy.maxHp;
            }
            return result;
        }
    }
}
