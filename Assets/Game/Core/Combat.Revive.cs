using System;
namespace ScrapGuard.Core
{
    public sealed partial class Combat
    {
        public bool Revived { get; private set; }
        float reviveGuard;
        public bool CanRevive { get { return !Challenge&&!Practice&&!ApexBattle&&!Revived&&Ended&&!Won&&Hp<=0&&Elapsed>=20&&endCause!=DamageCause.Timeout&&endCause!=DamageCause.Abandoned; } }
        public bool Revive()
        {
            if(!CanRevive)return false;
            Revived=true;Ended=false;Won=false;Hp=MaxHp*.5f;endCause=DamageCause.None;
            reviveGuard=3;contactTimer=3;Hazards.Clear();
            foreach(var e in Enemies)if(!e.core&&V2.Distance(e.position,Position)<150)
                e.position=ClampWorld(Slide(e.position,(e.position-Position).Normal*150,Math.Min(20,e.Radius)),20);
            Effects.Add(new BattleEffect{position=Position,kind=10,text="긴급 수리 완료 · 3초 보호"});return true;
        }
    }
}
