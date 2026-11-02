using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public static class BattleArt
    {
        static Texture2D enemies;
        static Sprite floor;
        static readonly Sprite[,] frames = new Sprite[4,4];
        public static Sprite Enemy(int kind,int frame)
        {
            if (frames[kind,frame] != null) return frames[kind,frame];
            if (enemies == null) enemies = Resources.Load<Texture2D>("Art/Enemies-v1");
            if (enemies == null) return null;
            float sx = enemies.width/1254f, sy = enemies.height/1254f;
            int[] xs = {0,319,627,943,1254}, ys = {0,300,583,886,1254};
            frames[kind,frame] = Sprite.Create(enemies,new Rect(xs[frame]*sx,enemies.height-ys[kind+1]*sy,(xs[frame+1]-xs[frame])*sx,(ys[kind+1]-ys[kind])*sy),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            return frames[kind,frame];
        }
        public static void Floor(Transform parent,int stage)
        {
            if (floor == null)
            {
                var texture = Resources.Load<Texture2D>("Art/Refinery-v1");
                if (texture != null) floor = Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            }
            var image = Ui.Box(parent,0,177,540,655,Color.white,false); image.sprite = floor;
            Color[] tint = { Color.white,new Color(1,.9f,.78f),new Color(.73f,.94f,.79f),new Color(1,.76f,.69f),new Color(.79f,.73f,1) };
            image.color = tint[stage];
            Ui.Box(parent,0,177,540,655,new Color(.02f,.04f,.08f,.16f),false);
            Color glow = stage == 2 ? Ui.Mint : stage == 3 ? Ui.Red : stage == 4 ? Ui.Hex("B185FF") : Ui.Gold;
            for (int i = 0; i < 5; i++)
            {
                Ui.Box(parent,10,232+i*117,3,24,new Color(glow.r,glow.g,glow.b,.55f),false);
                Ui.Box(parent,527,232+i*117,3,24,new Color(glow.r,glow.g,glow.b,.55f),false);
            }
        }
    }
    public sealed class CombatRing : MaskableGraphic
    {
        public float Thickness = .065f;
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear(); var rect = rectTransform.rect; Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width,rect.height)*.5f;
            for (int i=0;i<=64;i++)
            {
                float a = i*Mathf.PI*2/64; Vector2 dir = new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                helper.AddVert(center+dir*radius,color,Vector2.zero);
                helper.AddVert(center+dir*radius*(1-Thickness),color,Vector2.zero);
                if(i==0)continue; int n=i*2; helper.AddTriangle(n-2,n,n-1);helper.AddTriangle(n-1,n,n+1);
            }
        }
        public static RectTransform Create(Transform parent,float diameter,Color color,float thickness=.065f)
        {
            var rect=Ui.Rect(parent,"CombatRing",0,0,diameter,diameter);rect.pivot=new Vector2(.5f,.5f);
            var ring=rect.gameObject.AddComponent<CombatRing>();ring.color=color;ring.Thickness=thickness;ring.raycastTarget=false;return rect;
        }
    }
}
