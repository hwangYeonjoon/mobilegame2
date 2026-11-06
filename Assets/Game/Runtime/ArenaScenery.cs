using System.Collections.Generic;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ScrapGuard
{
    public static class ArenaArt
    {
        static readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        public static Sprite Cell(string name,int columns,int rows,int column,int row)
        {
            string key=name+":"+column+":"+row;
            if(sprites.TryGetValue(key,out var sprite))return sprite;
            if(!textures.TryGetValue(name,out var texture))textures[name]=texture=Resources.Load<Texture2D>("Art/"+name);
            if(texture==null)return null;
            float w=texture.width/(float)columns,h=texture.height/(float)rows;
            // One texel inset stops bilinear sampling from borrowing the neighboring cell.
            sprite=Sprite.Create(texture,new Rect(column*w+1,texture.height-(row+1)*h+1,w-2,h-2),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            sprites[key]=sprite;return sprite;
        }
        public static Sprite Floor(int region) { return Cell("ArenaFloors-v2",4,2,region%4,region/4); }
        public static Sprite Landmark(int region){return Cell("Landmarks-v2",4,2,region%4,region/4);}
        public static Sprite Prop(int region) { return Cell("ArenaProps-v3",4,2,region%4,region/4); }
        public static Sprite Surface(int region) { int cell=region-1;return Cell("TerrainDetails-v2",4,2,cell%4,cell/4); }
    }

    // The tagged root is the exact shared simulation rectangle; the roof may overhang upwards.
    public sealed class ArenaGeometryTag : MonoBehaviour
    {
        public MapRect Bounds;
        public bool Solid;
        public int Region;
    }

    public sealed class ArenaScenery : MonoBehaviour
    {
        public static readonly Color[] Accents={Ui.Hex("F3B968"),Ui.Hex("F5CF77"),Ui.Hex("A4D76B"),Ui.Hex("92E4FA"),Ui.Hex("BD9AF6"),Ui.Hex("FFE68A"),Ui.Hex("FFAA70"),Ui.Hex("B4ACFF")};
        readonly List<AmbientLight> lights=new List<AmbientLight>();
        readonly List<Image> lamps=new List<Image>();
        readonly List<RectTransform> rotors=new List<RectTransform>();
        readonly List<RectTransform> bubbles=new List<RectTransform>();
        readonly List<Vector2> bubbleOrigins=new List<Vector2>();
        readonly List<RectTransform> belts=new List<RectTransform>();
        readonly List<Vector2> beltOrigins=new List<Vector2>();
        readonly List<RectTransform> dust=new List<RectTransform>();
        int region;
        public static ArenaScenery Build(Transform parent,int stage,bool challenge)
        {
            var root=Ui.Rect(parent,"Regional scenery",0,0,540,960);
            var scene=root.gameObject.AddComponent<ArenaScenery>();
            scene.region=challenge?7:Campaign.Get(stage).Region;
            int region=scene.region;var accent=Accents[region];
            var floor=Ui.Box(root,0,177,540,655,Color.white,false);
            floor.sprite=ArenaArt.Floor(region);
            // Quiet floor beneath actors; props and warning lines retain their contrast.
            Ui.Box(root,0,177,540,655,new Color(.025f,.04f,.06f,.24f),false);
            for(int side=0;side<2;side++)
            {
                var glow=Ui.Rect(root,"Facility light",side==0?-100:360,side==0?230:560,280,300).gameObject.AddComponent<AmbientLight>();
                glow.raycastTarget=false;glow.color=new Color(accent.r,accent.g,accent.b,.16f);scene.lights.Add(glow);
            }
            for(int side=0;side<2;side++)for(int i=0;i<8;i++)
            {
                Ui.Box(root,side==0?4:528,199+i*76,8,44,Ui.Hex("17222D"),false);
                scene.lamps.Add(Ui.Box(root,side==0?7:531,205+i*76,2,26,accent,false));
            }
            // Peripheral depth stays under actors and danger warnings; only 12 ambient motes.
            for(int side=0;side<2;side++)
            {
                Ui.Box(root,side==0?0:524,177,16,655,new Color(.02f,.03f,.05f,.6f),false);
                Ui.Box(root,side==0?16:521,177,3,655,new Color(accent.r,accent.g,accent.b,.18f),false);
            }
            for(int i=0;i<12;i++)
            {
                var mote=Ui.Disc(root,24+i*43,220+i%4*135,2+i%2,new Color(accent.r,accent.g,accent.b,.18f));
                scene.dust.Add(mote.rectTransform);
            }
            if(challenge)
            {
                var ring=CombatRing.Create(root,260,new Color(accent.r,accent.g,accent.b,.25f),.012f);
                ring.anchoredPosition=new Vector2(270,-330);return scene;
            }
            var definition=Campaign.Get(stage);
            foreach(var z in Campaign.Zones(definition))
            {
                var zone=Ui.Rect(root,"Terrain region "+region,z.X,z.Y,z.W,z.H);
                var tag=zone.gameObject.AddComponent<ArenaGeometryTag>();tag.Bounds=z;tag.Region=region;
                zone.gameObject.AddComponent<RectMask2D>();
                // Long strips repeat along their length instead of stretching rails and grating.
                bool horizontal=z.W>z.H*2;
                float tile=horizontal?z.H:z.W;
                float length=horizontal?z.W:z.H;
                if(region==2 || region==3)
                {var surface=Ui.Box(zone,0,0,z.W,z.H,Color.white,false);surface.sprite=ArenaArt.Surface(region);}
                else for(float at=0;at<length;at+=tile)
                {
                    var surface=Ui.Box(zone,horizontal?at:0,horizontal?0:at,tile,tile,Color.white,false);
                    surface.sprite=ArenaArt.Surface(region);
                }
                Ui.Box(zone,0,0,z.W,z.H,new Color(0,0,0,region==3?.38f:.28f),false);
                if(region==2)for(int i=0;i<5;i++)
                {
                    var bubble=CombatRing.Create(zone,5+i%3*3,new Color(.73f,1,.44f,.4f),.14f);
                    bubble.anchoredPosition=new Vector2(17+i*(z.W-30)/5,-22-i%3*22);
                    scene.bubbles.Add(bubble);scene.bubbleOrigins.Add(bubble.anchoredPosition);
                }
                if(region==4)for(int i=0;i<9;i++)
                {
                    var arrow=Ui.Label(zone,z.X<270?"˄":"˅",0,i*64,z.W,30,26,new Color(.8f,.7f,1,.23f),TextAnchor.MiddleCenter).rectTransform;
                    scene.belts.Add(arrow);scene.beltOrigins.Add(arrow.anchoredPosition);
                }
            }
            foreach(var b in Campaign.Blocks(definition))
            {
                // Cast shadow is decorative; the tagged rectangle below remains the exact collision footprint.
                Ui.Box(root,b.X+7,b.Y+9,b.W,b.H,new Color(0,0,0,.22f),true,"Machinery cast shadow");
                var block=Ui.Rect(root,"Solid machinery "+region,b.X,b.Y,b.W,b.H);
                var tag=block.gameObject.AddComponent<ArenaGeometryTag>();tag.Bounds=b;tag.Solid=true;tag.Region=region;
                Ui.Box(block,0,0,b.W,b.H,Ui.Hex("17212A"),false);
                Ui.Box(block,0,b.H-3,b.W,3,new Color(accent.r,accent.g,accent.b,.7f),false);
                bool landmark=b.W>=120;
                var shadow=Ui.Disc(block,-9,b.H-15,b.W+18,new Color(0,0,0,.3f));shadow.rectTransform.sizeDelta=new Vector2(b.W+18,25);
                var prop=Ui.Box(block,landmark?-9:-3,landmark?-75:-22,b.W+(landmark?18:6),b.H+(landmark?76:23),Color.white,false);
                prop.sprite=landmark?ArenaArt.Landmark(region):ArenaArt.Prop(region);prop.preserveAspect=landmark;
                if(landmark)
                {
                    Ui.Box(block,10,b.H-8,b.W-20,3,new Color(accent.r,accent.g,accent.b,.65f),false);
                    var beacon=CombatRing.Create(block,14,accent,.16f);beacon.anchoredPosition=new Vector2(b.W*.5f,62);
                    scene.rotors.Add(beacon);
                }
                // Taller machinery has a visible footing on the exact collision boundary.
                for(int i=0;i<3;i++)scene.lamps.Add(Ui.Box(block,7+i*9,b.H-6,4,2,accent,false));
                if(region==3)
                {
                    var fan=Ui.Rect(block,"Fan",b.W*.52f,-1,20,20);fan.pivot=new Vector2(.5f,.5f);
                    for(int blade=0;blade<4;blade++)
                    {var fin=Ui.Box(fan,9,2,3,7,new Color(.7f,.95f,1,.6f),false).rectTransform;fin.RotateAround(fan.position,Vector3.forward,blade*90);}
                    scene.rotors.Add(fan);
                }
            }
            Ui.Label(root,Campaign.Regions[region]+"  /  "+definition.Objective,16,801,508,23,12,new Color(accent.r,accent.g,accent.b,.8f),TextAnchor.MiddleCenter);
            return scene;
        }
        public void Render(float elapsed)
        {
            for(int i=0;i<lamps.Count;i++){var c=Accents[region];c.a=.5f+.2f*Mathf.Sin(elapsed*2+i*.6f);lamps[i].color=c;}
            for(int i=0;i<lights.Count;i++){var c=Accents[region];c.a=.12f+.025f*Mathf.Sin(elapsed*.8f+i*2);lights[i].color=c;}
            for(int i=0;i<dust.Count;i++)dust[i].anchoredPosition=new Vector2(25+i*43+Mathf.Sin(elapsed*.3f+i)*6,-(198+(i*71+elapsed*(2+i%3))%600));
            foreach(var rotor in rotors)rotor.localEulerAngles=new Vector3(0,0,elapsed*120);
            for(int i=0;i<bubbles.Count;i++)bubbles[i].anchoredPosition=bubbleOrigins[i]+Vector2.up*Mathf.Sin(elapsed*1.5f+i)*4;
            for(int i=0;i<belts.Count;i++)belts[i].anchoredPosition=beltOrigins[i]+Vector2.up*(elapsed*16%64);
        }
    }
}
