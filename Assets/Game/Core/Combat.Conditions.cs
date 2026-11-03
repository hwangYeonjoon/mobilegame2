using System;
namespace ScrapGuard.Core
{
    public static class OperationConditions
    {
        public static readonly string[] Names={"기본 작전","돌진 순찰대","불안정 전력"};
        public static readonly string[] Details={"지역의 기본 적과 지형 규칙", "빠른 적이 돌진형으로 등장 · 직선 돌진을 옆으로 피하세요", "8초마다 현재 위치에 전격 예고 · 1.6초 안에 벗어나세요"};
        public static int Select(int region,int seed,bool challenge){return challenge || region==0?0:(int)(((uint)seed/3)%3);}
    }
    public sealed partial class Combat
    {
        public readonly int Condition;
        float conditionTimer=8;
        bool conditionAnnounced;
        public int ConditionPulses { get; private set; }
        void UpdateOperationCondition(float dt)
        {
            if(Condition==0)return;
            if(!conditionAnnounced){conditionAnnounced=true;Effects.Add(new BattleEffect{position=Position,kind=7,text=OperationConditions.Names[Condition]});}
            if(Condition!=2)return;
            conditionTimer-=dt;if(conditionTimer>0)return;
            conditionTimer=8;ConditionPulses++;
            Warn(Position,58,1.6f,90+Definition.Region*8,0,0,DamageCause.Electricity);
        }
    }
}
