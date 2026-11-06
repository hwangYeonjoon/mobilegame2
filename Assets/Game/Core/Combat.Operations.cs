using System;
namespace ScrapGuard.Core
{
    public sealed partial class Combat
    {
        public int TaskNode { get; private set; }
        float nodeWork;
        public bool SpecialOperation { get { return !Challenge&&(Stage==2||Stage==4||Stage==8); } }
        public bool PoweredSafety { get { if(Task!=FieldTask.PowerRelay)return false;for(int i=0;i<TaskNode;i++)if(V2.Distance(Position,OperationNode(i))<78)return true;return false; } }
        public V2 OperationNode(int node)
        {
            if(Task==FieldTask.PowerRelay)return node==0?new V2(100,480):node==1?new V2(440,400):new V2(270,620);
            return node==0?new V2(145,445):node==1?new V2(390,480):new V2(270,620);
        }
        public string OperationInstruction
        {
            get
            {
                if(TaskCompleted)return CanAssemble?"작전 목표 완료 · 개조를 선택하세요":BuildInstruction;
                if(TaskFailed)return "현장 목표 종료 · 생존을 이어가세요";
                if(Elapsed<12)return "12초 후 "+TaskTitle;
                if(Task==FieldTask.Convoy)return "드론 옆에서 이동 · 적을 치워 통로 확보";
                return (Task==FieldTask.PowerRelay?"전원 연결 ":"거대 로봇 부품 회수 ")+(Math.Min(2,TaskNode)+1)+" / 3 · 표시된 원 안에 머무르세요";
            }
        }
        void UpdateSpecialOperation(float dt)
        {
            if(Task==FieldTask.Convoy)
            {
                int threats=0;foreach(var enemy in Enemies)if(enemy.hp>0&&V2.Distance(enemy.position,TaskPosition)<65)threats++;
                TaskIntegrity=Math.Max(0,TaskIntegrity-Math.Min(3,threats)*dt*3);
                if(TaskIntegrity<=0){TaskFailed=true;return;}
                if(threats==0&&V2.Distance(Position,TaskPosition)<105)TaskProgress=Math.Min(1,TaskProgress+dt/36);
                if(TaskProgress>=1)CompleteTask();return;
            }
            if(V2.Distance(Position,OperationNode(TaskNode))<65)nodeWork+=dt;
            float required=Task==FieldTask.PowerRelay?5:4;
            TaskProgress=Math.Min(1,(TaskNode+nodeWork/required)/3);
            if(nodeWork<required)return;
            TaskNode++;nodeWork=0;
            Effects.Add(new BattleEffect{position=OperationNode(TaskNode-1),kind=10,radius=60,text=Task==FieldTask.PowerRelay?"전력 복구 · 푸른 원에서 피해 감소":"부품 확보 · 다음 회수 지점으로"});
            if(TaskNode>=3)CompleteTask();
        }
    }
}
