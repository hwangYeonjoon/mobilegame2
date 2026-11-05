using System;
using System.Collections.Generic;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        int outfitHeroFilter=-1,outfitOwnedFilter;
        List<int> FilteredOutfits(int collection)
        {
            var ids=new List<int>();
            for(int i=1;i<=Outfits.Count;i++)
            {
                if(collection>=0&&Outfits.Collection(i)!=collection)continue;
                if(outfitHeroFilter>=0&&Outfits.Hero(i)!=outfitHeroFilter)continue;
                if(outfitOwnedFilter>0&&!Outfits.Owned(State,i))continue;
                if(outfitOwnedFilter==2&&!State.heroes[Outfits.Hero(i)].unlocked)continue;
                ids.Add(i);
            }
            return ids;
        }
        void OutfitCollection(){OutfitCollection(-1);}
        void OutfitCollection(int collection)
        {
            int owned=0;for(int i=1;i<=Outfits.Count;i++)if(Outfits.Owned(State,i))owned++;
            var card=Dialog("대원 의상 컬렉션","보유 "+owned+" / "+Outfits.Count+" · 의상마다 동작과 연출 포함\n능력치 변화 없음 · 무료 보석 사용 가능",900);
            RefreshOutfitCatalog(()=>{if(card!=null)OutfitCollection(collection);});
            Ui.Button(card,"시리즈: "+(collection<0?"전체":Outfits.Collections[collection]),24,140,444,60,
                ()=>OutfitCollection(collection+1>=Outfits.Collections.Length?-1:collection+1),Ui.Raised,true,19);
            Ui.Button(card,outfitHeroFilter<0?"대원: 전체":Catalog.HeroNames[outfitHeroFilter],24,214,218,64,
                ()=>{outfitHeroFilter=outfitHeroFilter+1>=HeroCatalog.Count?-1:outfitHeroFilter+1;OutfitCollection(collection);},Ui.Raised,true,18);
            string[] ownership={"전체 의상","보유 의상","장착 가능"};
            Ui.Button(card,ownership[outfitOwnedFilter],254,214,214,64,
                ()=>{outfitOwnedFilter=(outfitOwnedFilter+1)%3;OutfitCollection(collection);},outfitOwnedFilter>0?Ui.Mint:Ui.Raised,true,20);
            var ids=FilteredOutfits(collection);
            Ui.Label(card,ids.Count+"종 표시 · 조건 버튼을 눌러 변경",24,290,444,29,18,Ui.Muted);
            var list=Ui.Scroll(card,24,332,444,462,Math.Max(462,((ids.Count+1)/2)*292));
            if(ids.Count==0)
            {
                Ui.Label(list,"이 조건에 맞는 의상이 없어요.\n다른 대원이나 전체 의상을 선택해 보세요.",12,90,420,85,20,Ui.Muted,TextAnchor.MiddleCenter);
                Ui.Button(list,"필터 초기화",72,210,300,72,()=>{outfitHeroFilter=-1;outfitOwnedFilter=0;OutfitCollection(-1);},Ui.Mint,true,21);
            }
            for(int n=0;n<ids.Count;n++)
            {
                int id=ids[n],hero=Outfits.Hero(id);bool has=Outfits.Owned(State,id),unlocked=State.heroes[hero].unlocked;
                var row=Ui.Button(list,"의상 체험 · "+id,n%2*228,n/2*292,216,280,()=>OutfitRoom(id),Ui.Background).transform;
                // The full card is the touch target; children remain non-blocking.
                row.GetComponentInChildren<UnityEngine.UI.Text>().text="";
                CharacterArt.Draw(row,20,8,176,157,hero,0,id);
                Ui.Label(row,Outfits.Label(id),10,169,196,42,21,OutfitVisuals.Accent(id),TextAnchor.MiddleCenter);
                Ui.Label(row,Outfits.HeroName(id)+" 전용",10,215,196,26,17,Ui.Muted,TextAnchor.MiddleCenter);
                string status=has?(Outfits.Active(State,hero)==id?"장착 중":unlocked?"보유 · 장착 가능":"보유 · 대원 모집 필요"):!unlocked?"대원 모집 후 구매":OutfitSaleLabel(id);
                Ui.Label(row,status,10,247,196,26,17,has?Ui.Mint:Ui.Gold,TextAnchor.MiddleCenter);
            }
            Ui.Button(card,"필터 초기화 · 전체 보기",24,815,444,64,()=>{outfitHeroFilter=-1;outfitOwnedFilter=0;OutfitCollection(-1);},Ui.Raised,true,20);
        }
    }
}
