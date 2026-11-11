using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        void RegionalEnemyGuide(int region,int stage)
        {
            int count=0;for(int i=0;i<6;i++)if(System.Array.IndexOf(Campaign.Get(stage).Enemies,(EnemyRole)i)>=0||i==0||region>0&&i==1)count++;
            float listHeight=Mathf.Min(500,count*90);
            var card=Dialog(Campaign.Regions[region]+" · 몬스터","외형으로 역할을 읽고 필요한 부품을 회수하세요",320+listHeight);
            Ui.Label(card,RegionalEnemies.Advice[region],24,126,444,60,19,Ui.Mint);
            var list=Ui.Scroll(card,24,196,444,listHeight,count*90);
            int displayed=0;
            for(int i=0;i<6;i++)
            {
                var role=(EnemyRole)i;bool regular=System.Array.IndexOf(Campaign.Get(stage).Enemies,role)>=0;
                if(!regular&&role!=EnemyRole.Pursuer&&!(region>0&&role==EnemyRole.Charger))continue;
                var row=Ui.Box(list,0,displayed++*90,444,84,Ui.Background).transform;
                var art=Ui.Box(row,4,3,78,78,Color.white,false);art.sprite=BattleArt.Regional(region,role);art.preserveAspect=true;
                Ui.Label(row,RegionalEnemies.Name(region,role),96,5,335,30,20);
                Ui.Label(row,(regular?"주요 편성":"상황별 등장")+" · "+RobotAssembly.Parts[RobotAssembly.PartFor(role)]+" 회수",96,42,335,30,15,Ui.Muted);
            }
            Ui.Label(card,"처치 누적으로 부품 회수 · 포/동/장 표식은 해당 부품 운반 적",24,208+listHeight,444,30,13,Ui.Muted);
            Ui.Button(card,"작전 정보로",24,256+listHeight,444,44,()=>CampaignDetails(stage),Ui.Raised,true,17);
        }
    }
}
