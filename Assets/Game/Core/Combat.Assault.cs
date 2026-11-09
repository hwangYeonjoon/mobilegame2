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
        public bool CanActivateMachine {get{return !Challenge&&!Ended&&!Choosing&&MachineTime<=0&&MachineRecovery<=0&&Salvage>=MachineCost;}}
        public bool ActivateMachine(int design=0)
        {
            if(design==0)design=MachineDesign;
            if(!RobotAvailable(design)||!CanActivateMachine)return false;
            Salvage-=MachineCost;MachinesBuilt++;MachineTime=RobotDuration(design);MachineRecovery=MachineTime+(RobotRank(design)>0?30:IntroBattle&&MachinesBuilt==1?20:40);machineShot=0;MachinePosition=ClampWorld(Slide(Position,new V2(0,45),20),24);
            BeginRobot(design);
            for(int i=Hazards.Count-1;i>=0;i--)if(Hazards[i].source==DamageSource.EnemyAttack&&V2.Distance(Hazards[i].position,Position)<230){Hazards.RemoveAt(i);MachineIntercepts++;}
            Effects.Add(new BattleEffect{position=Position,kind=10,text=RobotAssembly.Name(MachineDesign)+" 합체!"});return true;
        }
        public V2 MachinePosition { get; private set; }
        public int MachineCost { get { return MachinesBuilt==0?40:120; } }
        public int WeaponEvolution { get; private set; }
        public string EvolutionName { get { return WeaponEvolution==0?"리벳 Lv.5 + 고밀도/장전 Lv.3":WeaponEvolution==1?"중장 진화 · "+EvolutionWeapons[weapon]:"분산 진화 · "+EvolutionWeapons[weapon]; } }
        static readonly string[] EvolutionWeapons={"리벳", "산탄", "관통", "화염", "저격", "유탄"};
        float nextWave=22,machineShot,mortarTimer,bladeTimer,mineTimer;
        int bossPattern;
        public sealed class ScrapMine { public V2 position; public float life=8,arm=.6f; public bool chained; }
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
            MachineActiveSeconds+=Math.Min(dt,MachineTime);
            MachineRecovery=Math.Max(0,MachineRecovery-dt);
            MachineTime=Math.Max(0,MachineTime-dt);
            if(MachineTime<=0)return;
            MachinePosition=Position;
            machineShot-=dt;if(machineShot>0)return;machineShot=RobotAssembly.Frame(MachineDesign)==0?1.1f:RobotAssembly.Frame(MachineDesign)==1?.6f:.8f;
            UpdateRobotAttack();
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
        bool BossPattern()
        {
            if(Challenge){Warn(Position,65,1.4f,200,0,0,DamageCause.Boss,DamageSource.Boss);return true;}
            var boss=Enemies.Find(e=>e.finalBoss&&e.hp>0);if(boss==null||!BossActionReady(boss,false))return false;
            bossPattern++;BossPatterns++;
            float delay=BossWarningDelay,last=delay;
            int pattern=bossPattern%(BossLearningTier==1?2:BossLearningTier==2?2:3);
            if(pattern==1)
            {
                Warn(Position,65,delay,180,0,0,DamageCause.Boss,DamageSource.Boss);
                if(boss.hp<boss.maxHp*.5f&&BossExtraWarnings>0)
                {
                    last=delay+.6f;
                    for(int i=0;i<BossExtraWarnings;i++)Warn(Position+V2.Angle(i*2.094f+Facing)*130,45,last,140,0,0,DamageCause.Boss,DamageSource.Boss);
                }
            }
            else if(BossLearningTier==1)Warn(Position,0,delay,180,400,30,DamageCause.Boss,DamageSource.Boss);
            else if(pattern==0&&BossLearningTier==3)
            {
                last=delay+.5f;int count=BossRingWarnings;
                // Keep a real opening while density grows from six to ten circles.
                for(int i=2;i<count+2;i++)Warn(Position+V2.Angle(Facing+i*(float)Math.PI*2/(count+2))*190,32,last,150,0,0,DamageCause.Boss,DamageSource.Boss);
                Effects.Add(new BattleEffect{position=Position,kind=10,text="포위 예고 · 빈 방향으로 이동"});
            }
            else
            {
                last=delay+BossCurve(1,1,.8f);
                Warn(Position,0,delay,180,400,28,DamageCause.Boss,DamageSource.Boss);
                Warn(Position,0,last,180,28,400,DamageCause.Boss,DamageSource.Boss);
                if(bossPattern==2)Effects.Add(new BattleEffect{position=Position,kind=10,text="십자 예고 · 첫 줄을 피한 뒤 이동"});
            }
            ReserveBossAction(boss,false,last);return true;
        }
        int announcedEvolutions;
        public bool SkillEvolved(int skill)
        {
            if(skill<0||skill>=Skills.Length||Skills[skill]<5)return false;
            int support=skill==1?7:skill==2?6:skill==3?5:skill==8?4:skill==9?11:skill==10?7:-1;
            return support>=0&&Skills[support]>=3;
        }
        static readonly int[] EvolutionSkills={1,2,3,8,9,10};
        public int EvolutionMask {get{int mask=0;foreach(int skill in EvolutionSkills)if(SkillEvolved(skill))mask|=1<<skill;return mask;}}
        public static string EvolutionTitle(int skill)
        {switch(skill){case 1:return "행성 절단륜";case 2:return "극저온 폭풍";case 3:return "삼각 드론 편대";case 8:return "융단 포격";case 9:return "귀환 절단기";case 10:return "연쇄 지뢰망";default:return "주무기 진화";}}
        public static string EvolutionRecipe(int skill,bool challenge=false)
        {return CampaignEvolutionRecipe(skill);}
        static string CampaignEvolutionRecipe(int skill)
        {switch(skill){case 1:return "톱날 5 + 자력 3 · 넓은 궤도";case 2:return "코일 5 + 수리 3 · 광역 동결";case 3:return "드론 5 + 장전 3 · 3기 편대";case 8:return "박격포 5 + 고밀도 3 · 3연 포격";case 9:return "절단기 5 + 회수 엔진 3 · 귀환 궤도";case 10:return "지뢰 5 + 자력 3 · 연결 폭발";default:return "";}}
        void ResolveEvolution()
        {
            int now=EvolutionMask,unlocked=now&~announcedEvolutions;
            foreach(int skill in EvolutionSkills)if((unlocked&(1<<skill))!=0)Effects.Add(new BattleEffect{position=Position,kind=7,text=EvolutionTitle(skill)+" 진화!"});
            announcedEvolutions=now;
            if(now!=0 && FirstEvolutionSeconds<0)FirstEvolutionSeconds=Elapsed;
            if(WeaponEvolution!=0||Skills[0]<5)return;
            if(Skills[4]>=3)WeaponEvolution=1;
            else if(Skills[5]>=3)WeaponEvolution=2;
            if(WeaponEvolution>0 && FirstEvolutionSeconds<0)FirstEvolutionSeconds=Elapsed;
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
                    int shells=SkillEvolved(8)?3:1;
                    for(int shell=0;shell<shells;shell++)
                    {var impact=center+new V2((shell-(shells-1)*.5f)*95,0);Effects.Add(new BattleEffect{position=impact,kind=SkillEvolved(8)?11:2,radius=radius});
                    foreach(var e in Enemies)if(e.hp>0&&V2.Distance(e.position,impact)<radius)Hit(e,Damage*(1+Skills[8]*.3f)/ (shells>1?1.5f:1));}
                }
            }
            if(Skills[9]>0&&bladeTimer<=0)
            {
                bladeTimer=2.5f-Skills[9]*.2f;
                for(int i=0;i<4;i++)Bullets.Add(new Bullet{id=nextId++,position=Position,velocity=V2.Angle(Facing+i*(float)Math.PI/2)*260,damage=Damage*(SkillEvolved(9)?1.1f:.8f),life=SkillEvolved(9)?2.4f:1.3f,pierce=2+Skills[9],mod=true,returning=SkillEvolved(9),visual=SkillEvolved(9)?4:0});
            }
            if(Skills[10]>0&&mineTimer<=0&&Mines.Count<12){mineTimer=3-Skills[10]*.3f;Mines.Add(new ScrapMine{position=Position});}
            for(int i=Mines.Count-1;i>=0;i--)
            {
                var mine=Mines[i];mine.arm-=dt;mine.life-=dt;
                bool trigger=mine.chained;
                if(mine.arm<=0)foreach(var e in Enemies)if(e.hp>0&&V2.Distance(e.position,mine.position)<45){trigger=true;break;}
                if(trigger)
                {
                    float radius=85+Skills[10]*6;
                    if(SkillEvolved(10))foreach(var linked in Mines)if(linked!=mine&&linked.arm<=0&&V2.Distance(linked.position,mine.position)<190)linked.chained=true;
                    foreach(var e in Enemies)if(e.hp>0&&V2.Distance(e.position,mine.position)<radius)Hit(e,Damage*(1.5f+Skills[10]*.4f));
                    Effects.Add(new BattleEffect{position=mine.position,kind=SkillEvolved(10)?13:2,radius=radius});
                }
                if(trigger||mine.life<=0)Mines.RemoveAt(i);
            }
        }
    }
}
