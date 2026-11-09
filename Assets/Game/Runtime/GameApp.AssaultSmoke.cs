using System;
using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        [Serializable] sealed class AssaultReplayProbe
        {
            public GameState state;public System.Collections.Generic.List<ReplayStep> steps;
            public string id,mode;public int stage,seed,kills,machines,waves;public float hp,seconds;public bool won;
        }
        IEnumerator AssaultReplayCapture()
        {
            foreach(int stage in new[]{0,31})
            {
                var snapshot=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(State));snapshot.heroes[snapshot.selectedHero].level=30;
                foreach(var item in snapshot.items){item.level=20;item.rarity=2;}
                var run=new Combat(snapshot,stage,"cross-runtime-"+stage,71);var input=new ReplayRecorder();
                for(int frame=0;frame<6000&&!run.Ended;frame++)
                {
                    if(run.Choosing){int skill=run.Choices[0];foreach(int candidate in run.Choices)if(candidate>=8){skill=candidate;break;}run.Choose(skill);input.Choose(skill);}
                    int design=1+run.MachinesBuilt%9;if(run.CanPlanRobot(design,true)){run.PlanRobot(design,true);input.RobotPlan(design,true);}
                    if(run.CanActivateMachine){run.ActivateMachine();input.Machine();}
                if(run.CanUltimate){run.UseUltimate();input.Ultimate();if(run.Ended)break;}
                    var target=new V2(270+Mathf.Cos(run.Elapsed*.06f)*450,490+Mathf.Sin(run.Elapsed*.06f)*600);
                    run.Tick(ReplayRecorder.Step,input.Move((target-run.Position).Normal));
                    if(frame%120==0)yield return null;
                }
                run.Abandon();
                var probe=new AssaultReplayProbe{state=snapshot,steps=input.Steps,id=run.RunId,mode=run.Mode,stage=stage,seed=71,kills=run.Kills,machines=run.MachinesBuilt,waves=run.WaveNumber,hp=run.Hp,seconds=run.Elapsed,won=run.Won};
                File.WriteAllText(Path.Combine(smokeOutput,"runtime-replay-"+stage+".json"),JsonUtility.ToJson(probe));
            }
        }
        IEnumerator RobotAssemblySmoke()
        {
            int priorView=PlayerPrefs.GetInt("scrapguard.robot.clear",1);PlayerPrefs.SetInt("scrapguard.robot.clear",1);
            OpenBattle(0,"robot-visual-fixture",17,false);battlePaused=true;
            for(int id=1;id<=9;id++){RobotAssembler(id);yield return Capture("robot-design-"+id);}
            CloseModal();
            foreach(int id in new[]{1,5,9})
            {
                OpenBattle(0,"robot-frame-fixture-"+id,17,false);battlePaused=true;
                combat.Pickups.Add(new Pickup{id=9000,position=combat.Position,xp=40});
                for(int part=0;part<3;part++)combat.Pickups.Add(new Pickup{id=9100+part,position=combat.Position,part=part,partCount=12});
                combat.Tick(.05f,new V2());if(combat.Choosing)combat.Choose(combat.Choices[0]);
                combat.PlanRobot(id,true);combat.PlanRobot(id,true);combat.ActivateMachine(id);
                for(int n=0;n<210;n++)combat.Enemies.Add(new Enemy{id=10000+n,position=combat.Position+V2.Angle(n*.8f)*(65+n%30*9),hp=5000,maxHp=5000});
                RenderBattle();yield return Capture("robot-crowded-entry-"+id);
                for(int frame=0;frame<45;frame++){if(combat.Choosing)combat.Choose(combat.Choices[0]);combat.Tick(ReplayRecorder.Step,new V2(.3f,.1f));}
                RenderBattle();yield return Capture("robot-frame-"+id);
                if(id==5){float hp=combat.Hp,time=combat.Elapsed;int count=combat.Enemies.Count;PlayerPrefs.SetInt("scrapguard.robot.clear",0);RenderBattle();yield return Capture("robot-visibility-large");PlayerPrefs.SetInt("scrapguard.robot.clear",1);RenderBattle();yield return Capture("robot-visibility-clear");if(combat.Hp!=hp||combat.Elapsed!=time||combat.Enemies.Count!=count)smokeErrors.Add("Robot visibility preference changed simulation");}
                var records=combat.RobotReport();Store.Change(e=>{RobotAssembly.Record(e.State,records,0);return CommandResult.Ok("Isolated robot visual fixture");});
            }
            // Discovery is a suggestion: no forced modal, spend, or activation.
            OpenBattle(0,"robot-prompt-fixture",17,false);battlePaused=false;CloseModal();
            for(int part=0;part<3;part++)combat.Pickups.Add(new Pickup{id=9300+part,position=combat.Position,part=part,partCount=4});
            for(int step=0;step<10;step++)combat.Tick(ReplayRecorder.Step,new V2());RenderBattle();
            if(!RobotPromptActive||battlePaused||combat.MachinesBuilt!=0||combat.RobotPartsSpent!=0)smokeErrors.Add("Ready prompt must not force boarding or spend parts");
            yield return Capture("robot-ready-prompt");
            playerInDanger=true;RenderUnifiedNotice();if(combatNotice.text!="위험 구역 밖으로 이동!")smokeErrors.Add("Prompt covered a danger warning");playerInDanger=false;
            TriggerMachine();if(!battlePaused||RobotPromptActive||combat.RobotPartsSpent!=0)smokeErrors.Add("Prompt tap must open a ready design without spending");
            yield return Capture("robot-prompt-workshop");
            foreach(var button in modal.GetComponentsInChildren<UnityEngine.UI.Button>())if(button.GetComponentInChildren<UnityEngine.UI.Text>().text=="닫기"){button.onClick.Invoke();break;}
            RenderBattle();if(RobotPromptActive||battlePaused||combat.MachinesBuilt!=0)smokeErrors.Add("Dismissed prompt repeated or auto-boarded");
            combat.Enemies.Add(new Enemy{id=9340,hp=5000,maxHp=5000,role=EnemyRole.Artillery,position=combat.Position+new V2(250,0)});
            RobotAssembler(1);yield return Capture("robot-live-artillery-advice");CloseModal();
            CampaignDetails(17);yield return Capture("robot-region-advice");CloseModal();
            // Actual button callbacks: a first configuration, then immediate boarding and held reconfiguration.
            OpenBattle(0,"robot-input-fixture",17,false);battlePaused=true;
            for(int part=0;part<3;part++)combat.Pickups.Add(new Pickup{id=9400+part,position=combat.Position,part=part,partCount=12});
            combat.Pickups.Add(new Pickup{id=9450,position=combat.Position,xp=40});combat.Tick(.05f,new V2());while(combat.Choosing)combat.Choose(combat.Choices[0]);
            RobotAssembler(1);yield return Capture("robot-recipe-ready");
            foreach(var button in modal.GetComponentsInChildren<UnityEngine.UI.Button>())if(button.GetComponentInChildren<UnityEngine.UI.Text>().text=="특화형 제작하고 탑승"){button.onClick.Invoke();break;}
            if(combat.RobotRank(1)!=1||combat.RobotPartsSpent!=4||battlePaused||combat.MachineTime<=0)smokeErrors.Add("One press must craft, spend exactly four parts and board");
            RenderBattle();yield return Capture("robot-crafted-and-boarded");
            OpenBattle(0,"robot-select-fixture",17,false);battlePaused=true;
            for(int part=0;part<3;part++)combat.Pickups.Add(new Pickup{id=9470+part,position=combat.Position,part=part,partCount=12});
            combat.Tick(.05f,new V2());RobotAssembler(9);yield return Capture("robot-craft-no-scrap");
            foreach(var button in modal.GetComponentsInChildren<UnityEngine.UI.Button>())if(button.GetComponentInChildren<UnityEngine.UI.Text>().text=="특화형 제작하고 선택"){button.onClick.Invoke();break;}
            if(combat.RobotRank(9)!=1||combat.MachineTime>0||battlePaused)smokeErrors.Add("Without scrap, crafting selects and returns without a fake boarding");
            RobotAssembler(9);robotSupportOptions=true;RobotAssembler(9);yield return Capture("robot-support-options");CloseModal();
            OpenBattle(0,"robot-target-fixture",17,false);battlePaused=true;RobotAssembler(1);
            foreach(var button in modal.GetComponentsInChildren<UnityEngine.UI.Button>())if(button.GetComponentInChildren<UnityEngine.UI.Text>().text=="부품 수집 목표로 선택"){button.onClick.Invoke();break;}
            if(battlePaused||combat.RobotPlansBuilt!=0||combat.RobotPartsSpent!=0||robotTargetDesign!=1)smokeErrors.Add("Target selection must not manufacture missing parts");
            // Fresh ready state preserves selected plan, without advancing or altering a live user's run.
            OpenBattle(0,"robot-quick-fixture",17,false);battlePaused=false;CloseModal();
            combat.Pickups.Add(new Pickup{id=9500,position=combat.Position,xp=40});combat.Tick(.05f,new V2());while(combat.Choosing)combat.Choose(combat.Choices[0]);combat.PlanRobot(5,false);
            TriggerMachine();if(battlePaused||combat.MachineTime<=0)smokeErrors.Add("Quick press opened a menu instead of boarding");
            RenderBattle();yield return Capture("robot-quick-board");
            OpenBattle(0,"robot-hold-fixture",17,false);battlePaused=false;CloseModal();combat.PlanRobot(5,false);RenderBattle();
            var hold=machineButton.GetComponent<RobotInputButton>();var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            hold.OnPointerDown(pointer);yield return new WaitForSecondsRealtime(.55f);hold.OnPointerUp(pointer);hold.Click();
            if(!battlePaused||combat.MachineTime>0)smokeErrors.Add("Held press did not exclusively open the assembly menu");
            yield return Capture("robot-held-workshop");CloseModal();
            combat=null;Show("home");RobotHangar();yield return Capture("robot-hangar");
            Store.Change(e=>RobotAssembly.Showcase(e.State,1));Show("showcase");yield return Capture("robot-showcase");
            Store.Change(e=>RobotAssembly.Showcase(e.State,0));CloseModal();PlayerPrefs.SetInt("scrapguard.robot.clear",priorView);Show("home");
        }
        IEnumerator AssaultSmokeRoutine()
        {
            DismissTitleScreen();CloseModal();soakDriving=true;
            Store.Change(e=>{e.State.testProgress=true;e.State.unlockedStage=41;return CommandResult.Ok("Isolated assault QA");});
            Show("home");yield return Capture("01-home");HomeActivities();yield return Capture("02-activities");
            AudioOptions();yield return Capture("03-audio");CloseModal();
            OpenBattle(0,"assault-qa",17,false);battlePaused=true;
            combat.Tick(.05f,new V2());RenderBattle();yield return Capture("04-first-battle");
            combat.Position=new V2(810,1100);RenderBattle();yield return Capture("05-follow-camera");
            if(Mathf.Abs(arena.anchoredPosition.x)<100)smokeErrors.Add("Camera did not follow world position");
            combat.Enemies.Add(new Enemy{id=50000,hp=10000,maxHp=10000,kind=3,finalBoss=true,position=new V2(-400,0)});
            RenderBattle();yield return Capture("05b-offscreen-boss-bearing");
            if(!bossText.text.Contains("보스 위치"))smokeErrors.Add("Missing offscreen boss guidance");
            combat.Enemies.RemoveAll(e=>e.id==50000);
            combat.Position=new V2(270,510);combat.Elapsed=22;combat.Tick(.05f,new V2());
            if(combat.Choosing)combat.Choose(combat.Choices[0]);RenderBattle();yield return Capture("06-wave-warning");
            for(int i=0;i<210;i++)
            {
                var p=combat.Position+V2.Angle(i*2.39996f)*(115+i%9*19);
                combat.Enemies.Add(new Enemy{id=10000+i,hp=10000,maxHp=10000,kind=i%12==0?1:0,role=(EnemyRole)(i%6),position=p});
            }
            RenderBattle();yield return Capture("07-horde-210");
            combat.Pickups.Add(new Pickup{id=30000,xp=40,position=combat.Position});
            for(int i=0;i<20;i++){if(combat.Choosing)combat.Choose(combat.Choices[0]);combat.Tick(.05f,new V2());}
            if(combat.Choosing)combat.Choose(combat.Choices[0]);combat.ActivateMachine();combat.Tick(.5f,new V2());
            combat.Hazards.Clear();RenderBattle();yield return Capture("08-salvage-machine");
            if(combat.MachinesBuilt<1)smokeErrors.Add("Machine assembly did not trigger from collected salvage");
            combat.Skills[8]=combat.Skills[9]=combat.Skills[10]=5;combat.Tick(.05f,new V2());RenderBattle();yield return Capture("09-new-arsenal");
            combat.Choosing=true;combat.Skills[8]=combat.Skills[9]=2;combat.Choices=new[]{8,9,11};ShowChoices();yield return Capture("10-new-skill-choices");CloseModal();combat.Choosing=false;
            combat.Skills[0]=5;combat.Skills[4]=3;combat.Tick(.05f,new V2());RenderBattle();yield return Capture("11-evolution");
            for(int i=0;i<combat.Skills.Length;i++)combat.Skills[i]=5;
            combat.Tick(.05f,new V2());RenderBattle();yield return Capture("11b-all-attack-evolutions");
            if(combat.EvolutionMask!=((1<<1)|(1<<2)|(1<<3)|(1<<8)|(1<<9)|(1<<10)))smokeErrors.Add("Missing attack evolution");
            if(Resources.Load<Texture2D>("Art/ContinuousFloor-v1")?.wrapMode!=TextureWrapMode.Mirror)smokeErrors.Add("Floor is not seam-safe mirrored texture");
            foreach(string music in new[]{"Factory","Overdrive","Boss"})if(Resources.Load<AudioClip>("Audio/Licensed/"+music)==null)smokeErrors.Add("Missing music "+music);
            for(int i=0;i<12;i++)if(CombatSoundBank.Get(i)==null)smokeErrors.Add("Missing licensed sound "+i);
            combat.Elapsed=combat.Duration-32;combat.Choosing=false;combat.Tick(.05f,new V2());RenderBattle();yield return Capture("11c-boss-warning");
            combat.Elapsed=combat.Duration-30;combat.Choosing=false;combat.Tick(.05f,new V2());RenderBattle();yield return Capture("11d-boss-arena");
            if(!combat.BossArenaActive||!InBattleView(combat.Enemies.Find(e=>e.finalBoss).position,0))smokeErrors.Add("Boss arena failed to keep boss in view");
            if(bossText.transform.parent!=bossBackdrop.transform||bossFill.transform.parent!=bossBackdrop.transform)smokeErrors.Add("Boss HUD escaped its panel");
            if(ultimateLabel.text.Contains("\n"))smokeErrors.Add("Ultimate button overflow risk");
            DrawReviveChoice();yield return Capture("12-revive-choice");CloseModal();
            if(Resources.Load<AudioClip>("Audio/Licensed/Factory")==null)smokeErrors.Add("Missing combat music asset");
            combat=null;Show("home");yield return Capture("13-home-return");
            yield return RobotAssemblySmoke();
            yield return AssaultReplayCapture();
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Assault UI errors: "+smokeErrors.Count+"\n"+Screen.width+"x"+Screen.height+"\n"+string.Join("\n",smokeErrors));
            if(smokeWithNatural){smokeOutput=Path.Combine(smokeOutput,"Natural");Directory.CreateDirectory(smokeOutput);yield return NaturalCombatRoutine();yield break;}
            Debug.Log("ASSAULT_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
