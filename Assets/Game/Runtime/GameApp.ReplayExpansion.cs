using System;
using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator ReplayExpansionCapture()
        {
            foreach(string mode in new[]{Endgame.WeeklyMode,BossTraining.Mode})foreach(int seed in new[]{17,41,71})
            {
                var s=GameState.New();s.tutorialCompleted=true;s.heroes[0].level=10;foreach(var item in s.items)item.level=10;
                int stage=mode==BossTraining.Mode?17:4;var c=new Combat(s,stage,"expansion-"+mode+seed,seed,false,mode);var input=new ReplayRecorder();
                for(int frame=0;frame<3000&&!c.Ended;frame++)
                {
                    if(c.Choosing){int pick=c.Choices[0];c.Choose(pick);input.Choose(pick);}
                    int design=c.WeeklyFrame*4+1;
                    if(c.CanPlanRobot(design,true)){c.PlanRobot(design,true);input.RobotPlan(design,true);}
                    if(c.CanActivateMachine){c.ActivateMachine();input.Machine();}
                    if(c.CanUltimate){c.UseUltimate();input.Ultimate();if(c.Ended)break;}
                    var target=new V2(270+Mathf.Cos(c.Elapsed*.25f)*180,500+Mathf.Sin(c.Elapsed*.25f)*230);
                    c.Tick(ReplayRecorder.Step,input.Move((target-c.Position).Normal));if(frame%120==0)yield return null;
                }
                if(!c.Ended)c.Abandon();
                File.WriteAllText(Path.Combine(smokeOutput,"runtime-replay-"+mode+"-"+seed+".json"),JsonUtility.ToJson(new AssaultReplayProbe{state=s,steps=input.Steps,id=c.RunId,mode=mode,stage=stage,seed=seed,kills=c.Kills,machines=c.MachinesBuilt,waves=c.WaveNumber,hp=c.Hp,seconds=c.Elapsed,won=c.Won}));
            }
        }
        void BossTrainingPanel()
        {
            var card=Dialog("작전 패턴 연습","입장료·보상 없음 · 장착 장비 + 연습용 스킬\n생존은 일반 적 60초 · 토벌은 보스부터 시작",820);
            var list=Ui.Scroll(card,24,145,444,590,Math.Max(590,Math.Min(Campaign.Count,State.unlockedStage+1)*60));int row=0;
            for(int i=0;i<Campaign.Count;i++)
            {
                int stage=i;if(!CombatTraining.Available(State,stage))continue;
                bool survival=Campaign.Get(stage).Mission==Mission.Survive;
                Ui.Button(list,"작전 "+(stage+1)+" · "+(survival?"일반 적 60초":Campaign.Get(stage).Boss),0,row++*60,444,50,()=>{combat=null;StartBattle(stage,false,CombatTraining.ModeFor(stage));},Ui.Raised,true,17);
            }
            Ui.Button(card,"성장 화면",24,752,444,48,GrowthPanel,Ui.Mint,true,18);
        }
        void BossPracticeResult()
        {
            int stage=combat.Stage;var report=combat.Report();bool survival=combat.SurvivalPractice;
            var card=Dialog(survival?"생존 연습 결과":"보스 연습 결과",(survival?Campaign.Regions[Campaign.Get(stage).Region]:Campaign.Get(stage).Boss)+" · 재화·작전 진행에 반영되지 않아요.",650);
            Ui.Label(card,combat.Won?(survival?"60초 연습 완료":"패턴 연습 성공"):combat.Elapsed>=combat.Duration?"연습 완료":"다시 연습할 수 있어요",24,141,444,45,26,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Label(card,BossTraining.Advice(report),24,203,444,78,19,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Label(card,report.lastHitDamage>0?CombatTraining.SourceName(report.lastSource)+" · "+report.lastHitTime.ToString("0.0")+"초 · 체력 −"+Mathf.CeilToInt(report.lastHitDamage):"받은 체력 피해 없음",24,297,444,40,17,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,survival?"같은 지역 다시 연습":"같은 보스 다시 연습",24,369,444,54,()=>{combat=null;StartBattle(stage,false,CombatTraining.ModeFor(stage));},Ui.Mint,true,18);
            Ui.Button(card,"실제 작전에 도전",24,438,444,54,()=>{combat=null;StartBattle(stage);},Ui.Raised,true,18);
            Ui.Button(card,"다른 작전 연습",24,511,214,52,BossTrainingPanel,Ui.Raised,true,16);
            Ui.Button(card,"정비소",254,511,214,52,()=>{combat=null;Show("home");},Ui.Raised,true,18);
        }
        void SalvagePanel()
        {
            int left=Math.Max(0,300-State.dailySupplySeconds);
            var card=Dialog("진행하며 받는 현장 보급","일반·극한·주간 도전의 전투 시간 합산\n승패 무관 · 하루 한 번 · 보석과 광고 불필요",700);
            Ui.Label(card,"골드 4,500 + 공용 부품 40",24,146,444,42,26,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Label(card,"이미 깬 작전을 반복할 필요 없어요.\n60초 이상 진행한 전투만 정산 후 합산돼요.",24,210,444,64,18,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Label(card,"오늘 "+(300-left)+" / 300초",24,294,444,38,22,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Label(card,"남은 전투 시간 "+(left/60)+"분 "+(left%60)+"초\n모든 연습·실력전·짧은 중단은 제외\n메뉴와 일시정지 시간은 포함되지 않아요.",24,351,444,91,16,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,State.salvageClaimed?"오늘 수령 완료":"현장 보급 받기",24,464,444,54,()=>Change(new GameCommand("salvage_claim"),confirmed:()=>GrowthPathPanel("현장 보급 수령 · 강화 경로를 선택하세요.")),Ui.Gold,!State.salvageClaimed&&left==0,19);
            Ui.Button(card,"작전 이어가기",24,535,214,53,()=>{combat=null;StartBattle(State.unlockedStage);},Ui.Mint,true,18);
            Ui.Button(card,"주간 로봇 과제",254,535,214,53,()=>{combat=null;Show("weekly");},Ui.Raised,true,17);
            Ui.Button(card,"성장 화면",24,611,444,45,GrowthPanel,Ui.Raised,true,17);
        }
    }
}
