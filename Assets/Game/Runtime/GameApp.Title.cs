using UnityEngine;
using UnityEngine.UI;

namespace ScrapGuard
{
    public partial class GameApp
    {
        RectTransform titleScreen;
        void ShowTitleScreen()
        {
            titleScreen=Ui.Rect(Root,"ScrapGuardTitle",0,0,540,960);
            var background=Ui.Box(titleScreen,0,0,540,960,Ui.Background,false);
            background.raycastTarget=true;
            BattleArt.Floor(titleScreen,0);
            Ui.Box(titleScreen,0,0,540,960,new Color(.025f,.06f,.09f,.83f),false);
            Ui.Label(titleScreen,"SCRAP GUARD",32,74,476,29,17,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Label(titleScreen,"고철 수호대",24,115,492,76,52,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Label(titleScreen,"버려진 고철로, 나만의 수호대를.",24,202,492,40,22,Ui.Mint,TextAnchor.MiddleCenter);
            var ring=CombatRing.Create(titleScreen,334,new Color(.25f,.9f,.7f,.16f),.012f);
            ring.anchoredPosition=new Vector2(270,-457);
            Ui.Disc(titleScreen,126,322,288,new Color(.25f,.9f,.7f,.045f));
            var left=CharacterArt.Draw(titleScreen,28,386,161,204,1,0);left.color=new Color(.85f,.85f,.85f,1);
            var right=CharacterArt.Draw(titleScreen,350,384,167,207,2,0);right.color=new Color(.85f,.85f,.85f,1);
            var lead=CharacterArt.Draw(titleScreen,133,294,274,325,0,0);
            lead.gameObject.AddComponent<ShowroomMotion>();
            Ui.Label(titleScreen,"폭주한 로봇을 막고, 장비를 모아 성장하세요.\n완성한 수호대는 나만의 전시실에.",30,637,480,67,19,Ui.Ink,TextAnchor.MiddleCenter);
            string[] pillars={"생존","성장","전시"};
            for(int i=0;i<3;i++)
            {
                Ui.Box(titleScreen,73+i*136,734,122,43,Ui.Panel);
                Ui.Label(titleScreen,pillars[i],73+i*136,734,122,43,17,Ui.Mint,TextAnchor.MiddleCenter);
            }
            Ui.Button(titleScreen,"정비소로 들어가기",40,825,460,66,DismissTitleScreen,Ui.Mint,true,22);
            Ui.Label(titleScreen,"한 손으로 시작하는 생존 · 수집 · 성장",24,910,492,25,13,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void DismissTitleScreen()
        {
            if(titleScreen==null)return;
            titleScreen.gameObject.SetActive(false);Destroy(titleScreen.gameObject);titleScreen=null;
            if(!smokeActive && (!Online || cloudReady) && !networkBusy && !State.guideIntroSeen)Change(new ScrapGuard.Core.GameCommand("guide_seen"),GuidePanel);
        }
    }
}
