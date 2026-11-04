using System;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        void OutfitCollection(){OutfitCollection(-1);}
        void OutfitCollection(int collection)
        {
            int owned=0;for(int i=1;i<=Outfits.Count;i++)if(Outfits.Owned(State,i))owned++;
            string goal=Outfits.Valid(State.outfitTarget)?"수집 목표: "+Outfits.Label(State.outfitTarget):"마음에 드는 의상을 체험하고 수집 목표로 저장하세요.";
            var card=Dialog("대원 의상 컬렉션", "보유 "+owned+" / "+Outfits.Count+" · 대원별 장착을 따로 기억해요.\n"+goal,890);
            RefreshOutfitCatalog(()=>{if(card!=null)OutfitCollection(collection);});
            float tabWidth=444f/(Outfits.Collections.Length+1);
            for(int tab=-1;tab<Outfits.Collections.Length;tab++)
            {
                int choice=tab;
                Ui.Button(card,tab<0?"전체":Outfits.Collections[tab],24+(tab+1)*tabWidth,145,tabWidth-6,38,()=>OutfitCollection(choice),collection==tab?Ui.Gold:Ui.Raised,true,13);
            }
            int count=0;for(int i=1;i<=Outfits.Count;i++)if(collection<0||Outfits.Collection(i)==collection)count++;
            var list=Ui.Scroll(card,24,195,444,572,count*212);int index=0;
            for(int i=1;i<=Outfits.Count;i++)
            {
                int id=i,hero=Outfits.Hero(i);bool has=Outfits.Owned(State,i),unlocked=State.heroes[hero].unlocked;
                if(collection>=0&&Outfits.Collection(i)!=collection)continue;
                var row=Ui.Box(list,0,index++*212,444,198,Ui.Background).transform;
                CharacterArt.Draw(row,8,22,128,150,hero,0,id);
                Ui.Label(row,Outfits.Label(i),148,12,280,35,23,OutfitVisuals.Accent(i));
                Ui.Label(row,Outfits.HeroName(i)+" 전용 · 대원 별도",148,52,280,27,15,Ui.Muted);
                string state=has?(Outfits.Active(State,hero)==id?"장착 중":"보유 · 장착 가능"):OutfitSaleLabel(id);
                Ui.Label(row,state,148,83,280,26,14,has?Ui.Mint:Ui.Gold);
                int stage=HeroCatalog.All[hero].RecruitStage;
                string detail=!unlocked?(stage>=0?"작전 "+(stage+1).ToString("D2")+" 완료 후 대원 무료 모집":"대원 모집 후 의상 구매 가능"):State.outfitTarget==id?"수집 목표 · 부족 보석 "+Math.Max(0,Outfits.Price-State.gems):"능력치 변화 없음 · 동작과 연출 포함";
                Ui.Label(row,detail,148,115,280,32,12,Ui.Muted);
                Ui.Button(row,"체험 · "+Outfits.HeroName(i),148,151,280,35,()=>OutfitRoom(id),Ui.Raised,true,16);
            }
            Ui.Label(card,"의상 "+Outfits.Count+"종 · 각 ◆ "+Outfits.Price+"\n전용 사격·필살기·지원 등장 포함 · 무료 보석 사용 가능",24,791,444,59,14,Ui.Muted,TextAnchor.MiddleCenter);
        }
    }
}
