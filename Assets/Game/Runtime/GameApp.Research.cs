using System;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;

namespace ScrapGuard
{
    public partial class GameApp
    {
        PlaytestJournal research;
        int frameSamples,slowFrames;double frameSeconds;
        void SetupResearch(string profile)
        {
            research=new PlaytestJournal(Path.Combine(Path.GetDirectoryName(profile),"playtest-journal.json"));
            Track("session","start");
        }
        void Track(string kind,string subject,int number=0,float value=0)
        {research?.Add(kind,subject,number,value,smokeActive || Array.IndexOf(Environment.GetCommandLineArgs(),"-smoke")>=0 || State.testProgress || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SCRAP_TEST_RUN")));}
        void MeasureFrame()
        {
            if(combat==null||combat.Ended||battlePaused||combat.Choosing)return;
            float delta=Time.unscaledDeltaTime;if(delta<=0)return;
            frameSamples++;frameSeconds+=delta;if(delta>1f/30+.001f)slowFrames++;
        }
        void ResearchPanel()
        {
            var card=Dialog("플레이 테스트 기록","기록은 이 기기에만 저장됩니다. 자동 전송하지 않아요.",760);
            Ui.Label(card,"선택 동의 시 화면 이동, 전투 결과, 보석 증감,\\n의상 관심도와 전투 프레임을 기록합니다.\\n이름·계정 코드·광고 식별자는 기록하지 않아요.\\n최근 14일, 최대 2,000건을 보관합니다.".Replace("\\n","\n"),24,140,444,133,16,Ui.Muted);
            Ui.Button(card,research.Data.consent?"기록 켜짐 · 끄기":"기록 꺼짐 · 동의하고 켜기",24,300,444,53,()=>{research.Consent(!research.Data.consent);if(research.Data.consent)Track("session","start");ResearchPanel();},Ui.Mint,true,17);
            Ui.Label(card,"저장된 기록 "+research.Data.events.Count+"건\n전투 중 느린 프레임 "+slowFrames+" / "+frameSamples,24,374,444,62,16,Ui.Muted);
            Ui.Button(card,"기록 파일 내보내기",24,465,444,52,()=>{
                try {string path=research.Export();GUIUtility.systemCopyBuffer=path;Toast("파일 저장 완료 · 경로를 복사했어요.");}
                catch(IOException){Toast("저장 공간을 확인해 주세요.");}
            },Ui.Raised,true,17);
            Ui.Button(card,"기록 삭제 · 참여 식별자 초기화",24,538,444,52,()=>{research.Clear();ResearchPanel();},Ui.Raised,true,16);
            Ui.Label(card,"내보낸 파일을 전달할지는 직접 선택하세요.\n기록을 꺼도 진행 상황과 보상은 달라지지 않습니다.\n삭제는 기기 내 기록에 적용됩니다.",24,621,444,93,15,Ui.Muted);
        }
        void GemWalletPanel()
        {
            var card=Dialog("보석 잔액","무료 보석부터 사용하며 구매 효력은 같아요.",460);
            Ui.Label(card,"총 보석 "+State.gems.ToString("N0")+"개\n무료 "+(State.gems-State.paidGems).ToString("N0")+" · 유료 "+State.paidGems.ToString("N0"),24,141,444,101,25,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Label(card,State.gemDebt>0?"환불 조정 "+State.gemDebt+"보석\n일반 플레이는 가능해요. 보석 소비는 조정 후 이용할 수 있어요.":"환불 조정 내역 없음\n구매 가능 여부는 Play 상점에서 확인해 주세요.",24,277,444,105,16,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void StoreOfferPanel(StoreOffer offer)
        {
            Track("offer_view",offer.Id);
            var card=Dialog(offer.Name,"구성 안내 · 실제 가격은 Play 상점에서 확인해요.",650);
            Ui.Label(card,offer.ProposedWon.ToString("N0")+"원",24,140,444,62,36,Ui.Gold,TextAnchor.MiddleCenter);
            string benefit=offer.Gems>0?"보석 "+offer.Gems.ToString("N0")+"개 확정 지급\n무료 보석과 사용 효력이 같아요.\n캐릭터·의상·장비·부품을 선택해 구매해요.":"광고 연결 후 제공할 영구 권리 · 계정 복원 지원\n결과 추가 골드 하루 3회 + 부품 하루 2회\n보상 횟수를 늘리지 않고 광고 시청만 생략해요.";
            Ui.Label(card,benefit,24,227,444,120,18,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Label(card,"표시 금액은 제안가이며 청구 금액이 아닙니다.\n실제 결제 전 Google Play 가격을 확인해 주세요.",24,376,444,64,15,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,"Play 상점 · 가격 확인",24,460,444,54,ConnectPlayBilling,Ui.Mint,Online,17);
            Ui.Button(card,"관심 표시 · 결제 아님",24,532,444,54,()=>{Track("offer_interest",offer.Id);Toast(research.Data.consent?"이 기기의 테스트 기록에 남겼어요.":"관심도 기록은 설정에서 동의한 경우에만 저장돼요.");},Ui.Mint,true,17);
        }
        void ConceptRoom(){OutfitCollection();}
        void SkillTrialHub()
        {
            var progress=State.skillWeekly.Find(w=>w.week==CurrentWeek());
            var card=Dialog("동일 조건 실력전","성장 기록과 분리된 주간 순위 · 90초",730);
            Ui.Label(card,"모두 같은 정비사 루크 Lv.10\n기본 장비 Lv.10 · 고급 등급\n캐릭터·장비 구매와 길드 지원의 영향 없음",24,139,444,114,21,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Label(card,"이번 주 "+(progress==null?0:progress.bestScore).ToString("N0")+"점",24,280,444,54,30,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Label(card,"같은 주에는 적 배치와 성장 선택 난수가 같아요.\n외형은 유지하지만 전투 판정은 모두 같습니다.\n30초 이상 기록 · 재화·일일 임무 보상 없음",24,357,444,99,16,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,Endgame.Unlocked(State)?"동일 조건 도전 시작":"작전 05 완료 후 도전",24,495,444,58,()=>StartBattle(4,false,SkillTrial.Mode),Ui.Mint,Endgame.Unlocked(State),18);
            Ui.Button(card,"동일 조건 기록판",24,578,444,51,()=>{if(Online&&cloudReady)StartCoroutine(LoadWeeklyBoard(true));else Toast("서버 연결 후 공개 기록을 확인할 수 있어요.");},Ui.Raised,true,17);
            Ui.Button(card,"성장 도전으로 돌아가기",24,651,444,46,()=>Show("weekly"),Ui.Raised,true,16);
        }
    }
}
