using System;
using System.Collections.Generic;
namespace ScrapGuard.Core
{
    // Shared by client and authoritative replay. No frame-rate-dependent random calls.
    public sealed partial class Combat
    {
        public int EnemyLimit { get { return Challenge?150:240; } }
        public float WorldLeft { get { return Challenge?0:-540; } }
        public float WorldRight { get { return Challenge?540:1080; } }
        public float WorldTop { get { return Challenge?164:-480; } }
        public float WorldBottom { get { return Challenge?816:1480; } }
        public int WaveNumber { get; private set; }
        public float WaveWarning { get; private set; }
        public V2 WaveCenter { get; private set; }
        public float EscapeAngle { get; private set; }
        public int Salvage { get; private set; }
        public int MachinesBuilt { get; private set; }
        public float MachineTime { get; private set; }
        public V2 MachinePosition { get; private set; }
        public int MachineCost { get { return MachinesBuilt==0?40:120; } }
        public int WeaponEvolution { get; private set; }
        public string EvolutionName { get { return WeaponEvolution==0?"리벳 Lv.5 + 고밀도/장전 Lv.3":WeaponEvolution==1?"중장 진화 · "+EvolutionWeapons[weapon]:"분산 진화 · "+EvolutionWeapons[weapon]; } }
        static readonly string[] EvolutionWeapons={"리벳", "산탄", "관통", "화염", "저격", "유탄"};
        float nextWave=22,machineShot,mortarTimer,bladeTimer,mineTimer;
        int bossPattern;
        public sealed class ScrapMine { public V2 position; public float life=8,arm=.6f; }
        public readonly List<ScrapMine> Mines=new List<ScrapMine>();
        V2 ClampWorld(V2 p,float margin)
        { return new V2(Math.Max(WorldLeft+margin,Math.Min(WorldRight-margin,p.x)),Math.Max(WorldTop+margin,Math.Min(WorldBottom-margin,p.y))); }
        V2 SpawnPoint(float distance)
        {
            for(int i=0;i<24;i++)
            {
                var p=ClampWorld(Position+V2.Angle((float)random.NextDouble()*(float)Math.PI*2)*distance,20);
                if(V2.Distance(p,Position)>180&&!IsBlocked(p,20))return p;
            }
            // Deterministic safe fallback around a corner; never spawn on the player.
            for(int i=0;i<16;i++)
            {
                var p=ClampWorld(Position+V2.Angle(i*(float)Math.PI/8)*distance,20);
                if(V2.Distance(p,Position)>150&&!IsBlocked(p,20))return p;
            }
            return ClampWorld(new V2(Position.x<270?WorldRight-30:WorldLeft+30,Position.y),20);
        }
        void UpdateAssault(float dt)
        {
            if(Challenge)return;
            if(WaveWarning>0)
            {
                WaveWarning-=dt;
                if(WaveWarning<=0)ReleaseWave();
            }
            else if(Elapsed>=nextWave&&!FinalBossActive)
            {
                WaveNumber++;WaveWarning=2.4f;WaveCenter=Position;
                EscapeAngle=Facing;nextWave=Elapsed+32+(Stage%3)*6;
                Effects.Add(new BattleEffect{position=Position,kind=10,text=WaveNumber%2==0?"포위 접근 · 빈 방향으로 탈출!":"고철 군집 접근 · 기계 조립 준비!"});
            }
            MachineTime=Math.Max(0,MachineTime-dt);
            if(MachineTime<=0 && Salvage>=MachineCost)
            {
                Salvage-=MachineCost;MachinesBuilt++;MachineTime=12;machineShot=0;MachinePosition=ClampWorld(Slide(Position,new V2(0,85),20),24);
                Effects.Add(new BattleEffect{position=Position,kind=10,text="고철 합체! · 이동식 분쇄 기계 가동"});
            }
            if(MachineTime<=0)return;
            V2 follow=ClampWorld(Position+new V2(0,85),24);
            MachinePosition=Slide(MachinePosition,(follow-MachinePosition).Normal*Math.Min(V2.Distance(follow,MachinePosition),100*dt),20);
            machineShot-=dt;if(machineShot>0)return;machineShot=.7f;
            float radius=FieldModule==2?225:180;
            foreach(var enemy in Enemies)if(enemy.hp>0&&V2.Distance(enemy.position,MachinePosition)<radius)
            {
                Hit(enemy,Damage*(enemy.kind>=2?1.6f:3.2f));
                if(!enemy.finalBoss&&!enemy.core)enemy.position=Slide(enemy.position,(enemy.position-MachinePosition).Normal*18,15);
            }
            if(FieldModule==3)Hp=Math.Min(MaxHp,Hp+MaxHp*.005f);
            if(FieldModule==1||FieldModule==4)
            {
                var target=Nearest(MachinePosition);
                if(target!=null)Bullets.Add(new Bullet{id=nextId++,position=MachinePosition,velocity=(target.position-MachinePosition).Normal*600,damage=Damage*2,life=1.2f,pierce=8,mod=true});
            }
            Effects.Add(new BattleEffect{position=MachinePosition,kind=4,radius=radius});
        }
        void ReleaseWave()
        {
            int count=Math.Min(EnemyLimit-Enemies.Count,18+Math.Min(42,WaveNumber*5+Definition.Region*3));
            for(int i=0;i<count;i++)
            {
                // 80-degree gap points toward the announced escape direction.
                float angle=EscapeAngle+.7f+(float)(Math.PI*2-1.4)*(i+.5f)/Math.Max(1,count);
                var p=ClampWorld(WaveCenter+V2.Angle(angle)*(WaveNumber%2==0?300:380),20);
                if(IsBlocked(p,18)||V2.Distance(p,Position)<135)continue;
                float hp=50*Definition.Difficulty;
                Enemies.Add(new Enemy{id=nextId++,position=p,hp=hp,maxHp=hp,speed=WaveNumber%2==0?50:62,role=EnemyRole.Pursuer});
            }
        }
        void BossPattern()
        {
            if(Challenge){Warn(Position,65,1.4f,200,0,0,DamageCause.Boss);return;}
            var boss=Enemies.Find(e=>e.finalBoss&&e.hp>0);if(boss==null)return;
            bossPattern++;
            if(bossPattern%3==1)
            {
                Warn(Position,65,1.5f,180,0,0,DamageCause.Boss);
                if(boss.hp<boss.maxHp*.5f)
                    for(int i=0;i<3;i++)Warn(Position+V2.Angle(i*2.094f+Facing)*130,45,2.1f,140,0,0,DamageCause.Boss);
            }
            else if(bossPattern%3==2)
            {
                // Cross sweeps are staggered, allowing movement between them.
                Warn(Position,0,1.5f,180,480,30,DamageCause.Boss);
                Warn(Position,0,2.3f,180,30,480,DamageCause.Boss);
            }
            else
            {
                for(int i=2;i<12;i++)Warn(Position+V2.Angle(Facing+i*(float)Math.PI/6)*190,35,2,150,0,0,DamageCause.Boss);
                Effects.Add(new BattleEffect{position=Position,kind=10,text="봉쇄 포격 · 열린 틈으로 이동"});
            }
        }
        void ResolveEvolution()
        {
            if(WeaponEvolution!=0||Skills[0]<5)return;
            if(Skills[4]>=3)WeaponEvolution=1;
            else if(Skills[5]>=3)WeaponEvolution=2;
            if(WeaponEvolution>0)Effects.Add(new BattleEffect{position=Position,kind=10,text=EvolutionName});
        }
        void UpdateArsenal(float dt)
        {
            ResolveEvolution();mortarTimer-=dt;bladeTimer-=dt;mineTimer-=dt;
            if(Skills[8]>0&&mortarTimer<=0)
            {
                var target=Nearest(Position);
                if(target!=null)
                {
                    mortarTimer=3.2f-Skills[8]*.3f;float radius=65+Skills[8]*10;var center=target.position;
                    Effects.Add(new BattleEffect{position=center,kind=2,radius=radius});
                    foreach(var e in Enemies)if(e.hp>0&&V2.Distance(e.position,center)<radius)Hit(e,Damage*(1+Skills[8]*.3f));
                }
            }
            if(Skills[9]>0&&bladeTimer<=0)
            {
                bladeTimer=2.5f-Skills[9]*.2f;
                for(int i=0;i<4;i++)Bullets.Add(new Bullet{id=nextId++,position=Position,velocity=V2.Angle(Facing+i*(float)Math.PI/2)*260,damage=Damage*.8f,life=1.3f,pierce=2+Skills[9],mod=true});
            }
            if(Skills[10]>0&&mineTimer<=0&&Mines.Count<12){mineTimer=3-Skills[10]*.3f;Mines.Add(new ScrapMine{position=Position});}
            for(int i=Mines.Count-1;i>=0;i--)
            {
                var mine=Mines[i];mine.arm-=dt;mine.life-=dt;
                bool trigger=false;
                if(mine.arm<=0)foreach(var e in Enemies)if(e.hp>0&&V2.Distance(e.position,mine.position)<45){trigger=true;break;}
                if(trigger)
                {
                    float radius=85+Skills[10]*6;
                    foreach(var e in Enemies)if(e.hp>0&&V2.Distance(e.position,mine.position)<radius)Hit(e,Damage*(1.5f+Skills[10]*.4f));
                    Effects.Add(new BattleEffect{position=mine.position,kind=2,radius=radius});
                }
                if(trigger||mine.life<=0)Mines.RemoveAt(i);
            }
        }
    }
}
