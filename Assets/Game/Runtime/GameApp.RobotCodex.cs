using System;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        const string RobotGoalKey="scrapguard.robot.codex-goal.v1";
        int SavedRobotGoal
        {
            get
            {
                int id=PlayerPrefs.GetInt(RobotGoalKey,0);
                return RobotAssembly.Valid(id)&&(id!=RobotSeason.Seeker||RobotSeason.HasBlueprint(State))?id:0;
            }
        }
        int BattleRobotGoal()
        {
            // A wish never grants parts or records a simulation action. Weekly objectives take precedence.
            if(combat.Challenge)return combat.WeeklyFrame*4+1;
            int id=SavedRobotGoal;
            return id!=0&&combat.RobotDesignUnlocked(id)?id:combat.MachineDesign;
        }
        void SetRobotGoal(int id)
        {
            if(!RobotAssembly.Valid(id)||id==RobotSeason.Seeker&&!RobotSeason.HasBlueprint(State))return;
            PlayerPrefs.SetInt(RobotGoalKey,id);PlayerPrefs.Save();Track("robot_codex","goal",id);
            Show("home");Toast(RobotAssembly.Name(id)+" · 출격 후 필요한 부품을 안내해요.");
        }
        static string RobotRecipeText(int id,int rank)
        {
            var cost=RobotAssembly.Recipe(id,rank);string text="";
            for(int i=0;i<3;i++)if(cost[i]>0)text+=(text.Length==0?"":" · ")+RobotAssembly.Parts[i]+" "+cost[i]+"개";
            return text;
        }
        void RobotCodex()
        {
            RobotAssembly.Normalize(State);int completed=State.robotRecords.FindAll(r=>r!=null&&r.rank>=1&&r.builds>0).Count;
            Ui.Label(screen,"만들고 싶은 모습부터 골라보세요 · 특화 조립 "+completed+"/"+RobotAssembly.Count,24,176,492,39,18,Ui.Mint);
            Ui.Label(screen,"포신·구동부·장갑은 매 판 회수해요. 조립 기록은 남아요.",24,218,492,35,15,Ui.Muted);
            Ui.Button(screen,"내 조립 기록 · 전시",24,266,238,46,()=>RobotHangar(),Ui.Raised,true,17);
            Ui.Button(screen,"새 설계 · 외형 공방",278,266,238,46,RobotSeasonHub,Ui.Raised,true,17);
            var list=Ui.Scroll(screen,24,330,492,MenuBottom-330,RobotAssembly.Count*225);
            for(int i=1;i<=RobotAssembly.Count;i++)
            {
                int id=i;var record=State.robotRecords.Find(r=>r!=null&&r.design==id);bool built=record!=null&&record.rank>=1;
                bool locked=id==RobotSeason.Seeker&&!RobotSeason.HasBlueprint(State);
                var row=Ui.Box(list,0,(id-1)*225,492,213,Ui.Panel).transform;
                RobotRigView.Draw(row,6,14,162,id,1,RobotStyles.Active(State,RobotAssembly.Frame(id)));
                Ui.Label(row,RobotAssembly.Name(id),180,12,298,32,21,RobotRigView.Accent(RobotAssembly.Frame(id)));
                Ui.Label(row,RobotAssembly.Detail(id),180,51,298,58,14,Ui.Ink);
                Ui.Label(row,locked?"공방에서 설계 영구 제작 필요":RobotRecipeText(id,0),180,113,298,33,16,Ui.Gold);
                Ui.Label(row,built?(record.rank>=2?"완성형 조립 기록 ✓":"특화형 조립 기록 ✓"):"아직 조립하지 않은 특화형",14,166,276,30,15,built?Ui.Mint:Ui.Muted);
                Ui.Button(row,SavedRobotGoal==id?"목표 · 재료 보기":"모습 · 재료 보기",300,157,178,44,()=>RobotCodexDetail(id),SavedRobotGoal==id?Ui.Mint:Ui.Raised,true,16);
            }
        }
        void RobotCodexDetail(int id,int rank=1)
        {
            if(!RobotAssembly.Valid(id))return;
            bool locked=id==RobotSeason.Seeker&&!RobotSeason.HasBlueprint(State);int frame=RobotAssembly.Frame(id);
            var card=Dialog(RobotAssembly.Name(id),SavedRobotGoal==id?"현재 조립 목표 · 이 기기에 저장됨":"완성된 모습을 보고, 다음 출격에서 만들 로봇을 골라요.",870);
            RobotRigView.Draw(card,12,127,220,id,rank,RobotStyles.Active(State,frame));
            Ui.Label(card,rank==1?"특화형":"완성형",244,151,224,34,25,RobotRigView.Accent(frame));
            Ui.Label(card,RobotAssembly.Detail(id),244,199,224,92,17,Ui.Ink);
            Ui.Button(card,"특화형 모습",24,351,214,43,()=>RobotCodexDetail(id,1),rank==1?Ui.Mint:Ui.Raised,true,18);
            Ui.Button(card,"완성형 모습",254,351,214,43,()=>RobotCodexDetail(id,2),rank==2?Ui.Mint:Ui.Raised,true,18);
            int choices=frame==0?4:3;float width=444f/choices;
            for(int i=0;i<choices;i++)
            {
                int next=i==3?RobotSeason.Seeker:frame*3+i+1;
                Ui.Button(card,i==3?"유도":RobotAssembly.Supports[i],24+i*width,410,width-6,39,()=>RobotCodexDetail(next,rank),id==next?Ui.Blue:Ui.Raised,true,17);
            }
            Ui.Label(card,rank==1?"특화형 제작에 필요한 전투 부품":"특화형 제작 후, 아래 부품을 추가로 회수",24,463,444,29,17,Ui.Gold);
            var cost=RobotAssembly.Recipe(id,rank-1);
            for(int part=0;part<3;part++)
            {
                int p=part;float y=503+part*43;
                Ui.Label(card,RobotAssembly.Parts[p]+"  "+cost[p]+"개 필요",24,y,290,33,18,cost[p]>0?Ui.Ink:Ui.Muted);
                Ui.Button(card,"획득처",340,y,128,33,()=>RobotPartSources(id,p,rank),Ui.Raised,cost[p]>0,16);
            }
            Ui.Label(card,"출격할 때 전투 부품은 0개부터 시작해요.\n상단 공용 부품은 장비 강화용으로, 조립에 쓰지 않아요.",24,637,444,49,15,Ui.Muted);
            Ui.Button(card,locked?"유도 포신 설계 · 영구 제작 보기":"이 로봇을 목표로 · 출격 준비",24,708,444,56,()=>{if(locked)SeekerBlueprintPanel();else SetRobotGoal(id);},Ui.Mint,true,19);
            Ui.Button(card,"로봇 도감으로",24,783,214,45,()=>Show("robots"),Ui.Raised,true,17);
            Ui.Button(card,locked?"공방에서 재료 확인":"목표 없이 자유 조립",254,783,214,45,()=>{PlayerPrefs.DeleteKey(RobotGoalKey);PlayerPrefs.Save();Show("home");},Ui.Raised,!locked,16);
        }
        void RobotPartSources(int design,int part,int rank)
        {
            int stage=selectedStage;
            if(!Array.Exists(Campaign.Get(stage).Enemies,r=>RobotAssembly.PartFor(r)==part))
                for(int i=State.unlockedStage;i>=0;i--)if(Array.Exists(Campaign.Get(i).Enemies,r=>RobotAssembly.PartFor(r)==part)){stage=i;break;}
            var definition=Campaign.Get(stage);bool regular=Array.Exists(definition.Enemies,r=>RobotAssembly.PartFor(r)==part);
            var card=Dialog(RobotAssembly.Parts[part]+" 획득처","적을 처치하고 떨어진 부품을 회수하세요. 처치마다 지급되지는 않아요.",760);
            Ui.Label(card,RobotAssembly.Name(design)+"\n"+(rank==1?"특화형":"완성형 추가")+" · "+RobotRecipeText(design,rank-1),24,133,444,62,19,Ui.Mint);
            Ui.Label(card,"작전 "+(stage+1).ToString("D2")+" · "+Campaign.Regions[definition.Region],24,211,444,32,22);
            if(!regular)
            {
                var badge=Ui.Box(card,24,266,82,82,RobotRigView.Accent(part)).transform;
                Ui.Label(badge,new[]{"포","동","장"}[part],0,0,82,82,36,Ui.Background,TextAnchor.MiddleCenter);
                Ui.Label(card,RobotAssembly.Parts[part]+" 운반 적",122,271,346,32,23,Ui.Ink);
                Ui.Label(card,"전장 위의 '"+new[]{"포","동","장"}[part]+"' 표식을 찾아보세요",122,309,346,46,17,Ui.Mint);
                Ui.Label(card,"해당 부품을 주는 주요 적이 없는 지역에서도\n부품 운반 적을 처치하면 회수할 수 있어요.",24,389,444,68,18,Ui.Ink);
            }
            int row=0;
            for(int i=0;regular&&i<6;i++)
            {
                var role=(EnemyRole)i;if(RobotAssembly.PartFor(role)!=part||Array.IndexOf(definition.Enemies,role)<0)continue;
                float y=266+row++*102;
                var art=Ui.Box(card,24,y,82,82,Color.white,false);art.sprite=BattleArt.Regional(definition.Region,role);art.preserveAspect=true;
                Ui.Label(card,RegionalEnemies.Name(definition.Region,role),122,y+5,346,32,21);
                Ui.Label(card,"이 작전의 주요 적 · "+RobotAssembly.Parts[part]+" 회수",122,y+42,346,35,16,Ui.Mint);
            }
            Ui.Label(card,(regular?"주요 적과 부품 운반 적을 찾아보세요.":"이 작전에서는 부품 운반 적의 표식을 확인하세요.")+"\n포 / 동 / 장 표식은 각각 포신 / 구동부 / 장갑이에요.\n이번 판에서만 사용 · 다음 판으로 재료가 쌓이지 않아요.",24,489,444,90,16,Ui.Muted);
            Ui.Button(card,"작전 "+(stage+1).ToString("D2")+" 출격 준비",24,602,444,55,()=>{selectedStage=stage;SetRobotGoal(design);},Ui.Mint,design!=RobotSeason.Seeker||RobotSeason.HasBlueprint(State),19);
            Ui.Button(card,"로봇으로 돌아가기",24,682,444,45,()=>RobotCodexDetail(design,rank),Ui.Raised,true,17);
        }
    }
}
