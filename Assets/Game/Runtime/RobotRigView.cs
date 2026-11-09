using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    // Shared articulated metal parts: combat, assembly preview and public showcase.
    public sealed class RobotRigView : MonoBehaviour
    {
        RectTransform body;RobotRigGraphic armor;int design=-1;
        public static RobotRigView Draw(Transform parent,float x,float y,float size,int id)
        {
            var root=Ui.Rect(parent,"Assembled robot",x,y,size,size);var view=root.gameObject.AddComponent<RobotRigView>();
            view.armor=root.gameObject.AddComponent<RobotRigGraphic>();view.armor.raycastTarget=false;
            var image=Ui.Box(root,size*.13f,size*.16f,size*.74f,size*.71f,Color.white,false);
            image.sprite=ArenaArt.Cell("SalvageMachine-v1",1,1,0,0);image.preserveAspect=true;view.body=image.rectTransform;
            view.SetDesign(id);return view;
        }
        public void SetDesign(int id)
        {
            if(id==design)return;design=id;armor.design=id;armor.SetVerticesDirty();
            body.localScale=Vector3.one*(RobotAssembly.Frame(id)==2?1.07f:1);
        }
        public void Animate(float time,bool moving)
        {
            armor.phase=moving?Mathf.Sin(time*12):0;armor.SetVerticesDirty();
            body.localEulerAngles=new Vector3(0,0,RobotAssembly.Frame(design)==1&&moving?Mathf.Sin(time*10)*3:0);
        }
        public static Color Accent(int part){return part==0?Ui.Blue:part==1?Ui.Mint:Ui.Gold;}
    }
    public sealed class RobotRigGraphic : MaskableGraphic
    {
        public int design=5;public float phase;
        VertexHelper mesh;float scale;
        static readonly Color Dark=Ui.Hex("172128"),Steel=Ui.Hex("59666A"),Bronze=Ui.Hex("8F652F"),Gold=Ui.Hex("C18C3D");
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();mesh=vh;scale=rectTransform.rect.width/320;
            int frame=RobotAssembly.Frame(design),support=RobotAssembly.Support(design);Color accent=RobotRigView.Accent(frame);
            if(frame==0)
            {
                for(int side=0;side<2;side++)
                {
                    float x=side==0?34:237;
                    Panel(x,100,49,174,Steel);Panel(x+7,108,35,157,Dark);
                    for(int n=0;n<9;n++){Box(x+4,117+n*16,41,7,Ui.Hex("768079"));Box(x+7,123+n*16,35,3,Ui.Hex("303A3C"));}
                    Cannon(side==0?87:207,30,26,111,accent);
                }
            }
            else if(frame==1)
            {
                for(int side=0;side<2;side++)for(int n=0;n<2;n++)
                {
                    float x=side==0?18:245,y=116+n*99+(side==0?phase:-phase)*7;
                    Panel(side==0?x+20:x-21,y,58,27,Bronze);Panel(x+7,y+10,36,56,Steel);
                    Panel(x+12,y+17,26,31,Gold);Box(x+17,y+24,16,4,accent);
                    Panel(x,y+51,57,27,Dark);Panel(x+5,y+53,47,17,Bronze);
                    for(int t=0;t<3;t++)Box(x+11+t*13,y+58,5,11,Ui.Hex("D6B47D"));
                }
            }
            else
            {
                for(int side=0;side<2;side++)
                {
                    float x=side==0?18:244;
                    Panel(x,82,58,169,Steel);Panel(x+5,90,48,151,Bronze);
                    for(int n=0;n<3;n++){Panel(x+9,98+n*45,40,36,Gold);Box(x+17,107+n*45,24,4,Ui.Hex("E9C582"));Box(x+16,123+n*45,26,4,Dark);}
                    Box(x+5,109,3,85,accent);Panel(side==0?56:210,245,54,35,Dark);Panel(side==0?62:216,248,42,22,Steel);
                }
            }
            if(support==0){Cannon(118,22,17,82,Ui.Blue);Cannon(185,22,17,82,Ui.Blue);}
            else if(support==1)
            {
                for(int n=0;n<2;n++){float x=107+n*79;Panel(x,231,28,58,Steel);Panel(x+4,236,20,40,Bronze);Box(x+8,241,12,25,Ui.Mint);Panel(x+2,275,24,12,Dark);Box(x+7,285,14,13,Ui.Hex("43B69E"));Box(x+11,286,6,9,Ui.Hex("D8FFF0"));}
            }
            else
            {
                for(int n=0;n<2;n++){float x=65+n*153;Panel(x,173,37,78,Steel);Panel(x+4,178,29,68,Gold);Box(x+10,186,17,5,Ui.Hex("FFF1B0"));Box(x+12,199,13,30,Bronze);}
            }
        }
        void Cannon(float x,float y,float w,float h,Color accent)
        {
            Panel(x+3,y,w-6,h,Steel);Panel(x,y+h*.56f,w,h*.44f,Bronze);
            Panel(x-2,y+7,w+4,24,Dark);Box(x+4,y+12,w-8,12,Ui.Hex("060D11"));Box(x+3,y+37,4,h*.43f,Ui.Hex("ABB6B3"));
            Box(x+w-6,y+37,3,h*.40f,Ui.Hex("303D42"));Box(x+4,y+h-16,w-8,4,accent);
        }
        void Panel(float x,float y,float w,float h,Color c)
        {
            Plate(x-2,y-2,w+4,h+4,5,Dark);Plate(x,y,w,h,4,Color.Lerp(c,Color.black,.35f));
            Plate(x+2,y+2,w-5,h-6,3,c);Box(x+6,y+3,w-12,2,Color.Lerp(c,Color.white,.4f));
            Box(x+w-5,y+7,2,h-15,Color.Lerp(c,Color.black,.45f));
            Box(x+5,y+h-10,3,3,Ui.Hex("D5C4A4"));Box(x+w-9,y+7,3,3,Ui.Hex("D5C4A4"));
        }
        void Plate(float x,float y,float w,float h,float cut,Color c)
        {
            int i=mesh.currentVertCount;float k=Mathf.Min(cut,Mathf.Min(w,h)*.25f);
            Vertex(x+k,y,c);Vertex(x+w-k,y,c);Vertex(x+w,y+k,c);Vertex(x+w,y+h-k,c);Vertex(x+w-k,y+h,c);Vertex(x+k,y+h,c);Vertex(x,y+h-k,c);Vertex(x,y+k,c);
            for(int n=1;n<7;n++)mesh.AddTriangle(i,i+n,i+n+1);
        }
        void Vertex(float x,float y,Color c){mesh.AddVert(new Vector3(x*scale,-y*scale),c,Vector2.zero);}
        void Box(float x,float y,float w,float h,Color c)
        {
            int i=mesh.currentVertCount;Vertex(x,y,c);Vertex(x+w,y,c);Vertex(x+w,y+h,c);Vertex(x,y+h,c);
            mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);
        }
    }
}
