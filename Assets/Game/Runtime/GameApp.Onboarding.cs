using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        Text guideHint;
        void GuidePanel()
        {
            int next=StarterGuide.Next(State);
            var card=Dialog("첫 수호대원 안내", "생존 → 필살기 → 강화 → 개조 → 지역 정복\n언제든 닫고 자유롭게 플레이할 수 있어요.",820);
            for(int i=0;i<5;i++)
            {
                float y=145+i*106;bool done=StarterGuide.Done(State,i);
                Ui.Box(card,24,y,444,94,Ui.Background);
                Ui.Label(card,(done?"✓ ":(i+1)+". ")+StarterGuide.Names[i],38,y+8,414,29,19,done?Ui.Muted:i==next?Ui.Mint:Ui.Ink);
                Ui.Label(card,StarterGuide.Details[i],38,y+40,414,46,14,Ui.Muted);
            }
            string action=next==2?"장착 무기 강화하기":next==3?"개조 조합 보기":next>=4?"작전 지도 열기":"첫 작전 출격";
            Ui.Button(card,action,24,705,444,57,()=>{CloseModal();if(next==2)ItemDetail(State.equipped[0]);else if(next==3)ModBook();else if(next>=4)CampaignMap();else StartBattle(0);},Ui.Mint,true,19);
            Ui.Label(card,"전투 목표는 작전 종료 후 저장돼요. 추가 재화 보상은 없어요.",24,773,444,26,12,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void RenderGuideHint()
        {
            if(guideHint==null)return;
            bool show=combat.Stage==0 && !combat.Challenge && (!State.guideMoved || !State.guideUltimate);
            guideHint.gameObject.SetActive(show);
            if(!show)return;
            guideHint.text=!State.guideMoved && combat.DistanceMoved<120?"화면을 드래그해 이동하세요 · 공격은 자동이에요":
                !State.guideUltimate && combat.UltimateUses==0?(combat.CanUltimate?"필살기 충전 완료! 아래 필살기 버튼을 눌러보세요":"적을 처치하면 필살기가 충전돼요 · 경험치를 모아 성장하세요"):
                "잘했어요! 전투 후 장비를 강화해 다음 작전을 준비하세요";
        }
    }
}
