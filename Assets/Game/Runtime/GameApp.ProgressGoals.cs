using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        void ProgressGoals()
        {
            Track("screen","progress_goals");
            var targets=ProgressTargets.Build(State);
            var card=Dialog("다음 성장 · 수집 목표","현재 저장된 진행과 보유 재화를 기준으로 보여 드려요.\n목표 확인만으로 구매되거나 재화가 사용되지 않아요.",890);
            var list=Ui.Scroll(card,24,140,444,628,Mathf.Max(628,targets.Count*240));
            if(targets.Count==0)Ui.Label(list,"현재 대원과 지역 도색을 모두 수집했어요!\n의상 컬렉션에서 다음 모습을 골라 보세요.",12,120,420,100,22,Ui.Mint,TextAnchor.MiddleCenter);
            for(int i=0;i<targets.Count;i++)
            {
                var goal=targets[i];var row=Ui.Box(list,0,i*240,444,226,Ui.Background).transform;
                if(goal.Kind=="hero")CharacterArt.Draw(row,8,14,90,97,goal.Id,0);
                else if(goal.Kind=="outfit")CharacterArt.Draw(row,8,14,90,97,Outfits.Hero(goal.Id),0,goal.Id);
                else if(goal.Kind=="region")CharacterArt.Draw(row,8,14,90,97,State.selectedHero,goal.Id+3);
                else InventoryArt.Draw(row,InventoryArt.Equipment(Campaign.Get(goal.Id).FirstItem),8,15,88);
                Ui.Label(row,goal.Title,112,10,316,52,22,goal.Ready?Ui.Mint:Ui.Ink);
                Ui.Label(row,goal.Detail,112,68,316,57,17,Ui.Muted);
                float progress=Mathf.Clamp01(goal.Current/(float)goal.Required);
                Ui.Box(row,14,134,280,7,Ui.Raised);Ui.Box(row,14,134,280*progress,7,Ui.Mint);
                Ui.Label(row,goal.Current.ToString("N0")+" / "+goal.Required.ToString("N0"),14,151,184,36,20,Ui.Gold);
                string action=goal.Kind=="outfit"?"의상 체험":goal.Ready?"획득 조건 확인":goal.Kind=="stage"?"작전 보기":"획득 방법";
                Ui.Button(row,action,214,151,216,62,()=>OpenProgressTarget(goal),Ui.Raised,true,19);
            }
            Ui.Button(card,"개조 조합 도감",24,794,214,66,ModBook,Ui.Raised,true,19);
            Ui.Button(card,"전체 의상 컬렉션",254,794,214,66,OutfitCollection,Ui.Raised,true,19);
        }
        void OpenProgressTarget(ProgressTarget goal)
        {
            if(goal.Kind=="stage")CampaignDetails(goal.Id);
            else if(goal.Kind=="region")RegionCollection(goal.Id);
            else if(goal.Kind=="outfit")OutfitRoom(goal.Id);
            else if(goal.Id==2)Attendance();
            else {CloseModal();Show("heroes");}
        }
    }
}
