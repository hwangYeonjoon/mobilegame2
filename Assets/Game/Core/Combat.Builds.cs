using System;
namespace ScrapGuard.Core
{
    public sealed partial class Combat
    {
        V2 fieldAnchor;
        float deployStill,buildTimer,buildDistance,magnetTimer;
        public int BuildUses { get; private set; }
        public V2 FieldAnchor { get { return fieldAnchor; } }
        public bool InTurretZone { get { return FieldModule==1&&V2.Distance(Position,fieldAnchor)<=125; } }
        public float BuildCharge { get { return FieldModule==4?Math.Min(1,(DistanceMoved-buildDistance)/150):FieldModule==2?1-Math.Max(0,magnetTimer)/3:0; } }
        public string BuildInstruction { get { return FieldModule==1?InTurretZone?"거점 안 · 포탑 사격 강화":"새 위치에서 1초 멈추면 포탑 이동":FieldModule==2?"근접 자력장 · 끌어온 적을 톱날로 절단":FieldModule==4?"이동 충전 → 적을 일렬로 유도해 관통":"수리 드론 · 위험한 순간에는 거리 확보"; } }
        void InitializeBuild(){fieldAnchor=Position;deployStill=0;buildDistance=DistanceMoved;magnetTimer=0;}
        void UpdateBuild(float dt)
        {
            buildTimer=Math.Max(0,buildTimer-dt);magnetTimer-=dt;
            if(FieldModule==1)
            {
                deployStill=LastMovement.Length>.1f?0:deployStill+dt;
                if(V2.Distance(Position,fieldAnchor)>150&&deployStill>=1){fieldAnchor=Position;deployStill=0;BuildUses++;Effects.Add(new BattleEffect{position=fieldAnchor,kind=12,radius=125});}
            }
            if(FieldModule==2&&magnetTimer<=0)
            {
                magnetTimer=3;BuildUses++;
                foreach(var e in Enemies)if(e.hp>0&&!e.core&&!e.finalBoss&&V2.Distance(e.position,Position)<175)
                {e.position=Slide(e.position,(Position-e.position).Normal*35,Math.Min(20,e.Radius));e.slowTime=Math.Max(e.slowTime,.7f);}
                Effects.Add(new BattleEffect{position=Position,kind=13,radius=175});
            }
            if(FieldModule!=4||buildTimer>0||DistanceMoved-buildDistance<150)return;
            var target=Nearest(Position);if(target==null)return;
            buildDistance=DistanceMoved;buildTimer=.75f;BuildUses++;
            var direction=(target.position-Position).Normal;
            Bullets.Add(new Bullet{id=nextId++,position=Position,velocity=direction*600,damage=Damage*2.2f,life=1.2f,pierce=6,mod=true});
            Effects.Add(new BattleEffect{position=Position,end=Position+direction*300,kind=8});
        }
    }
}
