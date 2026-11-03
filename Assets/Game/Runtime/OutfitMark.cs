using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ScrapGuard
{
    // Small, bounded meshes: silhouettes and projectiles remain distinct without particle spam.
    public sealed class OutfitMark : MaskableGraphic
    {
        int outfit;bool projectile;
        public int Outfit { get { return outfit; } }
        public static OutfitMark Attach(Image actor,int id)
        {
            var rect=Ui.Rect(actor.transform.parent,"Outfit silhouette",0,0,0,0);
            var mark=rect.gameObject.AddComponent<OutfitMark>();mark.raycastTarget=false;mark.Configure(id,false);
            actor.gameObject.AddComponent<OutfitAttachment>().Initialize(mark);return mark;
        }
        public void Configure(int id,bool shot)
        {
            if(outfit!=id || projectile!=shot){outfit=id;projectile=shot;SetVerticesDirty();}
            enabled=Outfits.Valid(id);raycastTarget=false;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!Outfits.Valid(outfit))return;
            Color tint=projectile?color:OutfitVisuals.Accent(outfit);
            if(projectile)
            {
                if(outfit==1){Poly(vh,tint,.60f,.3f,.83f,.15f,1,.5f,.83f,.85f,.60f,.7f);Line(vh,tint,.08f,.5f,.65f,.5f,.09f);}
                if(outfit==2){Line(vh,tint,0,.26f,.88f,.26f,.07f);Line(vh,tint,0,.74f,.88f,.74f,.07f);Poly(vh,Color.white,.72f,.15f,1,.5f,.72f,.85f,.85f,.5f);}
                if(outfit==3){Poly(vh,tint,0,.7f,.43f,.36f,.38f,.68f,.85f,.18f,1,.45f,.65f,.7f,.68f,.40f,.18f,.94f);}
                if(outfit==4){Poly(vh,tint,.37f,.5f,.7f,.12f,1,.5f,.7f,.88f);Line(vh,Color.white,.45f,.5f,.9f,.5f,.045f);Line(vh,tint,0,.5f,.27f,.5f,.06f);}
                return;
            }
            // Keep ornamentation at the shoulders/back, leaving the face and weapon readable.
            if(outfit==1)
            {
                Line(vh,tint,.12f,.45f,.08f,.64f,.007f);Line(vh,tint,.08f,.64f,.23f,.74f,.007f);
                Line(vh,Ui.Hex("33434D"),.75f,.62f,.75f,.85f,.025f);Line(vh,tint,.75f,.62f,.75f,.85f,.009f);
                Line(vh,Ui.Hex("33434D"),.82f,.59f,.82f,.78f,.024f);Line(vh,tint,.82f,.59f,.82f,.78f,.008f);
            }
            if(outfit==2)
            {
                var scarf=Ui.Hex("DB705C");
                Line(vh,scarf,.65f,.68f,.90f,.57f,.008f);Line(vh,scarf,.90f,.57f,.83f,.45f,.007f);
                Line(vh,scarf,.83f,.45f,.95f,.35f,.006f);
            }
            if(outfit==3)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    Line(vh,tint,.5f+side*.20f,.56f,.5f+side*.42f,.80f,.009f);
                    Line(vh,tint,.5f+side*.42f,.80f,.5f+side*.31f,.54f,.005f);
                    Line(vh,tint,.5f+side*.28f,.39f,.5f+side*.45f,.63f,.008f);
                }
            }
            if(outfit==4)
                for(int side=-1;side<=1;side+=2)
                {
                    Line(vh,tint,.5f+side*.18f,.51f,.5f+side*.32f,.82f,.005f);
                    Line(vh,tint,.5f+side*.32f,.82f,.5f+side*.43f,.60f,.005f);
                    Line(vh,tint,.5f+side*.43f,.60f,.5f+side*.30f,.39f,.005f);
                    Line(vh,Ui.Hex("CBFAFF"),.5f+side*.32f,.80f,.5f+side*.30f,.41f,.004f);
                }
        }
        Vector2 Point(float x,float y){var r=rectTransform.rect;return new Vector2(r.xMin+x*r.width,r.yMin+y*r.height);}
        void Poly(VertexHelper vh,Color c,params float[] xy)
        {
            int n=vh.currentVertCount;for(int i=0;i<xy.Length;i+=2)vh.AddVert(Point(xy[i],xy[i+1]),c,Vector2.zero);
            for(int i=1;i<xy.Length/2-1;i++)vh.AddTriangle(n,n+i,n+i+1);
        }
        void Line(VertexHelper vh,Color c,float x,float y,float xx,float yy,float width)
        {
            Vector2 a=Point(x,y),b=Point(xx,yy),d=(b-a).normalized;
            Vector2 normal=new Vector2(-d.y,d.x)*Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*width;
            int n=vh.currentVertCount;vh.AddVert(a-normal,c,Vector2.zero);vh.AddVert(a+normal,c,Vector2.zero);vh.AddVert(b+normal,c,Vector2.zero);vh.AddVert(b-normal,c,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
    }
    public sealed class OutfitAttachment : MonoBehaviour
    {
        public OutfitMark Mark { get; private set; }
        public void Initialize(OutfitMark mark){Mark=mark;Sync();}
        void LateUpdate(){Sync();}
        void Sync()
        {
            if(Mark==null)return;
            var source=(RectTransform)transform;var target=Mark.rectTransform;
            target.anchorMin=source.anchorMin;target.anchorMax=source.anchorMax;target.pivot=source.pivot;
            target.sizeDelta=source.sizeDelta;target.anchoredPosition=source.anchoredPosition;target.localScale=source.localScale;target.localRotation=source.localRotation;
            // The sprite owns the foreground; ornaments never paint over its face or equipment.
            if(target.GetSiblingIndex()!=source.GetSiblingIndex()-1)
            {int index=source.GetSiblingIndex();if(target.GetSiblingIndex()<index)index--;target.SetSiblingIndex(index);}
            Mark.gameObject.SetActive(gameObject.activeInHierarchy);
        }
        void OnDisable(){if(Mark!=null)Mark.gameObject.SetActive(false);}
        void OnDestroy(){if(Mark!=null){if(Application.isPlaying)Destroy(Mark.gameObject);else DestroyImmediate(Mark.gameObject);}}
    }
    public static class StylePresentation
    {
        public const float PlayerScale=.88f;
        public static string Look(int outfit,int skin){return Outfits.Valid(outfit)?Outfits.Label(outfit):ScrapGuard.Core.Social.Skins[Mathf.Clamp(skin,0,ScrapGuard.Core.Social.Skins.Length-1)];}
        public static void SetLook(Image image,int hero,int skin,int outfit,int pose)
        {
            image.sprite=CharacterArt.Get(hero,skin,outfit,pose);
            image.color=Outfits.Valid(outfit)?Color.white:CharacterArt.Tint(skin);
            var attachment=image.GetComponent<OutfitAttachment>();if(attachment!=null)attachment.Mark.Configure(outfit,false);
        }
        public static void Shot(Transform parent,Vector2 origin,int outfit,bool flipped=false)
        {
            var root=BulletVisual.Create(parent);
            root.GetComponent<BulletVisual>().Render(new Bullet{position=new V2(origin.x,-origin.y),velocity=new V2(flipped?-1:1,0),chilling=outfit==4},1,outfit,0);
            root.gameObject.AddComponent<PreviewProjectile>().Initialize(flipped?-1:1);
        }
        public static RectTransform Arrival(Transform parent,float x,float y,string name,int hero,int skin,int outfit,string subtitle,Combat clock=null)
        {
            var accent=Outfits.Valid(outfit)?OutfitVisuals.Accent(outfit):Ui.Mint;
            var card=Ui.Box(parent,x,y,414,90,Ui.Hex("132633"),true,"Support entrance").rectTransform;
            Ui.Box(card,0,0,4,90,accent,false);
            CharacterArt.Draw(card,8,-3,96,96,hero,skin,outfit);
            Ui.Label(card,name,112,7,288,25,19,Ui.Ink);
            Ui.Label(card,Look(outfit,skin),112,35,288,24,16,accent);
            Ui.Label(card,subtitle,112,63,288,20,12,Ui.Muted);
            card.gameObject.AddComponent<SupportEntrance>().Initialize(clock);
            return card;
        }
    }
    public sealed class SupportEntrance : MonoBehaviour
    {
        Combat clock;float previous,age;Vector2 origin;CanvasGroup group;
        public void Initialize(Combat source)
        {
            clock=source;previous=source==null?0:source.Elapsed;origin=((RectTransform)transform).anchoredPosition;
            group=gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
        }
        void Update()
        {
            if(group==null)return;
            age+=clock==null?Time.unscaledDeltaTime:Mathf.Max(0,clock.Elapsed-previous);if(clock!=null)previous=clock.Elapsed;
            float enter=Mathf.Clamp01(age/.22f),leave=Mathf.Clamp01((age-2.4f)/.35f);
            ((RectTransform)transform).anchoredPosition=origin+Vector2.left*(1-enter)*(1-enter)*65;
            group.alpha=enter*(1-leave);if(age>=2.75f)Destroy(gameObject);
        }
    }
    public sealed class PreviewProjectile : MonoBehaviour
    {
        float age;int direction;
        public void Initialize(int value){direction=value;}
        void Update(){age+=Time.unscaledDeltaTime;((RectTransform)transform).anchoredPosition+=Vector2.right*(direction*170*Time.unscaledDeltaTime);if(age>.55f)Destroy(gameObject);}
    }
}
