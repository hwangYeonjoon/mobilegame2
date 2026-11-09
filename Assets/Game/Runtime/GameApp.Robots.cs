using System;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        RobotRigView battleRobot;
        bool robotMenuOpen;float robotMenuOpenedAt;
        void FinishRobotMenu(string action,int design){Track("robot_choice",action,design,Time.unscaledTime-robotMenuOpenedAt);robotMenuOpen=false;}
        int robotTargetDesign=RobotAssembly.DefaultDesign;
        void RobotAssembler(int design)
        {
            if(combat==null||combat.Ended||combat.Challenge||combat.Choosing||combat.MachineTime>0)return;
            if(!robotMenuOpen){robotMenuOpen=true;robotMenuOpenedAt=Time.unscaledTime;Track("robot_choice","open",combat.MachinesBuilt,combat.Elapsed);}
            battlePaused=true;dragging=false;robotTargetDesign=design;
            int main=RobotAssembly.Frame(design),sub=RobotAssembly.Support(design),rank=combat.RobotRank(design);var parts=combat.RobotParts;var cost=combat.RobotRecipe(design);
            var card=Dialog("이번 판의 로봇 설계","포격·자폭 적 → 포신 / 추격·돌진 적 → 구동부\n방패·소환 적 → 장갑 · 포/장 표식 운반 적도 회수",862);
            var close=card.GetChild(2).GetComponent<Button>();close.onClick.RemoveAllListeners();close.onClick.AddListener(()=>{FinishRobotMenu("close",design);battlePaused=false;CloseModal();});
            RobotRigView.Draw(card,24,133,202,design,rank);
            Ui.Label(card,RobotAssembly.Name(design),234,143,234,61,23,RobotRigView.Accent(main));
            Ui.Label(card,rank==0&&design==RobotAssembly.DefaultDesign?"기본 보행기 · 주변 적을 밟아 돌파\n특화형 제작 후 보조 기능 활성":RobotAssembly.Detail(design),234,210,234,100,16,Ui.Ink);
            Ui.Label(card,rank==0?(design==RobotAssembly.DefaultDesign?"기본형 · 부품 없이 탑승":"미완성 · 회수 부품 필요"):rank==1?"특화형 · 이번 판 재사용":"완성형 · 이번 판 재사용",24,324,444,30,18,Ui.Gold,TextAnchor.MiddleCenter);
            for(int i=0;i<3;i++){int pick=i;Ui.Button(card,RobotAssembly.Frames[i],24+i*150,370,144,55,()=>RobotAssembler(pick*3+sub+1),main==i?Ui.Mint:Ui.Raised,true,16);}
            for(int i=0;i<3;i++){int pick=i;Ui.Button(card,RobotAssembly.Supports[i]+" · "+RobotAssembly.Parts[i],24+i*150,444,144,52,()=>RobotAssembler(main*3+pick+1),sub==i?Ui.Mint:Ui.Raised,true,16);}
            string recipe="";for(int i=0;i<3;i++)if(cost[i]>0)recipe+=(recipe.Length>0?" / ":"")+RobotAssembly.Parts[i]+" "+parts[i]+"/"+cost[i];
            Ui.Label(card,rank<2?"다음 설계에 필요한 부품\n"+recipe:"완성형 제작 완료 · 다음 탑승에도 유지",24,515,444,57,17,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Button(card,rank==0?"회수 부품으로 특화형 제작":rank==1?"회수 부품으로 완성형 제작":"완성형 제작 완료",24,591,444,55,()=>
            {
                if(!combat.PlanRobot(design,true))return;recorder.RobotPlan(design,true);Track("combat_robot","upgrade",design,combat.RobotRank(design));RobotAssembler(design);
            },Ui.Gold,combat.CanPlanRobot(design,true),18);
            string ready=combat.CanActivateMachine?"탑승 · 고철 "+combat.MachineCost:"선택 후 전투로";
            Ui.Button(card,combat.RobotAvailable(design)?ready:"부품을 회수해 설계를 완성하세요",24,670,444,63,()=>
            {
                if(!combat.PlanRobot(design,false))return;recorder.RobotPlan(design,false);
                if(combat.CanActivateMachine){combat.ActivateMachine();recorder.Machine();}
                FinishRobotMenu("select",design);battlePaused=false;CloseModal();RenderBattle();
            },Ui.Mint,combat.CanPlanRobot(design,false),18);
            Ui.Label(card,"탑승 "+combat.RobotDuration(design)+"초 · 재탑승에는 고철만 사용\n후반에는 특화·완성형 가동 시간 +4초\n짧게 눌러 즉시 탑승 · 길게 눌러 설계 변경",24,754,444,80,15,Ui.Muted,TextAnchor.MiddleCenter);
        }
        string RobotRecoveryHint()
        {
            if(combat==null||combat.RobotRank(robotTargetDesign)>=2)return "";
            var cost=combat.RobotRecipe(robotTargetDesign);var stock=combat.RobotParts;int needed=-1;
            for(int i=0;i<3;i++)if(stock[i]<cost[i]&&(needed<0||cost[i]-stock[i]>cost[needed]-stock[needed]))needed=i;
            if(needed<0)return "부품 준비 완료 · 합체 버튼을 길게 눌러 설계 강화";
            Enemy target=null;float nearest=float.MaxValue;
            foreach(var enemy in combat.Enemies)if(enemy.hp>0&&RobotAssembly.PartFor(enemy)==needed){float d=V2.Distance(combat.Position,enemy.position);if(d<nearest){nearest=d;target=enemy;}}
            return (target==null?"":BattleBearing(target.position)+" ")+RobotAssembly.Parts[needed]+" 회수 "+stock[needed]+"/"+cost[needed]+" · "+(needed==0?"포격·자폭 / 포 표식":needed==1?"추격·돌진 적":"방패·소환 / 장 표식");
        }
        void RobotHangar(bool currentRun=false)
        {
            RobotAssembly.Normalize(State);
            var records=currentRun&&combat!=null?combat.RobotReport():State.robotRecords;
            var card=Dialog(currentRun?"이번 판에 조립한 로봇":"나의 로봇 조립실",currentRun?"조합별 활약을 비교하고 다음 판의 로봇을 골라보세요.":"전투에서 조립한 모습이 남아요 · 능력치는 다음 판에 이월되지 않아요.",870);
            var list=Ui.Scroll(card,24,134,444,650,9*226+50);
            for(int i=1;i<=9;i++)
            {
                int id=i;var record=records.Find(r=>r!=null&&r.design==id);bool built=record!=null&&record.builds>0;float y=(i-1)*226;
                var row=Ui.Box(list,0,y,444,214,Ui.Background).transform;RobotRigView.Draw(row,8,14,136,id,record==null?1:record.rank);
                Ui.Label(row,RobotAssembly.Name(id)+(built?" ✓":""),153,12,277,36,20,built?Ui.Mint:Ui.Muted);
                Ui.Label(row,RobotAssembly.Detail(id),153,54,277,74,14,Ui.Ink);
                Ui.Label(row,built?(record.rank>=2?"완성형":record.rank==1?"특화형":"기본형")+" · 탑승 "+record.builds+"회 · 처치 "+record.kills+"\n누적 피해 "+Mathf.RoundToInt(record.damage).ToString("N0"):"아직 만들지 않은 조합 · 다음 출격에서 도전",14,159,280,43,14,Ui.Muted);
                bool owned=State.robotRecords.Exists(r=>r.design==id&&r.builds>0);
                Ui.Button(row,State.showcaseRobot==id?"전시 중":"전시",318,159,112,43,()=>Change(new GameCommand("robot_showcase"){stage=id},()=>RobotHangar(currentRun)),Ui.Gold,owned,16);
            }
            Ui.Button(card,"대원 전시로 돌아가기",24,803,214,44,()=>Change(new GameCommand("robot_showcase"){stage=0},()=>RobotHangar(currentRun)),Ui.Raised,true,15);
            Ui.Button(card,currentRun?"전투 분석 보기":"내 전시실 보기",254,803,214,44,()=>{if(currentRun)BattleAnalysis();else Show("showcase");},Ui.Raised,true,15);
        }
    }
}
