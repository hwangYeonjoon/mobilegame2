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
            var card=Dialog("보스 패턴 연습","입장료·보상 없음 · 장착 장비 + 연습용 스킬\n보스부터 시작 · 실제 작전 승리를 보장하지 않아요.",820);
            var list=Ui.Scroll(card,24,145,444,590,Math.Max(590,Campaign.Count*55));int row=0;
            for(int i=0;i<Campaign.Count;i++)
            {
                int stage=i;if(!BossTraining.Available(State,stage))continue;
                Ui.Button(list,"작전 "+(stage+1)+" · "+Campaign.Get(stage).Boss,0,row++*60,444,50,()=>{combat=null;StartBattle(stage,false,BossTraining.Mode);},Ui.Raised,true,17);
            }
            Ui.Button(card,"성장 화면",24,752,444,48,GrowthPanel,Ui.Mint,true,18);
        }
        void BossPracticeResult()
        {
            int stage=combat.Stage;var report=combat.Report();
            var card=Dialog("보스 연습 결과",Campaign.Get(stage).Boss+" · 재화와 작전 진행은 변하지 않아요.",650);
            Ui.Label(card,combat.Won?"패턴 연습 성공":combat.Elapsed>=90?"90초 연습 완료":"다시 연습할 수 있어요",24,141,444,45,26,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Label(card,BossTraining.Advice(report),24,203,444,78,19,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Label(card,report.lastHitDamage>0?"마지막 피해: "+report.lastHitTime.ToString("0.0")+"초 · 체력 −"+Mathf.CeilToInt(report.lastHitDamage):"받은 체력 피해 없음",24,297,444,40,17,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,"같은 보스 다시 연습",24,369,444,54,()=>{combat=null;StartBattle(stage,false,BossTraining.Mode);},Ui.Mint,true,18);
            Ui.Button(card,"실제 작전에 도전",24,438,444,54,()=>{combat=null;StartBattle(stage);},Ui.Raised,true,18);
            Ui.Button(card,"다른 보스 선택",24,511,214,52,BossTrainingPanel,Ui.Raised,true,16);
            Ui.Button(card,"정비소",254,511,214,52,()=>{combat=null;Show("home");},Ui.Raised,true,18);
        }
        void SalvagePanel()
        {
            int stage=Growth.SalvageStage(State),left=Math.Max(0,3-State.dailySalvageWins);
            var card=Dialog("보석을 아끼는 회수 임무","완료한 작전에서 90초 이상 전투 후 3회 승리\n하루 한 번 · 광고나 보석 사용 없이 획득",700);
            Ui.Label(card,"골드 4,500 + 공용 부품 40",24,146,444,42,26,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Label(card,"성장 보급 1개의 재료와 같아요.\n작전 자체의 반복 보상도 함께 받아요.",24,210,444,64,18,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Label(card,"오늘 "+State.dailySalvageWins+" / 3회 승리",24,294,444,38,22,Ui.Mint,TextAnchor.MiddleCenter);
            string estimate=stage<0?"먼저 첫 작전을 완료하세요.":"추천: 작전 "+(stage+1)+" · "+Campaign.Get(stage).Objective+"\n남은 "+left+"회 전투 약 "+Math.Ceiling(left*(Campaign.Get(stage).Duration+(Campaign.Get(stage).Mission==Mission.Eliminate?30:0))/60f)+"분 + 선택·메뉴 시간\n패배하면 시간이 더 필요해요.";
            Ui.Label(card,estimate,24,351,444,91,16,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,State.salvageClaimed?"오늘 수령 완료":"회수 보상 받기",24,464,444,54,()=>Change(new GameCommand("salvage_claim"),confirmed:()=>GrowthPanel("회수 보급 수령 · 원하는 장비를 강화하세요.")),Ui.Gold,!State.salvageClaimed&&left==0,19);
            Ui.Button(card,"추천 작전 반복",24,535,214,53,()=>{combat=null;StartBattle(stage);},Ui.Mint,stage>=0,18);
            Ui.Button(card,"주간 로봇 과제",254,535,214,53,()=>{combat=null;Show("weekly");},Ui.Raised,true,17);
            Ui.Button(card,"성장 화면",24,611,444,45,GrowthPanel,Ui.Raised,true,17);
        }
    }
}
