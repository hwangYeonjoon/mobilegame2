using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace ScrapGuard
{
    public partial class GameApp
    {
        void OutfitRoom(){OutfitRoom(1);}
        void OutfitRoom(int id)
        {
            Track("preview",Outfits.Product(id));
            var card=Dialog(Outfits.Label(id), (id==1?"루크":"레이")+" 전용 의상 · 보석 600개\n외형·연출만 변경 · 대원 별도 · 능력치 변화 없음",880);
            var stage=Ui.Box(card,24,144,444,361,Ui.Background);stage.raycastTarget=true;stage.gameObject.AddComponent<RectMask2D>();
            Ui.Label(stage.transform,"외형 체험 · 드래그로 이동",14,8,416,28,14,Ui.Muted,TextAnchor.MiddleCenter);
            var preview=stage.gameObject.AddComponent<OutfitPreview>();preview.Initialize(id);
            Ui.Button(card,"사격",24,519,140,45,preview.Fire,Ui.Raised,true,17);
            Ui.Button(card,"필살기",176,519,140,45,preview.Ultimate,Ui.Gold,true,17);
            Ui.Button(card,"지원 등장",328,519,140,45,preview.Support,Ui.Raised,true,17);
            Ui.Label(card,id==1?"중장갑 · 리벳 대포 · 크레인 전개\n구리빛 탄환 / 기어 충격파 / 지원 등장 연출":"철도 장갑 · 레일건 · 붉은 스카프\n주황빛 사격 / 레일 섬광 / 지원 등장 연출",24,580,444,62,18,Ui.Ink,TextAnchor.MiddleCenter);
            Button comparison=null;
            comparison=Ui.Button(card,"기본 외형과 비교",24,653,444,43,()=>{preview.Compare();comparison.name=preview.BaseLook?(id==1?"중장비 의상으로 돌아가기":"레일 레인저로 돌아가기"):"기본 외형과 비교";comparison.GetComponentInChildren<Text>().text=comparison.name;},Ui.Raised,true,16);
            bool owned=Outfits.Owned(State,id),equipped=State.selectedOutfit==id;
            bool heroOwned=State.heroes[Outfits.Hero(id)].unlocked;
            Ui.Button(card,owned?(equipped?"의상 해제 · 기존 도색 표시":"전용 대원에 의상 장착"):!heroOwned?"대원을 먼저 모집하세요":"구성 확인하고 구매 · ◆ 600",24,727,444,57,
                ()=>{if(owned)Change(new GameCommand("outfit_equip"){skin=equipped?0:id},()=>OutfitRoom(id));else if(!heroOwned){CloseModal();Show("heroes");}else OutfitPurchase(id);},Ui.Gold,true,18);
            Ui.Label(card,"체험은 재화·기록에 영향을 주지 않아요.\n구매 의상은 정비소 · 전투 · 전시 · 길드 지원에 적용",24,805,444,38,12,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void OutfitPurchase(){OutfitPurchase(1);}
        void OutfitPurchase(int id)
        {
            var card=Dialog("의상을 구매할까요?",Outfits.Label(id)+" 1종 · "+(id==1?"루크":"레이")+" 전용 · 대원 별도",490);
            Ui.Label(card,"보석 600개 차감\n이동·사격·필살기 자세와 전용 연출 포함\n능력치 보너스 없음 · 구매 후 별도 장착",24,128,444,104,18,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Button(card,"보석 600개로 구매",24,255,444,55,()=>Change(new GameCommand("cosmetic_buy"){value=Outfits.Product(id)},()=>OutfitRoom(id)),Ui.Gold,State.gems>=Outfits.Price && State.gemDebt==0 && State.heroes[Outfits.Hero(id)].unlocked,19);
            Ui.Button(card,"무료 보석 획득처 · 부족분 확인",24,323,444,43,()=>GemSources(Outfits.Price,()=>OutfitPurchase(id)),Ui.Raised,true,16);
            Ui.Button(card,"체험으로 돌아가기",24,393,444,43,()=>OutfitRoom(id),Ui.Raised,true,16);
        }
    }
    // Standalone presentation: deliberately holds no GameState, Economy, replay or network object.
    public sealed class OutfitPreview : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        Image actor;RectTransform area;Vector2 position=new Vector2(222,209),target;
        bool moving;float poseUntil;int pose,outfit=1,hero;
        public bool BaseLook { get; private set; }
        public void Compare(){BaseLook=!BaseLook;}
        public void Initialize(int id=1)
        {
            outfit=id;hero=Outfits.Hero(id);area=(RectTransform)transform;
            for(int i=0;i<7;i++)Ui.Box(transform,12,60+i*42,420,1,new Color(.3f,.5f,.5f,.15f),false);
            actor=CharacterArt.Draw(transform,102,84,240,250,hero,0,outfit);
        }
        void Aim(PointerEventData e)
        {
            Vector2 point;if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(area,e.position,e.pressEventCamera,out point))return;
            target=new Vector2(Mathf.Clamp(point.x+area.rect.width*area.pivot.x,122,322),Mathf.Clamp(area.rect.height*(1-area.pivot.y)-point.y,165,225));moving=true;
        }
        public void OnPointerDown(PointerEventData e){Aim(e);}
        public void OnDrag(PointerEventData e){Aim(e);}
        public void OnPointerUp(PointerEventData e){moving=false;}
        public void Fire()
        {
            pose=2;poseUntil=Time.unscaledTime+.5f;
            var shot=Ui.Box(transform,position.x-135,position.y-20,70,8,BaseLook?Ui.Gold:OutfitVisuals.Copper,false);
            shot.gameObject.AddComponent<CombatBurst>().Configure(.45f,.4f,1.3f);
        }
        public void Ultimate(){pose=3;poseUntil=Time.unscaledTime+1;
            if(!BaseLook)OutfitVisuals.Special(transform,new Vector2(position.x,-position.y),135,outfit);
            else {var ring=CombatRing.Create(transform,270,Ui.Mint,.025f);ring.anchoredPosition=new Vector2(position.x,-position.y);ring.gameObject.AddComponent<CombatBurst>().Configure(.9f,.25f,1);}}
        public void Support()
        {
            var helper=CharacterArt.Draw(transform,310,48,100,115,hero,0,BaseLook?0:outfit);
            helper.gameObject.AddComponent<CombatBurst>().Configure(2,1,1);
            if(!BaseLook)OutfitVisuals.Special(transform,new Vector2(360,-107),65,outfit);
        }
        void Update()
        {
            if(actor==null)return;
            if(moving)position=Vector2.MoveTowards(position,target,170*Time.unscaledDeltaTime);
            actor.rectTransform.anchoredPosition=new Vector2(position.x-120,-position.y+125);
            int frame=Time.unscaledTime<poseUntil?pose:moving && Vector2.Distance(position,target)>1 && Mathf.Sin(Time.unscaledTime*11)>0?1:0;
            actor.sprite=CharacterArt.Get(hero,0,BaseLook?0:outfit,frame);
        }
    }
    public static class OutfitVisuals
    {
        public static readonly Color Copper=Ui.Hex("FFAD58");
        public static void Special(Transform parent,Vector2 origin,float radius,int outfit)
        {
            if(outfit==1){Gear(parent,origin,radius);return;}
            var root=Ui.Rect(parent,"RailFlash",origin.x,-origin.y,0,0);
            for(int i=-1;i<=1;i++)Ui.Box(root,-radius,i*14-3,radius*2,6,i==0?Color.white:Copper,false);
            root.gameObject.AddComponent<CombatBurst>().Configure(.65f,.35f,1);
        }
        public static void Gear(Transform parent,Vector2 origin,float radius)
        {
            var root=Ui.Rect(parent,"EngineerGear",origin.x,-origin.y,0,0);
            var ring=CombatRing.Create(root,radius*2,Copper,.03f);ring.anchoredPosition=Vector2.zero;
            for(int i=0;i<12;i++)
            {
                float angle=i*Mathf.PI/6;
                var tooth=Ui.Box(root,Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,13,22,Copper,false).rectTransform;
                tooth.pivot=new Vector2(.5f,.5f);tooth.localEulerAngles=new Vector3(0,0,-angle*Mathf.Rad2Deg-90);
            }
            root.gameObject.AddComponent<CombatBurst>().Configure(.9f,.25f,1);
        }
    }
}
