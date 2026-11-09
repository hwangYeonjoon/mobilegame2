using System;

namespace ScrapGuard.Core
{
    public sealed partial class Combat
    {
        public readonly StageDefinition Definition;
        public readonly MapRect[] Blocks, Zones;
        V2 velocity;
        float terrainTimer=5, poisonTimer;
        int terrainPulse;
        public bool IsBlocked(V2 position, float radius=14)
        {
            if(BossArenaActive)return false;
            foreach(var block in Blocks)if(block.Contains(position,radius))return true;
            return false;
        }
        V2 Slide(V2 from,V2 delta,float radius)
        {
            var next=from+new V2(delta.x,0);if(!IsBlocked(next,radius))from=next;
            next=from+new V2(0,delta.y);if(!IsBlocked(next,radius))from=next;
            return from;
        }
        void MovePlayer(float dt,V2 move)
        {
            V2 target=(move.Length>1?move.Normal:move)*baseSpeed*MachineSpeed*(1+Skills[11]*.035f);
            bool ice=false;if(!BossArenaActive)foreach(var zone in Zones)if(Definition.Region==3 && zone.Contains(Position))ice=true;
            velocity=ice?velocity+(target-velocity)*Math.Min(1,dt*3):target;
            V2 drift=new V2();
            if(!Challenge && !BossArenaActive && Definition.Region==4)for(int i=0;i<Zones.Length;i++)if(Zones[i].Contains(Position))drift.y=i%2==0?-55:55;
            Position=Slide(Position,(velocity+drift)*dt,14);
        }
        void UpdateTerrain(float dt)
        {
            if(Challenge||BossArenaActive)return;
            poisonTimer-=dt;
            if(Definition.Region==2 && poisonTimer<=0)
            {
                foreach(var zone in Zones)if(zone.Contains(Position)){Hurt(65+Definition.Region*15,DamageCause.Poison,DamageSource.Terrain);poisonTimer=.8f;break;}
            }
            terrainTimer-=dt;
            if(terrainTimer>0 || (Definition.Region!=1 && Definition.Region!=4 && Definition.Region!=5 && Definition.Region!=6))return;
            if(Definition.Region>=5)
            {
                terrainTimer=Definition.Region==5?5.5f:6.5f;
                var zone=Zones[terrainPulse++%Zones.Length];
                Warn(zone.Center,0,Definition.Region==5?1.6f:2f,Definition.Region==5?190:230,zone.W,zone.H,Definition.Region==5?DamageCause.Electricity:DamageCause.Heat,DamageSource.Terrain);
                return;
            }
            terrainTimer=Definition.Region==1?7:9;
            if(Definition.Region==1)
            {
                var zone=Zones[terrainPulse++%Zones.Length];
                Warn(zone.Center,0,1.8f,170,zone.W,zone.H,DamageCause.Rail,DamageSource.Terrain);
            }
            else
            {
                float y=310+(terrainPulse++%3)*160;
                Warn(new V2(270,y),0,1.6f,200,480,32,DamageCause.Laser,DamageSource.Terrain);
            }
        }
        EnemyRole BossRole()
        {
            if(Challenge)return EnemyRole.Pursuer;
            return Campaign.BossRole(Definition.Region);
        }
        bool Warn(V2 position,float radius,float delay,float damage,float width=0,float height=0,DamageCause cause=DamageCause.Artillery,DamageSource source=DamageSource.EnemyAttack)
        {
            if(Hazards.Count>=48)return false;
            if(!Challenge&&source==DamageSource.EnemyAttack)
            {
                int count=0;foreach(var h in Hazards)if(h.source==DamageSource.EnemyAttack)count++;
                if(enemyWarningRecovery>0||count>=(Elapsed<25?2:3))return false;
                enemyWarningRecovery=.8f;PeakEnemyWarnings=Math.Max(PeakEnemyWarnings,count+1);
                delay=Math.Max(delay,1.6f);
            }
            Hazards.Add(new Hazard {id=nextId++,position=position,radius=radius,delay=delay,damage=damage,width=width,height=height,cause=cause,source=source});return true;
        }
        void UpdateEnemyRole(Enemy e,float dt)
        {
            if(e.core)return;
            V2 goal=TaskActive && Task==FieldTask.Defense && !e.finalBoss?TaskPosition:Position;
            V2 direction=(goal-e.position).Normal;float distance=V2.Distance(goal,e.position);
            e.slowTime=Math.Max(0,e.slowTime-dt);e.freezeTime=Math.Max(0,e.freezeTime-dt);
            if(e.freezeTime>0)return;
            e.cooldown-=dt;float speed=e.speed;
            if(e.role==EnemyRole.Charger)
            {
                if(e.windup>0)
                {
                    e.windup-=dt;if(e.windup<=0){e.dash=.65f;e.attackFlash=.65f;}return;
                }
                if(e.dash>0){e.dash-=dt;direction=e.direction;speed=e.finalBoss?(IntroBattle?145:190):240;}
                else if(e.cooldown<=0 && distance<330){e.direction=direction;e.windup=e.finalBoss&&IntroBattle?1.3f:.8f;e.cooldown=e.finalBoss&&IntroBattle?6f:4.5f;
                    if(e.finalBoss && Definition.Region==6)Warn(Position,75,1.8f,220,0,0,DamageCause.Heat,DamageSource.Boss);
                    return;}
            }
            else if(e.role==EnemyRole.Artillery)
            {
                if(distance<210)speed=distance<120?-e.speed*.5f:0;
                if(e.cooldown<=0)
                {
                    e.cooldown=e.finalBoss?(Definition.Region==1?5f:3f):4.8f;e.attackFlash=.4f;
                    if(e.finalBoss && Definition.Region==5)
                    {
                        Warn(Position,0,1.5f,185,400,28,DamageCause.Electricity,DamageSource.Boss);
                        if(e.hp<e.maxHp*.5f)Warn(Position,0,1.9f,185,28,400,DamageCause.Electricity,DamageSource.Boss);
                    }
                    else Warn(Position,e.finalBoss?72:43,1.25f,e.finalBoss?190:100,0,0,DamageCause.Artillery,e.finalBoss?DamageSource.Boss:DamageSource.EnemyAttack);
                    if(e.finalBoss && Definition.Region>1 && e.hp<e.maxHp*.5f)
                    { Warn(Position+new V2(100,0),50,1.7f,130,0,0,DamageCause.Artillery,DamageSource.Boss);Warn(Position-new V2(100,0),50,1.7f,130,0,0,DamageCause.Artillery,DamageSource.Boss); }
                }
            }
            else if(e.role==EnemyRole.Bomber)
            {
                if(e.windup>0)
                {
                    e.windup-=dt;
                    if(e.windup<=0){e.attackFlash=.4f;if(!e.finalBoss)e.hp=0;}
                    return;
                }
                if(distance<(e.finalBoss?150:85) && e.cooldown<=0)
                {
                    if(Warn(e.position,e.finalBoss?120:82,1.05f,e.finalBoss?230:150,0,0,DamageCause.Explosion,e.finalBoss?DamageSource.Boss:DamageSource.EnemyAttack))
                    {e.cooldown=5;e.windup=e.finalBoss?1.1f:1.65f;}
                    else e.cooldown=.3f;
                    return;
                }
                speed*=1.15f;
            }
            else if(e.role==EnemyRole.Shield)speed*=.7f;
            else if(e.role==EnemyRole.Summoner && e.cooldown<=0)
            {
                e.cooldown=e.finalBoss?5:9;e.attackFlash=.5f;
                if(Enemies.Count<EnemyLimit-5)
                {
                    var p=e.position+new V2(0,55);
                    if(!IsBlocked(p))Enemies.Add(new Enemy {id=nextId++,kind=1,role=EnemyRole.Pursuer,position=p,hp=90*Definition.Difficulty,maxHp=90*Definition.Difficulty,speed=68});
                }
            }
            if(e.slowTime>0)speed*=e.finalBoss?.85f:.7f;
            var before=e.position;
            e.position=Slide(e.position,direction*(speed*dt),Math.Min(e.Radius,20));
            if(!BossArenaActive && speed>0 && V2.Distance(before,e.position)<speed*dt*.2f)
            {
                foreach(var block in Blocks)if(block.Contains(before+direction*24,Math.Min(e.Radius,20)))
                {
                    var detour=Math.Abs(direction.x)>Math.Abs(direction.y)?new V2(0,before.y<block.Center.y?-1:1):new V2(before.x<block.Center.x?-1:1,0);
                    e.position=Slide(before,detour*(speed*dt),Math.Min(e.Radius,20));break;
                }
            }
            e.position=ClampWorld(e.position,15);
        }
    }
}
