using UnityEngine;
namespace ScrapGuard
{
    // Device presentation only. Never changes Combat, collision, damage or replay input.
    public sealed class BattleVisualBudget
    {
        public int Mode;
        float slowTime,healthyTime;
        bool throttled;
        public bool Compact { get; private set; }
        public int ParticleLimit { get { return Compact?48:160; } }
        public int FloatingLimit { get { return Compact?12:36; } }
        public void Sample(float delta,int enemies,int bullets)
        {
            if(delta>0 && delta<.25f)
            {
                slowTime=delta>1f/42?slowTime+delta:Mathf.Max(0,slowTime-delta);
                healthyTime=delta<1f/52?healthyTime+delta:0;
                if(slowTime>1.5f)throttled=true;
                if(healthyTime>5){throttled=false;slowTime=0;}
            }
            Compact=Mode==2 || Mode==0&&(throttled||enemies>=100||bullets>=160);
        }
    }
}
