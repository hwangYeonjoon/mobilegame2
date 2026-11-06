using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ScrapGuard
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AmbientLight : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            vh.AddVert(r.center,color,Vector2.zero);var edge=color;edge.a=0;
            for(int i=0;i<=40;i++)
            {
                float angle=i*Mathf.PI/20;
                vh.AddVert(r.center+new Vector2(Mathf.Cos(angle)*r.width*.5f,Mathf.Sin(angle)*r.height*.5f),edge,Vector2.zero);
                if(i>0)vh.AddTriangle(0,i,i+1);
            }
        }
    }
    public sealed class EnemyVisual : MonoBehaviour
    {
        Image body,shadow;
        Text mark;
        Vector2 previous;
        float lastFlash,movedAt=-1;
        bool initialized,raging;
        public int Pose { get; private set; }
        public bool JustHit { get; private set; }
        public bool JustRaged { get; private set; }
        public static RectTransform Create(Transform parent)
        {
            var root=Ui.Rect(parent,"Enemy actor",0,0,0,0);
            var visual=root.gameObject.AddComponent<EnemyVisual>();
            visual.shadow=Ui.Disc(root,-15,-1,30,new Color(0,0,0,.35f));
            visual.body=Ui.Box(root,0,0,60,60,Color.white,false);visual.body.preserveAspect=true;
            visual.body.rectTransform.pivot=new Vector2(.5f,.5f);
            var outline=visual.body.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.025f,.045f,.07f,.9f);outline.effectDistance=new Vector2(1.25f,-1.25f);
            visual.mark=Ui.Label(root,"",-22,-45,44,24,18,Ui.Gold,TextAnchor.MiddleCenter);
            return root;
        }
        void OnDisable(){ResetState();}
        public void ResetState(){initialized=false;lastFlash=0;movedAt=-1;raging=false;JustHit=false;JustRaged=false;}
        public void Render(Enemy enemy,int region,float time)
        {
            bool boss=enemy.finalBoss||enemy.core;
            var position=new Vector2(enemy.position.x,-enemy.position.y);
            float distance=initialized?(position-previous).sqrMagnitude:0;
            if(distance>.001f)movedAt=time;
            bool moving=initialized && time-movedAt<.12f && enemy.freezeTime<=0;
            Pose=enemy.windup>0?1:enemy.attackFlash>0||enemy.dash>0?2:moving && (int)(time*9+enemy.id)%2==1?3:0;
            float size=enemy.Radius*(boss?4.1f:3.9f);
            body.sprite=BattleArt.Role(enemy.core?7:boss?region:(int)enemy.role,Pose,boss);
            body.rectTransform.sizeDelta=new Vector2(size,size);
            float bounce=moving?Mathf.Abs(Mathf.Sin(time*10+enemy.id))*2:0;
            body.rectTransform.anchoredPosition=new Vector2(0,6+bounce+(Pose==1?-2:Pose==2?3:0));
            body.rectTransform.localScale=Pose==1?new Vector3(1.08f,.92f,1):enemy.flash>0?new Vector3(1.08f,.94f,1):Pose==2?new Vector3(.96f,1.06f,1):Vector3.one;
            body.rectTransform.localEulerAngles=new Vector3(0,0,moving?Mathf.Sin(time*10+enemy.id)*(boss?1.5f:3):0);
            JustHit=enemy.flash>lastFlash+.005f;lastFlash=enemy.flash;
            bool rage=boss && !enemy.core && enemy.hp<enemy.maxHp*.5f;
            JustRaged=rage&&!raging;raging=rage;
            body.color=enemy.flash>0?new Color(1,.65f,.52f):enemy.freezeTime>0?Ui.Hex("7CEAFF"):enemy.slowTime>0?Ui.Hex("BAEFFF"):rage?new Color(1,.87f,.81f):Color.white;
            shadow.rectTransform.sizeDelta=new Vector2(enemy.Radius*2,enemy.Radius*.58f);
            shadow.rectTransform.anchoredPosition=new Vector2(-enemy.Radius,enemy.Radius*.12f);
            shadow.color=new Color(0,0,0,enemy.windup>0?.5f:.32f);
            mark.text=enemy.windup>0?"!":rage?"Ⅱ":enemy.kind==2?"◆":"";
            mark.color=enemy.windup>0?Ui.Red:Ui.Gold;
            mark.rectTransform.anchoredPosition=new Vector2(-22,size*.5f+16);
            ((RectTransform)transform).anchoredPosition=position;
            previous=position;initialized=true;
        }
    }

    public sealed class BulletVisual : MonoBehaviour
    {
        Image trail,core,head;OutfitMark signature;
        public int Signature { get { return signature.enabled?signature.Outfit:0; } }
        public static RectTransform Create(Transform parent)
        {
            var root=Ui.Rect(parent,"Projectile",0,0,0,0);
            var v=root.gameObject.AddComponent<BulletVisual>();
            v.trail=Ui.Box(root,-28,-3,28,6,Color.white,false);
            v.head=Ui.Disc(root,-6,-4,8,Color.white);
            v.core=Ui.Box(root,-9,-1,10,2,Color.white,false);
            var shape=Ui.Rect(root,"Outfit projectile",-34,-7,38,14);
            v.signature=shape.gameObject.AddComponent<OutfitMark>();v.signature.raycastTarget=false;return root;
        }
        public void Render(Bullet bullet,int level,int outfit,float time,bool compact=false)
        {
            var root=(RectTransform)transform;
            bool styled=Outfits.Valid(outfit)&&!bullet.mod&&!bullet.drone;
            signature.Configure(styled?outfit:0,true);
            signature.color=bullet.chilling?Ui.Hex("91EAFF"):OutfitVisuals.Accent(outfit);
            trail.enabled=!styled&&!compact;core.enabled=head.enabled=!styled;
            root.anchoredPosition=new Vector2(bullet.position.x,-bullet.position.y);
            root.localEulerAngles=new Vector3(0,0,-Mathf.Atan2(bullet.velocity.y,bullet.velocity.x)*Mathf.Rad2Deg);
            var tint=bullet.chilling?Ui.Hex("91EAFF"):bullet.mod?Ui.Hex("C996FF"):bullet.drone?Ui.Mint:outfit>0?OutfitVisuals.Accent(outfit):Ui.Gold;
            float length=bullet.pierce>0?35:bullet.mod?30:15+level*2;
            trail.rectTransform.sizeDelta=new Vector2(length,bullet.blast?7:4);
            trail.rectTransform.anchoredPosition=new Vector2(-length,bullet.blast?3.5f:2);
            trail.color=new Color(tint.r,tint.g,tint.b,.38f);head.color=tint;
            head.rectTransform.sizeDelta=Vector2.one*(bullet.blast?11:bullet.pierce>0?7:6);
            head.rectTransform.anchoredPosition=new Vector2(-4,bullet.blast?5.5f:3);
            core.rectTransform.sizeDelta=new Vector2(bullet.pierce>0?18:9,2);
            core.color=new Color(1,1,1,.9f);
        }
    }

    // Ring/rectangle bounds correspond exactly to the simulation's damage footprint.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WarningShape : MaskableGraphic
    {
        public bool Circular;
        public void SetCircle(bool circle){if(Circular==circle)return;Circular=circle;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            if(Circular)
            {
                float radius=r.width*.5f;
                for(int i=0;i<=64;i++)
                {
                    var d=new Vector2(Mathf.Cos(i*Mathf.PI/32),Mathf.Sin(i*Mathf.PI/32));
                    vh.AddVert(r.center+d*radius,color,Vector2.zero);vh.AddVert(r.center+d*Mathf.Max(0,radius-2.5f),color,Vector2.zero);
                    if(i>0){int n=i*2;vh.AddTriangle(n-2,n,n-1);vh.AddTriangle(n-1,n,n+1);}
                }
            }
            else
            {
                Quad(vh,r.xMin,r.yMin,r.width,2.5f);Quad(vh,r.xMin,r.yMax-2.5f,r.width,2.5f);
                Quad(vh,r.xMin,r.yMin,2.5f,r.height);Quad(vh,r.xMax-2.5f,r.yMin,2.5f,r.height);
            }
        }
        void Quad(VertexHelper vh,float x,float y,float w,float h)
        {
            int n=vh.currentVertCount;vh.AddVert(new Vector2(x,y),color,Vector2.zero);vh.AddVert(new Vector2(x+w,y),color,Vector2.zero);
            vh.AddVert(new Vector2(x+w,y+h),color,Vector2.zero);vh.AddVert(new Vector2(x,y+h),color,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
    }

    // One batched UI mesh, fixed capacity, no gameplay RNG or per-particle GameObjects.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CombatParticles : MaskableGraphic
    {
        public const int Capacity=160;
        struct Particle {public Vector2 position,velocity;public float life,total,size,angle;public Color tint;}
        readonly Particle[] particles=new Particle[Capacity];
        int count,sequence,limit=Capacity;
        public int Limit { get { return limit; } set { int next=Mathf.Clamp(value,0,Capacity);if(limit==next)return;limit=next;count=Mathf.Min(count,limit);SetVerticesDirty(); } }
        public int Count { get { return count; } }
        public static CombatParticles Create(Transform parent)
        {
            var root=Ui.Rect(parent,"Scrap and sparks",0,0,540,960);
            var result=root.gameObject.AddComponent<CombatParticles>();result.raycastTarget=false;return result;
        }
        public void Burst(V2 position,int amount,Color tint,float speed=75,float size=4)
        {
            if(amount<=0||count>=limit)return;
            for(int i=0;i<amount && count<limit;i++)
            {
                float a=(++sequence*2.399963f);float life=.3f+(sequence%5)*.07f;
                particles[count++]=new Particle{position=new Vector2(position.x,-position.y),velocity=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*speed*(.5f+sequence%7*.1f),life=life,total=life,size=size*(.6f+sequence%3*.3f),angle=a,tint=tint};
            }
            SetVerticesDirty();
        }
        public void Advance(float delta)
        {
            if(delta<=0||count==0)return;
            for(int i=count-1;i>=0;i--)
            {
                var p=particles[i];p.life-=delta;
                if(p.life<=0){particles[i]=particles[--count];continue;}
                p.position+=p.velocity*delta;p.velocity*=Mathf.Max(0,1-delta*2);p.angle+=delta*4;particles[i]=p;
            }
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            for(int i=0;i<count;i++)
            {
                var p=particles[i];var tint=p.tint;tint.a*=Mathf.Clamp01(p.life/p.total*2);
                var axis=new Vector2(Mathf.Cos(p.angle),Mathf.Sin(p.angle))*p.size;var cross=new Vector2(-axis.y,axis.x)*.55f;
                int n=vh.currentVertCount;
                vh.AddVert(p.position-axis,tint,Vector2.zero);vh.AddVert(p.position+cross,tint,Vector2.zero);
                vh.AddVert(p.position+axis,tint,Vector2.zero);vh.AddVert(p.position-cross,tint,Vector2.zero);
                vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LightningArc : MaskableGraphic
    {
        public Vector2 From,To;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var delta=To-From;var normal=new Vector2(-delta.y,delta.x).normalized;
            var previous=From;
            for(int i=1;i<=10;i++)
            {
                var next=Vector2.Lerp(From,To,i/10f)+normal*(i==10?0:(i%2==0?-1:1)*6);
                var side=new Vector2(-(next-previous).y,(next-previous).x).normalized*1.5f;int n=vh.currentVertCount;
                vh.AddVert(previous-side,color,Vector2.zero);vh.AddVert(previous+side,color,Vector2.zero);
                vh.AddVert(next+side,color,Vector2.zero);vh.AddVert(next-side,color,Vector2.zero);
                vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);previous=next;
            }
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SawGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var center=rectTransform.rect.center;float radius=rectTransform.rect.width*.5f;
            vh.AddVert(center,Ui.Hex("345362"),Vector2.zero);
            for(int i=0;i<=48;i++)
            {
                float a=i*Mathf.PI/24;float r=radius*(i%4<2?1:.76f);
                vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,color,Vector2.zero);
                if(i>0)vh.AddTriangle(0,i,i+1);
            }
        }
    }
}
