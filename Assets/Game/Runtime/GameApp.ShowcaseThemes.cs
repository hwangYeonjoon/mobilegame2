using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public static class ShowcaseStage
    {
        public static RectTransform Draw(Transform parent,float x,float y,float w,float h,int theme,Color accent)
        {
            if(theme==0||!ShowcaseThemes.Valid(theme))return WorkshopStage.Draw(parent,x,y,w,h,accent);
            var root=Ui.Box(parent,x,y,w,h,Color.white).rectTransform;
            root.GetComponent<Image>().sprite=ArenaArt.Cell("ShowcaseThemes-v1",2,2,(theme-1)%2,(theme-1)/2);
            root.gameObject.AddComponent<RectMask2D>();
            Ui.Box(root,0,0,w,h,new Color(.02f,.04f,.07f,.12f),false);
            if(theme>=3)Ui.Label(root,theme==3?"06":"24",w-54,12,42,28,20,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Box(root,w*.3f,h*.96f,w*.4f,3,accent,false);
            return root;
        }
    }
    public partial class GameApp
    {
        void ThemeCollection(int selected=0)
        {
            selected=Mathf.Clamp(selected,0,ShowcaseThemes.Names.Length-1);
            bool owned=ShowcaseThemes.Owned(State,selected);int theme=selected;
            var card=Dialog("전시 배경 컬렉션","모든 대원·의상과 조합 가능 · 전투 능력치 변화 없음",850);
            var stage=ShowcaseStage.Draw(card,24,140,444,288,selected,Ui.Mint);
            CharacterArt.Draw(stage,112,selected==0?10:-7,220,selected==0?260:240,State.showcaseHero,State.selectedSkin,Outfits.Active(State,State.showcaseHero));
            Ui.Label(card,ShowcaseThemes.Names[selected],24,448,444,38,28,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Label(card,ShowcaseThemes.Goal(selected),24,495,444,38,20,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Button(card,"이전 배경",24,551,214,62,()=>ThemeCollection((theme+ShowcaseThemes.Names.Length-1)%ShowcaseThemes.Names.Length),Ui.Raised,true,21);
            Ui.Button(card,"다음 배경",254,551,214,62,()=>ThemeCollection((theme+1)%ShowcaseThemes.Names.Length),Ui.Raised,true,21);
            Ui.Button(card,owned?State.showcaseTheme==selected?"사용 중":"이 배경으로 전시":selected<=2?"구성 확인 · 보석 400":"작전 완료로 획득",24,638,444,68,()=>{
                if(owned)Change(new GameCommand("showcase_theme"){skin=theme},confirmed:()=>Show("showcase"));
                else ThemePurchase(theme);
            },Ui.Gold,owned||selected<=2,22);
            Ui.Button(card,"전시실로",24,737,444,64,()=>Show("showcase"),Ui.Raised,true,21);
        }
        void ThemePurchase(int id)
        {
            if(id<1||id>2)return;
            var card=Dialog("전시 배경 구매",ShowcaseThemes.Names[id]+" · 배경만 포함 · 대원과 의상은 별도",540);
            Ui.Label(card,"모든 대원에 사용 · 능력치 변화 없음\n필요 보석 400 · 보유 "+State.gems+"\n구매 후 잔액 "+Mathf.Max(0,State.gems-400),24,155,444,140,23,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Button(card,"보석 400개로 배경 구매",24,330,444,68,()=>Change(new GameCommand("cosmetic_buy"){value=ShowcaseThemes.Keys[id]},confirmed:()=>ThemeCollection(id)),Ui.Gold,State.gems>=400&&State.gemDebt==0&&!ShowcaseThemes.Owned(State,id),22);
            Ui.Button(card,"체험으로 돌아가기",24,427,444,64,()=>ThemeCollection(id),Ui.Raised,true,20);
        }
    }
}
