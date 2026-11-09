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
        bool robotSupportOptions;
        void ConfirmRobotPlan(int design,bool upgrade)
        {
            if(!combat.PlanRobot(design,upgrade))return;
            recorder.RobotPlan(design,upgrade);
            if(upgrade)Track("combat_robot","upgrade",design,combat.RobotRank(design));
            if(combat.CanActivateMachine&&combat.ActivateMachine())recorder.Machine();
            FinishRobotMenu("select",design);battlePaused=false;CloseModal();RenderBattle();
        }
        void RobotAssembler(int design)
        {
            if(combat==null||combat.Ended||combat.Challenge||combat.Choosing||combat.MachineTime>0)return;
            if(!robotMenuOpen){robotMenuOpen=true;robotSupportOptions=false;robotMenuOpenedAt=Time.unscaledTime;Track("robot_choice","open",combat.MachinesBuilt,combat.Elapsed);}
            battlePaused=true;dragging=false;robotTargetDesign=design;
            int main=RobotAssembly.Frame(design),sub=RobotAssembly.Support(design),rank=combat.RobotRank(design);var parts=combat.RobotParts;var cost=combat.RobotRecipe(design);
            bool craft=combat.CanPlanRobot(design,true),available=combat.RobotAvailable(design),board=combat.CanActivateMachine;
            var card=Dialog("어떤 로봇을 만들까요?","회수한 부품으로 제작 · 만든 설계는 이번 판에 유지",800);
            var close=card.GetChild(2).GetComponent<Button>();close.onClick.RemoveAllListeners();close.onClick.AddListener(()=>{FinishRobotMenu("close",design);battlePaused=false;CloseModal();});
            RobotRigView.Draw(card,24,120,176,design,rank);
            Ui.Label(card,RobotAssembly.Frames[main],212,132,256,35,23,RobotRigView.Accent(main));
            Ui.Label(card,RobotAssembly.Role(main)+"\n"+RobotAssembly.Tradeoff(main),212,178,256,76,16,Ui.Ink);
            Ui.Label(card,rank==0?(design==RobotAssembly.DefaultDesign?"기본형 사용 가능":"미제작"):rank==1?"특화형 보유":"완성형 보유",212,267,256,26,16,Ui.Gold);
            for(int i=0;i<3;i++){int pick=i;Ui.Button(card,RobotAssembly.Frames[i],24+i*150,318,144,50,()=>RobotAssembler(pick==main?design:pick*4+1),main==i?Ui.Mint:Ui.Raised,true,16);}
            Ui.Button(card,"보조: "+new[]{"관통탄 추가","이동·범위 증가","소형 보호막"}[sub]+(robotSupportOptions?"  ▴":"  변경 ▾"),24,386,444,42,()=>{robotSupportOptions=!robotSupportOptions;RobotAssembler(design);},Ui.Raised,true,16);
            if(robotSupportOptions)for(int i=0;i<3;i++){int pick=i;Ui.Button(card,new[]{"관통탄","이동·범위","보호막"}[i],24+i*150,440,144,42,()=>{robotSupportOptions=false;RobotAssembler(main*3+pick+1);},sub==i?Ui.Mint:Ui.Raised,true,16);}
            float y=robotSupportOptions?498:444;
            string recipe="";for(int i=0;i<3;i++)if(cost[i]>0)recipe+=(recipe.Length>0?"\n":"")+RobotAssembly.Parts[i]+" 필요 "+cost[i]+"개 · 보유 "+parts[i]+"개";
            Ui.Label(card,rank<2?(rank==0?"특화형 제작":"완성형 강화")+"\n"+recipe:"완성형 · 재탑승에는 고철만 사용",24,y,444,76,17,craft?Ui.Mint:Ui.Gold,TextAnchor.MiddleCenter);
            string action=board?"탑승":"선택 후 전투로";
            bool upgrade=rank==0&&craft;
            string primary=upgrade?"특화형 제작하고 "+(board?"탑승":"선택"):available?action:"부품 수집 목표로 선택";
            Ui.Button(card,primary,24,y+92,444,58,()=>
            {
                if(!available&&!upgrade){FinishRobotMenu("target",design);battlePaused=false;CloseModal();RenderBattle();return;}
                ConfirmRobotPlan(design,upgrade);
            },Ui.Mint,true,19);
            if(rank==1&&craft)Ui.Button(card,"완성형 제작하고 "+(board?"탑승":"선택"),24,y+160,444,43,()=>ConfirmRobotPlan(design,true),Ui.Gold,true,16);
            else if(rank==0&&craft&&available)Ui.Button(card,"부품을 아끼고 기본형 "+(board?"탑승":"선택"),24,y+160,444,43,()=>ConfirmRobotPlan(design,false),Ui.Raised,true,15);
            string status=board?"탑승 시 고철 "+combat.MachineCost+"개 사용":combat.MachineRecovery>0?"탑승 대기 "+Mathf.CeilToInt(combat.MachineRecovery)+"초 · 설계 선택은 지금 가능":"탑승에 고철 "+combat.MachineCost+"개 필요 · 보유 "+combat.Salvage+"개";
            Ui.Label(card,status+"\n재탑승은 짧게 · 설계 변경은 길게 누르기",24,728,444,58,14,Ui.Muted,TextAnchor.MiddleCenter);
        }
        string RobotRecoveryHint()
        {
            if(combat==null||combat.RobotRank(robotTargetDesign)>=2)return "";
            var cost=combat.RobotRecipe(robotTargetDesign);var stock=combat.RobotParts;int needed=-1;
            for(int i=0;i<3;i++)if(stock[i]<cost[i]&&(needed<0||cost[i]-stock[i]>cost[needed]-stock[needed]))needed=i;
            if(needed<0)return "부품 준비 완료 · 합체 버튼을 길게 눌러 제작";
            foreach(var pickup in combat.Pickups)if(pickup.part==needed)return BattleBearing(pickup.position)+" 떨어진 "+RobotAssembly.Parts[needed]+" 회수 · "+(cost[needed]-stock[needed])+"개 더 필요";
            Enemy target=null;float nearest=float.MaxValue;
            foreach(var enemy in combat.Enemies)if(enemy.hp>0&&RobotAssembly.PartFor(enemy)==needed){float d=V2.Distance(combat.Position,enemy.position);if(d<nearest){nearest=d;target=enemy;}}
            return (target==null?"":BattleBearing(target.position)+" ")+RobotAssembly.Parts[needed]+" "+(cost[needed]-stock[needed])+"개 더 · "+(needed==0?"포격·자폭 / 포 표식":needed==1?"추격·돌진 적":"방패·소환 / 장 표식");
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
