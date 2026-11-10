using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator Content034Smoke()
        {
            Store.Change(e=>{e.State.gems=RobotStyles.PackPrice;e.State.gold=1800;e.State.parts=30;e.State.coreShards=12;e.State.items.Find(i=>i.id==e.State.equipped[0]).level=1;return CommandResult.Ok("Isolated cosmetics QA wallet and Lv.1 weapon");});
            RobotStylePanel();yield return Capture("24-robot-style-premium");
            RobotPackConfirm(0);yield return Capture("25-pack-preview");
            ClickVisibleButton("600 보석 구매 · 외형 3종 적용");yield return Capture("26-pack-cosmetic-equipped");
            if(State.gems!=0||!RobotStyles.Owned(State,0,1)||State.gold!=1800)smokeErrors.Add("Robot pack UI grant mismatch");
            GrowthPathPanel("보유 재료로 별도 강화 · 외형 구매는 재료를 지급하지 않아요.");
            var q=GrowthPaths.Quote(State,0);int attack=Catalog.Attack(State);
            ClickVisibleButton("골드 "+q.gold+" · 부품 "+q.parts+" 사용해 "+q.count+"단계 강화");yield return Capture("27-bulk-confirm");
            ClickVisibleButton("골드 "+q.gold+" · 부품 "+q.parts+" 사용");yield return Capture("28-bulk-applied");
            if(Catalog.Attack(State)!=q.attack||Catalog.Attack(State)<=attack)smokeErrors.Add("Batch upgrade UI mismatch");
            Store.Change(e=>{e.State.gold=30000;e.State.parts=300;return CommandResult.Ok("Craft cost fixture");});
            RobotStylePanel(1,2);yield return Capture("29-crafted-style-preview");
            ClickVisibleButton("재료를 사용해 제작");yield return Capture("30-craft-confirm");ClickVisibleButton("골드·부품·파편 사용 후 적용");yield return Capture("31-crafted-style-equipped");
            if(RobotStyles.Active(State,1)!=2||State.gold!=0||State.parts!=0||State.coreShards!=0)smokeErrors.Add("Craft UI ownership/debit mismatch");
            foreach(int region in new[]{7,8,9})
            {
                CampaignMap(region);yield return Capture("32-map-"+region);RegionCollection(region);yield return Capture("33-collection-"+region);
                StartBattle(region*6);yield return null;battlePaused=true;RenderBattle();yield return Capture("34-terrain-"+region);
                combat.Abandon();FinishBattle();combat=null;
                StartBattle(region*6,false,BossTraining.Mode);yield return null;battlePaused=true;
                // UI-only fast-forward to first distinctive boss warning; this is not performance evidence.
                for(int i=0;i<65&&!combat.Ended;i++)combat.Tick(ReplayRecorder.Step,new V2());
                RenderBattle();yield return Capture("35-boss-"+region);combat.Abandon();FinishBattle();combat=null;
            }
            foreach(int frame in new[]{0,1,2})
            {
                StartBattle(4,false,Endgame.WeeklyMode);yield return null;battlePaused=true;
                int design=frame*4+1;combat.PlanRobot(design,true);combat.ActivateMachine();RenderBattle();yield return Capture("36-robot-battle-"+frame);
                // Above is an isolated render fixture. Cancel without claiming unverifiable input.
                CloseModal();Store.Change(e=>{e.State.activeRun=e.State.activeMode=e.State.activeWeek="";return CommandResult.Ok("Close render fixture");});combat=null;
            }
            yield return ApexSmoke();
            foreach(int stage in new[]{42,47,48,53,54,59})
            {
                var s=GameState.New();s.tutorialCompleted=true;s.heroes[0].level=30;foreach(var item in s.items){item.level=20;item.rarity=2;}
                var c=new Combat(s,stage,"expansion034-"+stage,71,false,BossTraining.Mode);var input=new ReplayRecorder();
                for(int i=0;i<1800&&!c.Ended;i++)
                {
                    if(c.Choosing){int pick=c.Choices[0];c.Choose(pick);input.Choose(pick);}
                    var target=new V2(270+Mathf.Cos(c.Elapsed*.25f)*160,500+Mathf.Sin(c.Elapsed*.25f)*230);
                    c.Tick(ReplayRecorder.Step,input.Move((target-c.Position).Normal));if(i%120==0)yield return null;
                }
                if(!c.Ended)c.Abandon();
                File.WriteAllText(Path.Combine(smokeOutput,"runtime-replay-new-boss-"+stage+".json"),JsonUtility.ToJson(new AssaultReplayProbe{state=s,steps=input.Steps,id=c.RunId,mode=BossTraining.Mode,stage=stage,seed=71,kills=c.Kills,machines=c.MachinesBuilt,waves=c.WaveNumber,hp=c.Hp,seconds=c.Elapsed,won=c.Won}));
            }
        }
    }
}
