using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    // Three complete chassis sprites, shared by battle, workshop and public profile.
    public sealed class RobotRigView : MonoBehaviour
    {
        Image body;Outline outline;RectTransform socket,rankRing,arrivalRing;int design=-1,rank=-1,style=-1;float size;
        static readonly string[] Art={"RobotFrame-artillery-v1","RobotFrame-walker-v1","RobotFrame-fortress-v1"};
        public static RobotRigView Draw(Transform parent,float x,float y,float size,int id,int level=1,int appearance=0)
        {
            var root=Ui.Rect(parent,"Assembled robot",x,y,size,size);var view=root.gameObject.AddComponent<RobotRigView>();view.size=size;
            view.rankRing=CombatRing.Create(root,size*.88f,new Color(.4f,1,.8f,.22f),.012f);view.rankRing.anchoredPosition=new Vector2(size*.5f,-size*.52f);
            view.body=Ui.Box(root,0,0,size,size,Color.white,false);view.body.preserveAspect=true;
            view.outline=view.body.gameObject.AddComponent<Outline>();view.outline.effectDistance=new Vector2(2.5f,-2.5f);
            view.arrivalRing=CombatRing.Create(root,size*.9f,Ui.Blue,.018f);view.arrivalRing.anchoredPosition=new Vector2(size*.5f,-size*.52f);view.arrivalRing.gameObject.SetActive(false);
            view.socket=CombatRing.Create(root,size*.12f,Ui.Blue,.16f);view.socket.anchoredPosition=new Vector2(size*.5f,-size*.52f);
            view.SetDesign(id,level,appearance);return view;
        }
        public void SetDesign(int id,int level=1,int appearance=0)
        {
            appearance=Mathf.Clamp(appearance,0,2);if(id==design&&level==rank&&style==appearance)return;design=id;rank=level;style=appearance;
            body.sprite=style==0?ArenaArt.Cell(Art[RobotAssembly.Frame(id)],1,1,0,0):ArenaArt.Cell("RobotStyles-v1",3,2,RobotAssembly.Frame(id),style-1);
            if(body.sprite==null)Debug.LogError("Missing 2D robot artwork: "+Art[RobotAssembly.Frame(id)]);
            socket.GetComponent<Graphic>().color=Accent(RobotAssembly.Support(id));socket.gameObject.SetActive(level>0);
            outline.effectColor=Accent(RobotAssembly.Frame(id));
            rankRing.gameObject.SetActive(level>=2);body.color=level==0?new Color(.84f,.88f,.88f,1):Color.white;
        }
        public void SetBattleClarity(bool nearbyEnemy)
        {var c=body.color;c.a=nearbyEnemy?.58f:1;body.color=c;outline.enabled=!nearbyEnemy;}
        public void Animate(float time,bool moving,float arrival=-1)
        {
            int frame=RobotAssembly.Frame(design);float cycle=Mathf.Sin(time*(frame==1?12:7));
            body.rectTransform.localEulerAngles=new Vector3(0,0,moving?cycle*(frame==1?2.5f:1):0);
            body.rectTransform.anchoredPosition=new Vector2(0,moving&&frame==1?Mathf.Abs(cycle)*size*.012f:0);
            socket.localScale=Vector3.one*(1+Mathf.Sin(time*8)*.1f);
            bool entering=arrival>=0&&arrival<.65f;arrivalRing.gameObject.SetActive(entering);
            if(entering){float t=arrival/.65f;arrivalRing.localScale=Vector3.one*(.85f+t*(style==1?.8f:.45f));var color=style==1?Ui.Gold:style==2?Ui.Mint:Accent(frame);color.a=(1-t)*.8f;arrivalRing.GetComponent<Graphic>().color=color;}
        }
        public static Color Accent(int part){return part==0?Ui.Blue:part==1?Ui.Mint:Ui.Gold;}
    }
}
