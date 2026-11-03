using System;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        void OutfitCollection()
        {
            int owned=0;for(int i=1;i<=Outfits.Count;i++)if(Outfits.Owned(State,i))owned++;
            string goal=Outfits.Valid(State.outfitTarget)?"수집 목표: "+Outfits.Label(State.outfitTarget):"마음에 드는 의상을 체험하고 수집 목표로 저장하세요.";
            var card=Dialog("대원 의상 컬렉션", "보유 "+owned+" / "+Outfits.Count+" · 대원별 장착을 따로 기억해요.\n"+goal,890);
            var list=Ui.Scroll(card,24,145,444,622,Outfits.Count*212);
            for(int i=1;i<=Outfits.Count;i++)
            {
                int id=i,hero=Outfits.Hero(i);bool has=Outfits.Owned(State,i),unlocked=State.heroes[hero].unlocked;
                var row=Ui.Box(list,0,(i-1)*212,444,198,Ui.Background).transform;
                CharacterArt.Draw(row,8,22,128,150,hero,0,id);
                Ui.Label(row,Outfits.Label(i),148,12,280,35,23,OutfitVisuals.Accent(i));
                Ui.Label(row,Outfits.HeroName(i)+" 전용 · 대원 별도",148,52,280,27,15,Ui.Muted);
                string state=has?(Outfits.Active(State,hero)==id?"장착 중":"보유 · 장착 가능"):"◆ 600 · 확정 구매 · 상시 판매";
                Ui.Label(row,state,148,83,280,26,14,has?Ui.Mint:Ui.Gold);
                int stage=HeroCatalog.All[hero].RecruitStage;
                string detail=!unlocked?(stage>=0?"작전 "+(stage+1).ToString("D2")+" 완료 후 대원 무료 모집":"대원 모집 후 의상 구매 가능"):State.outfitTarget==id?"수집 목표 · 부족 보석 "+Math.Max(0,Outfits.Price-State.gems):"능력치 변화 없음 · 동작과 연출 포함";
                Ui.Label(row,detail,148,115,280,32,12,Ui.Muted);
                Ui.Button(row,"체험 · "+Outfits.HeroName(i),148,151,280,35,()=>OutfitRoom(id),Ui.Raised,true,16);
            }
            Ui.Label(card,"의상 4종 모두 상시 확정 구매 · 무료 보석도 사용 가능\n목표만 저장하며 보석은 구매 확인 때 차감돼요.",24,791,444,59,14,Ui.Muted,TextAnchor.MiddleCenter);
        }
    }
}
