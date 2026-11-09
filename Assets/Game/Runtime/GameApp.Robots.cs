using System;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        RobotRigView battleRobot;
        void RobotAssembler(int design)
        {
            if(combat==null||combat.Ended||combat.Challenge||combat.Choosing)return;
            battlePaused=true;dragging=false;
            int main=RobotAssembly.Frame(design),sub=RobotAssembly.Support(design);var parts=combat.RobotParts;
            var card=Dialog("나만의 고철 로봇","포격·자폭 적 → 포신 / 추격·돌진 적 → 구동부\n방패·소환 적 → 장갑 · 전투가 끝나면 부품 초기화",862);
            var close=card.GetChild(2).GetComponent<Button>();close.onClick.RemoveAllListeners();close.onClick.AddListener(()=>{battlePaused=false;CloseModal();});
            RobotRigView.Draw(card,24,133,202,design);
            Ui.Label(card,RobotAssembly.Name(design),234,143,234,61,23,RobotRigView.Accent(main));
            Ui.Label(card,RobotAssembly.Detail(design),234,210,234,111,16,Ui.Ink);
            Ui.Label(card,"주부품 · 기체와 공격 선택",24,348,444,30,19,Ui.Mint);
            for(int i=0;i<3;i++)
            {
                int pick=i;Ui.Button(card,RobotAssembly.Frames[i],24+i*150,388,144,62,()=>RobotAssembler(pick*3+sub+1),main==i?Ui.Mint:Ui.Raised,true,16);
            }
            Ui.Label(card,"보조부품 · 추가 기능 선택",24,472,444,30,19,Ui.Mint);
            for(int i=0;i<3;i++)
            {
                int pick=i;Ui.Button(card,RobotAssembly.Supports[i]+" · "+RobotAssembly.Parts[i],24+i*150,512,144,58,()=>RobotAssembler(main*3+pick+1),sub==i?Ui.Mint:Ui.Raised,true,16);
            }
            Ui.Label(card,"회수 부품  포신 "+parts[0]+" / 구동부 "+parts[1]+" / 장갑 "+parts[2],24,592,444,34,16,Ui.Gold);
            int recovered=combat.RecoveredFor(design);
            Ui.Label(card,"회수 부품 "+recovered+"개 사용 · 이번 합체 화력 +"+(recovered*15)+"%\n부족한 부품은 일반 고철로 제작 · 추가 비용 없음",24,635,444,61,16,Ui.Ink,TextAnchor.MiddleCenter);
            string cost=combat.MachineRecovery>0?"재사용까지 "+Mathf.CeilToInt(combat.MachineRecovery)+"초":"고철 "+combat.MachineCost+" 사용 · 보유 "+combat.Salvage;
            Ui.Button(card,"조립하고 탑승 · "+cost,24,718,444,68,()=>
            {
                if(!combat.ActivateMachine(design))return;
                recorder.Machine(design);battlePaused=false;CloseModal();Track("combat_hook","robot_"+design,combat.Stage,combat.Elapsed);RenderBattle();
            },Ui.Mint,combat.CanActivateMachine,17);
            Ui.Label(card,"10초 동안 직접 이동 · 다음 합체에는 다른 조합도 가능",24,802,444,31,14,Ui.Muted,TextAnchor.MiddleCenter);
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
                var row=Ui.Box(list,0,y,444,214,Ui.Background).transform;RobotRigView.Draw(row,8,14,136,id);
                Ui.Label(row,RobotAssembly.Name(id)+(built?" ✓":""),153,12,277,36,20,built?Ui.Mint:Ui.Muted);
                Ui.Label(row,RobotAssembly.Detail(id),153,54,277,74,14,Ui.Ink);
                Ui.Label(row,built?"조립 "+record.builds+"회 · 직접 처치 "+record.kills+"\n누적 피해 "+Mathf.RoundToInt(record.damage).ToString("N0"):"아직 만들지 않은 조합 · 다음 출격에서 도전",14,159,280,43,14,Ui.Muted);
                bool owned=State.robotRecords.Exists(r=>r.design==id&&r.builds>0);
                Ui.Button(row,State.showcaseRobot==id?"전시 중":"전시",318,159,112,43,()=>Change(new GameCommand("robot_showcase"){stage=id},()=>RobotHangar(currentRun)),Ui.Gold,owned,16);
            }
            Ui.Button(card,"대원 전시로 돌아가기",24,803,214,44,()=>Change(new GameCommand("robot_showcase"){stage=0},()=>RobotHangar(currentRun)),Ui.Raised,true,15);
            Ui.Button(card,currentRun?"전투 분석 보기":"내 전시실 보기",254,803,214,44,()=>{if(currentRun)BattleAnalysis();else Show("showcase");},Ui.Raised,true,15);
        }
    }
}
