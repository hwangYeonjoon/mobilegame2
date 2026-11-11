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
            if(!Challenge&&!BossArenaActive&&(Definition.Region==7||Definition.Region==8))
                for(int i=0;i<Zones.Length;i++)if(Zones[i].Contains(Position)){if(Definition.Region==7)drift.x=i%2==0?45:-45;else drift.y=i%2==0?-40:40;}
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
            if(Definition.Region>=7){UpdateExpansionTerrain();return;}
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
            if(!Challenge&&source==DamageSource.Boss)
            {
                int count=Hazards.FindAll(h=>h.source==DamageSource.Boss).Count;
                if(count>=BossWarningLimit)return false;
                PeakBossWarnings=Math.Max(PeakBossWarnings,count+1);
            }
            if(!Challenge&&source==DamageSource.EnemyAttack)
            {
                int count=0;foreach(var h in Hazards)if(h.source==DamageSource.EnemyAttack)count++;
                if(enemyWarningRecovery>0||count>=(SpawnElapsed<25?2:3))return false;
                enemyWarningRecovery=Definition.Region==2?1.15f:Definition.Region==4?1f:.8f;PeakEnemyWarnings=Math.Max(PeakEnemyWarnings,count+1);
                delay=Math.Max(delay,Definition.Region==2?2.1f:Definition.Region==4?1.9f:1.6f);
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
            if(RegionalHold(e,dt,distance,direction))return;
            if(e.role==EnemyRole.Charger)
            {
                if(e.windup>0)
                {
                    e.windup-=dt;if(e.windup<=0){e.dash=Regional(e,1)?.8f:Regional(e,7)?.45f:.65f;e.attackFlash=e.dash;}return;
                }
                if(e.dash>0){e.dash-=dt;direction=e.direction;speed=e.finalBoss?BossChargeSpeed:Regional(e,7)?285:240;}
                else if(e.cooldown<=0 && distance<330 && BossActionReady(e,true)){e.direction=RegionalChargeDirection(e,direction);e.windup=e.finalBoss?BossChargeWindup:Regional(e,1)||Regional(e,7)?1.15f:.8f;e.cooldown=e.finalBoss?BossChargeRecovery:Regional(e,7)?5.5f:4.5f;
                    if(Regional(e,1)||Regional(e,7))RegionalActions++;
                    ReserveBossAction(e,true,e.windup+.65f);
                    if(e.finalBoss && Definition.Region==6)Warn(Position,75,1.8f,220,0,0,DamageCause.Heat,DamageSource.Boss);
                    return;}
            }
            else if(e.role==EnemyRole.Artillery)
            {
                if(distance<210)speed=distance<120?-e.speed*.5f:0;
                if(e.cooldown<=0&&BossActionReady(e,true))
                {
                    e.cooldown=e.finalBoss?BossPatternInterval:4.8f;e.attackFlash=.4f;
                    float delay=e.finalBoss?BossWarningDelay:1.25f,last=delay;
                    if(e.finalBoss && Definition.Region==5)
                    {
                        Warn(Position,0,delay,185,400,28,DamageCause.Electricity,DamageSource.Boss);
                        if(e.hp<e.maxHp*.5f){last=delay+.8f;Warn(Position,0,last,185,28,400,DamageCause.Electricity,DamageSource.Boss);}
                    }
                    else if(!RegionalArtillery(e))Warn(Position,e.finalBoss?72:43,delay,e.finalBoss?190:100,0,0,DamageCause.Artillery,e.finalBoss?DamageSource.Boss:DamageSource.EnemyAttack);
                    if(e.finalBoss && Definition.Region>1 && e.hp<e.maxHp*.5f && BossExtraWarnings>0)
                    { last=delay+.6f;Warn(Position+new V2(100,0),50,last,130,0,0,DamageCause.Artillery,DamageSource.Boss);if(BossExtraWarnings>=2)Warn(Position-new V2(100,0),50,last,130,0,0,DamageCause.Artillery,DamageSource.Boss); }
                    ReserveBossAction(e,true,last);
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
                if(distance<(e.finalBoss?150:85) && e.cooldown<=0 && BossActionReady(e,true))
                {
                    float delay=e.finalBoss?BossWarningDelay:Regional(e,2)||Regional(e,6)?2:1.65f;
                    if(!e.finalBoss&&!Challenge)delay=Math.Max(delay,Definition.Region==2?2.1f:Definition.Region==4?1.9f:1.6f);
                    float radius=e.finalBoss?120:Regional(e,2)?70:Regional(e,6)?96:82;
                    var cause=Regional(e,2)?DamageCause.Poison:Regional(e,6)?DamageCause.Heat:DamageCause.Explosion;
                    if(Warn(e.position,radius,delay,e.finalBoss?230:Regional(e,6)?140:150,0,0,cause,e.finalBoss?DamageSource.Boss:DamageSource.EnemyAttack))
                    {if(Regional(e,2)||Regional(e,6))RegionalActions++;e.cooldown=5;e.windup=delay;ReserveBossAction(e,true,e.windup);}
                    else e.cooldown=.3f;
                    return;
                }
                speed*=1.15f;
            }
            else if(e.role==EnemyRole.Shield)speed*=.7f;
            else if(e.role==EnemyRole.Summoner && e.cooldown<=0 && BossActionReady(e,true))
            {
                e.cooldown=e.finalBoss?5:Regional(e,4)?11:9;e.attackFlash=.5f;ReserveBossAction(e,true,.5f);
                if(Enemies.Count<EnemyLimit-5&&(!Regional(e,4)||Enemies.FindAll(x=>x.hp>0&&x.summoned).Count<8))
                {
                    var p=e.position+new V2(0,55);
                    if(!IsBlocked(p))
                    {
                        var child=Regional(e,4)?EnemyRole.Shield:Regional(e,9)?EnemyRole.Charger:EnemyRole.Pursuer;
                        float hp=(child==EnemyRole.Shield?50:90)*Definition.Difficulty;
                        Enemies.Add(new Enemy {id=nextId++,kind=1,role=child,summoned=true,position=p,hp=hp,maxHp=hp,speed=child==EnemyRole.Shield?48:68,cooldown=child==EnemyRole.Charger?2:0});
                        if(Regional(e,4)||Regional(e,9))RegionalActions++;
                    }
                }
            }
            if(Regional(e,7)&&e.role==EnemyRole.Charger&&e.dash<=0&&distance>100)direction=(direction+new V2(-direction.y,direction.x)*.35f).Normal;
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
