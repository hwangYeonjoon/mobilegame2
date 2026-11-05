using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
using Social = ScrapGuard.Core.Social;

namespace ScrapGuard
{
    public partial class GameApp
    {
        int featuredOutfit=8;
        void StyleShop()
        {
            var currentScreen=screen;RefreshOutfitCatalog(()=>{if(currentScreen!=null&&currentScreen==screen)Show("shop");});
            var list=ShopScroll(258,MenuBottom-258,1190,3);
            var featured=WorkshopStage.Draw(list,0,0,492,394,Ui.Gold);
            int outfit=featuredOutfit;
            Ui.Label(featured,Outfits.Collections[Outfits.Collection(outfit)],22,14,448,30,18,Ui.Gold);
            Ui.Label(featured,Outfits.Label(outfit),22,57,245,72,30,Ui.Ink);
            Ui.Label(featured,Outfits.Details[outfit].Replace(" · ","\n"),22,138,220,85,19,Ui.Muted);
            Ui.Label(featured,Outfits.HeroName(outfit)+" 전용\n능력치 변화 없음",22,238,210,58,18,Ui.Gold);
            var art=CharacterArt.Draw(featured,230,42,246,264,Outfits.Hero(outfit),0,outfit);
            art.gameObject.AddComponent<ShowroomMotion>();
            bool owned=Outfits.Owned(State,outfit);
            Ui.Button(featured,owned?"보유 의상 체험 · 장착":OutfitForSale(outfit)?"움직여 보기 · 보석 600":OutfitSaleLabel(outfit),22,310,448,68,()=>OutfitRoom(outfit),Ui.Gold,true,22);
            Ui.Label(list,"다른 모습도 골라 보세요",0,413,492,34,23);
            for(int i=0;i<3;i++)
            {
                int id=(featuredOutfit+i)%Outfits.Count+1;float x=i*168;
                var thumb=Ui.Button(list,"",x,462,156,218,()=>{featuredOutfit=id;shopOffsets[3]=0;Show("shop");},Ui.Panel).transform;
                CharacterArt.Draw(thumb,9,7,138,136,Outfits.Hero(id),0,id);
                Ui.Label(thumb,Outfits.Label(id),8,148,140,61,18,Ui.Ink,TextAnchor.MiddleCenter);
            }
            Ui.Button(list,"전체 의상 · 대원별 / 보유별 찾기",0,699,492,72,OutfitCollection,Ui.Gold,true,21);
            var earned=Ui.Box(list,0,792,238,275,Ui.Panel).transform;
            CharacterArt.Draw(earned,117,10,112,122,State.showcaseHero,2);
            Ui.Label(earned,"빙하의\n수호자",14,18,118,69,22,Ui.Blue);
            Ui.Label(earned,"플레이로 획득\n처치 "+Mathf.Min(1000,State.totalKills)+"/1,000",14,129,210,62,18,Ui.Muted);
            Ui.Button(earned,"서리 도색 보기",12,199,214,64,()=>Wardrobe(2),Ui.Raised,true,20);
            var frame=Ui.Box(list,254,792,238,275,Ui.Panel).transform;
            Ui.Box(frame,145,20,70,102,Ui.Gold);Ui.Box(frame,149,24,62,94,Ui.Background);
            CharacterArt.Draw(frame,151,27,58,88,State.showcaseHero,State.selectedSkin);
            Ui.Label(frame,"태양의\n테두리",14,18,128,69,22,Ui.Gold);
            Ui.Label(frame,"전시 프레임\n보석 250 · 확정 구성",14,129,210,62,18,Ui.Muted);
            Ui.Button(frame,Social.HasFrame(State,3)?"전시실에서 적용":"프레임 살펴보기",12,199,214,64,
                ()=>{if(Social.HasFrame(State,3))CustomizeProfile();else CosmeticOffer("frame_solar");},Ui.Raised,true,19);
            Ui.Button(list,"전체 대원 공용 · 앰버 도색",0,1088,492,72,()=>Wardrobe(1),Ui.Raised,true,21);
        }
        void Wardrobe(int skin, int hero = -1)
        {
            if(skin>=3){RegionCollection(skin-3);return;}
            if (hero < 0) hero = State.showcaseHero;
            int previewHero = hero;
            var card = Dialog("스타일 피팅룸", "미리보기는 재화와 보유 상태를 바꾸지 않아요.", 880);
            for (int i = 0; i < HeroCatalog.Count; i++)
            {
                int next = i;
                Ui.Button(card,Catalog.HeroNames[i].Split(' ')[1],24+i*(450f/HeroCatalog.Count),130,450f/HeroCatalog.Count-6,43,
                    () => Wardrobe(skin,next),i == hero ? Ui.Mint : Ui.Raised,true,17);
            }
            Color accent = skin == 1 ? Ui.Gold : skin == 2 ? Ui.Blue : Ui.Mint;
            var stage = Ui.Box(card,24,188,444,337,Ui.Background).transform;
            Ui.Label(stage,skin == 1 ? "SOLAR FORGE" : skin == 2 ? "GLACIER" : "ORIGINAL",18,13,408,27,12,accent,TextAnchor.MiddleCenter);
            Ui.Disc(stage,111,62,222,new Color(accent.r,accent.g,accent.b,.09f));
            Ui.Box(stage,60,300,324,2,new Color(accent.r,accent.g,accent.b,.32f),false);
            var art = CharacterArt.Draw(stage,73,43,298,265,hero,skin);
            art.gameObject.AddComponent<ShowroomMotion>();
            Ui.Label(card,Social.Skins[skin],24,537,444,42,27,accent,TextAnchor.MiddleCenter);
            string[] details = { "현장 정비사의 개성이 담긴 오리지널 장갑", "황금 장갑 · 오렌지 코어 · 태양 문양 장식", "수정 장갑 · 푸른 코어 · 얼음 방열판" };
            Ui.Label(card,details[skin],24,581,444,30,15,Ui.Muted,TextAnchor.MiddleCenter);
            string[] paintNames={"기본", "앰버", "서리"};
            for (int i = 0; i < paintNames.Length; i++)
            {
                int next = i;
                Ui.Button(card,paintNames[i],24+i*150,631,144,48,
                    () => Wardrobe(next,previewHero),i == skin ? accent : Ui.Raised,true,17);
            }
            bool owned = Social.HasSkin(State,skin);
            string detail = owned ? "보유 중 · 전체 대원 공용 · 전투 능력치 변화 없음" : skin == 1 ? "보석 300개 · 도색 1종 확정 · 전체 대원 공용" : "적 1,000마리 처치 시 무료 획득 · 현재 " + State.totalKills;
            Ui.Label(card,detail,24,696,444,42,14,Ui.Muted,TextAnchor.MiddleCenter);
            if (owned)
                Ui.Button(card,State.selectedSkin == skin ? "이 도색을 사용 중이에요" : "내 수호대에 적용",24,761,444,61,
                    () => Change(new GameCommand("customize") { hero = State.showcaseHero, title = State.showcaseTitle, frame = State.showcaseFrame, skin = skin },() => Wardrobe(skin,previewHero)),null,State.selectedSkin != skin,19);
            else if (skin == 1)
                Ui.Button(card,"구성 확인하고 구매",24,761,444,61,() => CosmeticOffer("skin_amber"),Ui.Gold,true,19);
            else
                Ui.Button(card,"플레이로 획득하는 도색",24,761,444,61,null,Ui.Raised,false,18);
            Ui.Label(card,Outfits.Active(State,hero)>0?"전용 의상이 우선 표시돼요. 의상실에서 해제할 수 있어요.":"정비소 · 전투 · 전시 카드에 같은 외형으로 적용됩니다.",24,831,444,25,12,Ui.Muted,TextAnchor.MiddleCenter);
        }
    }
    public sealed class ShowroomMotion : MonoBehaviour
    {
        RectTransform rect;
        Vector2 origin;
        void Awake() { rect = (RectTransform)transform; origin = rect.anchoredPosition; }
        void Update()
        {
            float t = Time.unscaledTime;
            rect.anchoredPosition = origin + new Vector2(0,Mathf.Sin(t*1.7f)*3);
            rect.localEulerAngles = new Vector3(0,0,Mathf.Sin(t*.85f)*1.1f);
        }
    }
}
