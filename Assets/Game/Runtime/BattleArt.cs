using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public static class BattleArt
    {
        static Texture2D enemies;
        static Sprite floor;
        static readonly Sprite[,] frames = new Sprite[4,4];
        static Texture2D roles, bosses;
        static readonly Sprite[,] roleFrames=new Sprite[6,4],bossFrames=new Sprite[5,4];
        public static Sprite Role(int role,int state,bool boss=false)
        {
            role=Mathf.Clamp(role,0,boss?4:5);state=Mathf.Clamp(state,0,3);
            var cache=boss?bossFrames:roleFrames;if(cache[role,state]!=null)return cache[role,state];
            if(boss && bosses==null)bosses=Resources.Load<Texture2D>("Art/RegionBosses-v1");
            if(!boss && roles==null)roles=Resources.Load<Texture2D>("Art/EnemyRoles-v2");
            var texture=boss?bosses:roles;if(texture==null)return Enemy(boss?3:0,0);
            // Measured atlas gutters preserve saw arms and the summoner's drone pods.
            int[] xs=boss?new[]{0,321,577,827,1118,1402}:new[]{0,270,520,774,1030,1286,1536};
            int[] ys=boss?new[]{0,278,547,814,1122}:new[]{0,256,512,768,1024};
            float sx=texture.width/(float)xs[xs.Length-1],sy=texture.height/(float)ys[4];
            cache[role,state]=Sprite.Create(texture,new Rect(xs[role]*sx,texture.height-ys[state+1]*sy,(xs[role+1]-xs[role])*sx,(ys[state+1]-ys[state])*sy),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            return cache[role,state];
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
            var definition=ScrapGuard.Core.Campaign.Get(stage);
            int region=challenge?4:definition.Region;
            Color[] baseColors={Ui.Hex("29363A"),Ui.Hex("232E36"),Ui.Hex("233C35"),Ui.Hex("253F52"),Ui.Hex("302D42")};
            Color[] accents={Ui.Hex("DCAB67"),Ui.Hex("F1C46A"),Ui.Hex("8DD36E"),Ui.Hex("86DBED"),Ui.Hex("BB9BEF")};
            var accent=accents[region];
            Ui.Box(parent,0,177,540,655,baseColors[region],false);
            if(region==0)
            {
                if(floor==null)
                {
                    var texture=Resources.Load<Texture2D>("Art/Refinery-v1");
                    if(texture!=null)floor=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
                }
                var background=Ui.Box(parent,0,177,540,655,new Color(.65f,.68f,.66f,.7f),false);background.sprite=floor;
            }
            else
            {
                // Large quiet floor panels leave silhouettes and warnings readable.
                for(int y=0;y<7;y++)for(int x=0;x<5;x++)
                {
                    var tile=Ui.Box(parent,12+x*104,191+y*92,100,88,new Color(1,1,1,(x+y)%2==0?.035f:.015f),false);
                    Ui.Box(tile.transform,7,7,3,3,new Color(accent.r,accent.g,accent.b,.2f),false);
                }
            }
            for(int i=0;i<8;i++)
            {
                Ui.Box(parent,7,204+i*77,3,29,new Color(accent.r,accent.g,accent.b,.5f),false);
                Ui.Box(parent,530,204+i*77,3,29,new Color(accent.r,accent.g,accent.b,.5f),false);
            }
            if(challenge)
            {
                var ring=CombatRing.Create(parent,274,new Color(accent.r,accent.g,accent.b,.3f),.012f);
                ring.anchoredPosition=new Vector2(270,-330);return;
            }
            foreach(var z in ScrapGuard.Core.Campaign.Zones(definition))
            {
                if(region==1)
                {
                    Ui.Box(parent,z.X-10,z.Y,z.W+20,z.H,Ui.Hex("171F27"),false);
                    for(float y=z.Y;y<z.Y+z.H;y+=27)Ui.Box(parent,z.X-6,y,z.W+12,7,Ui.Hex("59524A"),false);
                    Ui.Box(parent,z.X+4,z.Y,5,z.H,Ui.Hex("87989F"),false);
                    Ui.Box(parent,z.X+z.W-9,z.Y,5,z.H,Ui.Hex("87989F"),false);
                }
                if(region==2)
                {
                    Ui.Box(parent,z.X-3,z.Y-3,z.W+6,z.H+6,Ui.Hex("101F1C"),false);
                    Ui.Box(parent,z.X,z.Y,z.W,z.H,new Color(.3f,.6f,.18f,.5f),false);
                    for(int i=0;i<4;i++)Ui.Disc(parent,z.X+15+i*27,z.Y+23+(i%2)*30,13,new Color(.6f,.9f,.25f,.3f));
                    Ui.Label(parent,"오염",z.X,z.Y+z.H/2-12,z.W,24,12,accent,TextAnchor.MiddleCenter);
                }
                if(region==3)
                {
                    Ui.Box(parent,z.X,z.Y,z.W,z.H,new Color(.35f,.7f,.9f,.15f),false);
                    for(float y=z.Y+15;y<z.Y+z.H;y+=66)
                    {var crack=Ui.Box(parent,z.X+20,y,z.W-40,2,new Color(.6f,.9f,1,.22f),false);crack.rectTransform.localEulerAngles=new Vector3(0,0,12);}
                    Ui.Label(parent,"냉각 구역",z.X,z.Y+20,z.W,28,12,accent,TextAnchor.MiddleCenter);
                }
                if(region==4)
                {
                    Ui.Box(parent,z.X-4,z.Y, z.W+8,z.H,Ui.Hex("161B28"),false);
                    for(float y=z.Y+8;y<z.Y+z.H-12;y+=24)Ui.Box(parent,z.X+5,y,z.W-10,5,Ui.Hex("4C4963"),false);
                    Ui.Label(parent,z.X<270?"↑":"↓",z.X,z.Y+z.H/2-24,z.W,48,32,accent,TextAnchor.MiddleCenter);
                }
            }
            foreach(var b in ScrapGuard.Core.Campaign.Blocks(definition))
            {
                Ui.Box(parent,b.X-4,b.Y+5,b.W+8,b.H+5,new Color(0,0,0,.35f),false);
                var block=Ui.Box(parent,b.X,b.Y,b.W,b.H,Ui.Hex(region==3?"536F7C":"44525B"),false).transform;
                Ui.Box(block,3,3,b.W-6,4,accent,false);
                Ui.Box(block,9,13,b.W-18,b.H-23,Ui.Hex("283840"),false);
                for(int i=0;i<3;i++)Ui.Box(block,14+i*15,b.H-13,7,3,new Color(accent.r,accent.g,accent.b,.6f),false);
            }
            Ui.Label(parent,ScrapGuard.Core.Campaign.Regions[region]+"  /  "+definition.Objective,16,795,508,24,12,new Color(accent.r,accent.g,accent.b,.7f),TextAnchor.MiddleCenter);
        }
        public static Color RoleColor(ScrapGuard.Core.EnemyRole role)
        {
            return new[]{Color.white,Ui.Hex("FFC273"),Ui.Hex("D8B0FF"),Ui.Hex("C8EF7A"),Ui.Hex("89D8ED"),Ui.Hex("FF96B5")}[(int)role];
        }
        public static string RoleMark(ScrapGuard.Core.EnemyRole role)
        {
            return new[]{"","»","⊕","!","◇","+"}[(int)role];
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
