using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        void ApexPanel()
        {
            var card=Dialog("극한 작전 · 선택 도전","성장한 장비와 로봇으로 다시 도전하세요.\n일반 작전 진행과 별개 · 실패해도 입장 비용 없음",880);
            for(int i=0;i<Apex.Stages.Length;i++)
            {
                int stage=Apex.Stages[i];var p=Apex.Find(State,stage);float y=155+i*180;
                Ui.Box(card,24,y,444,165,Ui.Raised);
                Ui.Label(card,"극한 "+(stage+1)+" · "+Campaign.Get(stage).Boss,38,y+12,410,36,21,Ui.Gold);
                Ui.Label(card,p!=null&&p.wins>0?"최고 "+TimeLabel(p.bestSeconds)+" · 완료 "+p.wins+"회\n첫 보상 수령 완료 · 재도전은 기록 갱신":"첫 완료 코어 파편 6개 · 로봇 외형 제작에 사용\n일반 "+(stage+1)+"작전 완료 후 개방",38,y+55,410,50,16,Ui.Muted);
                Ui.Button(card,Apex.Available(State,stage)?"조건 확인 · 출격":"일반 작전 먼저 완료",38,y+113,416,40,()=>ApexConfirm(stage),Ui.Mint,Apex.Available(State,stage),16);
            }
            Ui.Label(card,"3종 모두 완료: 칭호 ‘극한의 조립사’\n권장: 성장 상한 · 보스 회피 숙련 · 로봇 탑승\n지원·광고 부활 없음 · 순위는 동일 조건 실력전과 별도",24,707,444,85,16,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,"로봇 외형 제작",24,810,214,45,()=>RobotStylePanel(0,2),Ui.Gold,true,16);
            Ui.Button(card,"일반 작전 지도",254,810,214,45,()=>CampaignMap(),Ui.Raised,true,16);
        }
        void ApexConfirm(int stage)
        {
            var card=Dialog("극한 "+(stage+1)+" · 출격 확인",Campaign.Get(stage).Boss+" · 본인 대원과 장비 사용",610);
            Ui.Label(card,"일반 작전과 같은 전장에서 스킬과 로봇을 조립해요.\n보스 체력·피해 증가, 더 짧은 예고와 후속 표적 공격.\n안전 통로를 확인하고 로봇 탑승 시점을 선택하세요.",24,145,444,110,18,Ui.Ink);
            Ui.Label(card,"현재 공격 "+Catalog.Attack(State)+" · 체력 "+Catalog.Health(State)+"\n성장 상한 권장 · 미달이어도 무료로 도전 가능",24,285,444,63,18,Ui.Mint);
            Ui.Label(card,"첫 완료 파편 6개 · 이후 최고 시간 기록\n일반 작전 보상·주간 순위·길드 점수와 별도",24,369,444,61,17,Ui.Muted);
            Ui.Button(card,"극한 작전 시작",24,463,444,56,()=>StartBattle(stage,false,Apex.Mode),Ui.Gold,Apex.Available(State,stage),19);
            Ui.Button(card,"돌아가기",24,542,444,44,ApexPanel,Ui.Raised,true,17);
        }
        void ApexResult()
        {
            int stage=combat.Stage;var receipt=State.receipts.Find(r=>r.id==combat.RunId);var p=Apex.Find(State,stage);
            var card=Dialog(combat.Won?"극한 작전 완료":"극한 작전 재도전",Campaign.Get(stage).Boss+" · 일반 진행과 별도 기록",680);
            Ui.Label(card,TimeLabel(combat.Elapsed)+" · 처치 "+combat.Kills,24,151,444,45,26,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Label(card,(receipt?.shards??0)>0?"첫 완료 코어 파편 +"+receipt.shards:combat.Won?"기록 갱신 도전 · 추가 재화 보상 없음":"입장 비용 없음 · 일반 작전 진행은 그대로",24,225,444,62,20,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Label(card,"최고 기록 "+(p!=null&&p.wins>0?TimeLabel(p.bestSeconds):"미완료")+"\n"+(Apex.Mastered(State)?"칭호 ‘극한의 조립사’ 획득 · 전시실에서 적용":"극한 3종을 완료하면 전용 칭호 획득"),24,314,444,65,18,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,"다시 도전",24,423,214,52,()=>{combat=null;StartBattle(stage,false,Apex.Mode);},Ui.Mint,true,18);
            Ui.Button(card,"전투 분석",254,423,214,52,BattleAnalysis,Ui.Raised,true,18);
            Ui.Button(card,"로봇 외형 제작",24,499,444,53,()=>{combat=null;Show("weekly");RobotStylePanel(0,2);},Ui.Gold,true,18);
            Ui.Button(card,"극한 작전 목록",24,580,444,50,()=>{combat=null;Show("weekly");ApexPanel();},Ui.Raised,true,18);
            card.GetChild(2).gameObject.SetActive(false);
        }
    }
}
