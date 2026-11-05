using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ScrapGuard
{
    // Small, bounded meshes: silhouettes and projectiles remain distinct without particle spam.
    public sealed class OutfitMark : MaskableGraphic
    {
        int outfit;bool projectile,compact;
        public bool Compact { get { return compact; } set { if(compact!=value){compact=value;SetVerticesDirty();} } }
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
                Color highlight=new Color(1,1,1,tint.a);
                if(outfit==1){Poly(vh,tint,.60f,.3f,.83f,.15f,1,.5f,.83f,.85f,.60f,.7f);Line(vh,tint,.08f,.5f,.65f,.5f,.09f);}
                if(outfit==2){Line(vh,tint,0,.26f,.88f,.26f,.07f);Line(vh,tint,0,.74f,.88f,.74f,.07f);Poly(vh,Color.white,.72f,.15f,1,.5f,.72f,.85f,.85f,.5f);}
                if(outfit==3){Poly(vh,tint,0,.7f,.43f,.36f,.38f,.68f,.85f,.18f,1,.45f,.65f,.7f,.68f,.40f,.18f,.94f);}
                if(outfit==4){Poly(vh,tint,.37f,.5f,.7f,.12f,1,.5f,.7f,.88f);Line(vh,Color.white,.45f,.5f,.9f,.5f,.045f);Line(vh,tint,0,.5f,.27f,.5f,.06f);}
                if(outfit==5){Poly(vh,tint,.35f,.22f,.75f,.22f,1,.5f,.75f,.78f,.35f,.78f,.15f,.5f);Line(vh,highlight,.43f,.38f,.75f,.38f,.025f);Line(vh,highlight,.59f,.38f,.59f,.65f,.025f);}
                if(outfit==6){Oval(vh,tint,.65f,.5f,.27f,.34f,.045f);Line(vh,highlight,.32f,.5f,1,.5f,.025f);Line(vh,tint,0,.3f,.26f,.3f,.035f);Line(vh,tint,0,.7f,.26f,.7f,.035f);}
                if(outfit==7){for(int i=0;i<3;i++){float x=i*.28f;Line(vh,tint,x,.2f,x+.27f,.5f,.04f);Line(vh,tint,x+.27f,.5f,x,.8f,.04f);}}
                if(outfit==8)
                {
                    Poly(vh,highlight,.45f,.5f,.65f,.3f,.85f,.5f,.65f,.7f);
                    for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(vh,tint,.65f+Mathf.Cos(a)*.23f,.5f+Mathf.Sin(a)*.23f,.65f+Mathf.Cos(a)*.34f,.5f+Mathf.Sin(a)*.42f,.025f);}
                    Line(vh,tint,0,.5f,.3f,.5f,.04f);
                }
                if(outfit==9)
                    for(int i=0;i<3;i++)Oval(vh,tint,.25f+i*.25f,.5f,.10f,.16f+i*.12f,.027f);
                if(outfit==10)
                {
                    Poly(vh,tint,.12f,.1f,1,.5f,.12f,.9f);
                    for(int i=0;i<3;i++){float x=.24f+i*.19f;Line(vh,highlight,x,.25f+i*.08f,x+.12f,.75f-i*.08f,.024f);}
                }
                return;
            }
            if(compact){CompactSilhouette(vh,tint);return;}
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
            if(outfit==5)
                for(int side=-1;side<=1;side+=2)
                {
                    float x=.5f+side*.32f;Line(vh,tint,x,.43f,x,.87f,.008f);
                    Poly(vh,tint,x,.86f,x+side*.13f,.84f,x+side*.13f,.71f,x,.74f);
                }
            if(outfit==6){Oval(vh,tint,.5f,.57f,.40f,.34f,.006f);Line(vh,tint,.05f,.48f,.05f,.72f,.014f);Line(vh,tint,.95f,.48f,.95f,.72f,.014f);}
            if(outfit==7)
                for(int i=0;i<2;i++)
                {float y=.62f-i*.12f;Line(vh,tint,.68f,y,.96f,y-.08f,.008f);Line(vh,tint,.96f,y-.08f,.80f,y-.21f,.006f);}
            if(outfit==8)
                for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(vh,tint,.5f+Mathf.Cos(a)*.32f,.58f+Mathf.Sin(a)*.29f,.5f+Mathf.Cos(a)*.40f,.58f+Mathf.Sin(a)*.36f,.006f);}
            if(outfit==9)
                for(int side=-1;side<=1;side+=2)
                {
                    float x=.5f+side*.34f;Line(vh,tint,x,.54f,x,.80f,.005f);Oval(vh,tint,x,.82f,.025f,.025f,.005f);
                }
            if(outfit==10)
                for(int side=-1;side<=1;side+=2)
                {
                    Line(vh,tint,.5f+side*.20f,.66f,.5f+side*.42f,.55f,.008f);
                    Line(vh,tint,.5f+side*.42f,.55f,.5f+side*.43f,.29f,.008f);
                }
        }
        void CompactSilhouette(VertexHelper vh,Color tint)
        {
            // Fewer, thicker strokes at battle size; face/core/weapon stay unobscured.
            if(outfit==1){Line(vh,tint,.76f,.60f,.76f,.87f,.017f);Line(vh,tint,.86f,.58f,.86f,.80f,.014f);}
            if(outfit==2)Poly(vh,Ui.Hex("DB705C"),.66f,.69f,.95f,.58f,.83f,.43f,.89f,.59f);
            if(outfit==3||outfit==4)for(int side=-1;side<=1;side+=2)
            {
                float x=.5f+side*.2f,xx=.5f+side*.43f;
                Line(vh,tint,x,.53f,xx,.83f,.015f);
                Line(vh,tint,xx,.83f,.5f+side*(outfit==3?.32f:.45f),.42f,.012f);
            }
            if(outfit==5)for(int side=-1;side<=1;side+=2)
            {float x=.5f+side*.32f;Line(vh,tint,x,.45f,x,.87f,.015f);Poly(vh,tint,x,.86f,x+side*.14f,.84f,x+side*.14f,.72f,x,.74f);}
            if(outfit==6){Oval(vh,tint,.5f,.57f,.42f,.35f,.012f);Line(vh,tint,.06f,.48f,.06f,.72f,.025f);Line(vh,tint,.94f,.48f,.94f,.72f,.025f);}
            if(outfit==7)for(int i=0;i<2;i++){float y=.66f-i*.17f;Line(vh,tint,.69f,y,.96f,y-.08f,.018f);Line(vh,tint,.96f,y-.08f,.81f,y-.19f,.016f);}
            if(outfit==8)for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Line(vh,tint,.5f+Mathf.Cos(a)*.32f,.58f+Mathf.Sin(a)*.29f,.5f+Mathf.Cos(a)*.43f,.58f+Mathf.Sin(a)*.39f,.018f);}
            if(outfit==9)for(int side=-1;side<=1;side+=2){float x=.5f+side*.36f;Line(vh,tint,x,.52f,x,.8f,.014f);Oval(vh,tint,x,.83f,.04f,.04f,.014f);}
            if(outfit==10)for(int side=-1;side<=1;side+=2)Poly(vh,tint,.5f+side*.20f,.66f,.5f+side*.44f,.57f,.5f+side*.44f,.27f,.5f+side*.39f,.52f);
        }
        void Oval(VertexHelper vh,Color c,float x,float y,float rx,float ry,float width)
        {
            for(int i=0;i<16;i++){float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;Line(vh,c,x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry,x+Mathf.Cos(b)*rx,y+Mathf.Sin(b)*ry,width);}
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
            Mark.Compact=source.rect.height*Mathf.Abs(source.lossyScale.y)<100;
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
            root.GetComponent<BulletVisual>().Render(new Bullet{position=new V2(origin.x,-origin.y),velocity=new V2(flipped?-1:1,0),chilling=Outfits.Valid(outfit)&&Outfits.Hero(outfit)==4},1,outfit,0);
            root.gameObject.AddComponent<PreviewProjectile>().Initialize(flipped?-1:1);
        }
        public static RectTransform Arrival(Transform parent,float x,float y,string name,int hero,int skin,int outfit,string subtitle,Combat clock=null)
        {
            var accent=Outfits.Valid(outfit)?OutfitVisuals.Accent(outfit):Ui.Mint;
            var card=Ui.Box(parent,x,y,414,90,Ui.Hex("132633"),true,"Support entrance").rectTransform;
            Ui.Box(card,0,0,4,90,accent,false);
            card.gameObject.AddComponent<RectMask2D>();
            var motif=Ui.Rect(card,"Arrival signature",309,6,96,78).gameObject.AddComponent<OutfitMark>();
            motif.Configure(outfit,true);motif.color=new Color(accent.r,accent.g,accent.b,.16f);
            CharacterArt.Draw(card,8,-3,96,96,hero,skin,outfit);
            Ui.Label(card,name,112,7,288,25,19,Ui.Ink);
            Ui.Label(card,Look(outfit,skin)+(Outfits.Valid(outfit)?" · "+Outfits.Arrivals[outfit]:""),112,35,288,24,14,accent);
            Ui.Label(card,subtitle,112,63,288,20,12,Ui.Muted);
            card.gameObject.AddComponent<SupportEntrance>().Initialize(clock,outfit,motif.rectTransform);
            return card;
        }
    }
    public sealed class SupportEntrance : MonoBehaviour
    {
        Combat clock;float previous,age;Vector2 origin,motifOrigin;CanvasGroup group;int outfit;RectTransform motif;
        public void Initialize(Combat source,int style=0,RectTransform ornament=null)
        {
            outfit=style;motif=ornament;if(motif!=null)motifOrigin=motif.anchoredPosition;
            clock=source;previous=source==null?0:source.Elapsed;origin=((RectTransform)transform).anchoredPosition;
            group=gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
        }
        void Update()
        {
            if(group==null)return;
            age+=clock==null?Time.unscaledDeltaTime:Mathf.Max(0,clock.Elapsed-previous);if(clock!=null)previous=clock.Elapsed;
            float enter=Mathf.Clamp01(age/.22f),leave=Mathf.Clamp01((age-2.4f)/.35f);
            float ease=(1-enter)*(1-enter);
            ((RectTransform)transform).anchoredPosition=origin+(outfit==5||outfit==10?Vector2.up*(outfit==10?60:35):outfit==6||outfit==9?Vector2.down*(outfit==9?45:25):Vector2.left*(outfit==7?110:65))*ease;
            if(motif!=null)
            {
                if(outfit==6)motif.localEulerAngles=new Vector3(0,0,age*55);
                else if(outfit==8)motif.localEulerAngles=new Vector3(0,0,-age*80);
                else if(outfit==9)motif.localScale=Vector3.one*(.85f+Mathf.Sin(age*3)*.15f);
                else if(outfit==10)motif.anchoredPosition=motifOrigin+Vector2.down*(Mathf.Sin(age*4)*7);
                else if(outfit==7)motif.anchoredPosition=motifOrigin+Vector2.right*Mathf.Sin(age*5)*14;
                else motif.localScale=Vector3.one*(1+Mathf.Sin(age*3)*.08f);
            }
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
