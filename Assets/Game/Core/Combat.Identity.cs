using System;
namespace ScrapGuard.Core
{
    public static class HeroIdentity
    {
        public static readonly string[] Names={"정비 드론 거점","질주 관통","방호 반격","전격 연쇄","빙결 누적"};
        public static readonly string[] Tips={"멈춰서 드론 사격 · 위험하면 이동","이동으로 충전 · 다음 사격 관통","보호막으로 받아내면 근접 반격","세 번 사격하면 가까운 적에게 연쇄","같은 적을 세 번 맞혀 동결"};
    }
    public sealed partial class Combat
    {
        float identityStill,identityTimer,identityDistance,retaliationTimer;
        int identityShots;
        bool railReady;
        public int IdentityUses { get; private set; }
        public float IdentityCharge { get { return Hero==0?Math.Min(1,identityStill/1.2f):Hero==1?railReady?1:Math.Min(1,(DistanceMoved-identityDistance)/160):Hero==3?identityShots/3f:0; } }
        void UpdateIdentity(float dt)
        {
            identityTimer-=dt;retaliationTimer=Math.Max(0,retaliationTimer-dt);
            identityStill=LastMovement.Length>.1f?0:identityStill+dt;
            if(Hero==1 && DistanceMoved-identityDistance>=160)railReady=true;
            if(Hero!=0 || identityStill<1.2f || identityTimer>0)return;
            var target=Nearest(DronePosition);if(target==null)return;
            identityTimer=.85f;IdentityUses++;
            Shoot(DronePosition,(float)Math.Atan2(target.position.y-DronePosition.y,target.position.x-DronePosition.x),true);
        }
        void IdentityVolley()
        {
            if(Hero==1 && railReady)
            {
                foreach(var b in Bullets)if(!b.drone&&!b.mod&&V2.Distance(b.position,Position)<1)b.pierce+=2;
                railReady=false;identityDistance=DistanceMoved;IdentityUses++;
                Effects.Add(new BattleEffect{position=Position,kind=11,radius=24});
            }
            if(Hero!=3 || ++identityShots<3)return;
            identityShots=0;IdentityUses++;var first=Nearest(Position);if(first==null)return;
            Hit(first,Damage*.45f);Enemy next=null;float nearest=140;
            foreach(var e in Enemies)if(e!=first&&e.hp>0&&V2.Distance(e.position,first.position)<nearest){next=e;nearest=V2.Distance(e.position,first.position);}
            if(next!=null){Hit(next,Damage*.45f);Effects.Add(new BattleEffect{position=first.position,end=next.position,kind=8});}
        }
        void IdentityShieldHit(float absorbed)
        {
            if(Hero!=2 || absorbed<=0 || retaliationTimer>0)return;
            retaliationTimer=6;IdentityUses++;
            foreach(var e in Enemies)if(e.hp>0&&V2.Distance(e.position,Position)<130){Hit(e,Damage*.8f);if(!e.finalBoss&&!e.core)e.position=Slide(e.position,(e.position-Position).Normal*35,Math.Min(20,e.Radius));}
            Effects.Add(new BattleEffect{position=Position,kind=12,radius=130});
        }
        void IdentityColdHit(Enemy enemy)
        {
            if(enemy.core)return;
            enemy.slowTime=2;
            if(Hero!=4)return;
            if(++enemy.chillStacks<3)return;
            enemy.chillStacks=0;IdentityUses++;
            if(!enemy.finalBoss)enemy.freezeTime=Math.Max(enemy.freezeTime,.7f);
            Effects.Add(new BattleEffect{position=enemy.position,kind=13,radius=23});
        }
    }
}
