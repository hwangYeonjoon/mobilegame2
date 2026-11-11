using System;
using System.Collections.Generic;

namespace ScrapGuard.Core
{
    public struct V2
    {
        public float x, y;
        // Round once on the canonical grid, after double intermediate arithmetic.
        // A float intermediate can land on opposite sides of a grid boundary across runtimes.
        public V2(float x, float y) : this((double)x,(double)y) { }
        V2(double x,double y) { this.x=(float)(Math.Round(x*4096d)/4096d);this.y=(float)(Math.Round(y*4096d)/4096d); }
        public float Length { get { return (float)Math.Sqrt((double)x * x + (double)y * y); } }
        public V2 Normal { get { float d = Length; return d > .001f ? this / d : new V2(); } }
        public static V2 operator +(V2 a, V2 b) { return new V2((double)a.x + b.x, (double)a.y + b.y); }
        public static V2 operator -(V2 a, V2 b) { return new V2((double)a.x - b.x, (double)a.y - b.y); }
        public static V2 operator *(V2 a, float b) { return a*(double)b; }
        public static V2 operator *(V2 a, double b) { return new V2(a.x*b,a.y*b); }
        public static V2 operator /(V2 a, float b) { return new V2((double)a.x / b, (double)a.y / b); }
        public static float Distance(V2 a, V2 b) { return (a - b).Length; }
        public static V2 Angle(double a) { return new V2(Math.Cos(a), Math.Sin(a)); }
    }
    public class Enemy
    {
        internal int spatialIndex;
        public int id, kind, chillStacks;
        public V2 position;
        public float hp, maxHp, speed, cooldown, flash;
        public bool finalBoss, core, killedByMachine, summoned; public int robotDesign;
        public EnemyRole role; public int salvagePart=-1;
        public V2 direction;
        public float windup, dash, attackFlash, slowTime, freezeTime;
        public float Radius { get { return kind == 3 ? 35 : kind == 2 ? 23 : 15; } }
    }
    public class Bullet
    {
        public int visual; public bool returning, machine; public float age;
        public int id;
        public V2 position, velocity;
        public float damage, life;
        public int pierce, lastHit = -1;
        public bool blast, drone, mod, chilling; public int robotDesign;
    }
    public class Pickup { public int id, xp, part=-1, partCount=1; public V2 position; public bool heal; }
    public class Hazard { public int id; public V2 position; public float delay, radius, width, height, damage; public DamageCause cause; public DamageSource source; }
    public class BattleEffect { public V2 position, end; public string text; public int kind; public float radius; public bool robot; }
    public sealed partial class Combat
    {
        public const int RulesVersion = 28;
        public static readonly string[] UltimateNames = Array.ConvertAll(HeroCatalog.All,h=>h.Ultimate);
        public int Hero { get { return hero; } }
        public float UltimateCharge { get; private set; } = 50;
        public float UltimateCooldown { get; private set; }
        public float ShotFlash { get; private set; }
        public float HurtFlash { get; private set; }
        public float UltimateFlash { get; private set; }
        public int UltimateUses { get; private set; }
        public float DistanceMoved { get; private set; }
        public V2 LastMovement { get; private set; }
        public V2 UltimateOrigin { get; private set; }
        public V2 UltimateDirection { get; private set; }
        float ultimateGuard;
        public bool CanUltimate { get { return !Ended && !Choosing && UltimateCharge >= 100 && UltimateCooldown <= 0; } }

        public readonly string Mode;
        public readonly SupportSnapshot Support;
        public bool Challenge { get { return Endgame.IsChallenge(Mode); } }
        public int CoreScore { get; private set; }
        public bool SupportUsed { get; private set; }
        public float SupportTime { get; private set; }
        float supportShot;
        public bool CanSupport { get { return Support!=null && !SupportUsed && !Ended && !Choosing && Elapsed>=8 && !Challenge; } }
        readonly Random random;
        readonly int hero, weapon, module, armor, moduleGrowth;
        readonly float baseDamage, baseSpeed;
        float spawnTimer = .3f, shotTimer, sawTimer, coilTimer, droneTimer, contactTimer, shieldTimer, bossTimer;
        bool eliteSpawned, bossSpawned;
        int nextId;
        public readonly string RunId;
        public readonly int Stage;
        public readonly float Duration;
        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<Bullet> Bullets = new List<Bullet>();
        public readonly List<Pickup> Pickups = new List<Pickup>();
        public readonly List<Hazard> Hazards = new List<Hazard>();
        public readonly List<BattleEffect> Effects = new List<BattleEffect>();
        public readonly int[] Skills = new int[12];
        public V2 Position = new V2(270, 510);
        public float Hp, MaxHp, Shield, Elapsed, Facing;
        public int Kills, Level = 1, Xp;
        public bool Choosing, Ended, Won;
        public int[] Choices = new int[3];
        public int NeededXp { get { return Challenge?6+Level*4:(int)((8+Level*6+Math.Max(0,Level-12)*6)*Math.Max(1,Duration/180f)); } }
        public bool FinalBossActive { get { return bossSpawned; } }
        public float Damage { get { return baseDamage * (1 + Skills[4] * .20f) * (RiskActive?1.2f:1); } }
        public Combat(GameState state, int stage, string runId, int seed = 123, bool quick = false, string mode = "", SupportSnapshot support = null)
        {
            Mode=mode??"";
            if(Mode!="" && !Endgame.IsChallenge(Mode)&&!Practice&&!ApexBattle)throw new ArgumentException("Unknown combat mode");
            if(support!=null && (Challenge || Practice || ApexBattle || support.hero<0 || support.hero>=HeroCatalog.Count || support.power<1 || support.power>350))throw new ArgumentException("Invalid support snapshot");
            Support=support;
            GuildAtStart=state.activeGuild;
            Definition=Campaign.Get(stage);
            if(ApexBattle&&Array.IndexOf(Apex.Stages,stage)<0)throw new ArgumentException("Unknown apex operation");
            Condition=OperationConditions.Select(Definition.Region,seed,Challenge);
            FieldVariant=Definition.Region==0?0:(int)((uint)seed%3);
            Blocks=Challenge?new MapRect[0]:Campaign.WorldGeometry(Campaign.Blocks(Definition));
            Zones=Challenge?new MapRect[0]:Campaign.WorldGeometry(Campaign.Zones(Definition));
            if(Mode==SkillTrial.Mode)state=SkillTrial.Loadout();
            RunId = runId; Stage = stage; random = new Random(seed); hero = state.selectedHero;
            WeeklyFrame=RobotWeek.Frame(seed);
            IntroBattle=!Challenge&&!Practice&&stage==0&&!state.tutorialCompleted;
            weapon = state.items.Find(i => i.id == state.equipped[0]).definition;
            armor = state.items.Find(i => i.id == state.equipped[1]).definition;
            module = state.items.Find(i => i.id == state.equipped[2]).definition;
            moduleGrowth=Mode==SkillTrial.Mode?0:state.items.Find(i=>i.id==state.equipped[2]).level-1;
            baseDamage = Catalog.Attack(state,Mode==SkillTrial.Mode); MaxHp = Hp = Catalog.Health(state,Mode==SkillTrial.Mode);MinimumHp=Hp;
            baseSpeed = HeroCatalog.All[hero].Speed * (armor == 8 ? 1.08f : 1);
            Duration = SurvivalPractice ? 60 : BossPractice ? 90 : Challenge ? Endgame.Duration : quick ? 45 : !state.tutorialCompleted && stage == 0 ? 180 : Definition.Duration;
            Skills[0] = 1;
            if(BossPractice)InitializeBossPractice();
            if(SurvivalPractice)InitializeSurvivalPractice();
            if(hero==3)Skills[2]=1;
            if(Challenge)
            {
                if(RobotEnabled){Salvage=40;for(int i=0;i<3;i++)robotParts[i]=4;}
                Position=new V2(270,600);bossSpawned=true;bossTimer=3;
                Enemies.Add(new Enemy {id=nextId++,kind=3,core=true,position=new V2(270,330),hp=10000000,maxHp=10000000,speed=0});
            }
        }
        public void Tick(float dt, V2 movement)
        {
            Effects.Clear();
            if (Ended || Choosing) return;
            dt = Math.Max(0, Math.Min(.05f, dt)); Elapsed += dt;
            if(Hp<MaxHp*.5f)LowHealthSeconds+=dt;
            areaHitRecovery=Math.Max(0,areaHitRecovery-dt);bossHitRecovery=Math.Max(0,bossHitRecovery-dt);bossActionRecovery=Math.Max(0,bossActionRecovery-dt);enemyWarningRecovery=Math.Max(0,enemyWarningRecovery-dt);
            if(Practice&&Elapsed>=Duration){Ended=true;Won=SurvivalPractice&&Hp>0;endCause=Won?DamageCause.None:DamageCause.Timeout;return;}
            if(Challenge && Elapsed>=Duration) { Elapsed=Duration;Ended=true;Won=Hp>0;return; }
            LastMovement = movement;
            UltimateCharge = Math.Min(100,UltimateCharge + dt*2);
            UltimateCooldown = Math.Max(0,UltimateCooldown-dt);
            UltimateFlash = Math.Max(0,UltimateFlash-dt);
            ShotFlash = Math.Max(0,ShotFlash-dt); HurtFlash = Math.Max(0,HurtFlash-dt);
            if (ultimateGuard > 0 && ultimateGuard <= dt) Shield = 0;
            ultimateGuard = Math.Max(0,ultimateGuard-dt);
            var beforeMove=Position;
            MovePlayer(dt,movement);
            Position = ClampBossPoint(ClampWorld(Position,26),26);
            if(movement.Length>0)DistanceMoved+=V2.Distance(beforeMove,Position);
            if (movement.Length > .1f) Facing = (float)Math.Atan2(movement.y, movement.x);
            contactTimer -= dt; reviveGuard=Math.Max(0,reviveGuard-dt);
            if (hero == 2 && ultimateGuard <= 0)
            {
                shieldTimer += dt;
                if (shieldTimer >= 20) { Shield = MaxHp * .08f; shieldTimer = 0; }
                if (shieldTimer > 5) Shield = 0;
            }
            spawnTimer -= dt;
            if (spawnTimer <= 0 && !BossArenaActive)
            {
                spawnTimer = Math.Max(.25f, (Challenge?.85f:Definition.SpawnInterval) - SpawnProgress * (Challenge?.58f:.48f));
                if(!Challenge && Definition.Region==0)spawnTimer*=(int)(Elapsed/22)%2==0?1.2f:.72f;
                if(RiskActive)spawnTimer*=.75f;
                int group=Challenge?1:Elapsed<25?2:Math.Min(6,2+(int)(SpawnElapsed/45)+Definition.Region/2);
                for(int n=0;n<group && Enemies.Count<EnemyLimit;n++) Spawn(random.NextDouble() < (Challenge?.36:.18 + Definition.Region * .035) ? 1 : 0, false);
            }
            if (!eliteSpawned && Elapsed > Duration * .46f) { eliteSpawned = true; Spawn(2, false); }
            UpdateBossArena();
            if (!bossSpawned && (Challenge || Definition.Mission==Mission.Eliminate) && Elapsed >= Duration - 30) { bossSpawned = true;bossTimer=1.5f; Spawn(3, true); }
            if (Elapsed > Duration + 60) { Ended = true; Won = false; endCause=DamageCause.Timeout; return; }
            if (bossSpawned)
            {
                bossTimer -= dt;
                if (bossTimer <= 0)
                {
                    bossTimer = BossPattern()?(Challenge?3.2f:BossPatternInterval):.15f;
                }
            }
            if(!Challenge && Definition.Mission==Mission.Survive && Elapsed>=Duration) { Ended=true;Won=Hp>0;return; }
            if(BossArenaActive)BossCombatSeconds+=dt;
            UpdateAssault(dt);
            FirstRegionPacing(dt);
            UpdateFieldTask(dt);
            UpdateOperationCondition(dt);
            UpdateTerrain(dt);
            UpdateHazards(dt);
            if (Ended) return;
            UpdateEnemies(dt);
            SeparateEnemies(dt);
            if (Ended) return;
            UpdateMods(dt);
            UpdateEngineering(dt);
            UpdateBuild(dt);
            UpdateIdentity(dt);
            UpdateWeapons(dt);
            UpdateArsenal(dt);
            UpdateSupport(dt);
            UpdateBullets(dt);
            Collect(dt);
            if(BossArenaActive)foreach(var enemy in Enemies)enemy.position=ClampBossPoint(enemy.position,enemy.Radius);
        }
        void Spawn(int kind, bool final)
        {
            V2 p=final&&BossArenaActive?BossArenaCenter+new V2(0,-190):SpawnPoint(kind>=2?360:330);
            float difficulty = (Challenge?2.12f:Definition.Difficulty) + SpawnProgress * .55f;
            float hp = (kind == 3 ? (Challenge?7000:BossBaseHealth) : kind == 2 ? 1800 : kind == 1 ? 110 : Challenge?150:65) * difficulty;
            var role=Challenge?(RobotEnabled&&kind==1?(WeeklyFrame==0?EnemyRole.Pursuer:WeeklyFrame==1?EnemyRole.Charger:EnemyRole.Artillery):EnemyRole.Pursuer):Definition.Enemies[random.Next(Definition.Enemies.Length)];
            if(Stage==0 && Elapsed<25)role=EnemyRole.Pursuer;
            if(!Challenge && Condition==1 && kind==1)role=EnemyRole.Charger;
            if(!Challenge&&!final)role=OpeningEnemyRole(role);
            if(final)role=BossRole();
            int carrier=kind<2?NextCarrierPart():-1;
            if(IntroBattle&&Elapsed<=60&&carrier>=0)p=SpawnPoint(210);
            Enemies.Add(new Enemy { id = nextId++, kind = kind, role=role, salvagePart=carrier, cooldown=Challenge?0:2+(float)random.NextDouble()*2, finalBoss = final, position = p, hp = hp, maxHp = hp,
                speed = kind == 3 ? 31 : kind == 2 ? 42 : kind == 1 ? 74 : 41 });
            if (kind >= 2) Effects.Add(new BattleEffect { position = Position+new V2(0,-140), text = kind == 3 ? Definition.Boss+" 출현!" : "강화 로봇 접근", kind = 3 });
        }
        void UpdateHazards(float dt)
        {
            for (int i = Hazards.Count - 1; i >= 0; i--)
            {
                var h = Hazards[i]; h.delay -= dt;
                if (h.delay > 0) continue;
                Effects.Add(new BattleEffect { position = h.position, kind = 2, radius = h.radius });
                bool hit=h.width>0 ? Math.Abs(Position.x-h.position.x)<h.width/2+12 && Math.Abs(Position.y-h.position.y)<h.height/2+12 : V2.Distance(Position, h.position)<h.radius+12;
                if (hit) { HazardHits++;float before=DamageReceived;Hurt(h.damage>0?h.damage:200 + (Challenge?4:Definition.Region) * 40,h.cause==DamageCause.None?DamageCause.Boss:h.cause,h.source);HazardDamageReceived+=DamageReceived-before; }
                Hazards.RemoveAt(i);
            }
        }
        void UpdateEnemies(float dt)
        {
            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                var e = Enemies[i];
                if (e.hp <= 0) { Kill(i); continue; }
                e.flash = Math.Max(0, e.flash - dt);e.attackFlash=Math.Max(0,e.attackFlash-dt);
                UpdateEnemyRole(e,dt);
                if(e.hp<=0) { Kill(i);continue; }
                if (V2.Distance(Position, e.position) < 15 + e.Radius && contactTimer <= 0)
                {
                    Hurt(e.kind == 3 ? 170 : e.kind == 2 ? 110 : 70,e.dash>0?DamageCause.Charge:DamageCause.Contact,e.finalBoss?DamageSource.Boss:DamageSource.Contact);e.attackFlash=.25f; contactTimer = e.finalBoss&&!Challenge?BossContactInterval:.65f;
                    if(!e.core)e.position=Slide(e.position,(e.position - Position).Normal * 22,Math.Min(e.Radius,20));
                }
            }
        }
        void Hurt(float damage,DamageCause cause=DamageCause.Contact,DamageSource source=DamageSource.Unspecified)
        {
            if (Ended || UltimateFlash > .15f || reviveGuard>0) return;
            HurtFlash = .22f;
            source=ResolveDamageSource(source,cause);
            if(!Challenge)
            {
                bool area=source==DamageSource.EnemyAttack||source==DamageSource.Terrain||source==DamageSource.Condition;
                if(source==DamageSource.Boss){if(bossHitRecovery>0)return;bossHitRecovery=BossHitRecovery;damage*=BossDamageScale;}
                if(area&&areaHitRecovery>0)return;
                if(area)areaHitRecovery=.55f;
                if(source==DamageSource.Terrain)damage*=.35f;
                else if(source==DamageSource.EnemyAttack)damage*=.8f*(1+Stage*.022f)*(Definition.Region==2?.85f:Definition.Region==4?.9f:1);
                else if(source==DamageSource.Contact)damage*=1+Stage*.022f;
                if(Definition.Region>0&&(source==DamageSource.Terrain||source==DamageSource.EnemyAttack||source==DamageSource.Condition))damage*=RegionEntryThreat;
                if(!SurvivalPractice&&(area||source==DamageSource.Contact)&&Elapsed<20)damage*=.25f+.75f*Math.Max(0,(Elapsed-12)/8);
            }
            if(RobotDamageReduction>0)damage*=1-RobotDamageReduction*(source==DamageSource.Boss?.5f:1);
            if(RiskActive)damage*=1.2f;
            if(PoweredSafety)damage*=.7f;
            if (armor == 7) damage *= .9f;
            float absorbed = Math.Min(Shield, damage); Shield -= absorbed;
            IdentityShieldHit(absorbed);
            float actual=Math.Min(Hp,Math.Max(0,damage-absorbed));
            if((int)cause>0 && (int)cause<damageTaken.Length)damageTaken[(int)cause]+=actual;
            if(actual>0){lastCause=(int)cause;lastSource=(int)source;lastHitTime=Elapsed;lastHitDamage=actual;RecordRecentHit(actual,cause,source);}
            DamageReceived+=actual;damageBySource[(int)source]+=actual;
            if(source==DamageSource.Boss){if(cause==DamageCause.Contact||cause==DamageCause.Charge)BossContactDamage+=actual;else BossWarningDamage+=actual;}
            Hp -= damage - absorbed;MinimumHp=Math.Min(MinimumHp,Math.Max(0,Hp));
            Effects.Add(new BattleEffect { position = Position, text = "-" + (int)(damage - absorbed), kind = 1 });
            if (Hp <= 0) { Hp = 0; Ended = true; Won = false; endCause=cause; }
        }
        Enemy Nearest(V2 origin)
        {
            Enemy target = null; float distance = float.MaxValue;
            foreach (var e in Enemies)
            {
                if (e.hp <= 0) continue;
                var offset=origin-e.position; float d = (float)((double)offset.x*offset.x+(double)offset.y*offset.y);
                if (d < distance) { distance = d; target = e; }
            }
            return target;
        }
        void UpdateWeapons(float dt)
        {
            shotTimer -= dt; sawTimer -= dt; coilTimer -= dt; droneTimer -= dt;
            var target = Nearest(Position);
            if (shotTimer <= 0 && target != null)
            {
                float speed = weapon == 3 ? .17f : weapon == 4 ? .8f : weapon == 5 ? 1.2f : .5f;
                shotTimer = speed * Math.Max(.35f, 1 - Skills[5] * .12f);
                double angle = Math.Atan2(target.position.y - Position.y, target.position.x - Position.x);
                Facing = (float)angle; ShotFlash = .11f;
                int count = 1 + Skills[0] / 2 + (weapon == 1 ? 2 : 0)+(WeaponEvolution==2?2:0);
                for (int i = 0; i < count; i++) Shoot(Position, angle + (i - (count - 1) * .5) * .16, false);
                IdentityVolley();
            }
            if (Skills[1] > 0 && sawTimer <= 0)
            {
                sawTimer = SkillEvolved(1)?.18f:.24f;
                for (int n = 0; n < 1 + Skills[1]; n++)
                {
                    var p = SawPosition(n);
                    foreach (var e in Enemies) if (e.hp > 0 && V2.Distance(p, e.position) < e.Radius + (SkillEvolved(1)?28:18)) Hit(e, Damage * (SkillEvolved(1)?.7f:.45f));
                }
            }
            if (Skills[2] > 0 && coilTimer <= 0)
            {
                coilTimer = (2.7f - Skills[2] * .18f)*(hero==3?.8f:1);
                float radius = 90 + Skills[2] * 13+(HasMod(5)?40:0)+(SkillEvolved(2)?65:0);
                if(HasMod(5))foreach(var enemy in Enemies)if(!enemy.core && V2.Distance(enemy.position,Position)<radius)enemy.slowTime=Math.Max(enemy.slowTime,1.8f);
                if(SkillEvolved(2))
                { foreach(var e in Enemies)if(e.hp>0&&V2.Distance(Position,e.position)<radius){Hit(e,Damage*1.1f);if(!e.core&&!e.finalBoss)e.freezeTime=Math.Max(e.freezeTime,.6f);} Effects.Add(new BattleEffect{position=Position,kind=12,radius=radius}); }
                if(HasMod(1))ChainLightning();
                else
                {
                    Effects.Add(new BattleEffect { position = Position, kind = 4, radius = radius });
                    foreach (var e in Enemies) if (e.hp > 0 && V2.Distance(e.position, Position) < radius) Hit(e, Damage * (1 + Skills[2] * .2f));
                }
            }
            if (Skills[3] > 0 && droneTimer <= 0 && target != null)
            {
                droneTimer = 1.1f - Skills[3] * .1f;
                var p = DronePosition;
                Shoot(p, (float)Math.Atan2(target.position.y - p.y, target.position.x - p.x), true);
                if(SkillEvolved(3))foreach(int side in new[]{-1,1}){var wing=Position+new V2(side*75,-40);Shoot(wing,(float)Math.Atan2(target.position.y-wing.y,target.position.x-wing.x),true);}
            }
        }
        public V2 SawPosition(int n) { return Position + V2.Angle(Elapsed * 2.8 + n * (Math.PI * 2) / (1 + Skills[1])) * (SkillEvolved(1)?105:65); }
        public V2 DronePosition { get { return Position + new V2((float)Math.Sin(Elapsed * 2) * 35, -55); } }
        void Shoot(V2 origin, double angle, bool drone)
        {
            float multiplier = drone ? (hero == 0 ? 1.15f : 1) * (module == 10 ? 1.2f+moduleGrowth*.025f : 1) : weapon == 4 ? 1.7f : weapon == 5 ? 2.5f : weapon == 3 ? .5f : 1;
            if (!drone && WeaponEvolution==1) multiplier *= weapon==4?2.2f:1.6f;
            if (!drone && WeaponEvolution==2) multiplier *= .7f;
            float critical = hero == 1 ? .10f : .05f; if (module == 11) critical += .05f+moduleGrowth*.004f;
            float damage = Damage * multiplier * (random.NextDouble() < critical ? 1.5f : 1);
            Bullets.Add(new Bullet { id = nextId++, position = origin, velocity = V2.Angle(angle) * (drone ? 450 : 390),
                visual=drone?(SkillEvolved(3)?3:0):WeaponEvolution, damage = damage, life = weapon == 3 && !drone ? .45f : 1.7f, pierce = !drone && WeaponEvolution==1 ? (weapon==2?8:3) : weapon == 2 || Skills[0] == 5 ? 2 : 0,
                blast = (weapon == 5 || HasMod(3) || WeaponEvolution==1 && weapon==1) && !drone, drone = drone, chilling = hero==4 && !drone });
        }
        void UpdateBullets(float dt)
        {
            IndexEnemies();
            for (int i = Bullets.Count - 1; i >= 0; i--)
            {
                var b = Bullets[i]; b.age+=dt;
                if(b.returning&&b.age>.65f)b.velocity=(Position-b.position).Normal*330;
                b.position += b.velocity * dt; b.life -= dt;
                foreach (var e in BulletCandidates(b.position))
                {
                    if (e.hp <= 0 || e.id == b.lastHit || V2.Distance(e.position, b.position) > e.Radius + 6) continue;
                    Hit(e, b.damage,b.machine,b.robotDesign); b.lastHit = e.id;
                    if(b.chilling)IdentityColdHit(e);
                    if (b.blast)
                    {
                        Effects.Add(new BattleEffect { position = b.position, kind = 2, radius = 60 });
                        foreach (var other in Enemies) if (other != e && other.hp > 0 && V2.Distance(other.position, b.position) < 60) Hit(other, b.damage * .6f,b.machine,b.robotDesign);
                    }
                    if (b.pierce-- <= 0) { b.life = 0; break; }
                }
                if (b.life <= 0 || b.position.x < WorldLeft-60 || b.position.x > WorldRight+60 || b.position.y < WorldTop-60 || b.position.y > WorldBottom+60) Bullets.RemoveAt(i);
            }
        }
        void Hit(Enemy e, float damage,bool machine=false,int robotDesign=0)
        {
            if(e.core)
            {
                damage=(float)Math.Round(damage,MidpointRounding.AwayFromZero);
                CoreScore+=Math.Max(0,(int)Math.Min(Math.Max(0,e.hp),damage));
            }
            if(e.role==EnemyRole.Shield && !e.core)damage*=RegionalShieldDamage(e);
            damage=(float)(Math.Round(damage*256d)/256d);
            float effective=Math.Min(Math.Max(0,e.hp),Math.Max(0,damage));DamageDealt+=effective;if(machine){MachineDamage+=effective;int design=robotDesign==0?MachineDesign:robotDesign;if(RobotAssembly.Valid(design))this.robotDamage[design-1]+=effective;}
            if(MachineTime>0)DamageDuringMachine+=effective;
            if(effective>0){e.killedByMachine=machine;e.robotDesign=machine?(robotDesign==0?MachineDesign:robotDesign):0;}
            e.hp -= damage; e.flash = .08f;
            if (e.kind >= 2 || (e.id+Kills)%7==0)
                Effects.Add(new BattleEffect { position = e.position, text = ((int)damage).ToString(), kind = 0 });
        }
        void Kill(int index)
        {
            var e = Enemies[index]; RecoverRobotPart(e);if(e.killedByMachine&&RobotAssembly.Valid(e.robotDesign))robotKills[e.robotDesign-1]++; if(MachineTime>0)KillsDuringMachine++;if(e.killedByMachine)MachineKills++; DefeatedRoles|=1<<(int)e.role; Kills++; UltimateCharge = Math.Min(100,UltimateCharge + 2);
            int xp = e.kind >= 2 ? 20 : 2;
            if (Pickups.Count < 220) Pickups.Add(new Pickup { id = nextId++, position = e.position, xp = xp });
            else {var merged=Pickups.Find(p=>!p.heal&&p.part<0);if(merged!=null)merged.xp+=xp;}
            if (Kills % (Challenge?35:60) == 0 && Pickups.Count<230) Pickups.Add(new Pickup { id = nextId++, position = e.position + new V2(10, 0), heal = true });
            Effects.Add(new BattleEffect { position = e.position, kind = 5, radius = e.Radius });
            if (e.finalBoss) { Ended = true; Won = true; }
            Enemies.RemoveAt(index);
        }
        void Collect(float dt)
        {
            float radius = 65 + Skills[7] * 24 + (module == 9 ? 25+moduleGrowth*3 : 0);
            for (int i = Pickups.Count - 1; i >= 0; i--)
            {
                var p = Pickups[i]; float d = V2.Distance(Position, p.position);
                if (d < (IntroBattle&&Elapsed<=65&&p.part>=0?Math.Max(radius,120):radius)) p.position += (Position - p.position).Normal * (300 * dt);
                if (d > 18) continue;
                if(p.part>=0&&p.part<3){robotParts[p.part]=Math.Min(12,robotParts[p.part]+p.partCount);Effects.Add(new BattleEffect{position=Position,kind=6,text=RobotAssembly.Parts[p.part]+" +"+p.partCount});}
                else if (p.heal) { Heal(MaxHp*(Challenge?.15f:.10f),HealingSource.Pickup); Effects.Add(new BattleEffect { position = Position, kind = 6, text = Challenge?"수리 +15%":"수리 +10%" }); }
                else { GainExperience(p.xp); if(RobotEnabled)Salvage+=p.xp*(Skills[11]>0?2:1); }
                Pickups.RemoveAt(i);
            }
            if (Xp >= NeededXp && !Ended) OfferSkills();
        }
        void OfferSkills()
        {
            var pool = new List<int>(); for (int i = 0; i < Skills.Length; i++) if (Skills[i] < 5) pool.Add(i);
            if (pool.Count == 0) { Xp -= NeededXp; Level++; Heal(MaxHp*.1f,HealingSource.MaxedBuild); return; }
            Choosing = true; Choices = new int[Math.Min(3, pool.Count)];
            if(Stage==0 && Level==1 && !Challenge && pool.Contains(1) && pool.Contains(2) && pool.Contains(3)){Choices=new[]{1,2,3};return;}
            if(Stage==0 && Level==2 && !Challenge && pool.Contains(0) && pool.Contains(4) && pool.Contains(5)){Choices=new[]{0,4,5};return;}
            int focused=FocusedGrowth(pool);
            for (int i = 0; i < Choices.Length; i++) { int n = i==0&&focused>=0?pool.IndexOf(focused):random.Next(pool.Count); Choices[i] = pool[n]; pool.RemoveAt(n); }
        }
        public bool Choose(int skill)
        {
            if (!Choosing || Array.IndexOf(Choices, skill) < 0 || Skills[skill] >= 5) return false;
            Xp -= NeededXp; Level++; Skills[skill]++;
            if (skill == 6) { MaxHp *= 1.15f; Heal(MaxHp*.35f,HealingSource.RepairSkill); }
            Choosing = false; ResolveEvolution(); return true;
        }
        public bool UseUltimate()
        {
            if (!CanUltimate) return false;
            UltimateCharge = 0; UltimateCooldown = 16; UltimateFlash = .7f; UltimateUses++;
            UltimateOrigin = Position;
            var target = Nearest(Position);
            UltimateDirection = target == null ? V2.Angle(Facing) : (target.position-Position).Normal;
            if(hero==3)
            {
                foreach(var enemy in Enemies)if(enemy.hp>0 && V2.Distance(Position,enemy.position)<=310)
                { Hit(enemy,Damage*6);Effects.Add(new BattleEffect{position=Position,end=enemy.position,kind=8}); }
                return true;
            }
            if (hero == 2) { Shield = Math.Max(Shield,MaxHp*.35f); ultimateGuard = 5; shieldTimer = 0; }
            for (int i = Enemies.Count-1; i >= 0; i--)
            {
                var e = Enemies[i]; if (e.hp <= 0) continue;
                V2 offset = e.position-Position;
                float forward = offset.x*UltimateDirection.x + offset.y*UltimateDirection.y;
                float side = Math.Abs(offset.x*UltimateDirection.y-offset.y*UltimateDirection.x);
                bool hit = hero == 1 ? forward >= -e.Radius && forward <= 560 && side <= 38+e.Radius : offset.Length <= (hero == 2 ? 180 : hero==4 ? 260 : 245)+e.Radius;
                if (!hit) continue;
                Hit(e,Damage*(hero == 1 ? 12 : hero == 2 ? 7 : hero==4 ? 4 : 5));
                if(hero==4 && !e.core){e.freezeTime=e.finalBoss?.6f:2.5f;e.slowTime=5;}
                if(!e.core)e.position=Slide(e.position,offset.Normal*(hero == 1 ? 28 : 56),Math.Min(e.Radius,20));
                if (e.hp <= 0) Kill(i);
            }
            return true;
        }
        public bool UseSupport()
        {
            if(!CanSupport)return false;
            SupportUsed=true;SupportTime=8;supportShot=0;
            if(Support.hero==0)Heal(MaxHp*.25f,HealingSource.Support);
            if(Support.hero==2) { Shield=Math.Max(Shield,MaxHp*.4f);ultimateGuard=8;shieldTimer=0; }
            return true;
        }
        void UpdateSupport(float dt)
        {
            if(SupportTime<=0 || Support==null)return;
            SupportTime=Math.Max(0,SupportTime-dt);supportShot-=dt;
            if(supportShot>0)return;
            var target=Nearest(Position);if(target==null)return;
            supportShot=Support.hero==1 ? .35f : .65f;
            V2 origin=Position+new V2(-30,-38);
            Bullets.Add(new Bullet {id=nextId++,position=origin,velocity=(target.position-origin).Normal*460,
                damage=Support.power*(Support.hero==1 ? .8f : .65f),life=1.7f,pierce=Support.hero==3?2:Support.hero==1 ? 1 : 0,drone=true,chilling=Support.hero==4});
        }
        public void Abandon() { if(Ended)return; Ended = true; Won = false; endCause=DamageCause.Abandoned; }
    }
}
