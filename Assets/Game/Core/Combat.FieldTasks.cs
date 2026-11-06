using System;

namespace ScrapGuard.Core
{
    public enum FieldTask { None, Salvage, Defense, Escort, SalvageChain, PowerRelay, Convoy }
    public sealed partial class Combat
    {
        public FieldTask Task { get { return Challenge?FieldTask.None:Stage==2?FieldTask.SalvageChain:Stage==4?FieldTask.PowerRelay:Stage==8?FieldTask.Convoy:(Definition.Layout==0 && Stage==0 || Definition.Layout==1 || Definition.Layout==4)?FieldTask.Salvage:Definition.Layout==2?FieldTask.Defense:Definition.Layout==3?FieldTask.Escort:FieldTask.None; } }
        public V2 TaskPosition { get { return Task==FieldTask.SalvageChain||Task==FieldTask.PowerRelay?OperationNode(Math.Min(TaskNode,2)):Task==FieldTask.Convoy?new V2(270+(float)Math.Sin(TaskProgress*Math.PI*2)*80,660-TaskProgress*330):Task==FieldTask.Escort?new V2(270+(FieldVariant-1)*(Definition.Region==0?0:70),660-TaskProgress*300):new V2(270+(FieldVariant-1)*(Definition.Region==0?0:70),420); } }
        public float TaskProgress { get; private set; }
        public float TaskIntegrity { get; private set; }=100;
        public int TaskChoice { get; private set; }
        public bool TaskCompleted { get; private set; }
        public bool TaskFailed { get; private set; }
        public bool TaskActive { get { return Task!=FieldTask.None && Elapsed>=12 && Elapsed<(SpecialOperation?125:70) && !TaskCompleted && !TaskFailed; } }
        public bool CanChooseTask { get { return !Ended && !Choosing && TaskActive && Task==FieldTask.Salvage && TaskChoice==0 && V2.Distance(Position,TaskPosition)<=100; } }
        public bool RiskActive { get { return TaskActive && TaskChoice==2; } }
        public int TaskBonusGold { get { return Won && TaskCompleted?TaskChoice==2?400:200:0; } }
        public int TaskBonusParts { get { return Won && TaskCompleted?TaskChoice==2?4:2:0; } }
        public string TaskTitle { get { return Task==FieldTask.SalvageChain?"거대 로봇 해체":Task==FieldTask.PowerRelay?"정비소 전력 복구":Task==FieldTask.Convoy?"수송체 호송":Task==FieldTask.Salvage?"부품 회수":Task==FieldTask.Defense?"중계기 방어":"수송 드론 호위"; } }
        public string TaskInstruction
        {
            get
            {
                if(SpecialOperation)return OperationInstruction;
                if(TaskCompleted)return FieldModule==0?"부품 확보! 현장 개조를 선택하세요 · 이번 전투 전용":FieldEngineering.Name(FieldModule,Hero)+" 가동 · 승리 시 추가 재화 지급";
                if(TaskFailed)return TaskTitle+" 종료 · 본 작전은 계속됩니다";
                if(Elapsed<12)return "선택 임무 예고 · "+TaskTitle+" · 12초에 개방";
                if(Task==FieldTask.Salvage)return TaskChoice==2?"과부하 회수 · 20초 생존 / 화력 +20%, 받는 피해 +20%":"회수 장치에 접근해 안전 / 과부하 회수를 선택하세요";
                if(Task==FieldTask.Defense)return "푸른 원 안에서 18초 방어 · 장치 내구도 "+(int)TaskIntegrity+"%";
                return "드론 가까이에서 이동 · 적이 붙으면 수송이 멈춰요";
            }
        }
        public bool ChooseTask(int choice)
        {
            if(!CanChooseTask || choice<1 || choice>2)return false;
            TaskChoice=choice;
            if(choice==1){Heal(MaxHp*.12f);CompleteTask();}
            else Effects.Add(new BattleEffect{position=TaskPosition,kind=7,text="과부하 회수 시작 · 20초를 버티세요"});
            return true;
        }
        void CompleteTask()
        {
            TaskProgress=1;TaskCompleted=true;
            Xp+=6;
            Effects.Add(new BattleEffect{position=TaskPosition,kind=10,text=TaskTitle+" 성공!",radius=65});
        }
        void UpdateFieldTask(float dt)
        {
            if(Task==FieldTask.None || TaskCompleted || TaskFailed)return;
            if(Elapsed>=(SpecialOperation?125:70)){TaskFailed=true;return;}
            if(!TaskActive)return;
            if(SpecialOperation){UpdateSpecialOperation(dt);return;}
            if(Task==FieldTask.Salvage)
            {
                if(TaskChoice==2){TaskProgress=Math.Min(1,TaskProgress+dt/20);if(TaskProgress>=1)CompleteTask();}
                return;
            }
            int threats=0;foreach(var enemy in Enemies)if(enemy.hp>0 && V2.Distance(enemy.position,TaskPosition)<65)threats++;
            bool near=V2.Distance(Position,TaskPosition)<105;
            if(Task==FieldTask.Defense)
            {
                TaskIntegrity=Math.Max(0,TaskIntegrity-Math.Min(4,threats)*dt*6);
                if(TaskIntegrity<=0){TaskFailed=true;return;}
                if(near)TaskProgress=Math.Min(1,TaskProgress+dt/18);
            }
            else if(near && threats==0)TaskProgress=Math.Min(1,TaskProgress+dt/24);
            if(TaskProgress>=1)CompleteTask();
        }
        void FirstRegionPacing(float dt)
        {
            if(Challenge || Definition.Region!=0)return;
            // A deterministic early progression floor; collecting drops can reach it sooner.
            if(Stage==0 && Level==1 && Elapsed>=8)Xp=Math.Max(Xp,NeededXp);
            if(Stage==0 && Level==2 && Elapsed>=24)Xp=Math.Max(Xp,NeededXp);

        }

    }
}
