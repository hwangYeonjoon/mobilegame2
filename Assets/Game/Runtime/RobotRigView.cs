using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    // Three complete chassis sprites, shared by battle, workshop and public profile.
    public sealed class RobotRigView : MonoBehaviour
    {
        Image body;RectTransform socket,rankRing;int design=-1,rank=-1;float size;
        static readonly string[] Art={"RobotFrame-artillery-v1","RobotFrame-walker-v1","RobotFrame-fortress-v1"};
        public static RobotRigView Draw(Transform parent,float x,float y,float size,int id,int level=1)
        {
            var root=Ui.Rect(parent,"Assembled robot",x,y,size,size);var view=root.gameObject.AddComponent<RobotRigView>();view.size=size;
            view.rankRing=CombatRing.Create(root,size*.88f,new Color(.4f,1,.8f,.22f),.012f);view.rankRing.anchoredPosition=new Vector2(size*.5f,-size*.52f);
            view.body=Ui.Box(root,0,0,size,size,Color.white,false);view.body.preserveAspect=true;
            view.socket=CombatRing.Create(root,size*.12f,Ui.Blue,.16f);view.socket.anchoredPosition=new Vector2(size*.5f,-size*.52f);
            view.SetDesign(id,level);return view;
        }
        public void SetDesign(int id,int level=1)
        {
            if(id==design&&level==rank)return;design=id;rank=level;
            body.sprite=ArenaArt.Cell(Art[RobotAssembly.Frame(id)],1,1,0,0);
            if(body.sprite==null)Debug.LogError("Missing 2D robot artwork: "+Art[RobotAssembly.Frame(id)]);
            socket.GetComponent<Graphic>().color=Accent(RobotAssembly.Support(id));socket.gameObject.SetActive(level>0);
            rankRing.gameObject.SetActive(level>=2);body.color=level==0?new Color(.84f,.88f,.88f,1):Color.white;
        }
        public void Animate(float time,bool moving)
        {
            int frame=RobotAssembly.Frame(design);float cycle=Mathf.Sin(time*(frame==1?12:7));
            body.rectTransform.localEulerAngles=new Vector3(0,0,moving?cycle*(frame==1?2.5f:1):0);
            body.rectTransform.anchoredPosition=new Vector2(0,moving&&frame==1?Mathf.Abs(cycle)*size*.012f:0);
            socket.localScale=Vector3.one*(1+Mathf.Sin(time*8)*.1f);
        }
        public static Color Accent(int part){return part==0?Ui.Blue:part==1?Ui.Mint:Ui.Gold;}
    }
}
