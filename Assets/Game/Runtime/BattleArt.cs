using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public static class BattleArt
    {
        static Texture2D enemies;
        static readonly Sprite[,] frames = new Sprite[4,4];
        public static Sprite Role(int role,int state,bool boss=false)
        {
            role=Mathf.Clamp(role,0,boss?6:5);state=Mathf.Clamp(state,0,3);
            if(!boss)return ArenaArt.Cell("EnemyRoles-v3",6,4,role,state);
            return role<4 ? ArenaArt.Cell("RegionBosses-v2",4,4,role,state)
                : ArenaArt.Cell("ExpansionBosses-v2",3,4,role-4,state);
        }
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
        public static void Floor(Transform parent,int stage,bool challenge=false)
        {
            ArenaScenery.Build(parent,stage,challenge);
        }
        public static Color RoleColor(ScrapGuard.Core.EnemyRole role)
        {
            return new[]{Color.white,Ui.Hex("FFC273"),Ui.Hex("D8B0FF"),Ui.Hex("FF8D7A"),Ui.Hex("89D8ED"),Ui.Hex("FF96B5")}[(int)role];
        }
        public static string RoleMark(ScrapGuard.Core.EnemyRole role)
        {
            return new[]{"","»","⊕","!","◇","+"}[(int)role];
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
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
