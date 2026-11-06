using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        Text guideHint;
        Image guideBackdrop;
        void GuidePanel()
        {
            int next=StarterGuide.Next(State);
            var card=Dialog("첫 수호대원 안내", "한 번에 하나씩, 지금 할 일부터 시작해요.\n언제든 닫고 자유롭게 플레이할 수 있어요.",840);
            var focus=Ui.Box(card,24,140,444,161,Ui.Background).transform;
            CharacterArt.Draw(focus,9,18,122,127,State.selectedHero,State.selectedSkin,Outfits.Active(State,State.selectedHero));
            Ui.Label(focus,next<5?"지금 할 일 · "+(next+1)+" / 5":"첫 수호대원 과정 완료",146,13,279,26,14,Ui.Mint);
            Ui.Label(focus,next<5?StarterGuide.Names[next]:"새로운 지역에 도전하세요",146,47,279,37,22);
            Ui.Label(focus,next<5?StarterGuide.Details[next]:"모은 재료로 성장하고 나만의 수호대를 전시해 보세요.",146,91,279,56,15,Ui.Muted);
            string action=next==2?"장착 무기 강화하기":next==3?"개조 조합 보기":next>=4?"작전 지도 열기":"첫 작전 출격";
            Ui.Button(card,action,24,319,444,57,()=>{CloseModal();if(next==2)ItemDetail(State.equipped[0]);else if(next==3)ModBook();else if(next>=4)CampaignMap();else StartBattle(0);},Ui.Mint,true,19);
            for(int i=0;i<5;i++)
            {
                float y=400+i*66;bool done=StarterGuide.Done(State,i);
                Ui.Box(card,24,y,444,56,i==next?Ui.Raised:Ui.Background);
                Ui.Label(card,(done?"✓ ":(i+1)+". ")+StarterGuide.Names[i],38,y+10,320,35,18,done?Ui.Muted:i==next?Ui.Mint:Ui.Ink);
                Ui.Label(card,done?"완료":i==next?"진행 중":"다음",364,y+10,86,35,14,done?Ui.Muted:Ui.Mint,TextAnchor.MiddleRight);
            }
            Ui.Label(card,"전투 목표는 작전 종료 후 저장돼요.\n안내 완료에 따른 별도 재화 보상은 없어요.",24,751,444,54,13,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void RenderGuideHint()
        {
            if(guideHint==null)return;
            bool show=combat.Stage==0 && combat.Elapsed<18 && !combat.Challenge && (!State.guideMoved || !State.guideUltimate) && !bossBackdrop.gameObject.activeSelf;
            guideHint.gameObject.SetActive(show);
            guideBackdrop.gameObject.SetActive(show);
            if(!show)return;
            guideHint.text=!State.guideMoved && combat.DistanceMoved<120?"화면을 드래그해 이동하세요 · 공격은 자동이에요":
                !State.guideUltimate && combat.UltimateUses==0?(combat.CanUltimate?"필살기 충전 완료! 아래 필살기 버튼을 눌러보세요":"적을 처치하면 필살기가 충전돼요 · 경험치를 모아 성장하세요"):
                "잘했어요! 전투 후 장비를 강화해 다음 작전을 준비하세요";
        }
    }
}
