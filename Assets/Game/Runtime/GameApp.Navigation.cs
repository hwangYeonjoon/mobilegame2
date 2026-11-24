using System;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        void HomeCampaignProgress()
        {
            var stage=Campaign.Get(selectedStage);int first=stage.Region*6,done=0;
            for(int i=first;i<first+6;i++)if(State.clearedStages.Contains(i))done++;
            var panel=Ui.Box(screen,24,MenuBottom-268,492,130,Ui.Panel).transform;
            Ui.Label(panel,Campaign.Regions[stage.Region]+"  ·  "+done+"/6 완료",12,6,350,28,19,Ui.Mint);
            Ui.Button(panel,"지도",396,4,84,36,()=>CampaignMap(),Ui.Raised,true,16);
            Ui.Label(panel,"작전 "+(selectedStage+1).ToString("D2")+" · "+stage.Name+" / "+stage.Objective,12,40,468,30,16,Ui.Ink);
            for(int i=0;i<6;i++)
            {
                int id=first+i;bool cleared=State.clearedStages.Contains(id);
                Ui.Button(panel,(cleared?"✓ ":"")+(id+1).ToString("D2"),12+i*78,80,72,40,()=>{selectedStage=id;Show("home");},id==selectedStage?Ui.Mint:cleared?Ui.Blue:Ui.Raised,id<=State.unlockedStage,17);
            }
        }
        void GrowthHome()
        {
            Ui.Label(screen,"골드와 공용 부품으로 지금 사용하는 장비·대원을 강화해요.",24,176,492,42,17,Ui.Muted);
            Ui.Button(screen,"장비 교체 · 보관함",24,231,238,50,()=>Show("gear"),Ui.Raised,true,18);
            Ui.Button(screen,"대원 선택 · 모집",278,231,238,50,()=>Show("heroes"),Ui.Raised,true,18);
            var steps=Growth.Steps(State);
            var list=Ui.Scroll(screen,24,300,492,MenuBottom-300,Math.Max(230,steps.Count*116+90));
            for(int i=0;i<steps.Count;i++)
            {
                var p=steps[i];var row=Ui.Box(list,0,i*116,492,104,Ui.Panel).transform;
                Ui.Label(row,p.name+"  Lv."+p.level+" → "+(p.level+1),14,7,464,30,21);
                Ui.Label(row,GrowthDelta(p),14,38,464,24,16,Ui.Mint);
                Ui.Label(row,"골드 "+p.gold+" · 부품 "+p.parts,14,69,318,24,15,Ui.Muted);
                Ui.Button(row,p.affordable?"강화 +1":"재료 모으기",338,59,140,37,()=>
                {
                    if(!p.affordable){GrowthSources(p);return;}
                    string delta=GrowthDelta(p);
                    Change(p.slot==3?new GameCommand("hero_upgrade"):new GameCommand("upgrade",p.id),confirmed:()=>Toast(p.name+" 강화 완료 · "+delta));
                },p.affordable?Ui.Mint:Ui.Raised,true,16);
            }
            if(steps.Count==0)Ui.Label(list,"장착 장비와 대원이 최대 레벨이에요.\n로봇 도감에서 새로운 조합에 도전해 보세요.",14,12,464,90,21,Ui.Mint,TextAnchor.MiddleCenter);
            float y=Math.Max(116,steps.Count*116);
            Ui.Button(list,"무료 보급 · 집중 강화 · 연습",0,y,492,57,GrowthPanel,Ui.Raised,true,18);
        }
        void ActivitiesHome()
        {
            Ui.Label(screen,"보상 받기, 새로운 도전, 내 로봇 자랑을 여기서 해요.",24,176,492,42,17,Ui.Muted);
            string[] labels={"출석 보상"+(Store.Economy.CanAttend?" · 받을 수 있어요":" · 오늘 수령 완료"),"오늘의 임무 · 전투 "+Math.Min(3,State.dailyRuns)+"/3","주간 로봇 도전 · 폭풍 구조대","내 전시실 · 조립 기록","길드 · 지원 대원","성장 · 수집 목표","게임 안내"};
            Action[] actions={Attendance,Missions,()=>Show("weekly"),()=>Show("showcase"),OpenGuild,ProgressGoals,GuidePanel};
            var list=Ui.Scroll(screen,24,234,492,MenuBottom-234,labels.Length*84);
            for(int i=0;i<labels.Length;i++)Ui.Button(list,labels[i],0,i*84,492,72,actions[i],Ui.Panel,true,21);
        }
        int ResultDestination(bool won,int stage)
        {return won?Math.Min(stage+1,Math.Min(State.unlockedStage,Campaign.Count-1)):stage;}
    }
}
