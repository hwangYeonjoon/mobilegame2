using System;
using System.Collections.Generic;

namespace ScrapGuard.Core
{
    public struct V2
    {
        public float x, y;
        public V2(float x, float y) { this.x = x; this.y = y; }
        public float Length { get { return (float)Math.Sqrt(x * x + y * y); } }
        public V2 Normal { get { float d = Length; return d > .001f ? this / d : new V2(); } }
        public static V2 operator +(V2 a, V2 b) { return new V2(a.x + b.x, a.y + b.y); }
        public static V2 operator -(V2 a, V2 b) { return new V2(a.x - b.x, a.y - b.y); }
        public static V2 operator *(V2 a, float b) { return new V2(a.x * b, a.y * b); }
        public static V2 operator /(V2 a, float b) { return new V2(a.x / b, a.y / b); }
        public static float Distance(V2 a, V2 b) { return (a - b).Length; }
        public static V2 Angle(float a) { return new V2((float)Math.Cos(a), (float)Math.Sin(a)); }
    }
    public class Enemy
    {
        public int id, kind;
        public V2 position;
        public float hp, maxHp, speed, cooldown, flash;
        public bool finalBoss, core;
        public float Radius { get { return kind == 3 ? 35 : kind == 2 ? 23 : 15; } }
    }
    public class Bullet
    {
        public int id;
        public V2 position, velocity;
        public float damage, life;
        public int pierce, lastHit = -1;
        public bool blast, drone;
    }
    public class Pickup { public int id, xp; public V2 position; public bool heal; }
    public class Hazard { public int id; public V2 position; public float delay, radius; }
    public class BattleEffect { public V2 position; public string text; public int kind; public float radius; }
    public sealed class Combat
    {
        public const int RulesVersion = 4;
        public static readonly string[] UltimateNames = { "고철 폭풍", "광자 관통", "요새 강하" };
        public int Hero { get { return hero; } }
        public float UltimateCharge { get; private set; } = 50;
        public float UltimateCooldown { get; private set; }
        public float ShotFlash { get; private set; }
        public float HurtFlash { get; private set; }
        public float UltimateFlash { get; private set; }
        public int UltimateUses { get; private set; }
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
        readonly int hero, weapon, module, armor;
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
        public readonly int[] Skills = new int[8];
        public V2 Position = new V2(270, 510);
        public float Hp, MaxHp, Shield, Elapsed, Facing;
        public int Kills, Level = 1, Xp;
        public bool Choosing, Ended, Won;
        public int[] Choices = new int[3];
        public int NeededXp { get { return 6 + Level * 4; } }
        public bool FinalBossActive { get { return bossSpawned; } }
        public float Damage { get { return baseDamage * (1 + Skills[4] * .20f); } }
        public Combat(GameState state, int stage, string runId, int seed = 123, bool quick = false, string mode = "", SupportSnapshot support = null)
        {
            Mode=mode??"";
            if(Mode!="" && !Endgame.IsChallenge(Mode))throw new ArgumentException("Unknown combat mode");
            if(support!=null && (Challenge || support.hero<0 || support.hero>2 || support.power<1 || support.power>350))throw new ArgumentException("Invalid support snapshot");
            Support=support;
            RunId = runId; Stage = stage; random = new Random(seed); hero = state.selectedHero;
            weapon = state.items.Find(i => i.id == state.equipped[0]).definition;
            armor = state.items.Find(i => i.id == state.equipped[1]).definition;
            module = state.items.Find(i => i.id == state.equipped[2]).definition;
            baseDamage = Catalog.Attack(state); MaxHp = Hp = Catalog.Health(state);
            baseSpeed = new[] { 160f, 174f, 150f }[hero] * (armor == 8 ? 1.08f : 1);
            Duration = Challenge ? Endgame.Duration : quick ? 45 : !state.tutorialCompleted && stage == 0 ? 180 : 300 + stage * 15;
            Skills[0] = 1;
            if(Challenge)
            {
                Position=new V2(270,600);bossSpawned=true;bossTimer=3;
                Enemies.Add(new Enemy {id=nextId++,kind=3,core=true,position=new V2(270,330),hp=10000000,maxHp=10000000,speed=0});
            }
        }
        public void Tick(float dt, V2 movement)
        {
            Effects.Clear();
            if (Ended || Choosing) return;
            dt = Math.Max(0, Math.Min(.05f, dt)); Elapsed += dt;
            if(Challenge && Elapsed>=Duration) { Elapsed=Duration;Ended=true;Won=Hp>0;return; }
            LastMovement = movement;
            UltimateCharge = Math.Min(100,UltimateCharge + dt*2);
            UltimateCooldown = Math.Max(0,UltimateCooldown-dt);
            UltimateFlash = Math.Max(0,UltimateFlash-dt);
            ShotFlash = Math.Max(0,ShotFlash-dt); HurtFlash = Math.Max(0,HurtFlash-dt);
            if (ultimateGuard > 0 && ultimateGuard <= dt) Shield = 0;
            ultimateGuard = Math.Max(0,ultimateGuard-dt);
            Position += (movement.Length > 1 ? movement.Normal : movement) * (baseSpeed * dt);
            Position.x = Math.Max(26, Math.Min(514, Position.x)); Position.y = Math.Max(190, Math.Min(790, Position.y));
            if (movement.Length > .1f) Facing = (float)Math.Atan2(movement.y, movement.x);
            contactTimer -= dt;
            if (hero == 2 && ultimateGuard <= 0)
            {
                shieldTimer += dt;
                if (shieldTimer >= 20) { Shield = MaxHp * .08f; shieldTimer = 0; }
                if (shieldTimer > 5) Shield = 0;
            }
            spawnTimer -= dt;
            if (spawnTimer <= 0)
            {
                spawnTimer = Math.Max(.18f, .85f - Elapsed / Duration * .58f);
                if (Enemies.Count < 150) Spawn(random.NextDouble() < .2 + Stage * .04 ? 1 : 0, false);
            }
            if (!eliteSpawned && Elapsed > Duration * .46f) { eliteSpawned = true; Spawn(2, false); }
            if (!bossSpawned && Elapsed >= Duration - 30) { bossSpawned = true; Spawn(3, true); }
            if (Elapsed > Duration + 60) { Ended = true; Won = false; return; }
            if (bossSpawned)
            {
                bossTimer -= dt;
                if (bossTimer <= 0)
                {
                    bossTimer = 3.2f;
                    Hazards.Add(new Hazard { id = nextId++, position = Position, radius = 65, delay = 1.4f });
                }
            }
            UpdateHazards(dt);
            if (Ended) return;
            UpdateEnemies(dt);
            if (Ended) return;
            UpdateWeapons(dt);
            UpdateSupport(dt);
            UpdateBullets(dt);
            Collect(dt);
        }
        void Spawn(int kind, bool final)
        {
            int side = random.Next(4);
            V2 p = side == 0 ? new V2(15, 200 + random.Next(570)) : side == 1 ? new V2(525, 200 + random.Next(570)) :
                side == 2 ? new V2(30 + random.Next(480), 175) : new V2(30 + random.Next(480), 810);
            if (V2.Distance(p, Position) < 95) p = new V2(540 - p.x, 980 - p.y);
            float difficulty = 1 + Stage * .28f + Elapsed / Duration * .55f;
            float hp = (kind == 3 ? 7000 : kind == 2 ? 1800 : kind == 1 ? 110 : 150) * difficulty;
            Enemies.Add(new Enemy { id = nextId++, kind = kind, finalBoss = final, position = p, hp = hp, maxHp = hp,
                speed = kind == 3 ? 31 : kind == 2 ? 42 : kind == 1 ? 74 : 41 });
            if (kind >= 2) Effects.Add(new BattleEffect { position = new V2(270, 300), text = kind == 3 ? "폭주 코어 출현!" : "강화 로봇 접근", kind = 3 });
        }
        void UpdateHazards(float dt)
        {
            for (int i = Hazards.Count - 1; i >= 0; i--)
            {
                var h = Hazards[i]; h.delay -= dt;
                if (h.delay > 0) continue;
                Effects.Add(new BattleEffect { position = h.position, kind = 2, radius = h.radius });
                if (V2.Distance(Position, h.position) < h.radius) Hurt(200 + Stage * 40);
                Hazards.RemoveAt(i);
            }
        }
        void UpdateEnemies(float dt)
        {
            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                var e = Enemies[i];
                if (e.hp <= 0) { Kill(i); continue; }
                e.flash = Math.Max(0, e.flash - dt);
                e.position += (Position - e.position).Normal * (e.speed * dt);
                if (V2.Distance(Position, e.position) < 15 + e.Radius && contactTimer <= 0)
                {
                    Hurt(e.kind == 3 ? 170 : e.kind == 2 ? 110 : 70); contactTimer = .65f;
                    if(!e.core)e.position += (e.position - Position).Normal * 22;
                }
            }
        }
        void Hurt(float damage)
        {
            if (Ended || UltimateFlash > .15f) return;
            HurtFlash = .22f;
            if (armor == 7) damage *= .9f;
            float absorbed = Math.Min(Shield, damage); Shield -= absorbed; Hp -= damage - absorbed;
            Effects.Add(new BattleEffect { position = Position, text = "-" + (int)(damage - absorbed), kind = 1 });
            if (Hp <= 0) { Hp = 0; Ended = true; Won = false; }
        }
        Enemy Nearest(V2 origin)
        {
            Enemy target = null; float distance = float.MaxValue;
            foreach (var e in Enemies)
            {
                if (e.hp <= 0) continue;
                float d = V2.Distance(origin, e.position);
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
                float angle = (float)Math.Atan2(target.position.y - Position.y, target.position.x - Position.x);
                Facing = angle; ShotFlash = .11f;
                int count = 1 + Skills[0] / 2 + (weapon == 1 ? 2 : 0);
                for (int i = 0; i < count; i++) Shoot(Position, angle + (i - (count - 1) * .5f) * .16f, false);
            }
            if (Skills[1] > 0 && sawTimer <= 0)
            {
                sawTimer = .24f;
                for (int n = 0; n < 1 + Skills[1]; n++)
                {
                    var p = SawPosition(n);
                    foreach (var e in Enemies) if (e.hp > 0 && V2.Distance(p, e.position) < e.Radius + 18) Hit(e, Damage * .45f);
                }
            }
            if (Skills[2] > 0 && coilTimer <= 0)
            {
                coilTimer = 2.7f - Skills[2] * .18f;
                float radius = 90 + Skills[2] * 13;
                Effects.Add(new BattleEffect { position = Position, kind = 4, radius = radius });
                foreach (var e in Enemies) if (e.hp > 0 && V2.Distance(e.position, Position) < radius) Hit(e, Damage * (1 + Skills[2] * .2f));
            }
            if (Skills[3] > 0 && droneTimer <= 0 && target != null)
            {
                droneTimer = 1.1f - Skills[3] * .1f;
                var p = DronePosition;
                Shoot(p, (float)Math.Atan2(target.position.y - p.y, target.position.x - p.x), true);
            }
        }
        public V2 SawPosition(int n) { return Position + V2.Angle(Elapsed * 2.8f + n * (float)(Math.PI * 2) / (1 + Skills[1])) * 65; }
        public V2 DronePosition { get { return Position + new V2((float)Math.Sin(Elapsed * 2) * 35, -55); } }
        void Shoot(V2 origin, float angle, bool drone)
        {
            float multiplier = drone ? (hero == 0 ? 1.15f : 1) * (module == 10 ? 1.2f : 1) : weapon == 4 ? 1.7f : weapon == 5 ? 2.5f : weapon == 3 ? .5f : 1;
            if (!drone && Skills[0] == 5 && Skills[4] >= 3) multiplier *= 1.4f;
            float critical = hero == 1 ? .10f : .05f; if (module == 11) critical += .05f;
            float damage = Damage * multiplier * (random.NextDouble() < critical ? 1.5f : 1);
            Bullets.Add(new Bullet { id = nextId++, position = origin, velocity = V2.Angle(angle) * (drone ? 450 : 390),
                damage = damage, life = weapon == 3 && !drone ? .45f : 1.7f, pierce = weapon == 2 || Skills[0] == 5 ? 2 : 0,
                blast = weapon == 5 && !drone, drone = drone });
        }
        void UpdateBullets(float dt)
        {
            for (int i = Bullets.Count - 1; i >= 0; i--)
            {
                var b = Bullets[i]; b.position += b.velocity * dt; b.life -= dt;
                foreach (var e in Enemies)
                {
                    if (e.hp <= 0 || e.id == b.lastHit || V2.Distance(e.position, b.position) > e.Radius + 6) continue;
                    Hit(e, b.damage); b.lastHit = e.id;
                    if (b.blast)
                    {
                        Effects.Add(new BattleEffect { position = b.position, kind = 2, radius = 60 });
                        foreach (var other in Enemies) if (other != e && other.hp > 0 && V2.Distance(other.position, b.position) < 60) Hit(other, b.damage * .6f);
                    }
                    if (b.pierce-- <= 0) { b.life = 0; break; }
                }
                if (b.life <= 0 || b.position.x < -40 || b.position.x > 580 || b.position.y < 140 || b.position.y > 850) Bullets.RemoveAt(i);
            }
        }
        void Hit(Enemy e, float damage)
        {
            if(e.core)
            {
                damage=(float)Math.Round(damage,MidpointRounding.AwayFromZero);
                CoreScore+=Math.Max(0,(int)Math.Min(Math.Max(0,e.hp),damage));
            }
            e.hp -= damage; e.flash = .08f;
            if (e.kind >= 2 || random.NextDouble() < .15)
                Effects.Add(new BattleEffect { position = e.position, text = ((int)damage).ToString(), kind = 0 });
        }
        void Kill(int index)
        {
            var e = Enemies[index]; Kills++; UltimateCharge = Math.Min(100,UltimateCharge + 2);
            int xp = e.kind >= 2 ? 20 : 2;
            if (Pickups.Count < 220) Pickups.Add(new Pickup { id = nextId++, position = e.position, xp = xp });
            else Pickups[random.Next(Pickups.Count)].xp += xp;
            if (Kills % 35 == 0) Pickups.Add(new Pickup { id = nextId++, position = e.position + new V2(10, 0), heal = true });
            Effects.Add(new BattleEffect { position = e.position, kind = 5, radius = e.Radius });
            if (e.finalBoss) { Ended = true; Won = true; }
            Enemies.RemoveAt(index);
        }
        void Collect(float dt)
        {
            float radius = 65 + Skills[7] * 24 + (module == 9 ? 25 : 0);
            for (int i = Pickups.Count - 1; i >= 0; i--)
            {
                var p = Pickups[i]; float d = V2.Distance(Position, p.position);
                if (d < radius) p.position += (Position - p.position).Normal * (300 * dt);
                if (d > 18) continue;
                if (p.heal) { Hp = Math.Min(MaxHp, Hp + MaxHp * .15f); Effects.Add(new BattleEffect { position = Position, kind = 6, text = "수리 +15%" }); }
                else Xp += p.xp;
                Pickups.RemoveAt(i);
            }
            if (Xp >= NeededXp && !Ended) OfferSkills();
        }
        void OfferSkills()
        {
            var pool = new List<int>(); for (int i = 0; i < Skills.Length; i++) if (Skills[i] < 5) pool.Add(i);
            if (pool.Count == 0) { Xp -= NeededXp; Level++; Hp = Math.Min(MaxHp, Hp + MaxHp * .1f); return; }
            Choosing = true; Choices = new int[Math.Min(3, pool.Count)];
            for (int i = 0; i < Choices.Length; i++) { int n = random.Next(pool.Count); Choices[i] = pool[n]; pool.RemoveAt(n); }
        }
        public bool Choose(int skill)
        {
            if (!Choosing || Array.IndexOf(Choices, skill) < 0 || Skills[skill] >= 5) return false;
            Xp -= NeededXp; Level++; Skills[skill]++;
            if (skill == 6) { MaxHp *= 1.15f; Hp = Math.Min(MaxHp, Hp + MaxHp * .35f); }
            Choosing = false; return true;
        }
        public bool UseUltimate()
        {
            if (!CanUltimate) return false;
            UltimateCharge = 0; UltimateCooldown = 16; UltimateFlash = .7f; UltimateUses++;
            UltimateOrigin = Position;
            var target = Nearest(Position);
            UltimateDirection = target == null ? V2.Angle(Facing) : (target.position-Position).Normal;
            if (hero == 2) { Shield = Math.Max(Shield,MaxHp*.35f); ultimateGuard = 5; shieldTimer = 0; }
            for (int i = Enemies.Count-1; i >= 0; i--)
            {
                var e = Enemies[i]; if (e.hp <= 0) continue;
                V2 offset = e.position-Position;
                float forward = offset.x*UltimateDirection.x + offset.y*UltimateDirection.y;
                float side = Math.Abs(offset.x*UltimateDirection.y-offset.y*UltimateDirection.x);
                bool hit = hero == 1 ? forward >= -e.Radius && forward <= 560 && side <= 38+e.Radius : offset.Length <= (hero == 2 ? 180 : 245)+e.Radius;
                if (!hit) continue;
                Hit(e,Damage*(hero == 1 ? 12 : hero == 2 ? 7 : 5));
                if(!e.core)e.position += offset.Normal*(hero == 1 ? 28 : 56);
                if (e.hp <= 0) Kill(i);
            }
            return true;
        }
        public bool UseSupport()
        {
            if(!CanSupport)return false;
            SupportUsed=true;SupportTime=8;supportShot=0;
            if(Support.hero==0)Hp=Math.Min(MaxHp,Hp+MaxHp*.25f);
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
                damage=Support.power*(Support.hero==1 ? .8f : .65f),life=1.7f,pierce=Support.hero==1 ? 1 : 0,drone=true});
        }
        public void Abandon() { Ended = true; Won = false; }
    }
}
