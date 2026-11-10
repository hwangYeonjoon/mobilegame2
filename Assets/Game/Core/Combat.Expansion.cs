using System;
namespace ScrapGuard.Core
{
    public sealed partial class Combat
    {
        void UpdateExpansionTerrain()
        {
            if(terrainTimer>0||Zones.Length==0)return;
            terrainTimer=7;var zone=Zones[terrainPulse++%Zones.Length];
            // Regional hazards remain secondary; never add them inside the boss arena.
            Warn(zone.Center,0,2.2f,170,zone.W,zone.H,Definition.Region==8?DamageCause.Electricity:DamageCause.Laser,DamageSource.Terrain);
        }
        bool ExpansionBossPattern(Enemy boss)
        {
            float delay=2,last=delay;var center=BossArenaCenter;
            if(Definition.Region==7)
            {
                // Magnetic jaws leave a wide central passage, then strike the marked center.
                if(bossPattern%2==1)
                {
                    Warn(center+new V2(-170,0),0,delay,205,90,520,DamageCause.Boss,DamageSource.Boss);
                    Warn(center+new V2(170,0),0,delay,205,90,520,DamageCause.Boss,DamageSource.Boss);
                    Effects.Add(new BattleEffect{position=Position,kind=10,text="자력 압착 · 가운데 통로로"});
                }
                else Warn(Position,90,delay,210,0,0,DamageCause.Boss,DamageSource.Boss);
            }
            else if(Definition.Region==8)
            {
                // A travelling row of lightning, with one entire lane left open.
                int gap=bossPattern%3;last=delay+1.1f;
                for(int row=0;row<3;row++)for(int lane=0;lane<3;lane++)if(lane!=gap)
                    Warn(center+new V2((lane-1)*140,(row-1)*155),52,delay+row*.55f,180,0,0,DamageCause.Electricity,DamageSource.Boss);
                Effects.Add(new BattleEffect{position=Position,kind=10,text="연쇄 낙뢰 · 비어 있는 세로 통로로"});
            }
            else
            {
                // Carrier launch bays sweep in two beats. Never cover the full arena.
                last=delay+1.1f;bool horizontal=bossPattern%2==0;
                for(int i=0;i<2;i++)
                {
                    var p=center+(horizontal?new V2(0,i==0?-170:170):new V2(i==0?-145:145,0));
                    Warn(p,0,delay+i*1.1f,200,horizontal?420:62,horizontal?62:500,DamageCause.Laser,DamageSource.Boss);
                }
                Effects.Add(new BattleEffect{position=Position,kind=10,text="모함 발사 · 첫 레일이 꺼진 뒤 이동"});
            }
            boss.attackFlash=.6f;ReserveBossAction(boss,false,last);return true;
        }
    }
}
