using ScrapGuard.Core;
using UnityEngine;

namespace ScrapGuard
{
    public partial class GameApp
    {
        void CampaignMap(int region=-1)
        {
            if(region<0)region=Campaign.Get(selectedStage).Region;
            var card=Dialog("작전 지도", "정복 "+State.clearedStages.Count+" / "+Campaign.Count+" · 지역별 전투 규칙",820);
            for(int r=0;r<Campaign.Regions.Length;r++)
            {
                int choice=r;
                Ui.Button(card,(r+1).ToString(),24+r*(450f/Campaign.Regions.Length),122,450f/Campaign.Regions.Length-6,44,()=>CampaignMap(choice),r==region?Ui.Mint:Ui.Raised,true,18);
            }
            Ui.Label(card,Campaign.Regions[region],24,179,444,35,25,Ui.Ink);
            Ui.Label(card,Campaign.Mechanics[region],24,220,444,32,15,Ui.Muted);
            int row=0;
            foreach(var stage in Campaign.Stages)
            {
                if(stage.Region!=region)continue;
                int id=stage.Id;bool unlocked=id<=State.unlockedStage,cleared=State.clearedStages.Contains(id);
                float y=270+row++*76;
                var line=Ui.Box(card,24,y,444,68,Ui.Background).transform;
                Ui.Label(line,(id+1).ToString("D2")+"  "+stage.Name,12,5,309,29,18,unlocked?Ui.Ink:Ui.Muted);
                Ui.Label(line,stage.Objective+" · "+(cleared?"완료":unlocked?"도전 가능":"이전 작전 완료 필요"),12,36,309,23,13,cleared?Ui.Mint:Ui.Muted);
                Ui.Button(line,"정보",337,10,96,47,()=>CampaignDetails(id),Ui.Raised,true,16);
            }
            Ui.Button(card,"지역 수집 · 전용 도색",24,746,444,48,()=>RegionCollection(region),Ui.Mint,true,17);
        }
        void CampaignDetails(int id)
        {
            var stage=Campaign.Get(id);bool cleared=State.clearedStages.Contains(id),unlocked=id<=State.unlockedStage;
            var card=Dialog("작전 "+(id+1).ToString("D2")+" · "+stage.Name,Campaign.Regions[stage.Region],700);
            Ui.Label(card,stage.Objective+"  /  "+(id==0&&!State.tutorialCompleted?180:stage.Duration)+"초",24,128,444,38,23,Ui.Mint);
            Ui.Label(card,Campaign.Mechanics[stage.Region],24,181,444,45,18);
            string enemies="";foreach(var role in stage.Enemies)enemies+=(enemies.Length>0?" · ":"")+new[]{"추격","돌진","포격","자폭","방패","소환"}[(int)role];
            Ui.Label(card,"등장 유형  "+enemies+"\n"+(stage.Mission==Mission.Eliminate?"보스  "+stage.Boss:"끝까지 살아남으면 작전 성공"),24,243,444,76,17,Ui.Muted);
            Ui.Label(card,"성공 보상  골드 "+stage.Gold+" · 부품 "+stage.Parts,24,338,444,36,18,Ui.Gold);
            Ui.Label(card,cleared?"최초 보상 수령 완료":"최초 완료  보석 50 + "+Catalog.Items[stage.FirstItem].Name+"\n보관함이 가득 차면 부품 30개로 지급",24,390,444,67,16,Ui.Muted);
            string field=(id==0 || stage.Layout==1 || stage.Layout==4)?"회수 · 안전 / 과부하 선택":stage.Layout==2?"중계기 방어":stage.Layout==3?"수송 드론 호위":"지역 보스 공략";
            Ui.Label(card,"현장 임무: "+field+(stage.Region>0?" · 출격마다 경로 변형":""),24,460,444,25,13,Ui.Blue);
            Ui.Label(card,stage.Region==0?"첫 지역은 기본 작전으로 진행해요.":"출격 시 기본 / 돌진 순찰 / 불안정 전력 중 결정\n보상은 동일 · 전투 중 일시정지에서 조건 확인",24,494,444,51,14,Ui.Muted);
            Ui.Button(card,unlocked?"이 작전 선택":"이전 작전을 완료하세요",24,560,444,57,()=>{selectedStage=id;CloseModal();Show("home");},null,unlocked,19);
            Ui.Button(card,"지역 지도로",24,632,444,42,()=>CampaignMap(stage.Region),Ui.Raised,true,16);
        }
    }
}
