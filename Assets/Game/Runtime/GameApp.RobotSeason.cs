using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        int BattleRobotStyle(int frame){return combat!=null&&combat.Mode==RobotSeason.Preview?4:RobotStyles.Active(State,frame);}
        void RobotSeasonHub()
        {
            RobotSeason.Normalize(State);string week=CurrentWeek();int featured=RobotSeason.Featured(week);
            var card=Dialog("폭풍 구조대 · 테마 작전","같은 대여 장비 · 유도 포신 무료 대여\n지난 작전도 계속 도전할 수 있어요. 순위 경쟁과 별도예요.",920);
            for(int i=0;i<3;i++)
            {
                int pick=i;float y=135+i*148;bool done=(State.robotSeasonClears&(1<<i))!=0;
                Ui.Box(card,24,y,444,134,Ui.Raised);
                Ui.Label(card,(i==featured?"이번 주 ★ ":"")+RobotSeason.Titles[i]+(done?" ✓":""),37,y+8,418,30,20,Ui.Mint);
                Ui.Label(card,RobotSeason.Briefs[i]+"\n최고 로봇 처치 "+State.robotSeasonBest[i]+" · 첫 달성 파편 2개",37,y+44,402,42,14,Ui.Ink);
                Ui.Button(card,"90초 도전",290,y+88,163,35,()=>StartBattle(4,false,RobotSeason.Modes[pick]),Ui.Mint,Endgame.Unlocked(State),16);
            }
            Ui.Label(card,"달성: 90초 생존 + 특화 로봇으로 15처치\n추천 작전 달성 시 주간 파편 3개 · "+(State.robotSeasonWeeks.Contains(week)?"이번 주 수령 완료":"이번 주 미수령")+"\n일일 접속 조건 없음 · 보상은 검증 후 자동 지급",24,588,444,78,16,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Button(card,"유도 포신 제작 · 무료 재료 사용",24,680,444,48,SeekerBlueprintPanel,Ui.Mint,true,18);
            Ui.Button(card,"무료 제작 외형 · 기체 숙련",24,742,444,48,()=>RescueWorkshop(),Ui.Raised,true,18);
            Ui.Button(card,"폭풍 구조대 · 외형 체험과 구매",24,804,444,48,()=>StormPackPanel(),Ui.Gold,true,18);
            Ui.Label(card,Endgame.Unlocked(State)?"성장 상한 이후에도 조립·숙련 기록은 남아요.":"작전 05 완료 후 대여 작전과 설계 제작이 열려요.",24,865,444,30,14,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void SeekerBlueprintPanel()
        {
            bool owned=RobotSeason.HasBlueprint(State);
            var card=Dialog("유도 포신 · 영구 설계","한 적을 추적해 집중 포격 · 범위 피해 없음\n일반 전투에서는 회수한 포신·구동부로 조립합니다.",730);
            RobotRigView.Draw(card,136,135,220,RobotSeason.Seeker,1);
            Ui.Label(card,"골드 필요 6,000 · 보유 "+State.gold.ToString("N0")+"\n부품 필요 60 · 보유 "+State.parts+"\n설계는 영구 보유 · 매판 전투 부품은 새로 회수",24,375,444,87,19,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Button(card,owned?"설계 보유 중":"골드 6,000 · 부품 60 사용",24,490,444,52,()=>{
                var confirm=Dialog("유도 포신 제작 확인","골드 6,000 · 부품 60 차감\n추가 능력치 없음 · 새로운 조립 선택지 영구 해금",400);
                Ui.Button(confirm,"재료를 사용해 설계 제작",24,190,444,52,()=>Change(new GameCommand("robot_seeker_unlock"),confirmed:SeekerBlueprintPanel),Ui.Mint,true,18);
                Ui.Button(confirm,"취소",24,270,444,48,SeekerBlueprintPanel,Ui.Raised,true,18);
            },Ui.Mint,!owned&&Endgame.Unlocked(State)&&State.gold>=6000&&State.parts>=60,18);
            Ui.Button(card,"먼저 대여 작전에서 써 보기",24,560,444,48,()=>StartBattle(4,false,RobotSeason.Modes[1]),Ui.Blue,Endgame.Unlocked(State),17);
            Ui.Button(card,"무료 현장 보급",24,630,214,48,SalvagePanel,Ui.Raised,true,16);
            Ui.Button(card,"테마 작전으로",254,630,214,48,RobotSeasonHub,Ui.Raised,true,16);
        }
        void RescueWorkshop(int frame=0)
        {
            int kills=RobotSeason.Mastery(State,frame);bool owned=RobotStyles.Owned(State,frame,3);
            var card=Dialog("구조대 정비형 · 무료 제작","플레이로 숙련을 쌓고 남는 재료로 제작하세요.\n능력치 변화 없음 · 기체마다 영구 보유",820);
            for(int i=0;i<3;i++){int f=i;Ui.Button(card,RobotAssembly.Frames[i],24+i*150,135,144,43,()=>RescueWorkshop(f),frame==i?Ui.Mint:Ui.Raised,true,15);}
            RobotRigView.Draw(card,126,190,240,frame*4+1,2,3);
            Ui.Label(card,RobotSeason.Badge(kills)+" · 로봇 누적 "+kills+"처치\n제작 조건 200처치 · 명장 기록 1,000처치",24,445,444,65,21,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Label(card,"골드 필요 10,000 · 보유 "+State.gold.ToString("N0")+"\n부품 필요 100 · 보유 "+State.parts,24,525,444,60,18,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Button(card,owned?"이 기체에 적용":"재료 사용 · 제작 확인",24,611,444,52,()=>{
                if(owned){Change(new GameCommand("robot_style_equip"){frame=frame,skin=3},confirmed:()=>RescueWorkshop(frame));return;}
                var confirm=Dialog("구조대 정비형 제작 확인","골드 10,000 · 부품 100 차감\n선택한 기체 1종을 제작하고 바로 적용합니다.",400);
                Ui.Button(confirm,"재료를 사용해 제작하고 적용",24,190,444,52,()=>Change(new GameCommand("robot_rescue_craft"){frame=frame},confirmed:()=>RescueWorkshop(frame)),Ui.Mint,true,18);
                Ui.Button(confirm,"취소",24,270,444,48,()=>RescueWorkshop(frame),Ui.Raised,true,18);
            },Ui.Mint,owned||kills>=200&&State.gold>=10000&&State.parts>=100,18);
            Ui.Button(card,"로봇 전시실",24,686,214,48,()=>RobotHangar(),Ui.Raised,true,17);
            Ui.Button(card,"테마 작전",254,686,214,48,RobotSeasonHub,Ui.Raised,true,17);
        }
        void StormPackPanel(int frame=0)
        {
            Track("offer_view","robot_storm",frame);
            bool owned=RobotStyles.Owned(State,frame,4);
            var card=Dialog("폭풍 구조대 · 영구 외형 팩","3기체 외형 · 출동 링 · 공격 강조색\n600 보석 · 성장 재료와 능력치는 포함하지 않아요.",850);
            for(int i=0;i<3;i++){int f=i;Ui.Button(card,RobotAssembly.Frames[i],24+i*150,135,144,43,()=>StormPackPanel(f),frame==i?Ui.Mint:Ui.Raised,true,15);}
            RobotRigView.Draw(card,121,195,250,frame*4+1,2,4);
            Ui.Label(card,"구매 즉시 3기체 적용 · 기간 제한 없음\n이미 보유한 성운 기사·패스와 별개 상품\n보유 보석 "+State.gems+" · 가격 600",24,463,444,86,19,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Button(card,"전투에서 무료 체험 · 보상 없음",24,566,444,50,()=>StartBattle(4,false,RobotSeason.Preview),Ui.Blue,true,17);
            Ui.Button(card,owned?"3기체 외형 보유 중":"600 보석 · 구매 확인",24,634,444,52,()=>{
                var confirm=Dialog("폭풍 구조대 구매 확인","600 보석 차감 · 기체 외형 3종 영구 소유\n구매 후 바로 적용 · 능력치와 재료 변화 없음",420);
                Ui.Button(confirm,"600 보석 구매 · 3기체 적용",24,188,444,55,()=>Change(new GameCommand("robot_storm_buy"),confirmed:()=>{Track("purchase","robot_storm");StormPackPanel(frame);}),Ui.Gold,State.gems>=600&&State.gemDebt==0,18);
                Ui.Button(confirm,"취소",24,273,214,48,()=>StormPackPanel(frame),Ui.Raised,true,17);
                Ui.Button(confirm,"보석 획득·충전",254,273,214,48,()=>GemSources(600,()=>StormPackPanel(frame)),Ui.Raised,true,16);
            },Ui.Gold,!owned,18);
            Ui.Button(card,owned?"선택 기체에 적용":"무료 제작 외형 보기",24,706,444,48,()=>{if(owned)Change(new GameCommand("robot_style_equip"){frame=frame,skin=4},confirmed:()=>StormPackPanel(frame));else RescueWorkshop(frame);},Ui.Raised,true,17);
            Ui.Button(card,"테마 작전으로",24,772,444,43,RobotSeasonHub,Ui.Raised,true,17);
        }
        void RobotSeasonResult()
        {
            var receipt=State.receipts.Find(r=>r.id==combat.RunId);bool preview=combat.Mode==RobotSeason.Preview;
            var card=Dialog(preview?"폭풍 구조대 체험 완료":"구조 임무 결과",preview?"체험 외형은 실제 보유 외형을 변경하지 않아요.":"목표: 90초 생존 + 특화 로봇 15처치",620);
            Ui.Label(card,TimeLabel(combat.Elapsed)+" 생존\n로봇 처치 "+(receipt==null?0:receipt.score)+"\n획득 코어 파편 "+(receipt==null?0:receipt.shards),24,170,444,140,26,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Button(card,"테마 작전으로",24,350,444,54,()=>{combat=null;Show("weekly");RobotSeasonHub();},Ui.Mint,true,18);
            Ui.Button(card,preview?"외형 구성 다시 보기":"무료 제작 외형 보기",24,430,444,50,()=>{combat=null;Show("weekly");if(preview)StormPackPanel();else RescueWorkshop();},Ui.Raised,true,17);
            Ui.Label(card,preview?"체험은 재화·숙련·주간 기록에 반영되지 않아요.":"첫 달성과 주간 보상은 중복 지급되지 않아요.",24,520,444,50,15,Ui.Muted,TextAnchor.MiddleCenter);
            card.GetChild(2).gameObject.SetActive(false);
        }
    }
}
