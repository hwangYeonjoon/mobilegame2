using System;
using System.Collections;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
using Social=ScrapGuard.Core.Social;
namespace ScrapGuard
{
    public partial class GameApp
    {
        string CurrentWeek()
        {
            DateTimeOffset time;
            return Endgame.Week(Online && cloud.Latest!=null && DateTimeOffset.TryParse(cloud.Latest.serverTime,out time)?time:DateTimeOffset.UtcNow);
        }
        void WeeklyHub()
        {
            string week=CurrentWeek();var progress=Endgame.Find(State,week);
            var hero=Ui.Box(screen,24,176,492,204,Ui.Panel).transform;
            var core=Ui.Box(hero,321,26,145,146,Color.white,false);core.sprite=BattleArt.Role(4,0,true);core.preserveAspect=true;
            Ui.Label(hero,"주간 성장 도전 / 90초",20,15,300,27,12,Ui.Mint);
            Ui.Label(hero,"균열의 코어를 공략하세요",20,51,306,40,23);
            Ui.Label(hero,"이번 주 최고 기록",20,99,270,29,15,Ui.Muted);
            Ui.Label(hero,(progress==null?0:progress.bestScore).ToString("N0")+"점",20,131,290,50,35,Ui.Gold);
            Ui.Label(screen,week+" 주간 · 월요일 오전 5시 갱신",24,390,492,29,14,Ui.Muted);
            Ui.Label(screen,"코어에 준 피해량이 점수 · 30초 이상 참여 시 기록\n본인 대원·장비로 진행 · 길드 지원 없이 도전",24,428,492,52,15,Ui.Muted);
            Ui.Button(screen,Endgame.Unlocked(State)?"성장 도전 시작":"작전 05 완료 후 성장 도전",24,496,492,56,()=>StartBattle(4,false,Endgame.WeeklyMode),null,Endgame.Unlocked(State),19);
            Ui.Button(screen,"보상 없는 연습",24,566,238,50,()=>StartBattle(4,false,Endgame.PracticeMode),Ui.Raised,true,17);
            Ui.Button(screen,"이번 주 기록판",278,566,238,50,()=>{if(Online && cloudReady)StartCoroutine(LoadWeeklyBoard());else Info("서버 기록판","서버 계정의 공개 동의한 기록이 표시돼요.\n계정 화면에서 서버에 연결해 주세요.");},Ui.Raised,true,17);
            Ui.Button(screen,"달성 보상 · 트로피",24,635,492,55,()=>WeeklyRewards(week),Ui.Gold,true,18);
            Ui.Button(screen,"내 전시대 보기",24,708,238,48,()=>TrophyRoom(Social.Build(State,Online?cloud.Session.playerId:"",!Online)),Ui.Raised,true,16);
            Ui.Button(screen,"파편 교환소",278,708,238,48,CoreExchange,Ui.Raised,true,16);
            Ui.Label(screen,Online ? "프로필 공개 시 기록판에 참여해요.\n순위 보상 없이 개인 달성 보상을 받아요." : "로컬 기록은 서버 순위에 등록되지 않아요.\n작전 05 이전에도 연습할 수 있어요.",24,775,334,65,13,Ui.Muted);
            Ui.Button(screen,"기록 카드",374,780,142,52,WeeklyRecordCard,Ui.Raised,true,16);
            Ui.Button(screen,"동일 조건 실력전 · 별도 순위",24,830,492,30,SkillTrialHub,Ui.Mint,true,14);
        }
        void WeeklyRewards(string week)
        {
            var progress=Endgame.Find(State,week);var card=Dialog("코어 도전 보급품",week+" 주간 · 단계마다 한 번씩 수령\n기간이 지나도 달성한 미수령 보상은 남아요.",790);
            for(int i=0;i<4;i++)
            {
                int tier=i;float y=145+i*131;bool earned=progress!=null && progress.attempts>0 && progress.bestScore>=Endgame.Goals[i];bool claimed=progress!=null && (progress.claimed&(1<<i))!=0;
                Ui.Box(card,24,y,444,116,Ui.Raised);
                Ui.Label(card,i==0?"30초 이상 참여":"코어 "+Endgame.Goals[i].ToString("N0")+"점",39,y+10,276,30,20,earned?Ui.Mint:Ui.Ink);
                Ui.Label(card,"골드 "+Endgame.Gold[i]+" · 부품 "+Endgame.Parts[i]+"\n코어 파편 "+Endgame.Shards[i]+(Endgame.Gems[i]>0?" · 보석 "+Endgame.Gems[i]:""),39,y+49,271,49,15,Ui.Gold);
                Ui.Button(card,claimed?"수령 완료":earned?"받기":"도전 중",337,y+31,116,52,()=>Change(new GameCommand("weekly_claim"){value=week,stage=tier},()=>WeeklyRewards(week)),null,earned&&!claimed,16);
            }
            Ui.Button(card,"지난 주 보상 보기",24,691,444,54,PastWeeklyRewards,Ui.Raised,true,16);
        }
        void PastWeeklyRewards()
        {
            var card=Dialog("지난 도전 기록","주간별 최고 기록과 미수령 보상을 확인하세요.",740);
            var list=Ui.Scroll(card,24,140,444,560,Math.Max(560,State.weekly.Count*88));
            if(State.weekly.Count==0)Ui.Label(list,"아직 성장 도전 기록이 없어요.",0,120,444,70,19,Ui.Muted,TextAnchor.MiddleCenter);
            for(int i=0;i<State.weekly.Count;i++)
            {
                var w=State.weekly[State.weekly.Count-1-i];
                Ui.Button(list,w.week+" · "+w.bestScore.ToString("N0")+"점",0,i*88,444,70,()=>WeeklyRewards(w.week),Ui.Raised,true,18);
            }
        }
        void ChallengeResult()
        {
            var receipt=State.receipts.Find(r=>r.id==combat.RunId);bool practice=combat.Mode==Endgame.PracticeMode;bool skill=combat.Mode==SkillTrial.Mode;
            var card=Dialog(skill?"동일 조건 실력 기록":practice?"코어 도전 연습 완료":"이번 도전 기록",skill?"고정 대원·장비로 도전한 기록 · 재화 보상 없음":practice?"연습은 순위와 보상에 반영되지 않아요.":"30초 이상 진행한 최고 기록이 저장됩니다.",780);
            Ui.Label(card,combat.CoreScore.ToString("N0")+"점",24,155,444,76,46,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Label(card,TimeLabel(combat.Elapsed)+" 생존 · 처치 "+combat.Kills,24,243,444,38,19,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Label(card,skill?"모든 참가자가 같은 능력치와 스킬 후보로 겨룹니다.":practice?"장비와 대원을 키워 더 높은 기록에 도전하세요.":combat.Elapsed<30?"30초 미만이라 기록·보상이 반영되지 않았어요.":"내 전시대에서 최고 기록과 트로피를 볼 수 있어요.",24,302,444,65,17,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,"다시 도전",24,402,214,59,()=>{string mode=combat.Mode;combat=null;StartBattle(4,false,mode);});
            Ui.Button(card,"도전 로비",254,402,214,59,()=>{combat=null;Show("weekly");},Ui.Raised);
            Ui.Button(card,"달성 보상 확인",24,483,444,56,()=>{string week=receipt==null?CurrentWeek():receipt.week;combat=null;Show("weekly");WeeklyRewards(week);},Ui.Gold,!practice&&!skill&&combat.Elapsed>=30,18);
            Ui.Button(card,"전투 분석",24,565,214,51,BattleAnalysis,Ui.Raised,true,17);
            Ui.Button(card,"주간 기록 카드",254,565,214,51,()=>WeeklyRecordCard(skill),Ui.Mint,!practice&&combat.Elapsed>=30,17);
            Ui.Button(card,"도전 로비로",24,677,444,45,()=>{combat=null;Show("weekly");},Ui.Raised,true,16);
            card.GetChild(2).gameObject.SetActive(false);
        }
        IEnumerator LoadWeeklyBoard(bool skill=false)
        {
            if(networkBusy)yield break;SetNetworkBusy(true);WeeklyBoard board=null;
            yield return cloud.Get(skill?"/v1/skill":"/v1/weekly",(body,status)=>{if(status==200)board=JsonUtility.FromJson<WeeklyBoard>(body);});SetNetworkBusy(false);
            if(board==null){Toast("기록판을 불러오지 못했어요.");yield break;}
            var card=Dialog(skill?"주간 동일 조건 기록판":"주간 성장 기록판",board.week+" 주간 · 공개한 서버 기록만 표시\n"+(board.myRank>0?"내 순위 "+board.myRank+"위 · "+board.myScore.ToString("N0")+"점":"공개된 내 기록이 아직 없어요."),870);
            var list=Ui.Scroll(card,24,142,444,620,Math.Max(620,board.entries.Count*100));
            if(board.entries.Count==0)Ui.Label(list,"첫 기록의 주인공이 되어 보세요.\n작전 05 완료 후 성장 도전에 참여하세요.",12,170,420,100,20,Ui.Muted,TextAnchor.MiddleCenter);
            for(int i=0;i<board.entries.Count;i++)
            {
                var entry=board.entries[i];float y=i*100;
                Ui.Box(list,0,y,444,90,Ui.Raised);Ui.Label(list,entry.rank.ToString(),10,y+20,40,45,23,Ui.Gold,TextAnchor.MiddleCenter);
                CharacterArt.Draw(list,50,y+9,55,67,entry.hero,entry.skin,entry.outfit);
                Ui.Label(list,entry.name,115,y+10,210,32,19);Ui.Label(list,entry.score.ToString("N0")+"점",115,y+47,210,28,17,Ui.Mint);
                Ui.Button(list,"전시실",334,y+23,98,45,()=>StartCoroutine(LoadPublic(entry.id)),Ui.Panel,true,15);
            }
            Ui.Label(card,"상위 50명 · 동점은 먼저 달성한 기록이 우선\n차단한 계정은 서로의 기록판에 표시되지 않아요.",24,782,444,58,13,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void TrophyRoom(PublicProfile p)
        {
            var card=Dialog(p.name+"의 전시대","성장한 대원과 플레이로 획득한 트로피",870);
            Ui.Box(card,24,139,444,330,Ui.Background);
            CharacterArt.Draw(card,122,151,248,263,p.hero,p.skin,p.outfit);
            Ui.Label(card,Catalog.HeroNames[p.hero]+" Lv."+p.heroLevel,24,422,444,32,22,Ui.Mint,TextAnchor.MiddleCenter);
            string[] names={"첫 구역 정복","천 대의 증인",p.trophy<0?"코어 도전자":Endgame.TrophyNames[p.trophy]};
            bool[] earned={p.stages>0,p.kills>=1000,p.trophy>=0};
            for(int i=0;i<3;i++)
            {
                float x=24+i*151;Ui.Box(card,x,490,142,146,Ui.Raised);
                Ui.Disc(card,x+47,506,48,earned[i]?Ui.Gold:Ui.Panel);
                Ui.Label(card,earned[i]?"★":"·",x+47,506,48,48,30,earned[i]?Ui.Background:Ui.Muted,TextAnchor.MiddleCenter);
                Ui.Label(card,names[i],x+6,568,130,50,15,earned[i]?Ui.Gold:Ui.Muted,TextAnchor.MiddleCenter);
            }
            Ui.Label(card,"코어 최고 기록  "+p.coreBest.ToString("N0")+"점",24,663,444,48,25,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Label(card,"도색은 취향 · 트로피는 직접 달성한 성취\n"+(p.local?"로컬 체험의 전시대입니다.":"공개 프로필에서 다른 수호대가 볼 수 있어요."),24,726,444,64,16,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,"확인",24,802,444,45,CloseModal,Ui.Raised,true,17);
        }
    }
}
