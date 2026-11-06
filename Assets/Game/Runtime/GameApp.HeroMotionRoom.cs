using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        void HeroMotionRoom(int hero,int skin=0)
        {
            var card=Dialog("대원 동작 체험",Catalog.HeroNames[hero]+" · 미리보기는 보유 상태를 바꾸지 않아요.",850);
            var stage=WorkshopStage.Draw(card,24,145,444,350,Ui.Mint);
            var preview=stage.gameObject.AddComponent<GuardianMotionPreview>();preview.Initialize(hero,skin);
            string[] modes={"대기","걷기","사격","피격"};
            for(int i=0;i<4;i++){int mode=i;Ui.Button(card,modes[i],24+i*113,515,105,64,()=>preview.Mode=mode,Ui.Raised,true,20);}
            string[] paints={"기본","앰버","서리"};
            for(int i=0;i<3;i++){int paint=i;Ui.Button(card,paints[i],24+i*150,601,144,60,()=>HeroMotionRoom(hero,paint),skin==i?Ui.Mint:Ui.Raised,true,20);}
            Ui.Label(card,HeroIdentity.Names[hero]+"\n"+HeroIdentity.Tips[hero],24,685,444,65,19,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,"대원 목록으로",24,766,444,60,()=>Show("heroes"),Ui.Raised,true,21);
        }
    }
    public sealed class GuardianMotionPreview : MonoBehaviour
    {
        Image actor;int hero,skin;
        public int Mode=1;
        public void Initialize(int id,int paint)
        {
            hero=id;skin=paint;actor=CharacterArt.Draw(transform,92,38,260,268,hero,skin);
            actor.rectTransform.pivot=new Vector2(.5f,.5f);actor.rectTransform.anchoredPosition=new Vector2(222,-172);
        }
        void Update()
        {
            if(actor==null)return;
            float time=Time.unscaledTime;bool pulse=time%1<.3f;
            actor.sprite=GuardianMotion.Sample(hero,skin,0,time,Mode==1?1:0,Mode==2&&pulse,Mode==3&&pulse,false);
            actor.color=Mode==3&&pulse?new Color(1,.65f,.6f):CharacterArt.Tint(skin);
            actor.rectTransform.localScale=GuardianMotion.PoseScale(time,Mode==1?1:0,Mode==2&&pulse,Mode==3&&pulse);
        }
    }
}
