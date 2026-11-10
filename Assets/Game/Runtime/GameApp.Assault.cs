using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        RectTransform machineActor,machineRotor,escapeArrow,bossFence;
        Text salvageText,objectiveBearing;
        Button machineButton; Image machineFill; RectTransform machineAura;
        readonly RectTransform[] wingDrones=new RectTransform[2];
        readonly LightningArc[] mineLinks=new LightningArc[12];
        bool playerInDanger;
        readonly RectTransform[] mineActors=new RectTransform[12];
        int trackedMachines,trackedWaves,trackedEvolution,trackedSkillEvolutions;
        Vector2 BattleCameraOffset()
        {
            if(combat.Challenge)return new Vector2(0,-menuExtra/2);
            if(combat.BossArenaActive)return new Vector2(270-combat.BossArenaCenter.x,combat.BossArenaCenter.y-480-menuExtra/2);
            float x=Mathf.Clamp(combat.Position.x-270,combat.WorldLeft,combat.WorldRight-540);
            float y=Mathf.Clamp(combat.Position.y-510-menuExtra/2,combat.WorldTop-160,combat.WorldBottom-(MenuHeight-176));
            return new Vector2(-x,y);
        }
        bool InBattleView(V2 p,float margin=85)
        {
            var camera=BattleCameraOffset();float x=p.x+camera.x,y=p.y-camera.y;
            return x>=-margin&&x<=540+margin&&y>=-margin&&y<=MenuHeight-176+margin;
        }
        string BattleBearing(V2 point)
        {
            var delta=point-combat.Position;
            return Mathf.Abs(delta.x)>Mathf.Abs(delta.y)?delta.x>0?"→":"←":delta.y>0?"↓":"↑";
        }
        void SetupAssaultPresentation()
        {
            robotWorkshopObserved=robotPartsObserved=robotCraftObserved=robotBoardObserved=robotSpecializedObserved=robotPromptUsed=false;robotPromptRemaining=nextRobotProbe=0;readyRobotDesign=0;
            trackedMachines=trackedWaves=trackedEvolution=trackedSkillEvolutions=0;robotTargetDesign=combat.MachineDesign;robotMenuOpen=false;
            bossFence=Ui.Rect(arena,"Boss arena fence",0,0,470,600);
            Ui.Box(bossFence,0,0,470,4,Ui.Blue,false);Ui.Box(bossFence,0,596,470,4,Ui.Blue,false);
            Ui.Box(bossFence,0,0,4,600,Ui.Blue,false);Ui.Box(bossFence,466,0,4,600,Ui.Blue,false);
            machineActor=Ui.Rect(identityLayer,"Salvage assembly",0,0,0,0);
            machineActor.SetAsLastSibling();
            machineAura=CombatRing.Create(machineActor,230,new Color(.6f,1,.8f,.55f),.018f);
            battleRobot=RobotRigView.Draw(machineActor,-155,-170,310,combat.MachineDesign);
            machineRotor=CombatRing.Create(machineActor,22,new Color(.4f,1,.8f,.7f),.06f);
            machineRotor.anchoredPosition=new Vector2(0,26);

            escapeArrow=Ui.Label(hazardLayer,"↑",0,0,52,58,46,Ui.Mint,TextAnchor.MiddleCenter).rectTransform;
            escapeArrow.pivot=new Vector2(.5f,.5f);
            for(int i=0;i<mineActors.Length;i++)
            {
                mineActors[i]=CombatRing.Create(pickupLayer,28,Ui.Gold,.16f);
                Ui.Label(mineActors[i],"+",-10,-12,20,24,18,Ui.Gold,TextAnchor.MiddleCenter);
                mineLinks[i]=Ui.Rect(pickupLayer,"Mine link",0,0,540,960).gameObject.AddComponent<LightningArc>();mineLinks[i].raycastTarget=false;mineLinks[i].color=new Color(.4f,1,.8f,.65f);
            }
            machineButton=Ui.Button(screen,"",180,899,155,49,TriggerMachine,Ui.Mint,false,16);
            var robotInput=machineButton.gameObject.AddComponent<RobotInputButton>();robotInput.Tap=TriggerMachine;robotInput.Hold=()=>RobotAssembler(robotTargetDesign);machineButton.onClick.RemoveAllListeners();machineButton.onClick.AddListener(robotInput.Click);
            salvageText=machineButton.GetComponentInChildren<Text>();machineFill=Ui.Box(machineButton.transform,0,48,0,3,Ui.Gold,false);
            machineButton.gameObject.SetActive(combat.RobotEnabled);
            for(int i=0;i<2;i++)wingDrones[i]=Ui.Robot(identityLayer,0,0,.24f,0);
            var outline=playerArt.gameObject.GetComponent<Outline>();if(outline==null)outline=playerArt.gameObject.AddComponent<Outline>();
            outline.effectColor=new Color(.55f,1,.9f,.95f);outline.effectDistance=new Vector2(2,-2);
            objectiveBearing=Ui.Label(screen,"",24,718,492,27,16,Ui.Blue,TextAnchor.MiddleCenter);
            StartBattleMusic();
        }
        void TriggerMachine()
        {
            if(combat==null||battlePaused||networkBusy||!combat.RobotEnabled)return;
            if(RobotPromptActive){RobotAssembler(robotPromptDesign);return;}
            if(!combat.RobotPlanned){RobotAssembler(robotTargetDesign);return;}
            if(!combat.ActivateMachine())return;recorder.Machine();dragging=false;RenderBattle();
        }
        void RenderUnifiedNotice()
        {
            // One message lane. Immediate threats override goals and celebrations.
            string message="";
            if(playerInDanger)message="위험 구역 밖으로 이동!";
            else if(combat.BossArenaWarning>0)message="보스 결전 "+Mathf.CeilToInt(combat.BossArenaWarning)+"초 · 주변 전투 구역 고정";
            else if(combat.WaveWarning>0)message="포위 접근 · "+BattleBearing(combat.WaveCenter+V2.Angle(combat.EscapeAngle)*150)+" 열린 방향으로";
            else if(activeBattleNotice.Length>0&&combat.Elapsed<noticeUntil)message=activeBattleNotice;
            else if(combat.IntroBattle&&combat.BossArenaActive)message=combat.CanActivateMachine?"아래 합체 버튼으로 보스를 공격하세요":combat.CanUltimate?"필살기 준비 완료 · 오른쪽 아래 버튼을 누르세요":"공격은 자동 · 보스의 빨간 예고 밖으로 이동하세요";
            else if(combat.BossPractice)message="보스 패턴 연습 · 경고 밖으로 이동하세요";
            else if(combat.BossArenaActive)message="";
            else if(combat.Mode==Endgame.WeeklyMode&&combat.Elapsed<8)message="합체 버튼 → "+RobotAssembly.Frames[combat.WeeklyFrame]+" 특화형 제작";
            else if(RobotPromptActive)message="↓ 모은 부품으로 로봇을 만들어 보세요";
            else if(combat.IntroBattle&&combat.Elapsed>8&&combat.Elapsed<14)message="포·동·장 표식 적을 부수고 부품을 회수하세요";
            else if(!combat.Challenge&&combat.Elapsed>12&&combat.Elapsed%12<(combat.IntroBattle?7:4))message=RobotRecoveryHint();
            else if(guideHint.gameObject.activeSelf)message=guideHint.text;
            else if(objectiveBearing.text.Length>0)message=objectiveBearing.text;
            else if(combat.CanActivateMachine)message="합체 준비 완료 · 아래 버튼으로 원하는 순간 발동";
            else if(combat.TaskActive&&combat.Elapsed%12<3)message=combat.TaskInstruction;
            guideHint.gameObject.SetActive(false);guideBackdrop.gameObject.SetActive(false);
            if(taskHud!=null)taskHud.gameObject.SetActive(false);
            objectiveBearing.gameObject.SetActive(false);dangerHint.gameObject.SetActive(false);
            combatNotice.gameObject.SetActive(message.Length>0);combatNotice.text=message;
            combatNotice.rectTransform.anchoredPosition=new Vector2(35,(bossBackdrop.gameObject.activeSelf?-135:-94)+menuExtra/2);
            combatNotice.rectTransform.sizeDelta=new Vector2(470,42);combatNotice.fontSize=17;
            combatNotice.color=playerInDanger?Ui.Gold:combat.WaveWarning>0?Ui.Mint:Ui.Ink;
            if(playerInDanger||combat.WaveWarning>0||combat.BossArenaActive)streakLabel.text="";
        }
        void RenderAssaultPresentation()
        {
            ObserveRobotExperience();
            machineActor.gameObject.SetActive(combat.MachineTime>0);
            float arrival=combat.MachineActiveDuration-combat.MachineTime;
            battleRobot.SetDesign(combat.MachineDesign,combat.RobotRank(combat.MachineDesign));battleRobot.Animate(combat.Elapsed,combat.LastMovement.Length>.1f,combat.MachineTime>0?arrival:-1);
            machineActor.anchoredPosition=new Vector2(combat.MachinePosition.x,-combat.MachinePosition.y);
            float unfold=Mathf.Clamp01(arrival/.18f);
            bool clearer=PlayerPrefs.GetInt("scrapguard.robot.clear",1)==1;
            machineActor.localScale=Vector3.one*Mathf.Lerp(.92f,1,unfold)*(clearer?.82f:1);
            bool nearby=false;if(clearer&&combat.MachineTime>0)foreach(var e in combat.Enemies)if(e.hp>0&&V2.Distance(e.position,combat.Position)<140){nearby=true;break;}
            battleRobot.SetBattleClarity(nearby);
            bossFence.gameObject.SetActive(combat.BossArenaActive);
            bossFence.anchoredPosition=new Vector2(combat.BossArenaCenter.x-235,-combat.BossArenaCenter.y+300);
            machineAura.GetComponent<CombatRing>().color=new Color(.6f,1,.8f,combat.FinalBossActive?.1f:.3f);
            machineAura.localScale=Vector3.one*(1+.04f*Mathf.Sin(combat.Elapsed*5));
            machineRotor.localEulerAngles=new Vector3(0,0,-combat.Elapsed*260);
            escapeArrow.gameObject.SetActive(combat.WaveWarning>0);
            var exit=combat.WaveCenter+V2.Angle(combat.EscapeAngle)*150;
            escapeArrow.anchoredPosition=new Vector2(exit.x,-exit.y);
            escapeArrow.localEulerAngles=new Vector3(0,0,-combat.EscapeAngle*Mathf.Rad2Deg-90);
            for(int i=0;i<mineActors.Length;i++)
            {
                mineActors[i].gameObject.SetActive(i<combat.Mines.Count);mineActors[i].GetComponent<Graphic>().color=combat.SkillEvolved(10)?Ui.Hex("9AFFD9"):Ui.Gold;mineActors[i].localScale=Vector3.one*(combat.SkillEvolved(10)?1.4f:1);
                mineLinks[i].gameObject.SetActive(false);
                if(combat.SkillEvolved(10)&&i+1<combat.Mines.Count&&V2.Distance(combat.Mines[i].position,combat.Mines[i+1].position)<190)
                {mineLinks[i].gameObject.SetActive(true);var from=combat.Mines[i].position;var to=combat.Mines[i+1].position;mineLinks[i].From=new Vector2(from.x,-from.y);mineLinks[i].To=new Vector2(to.x,-to.y);mineLinks[i].SetVerticesDirty();}
                if(i<combat.Mines.Count){var p=combat.Mines[i].position;mineActors[i].anchoredPosition=new Vector2(p.x,-p.y);}
            }
            machineButton.interactable=combat.RobotEnabled&&!combat.Ended&&combat.MachineTime<=0&&!combat.Choosing&&!battlePaused&&!networkBusy;
            salvageText.text=combat.MachineTime>0?(RobotAssembly.Frame(combat.MachineDesign)==0?"포격":RobotAssembly.Frame(combat.MachineDesign)==1?"돌파":"철벽")+" 가동 "+Mathf.CeilToInt(combat.MachineTime)+"초":combat.MachineRecovery>0?"합체 준비 "+Mathf.CeilToInt(combat.MachineRecovery)+"초":combat.CanActivateMachine?(Application.isMobilePlatform?(combat.RobotPlanned?"즉시 탑승":"로봇 조립"):(combat.RobotPlanned?"탑승 · E":"조립 · E")):"조립 · 고철 "+combat.Salvage+"/"+combat.MachineCost;
            machineButton.image.color=combat.CanActivateMachine?Ui.Mint:Ui.Raised;
            salvageText.color=combat.CanActivateMachine?Ui.Background:Ui.Ink;
            if(RobotPromptActive){salvageText.text="눌러서 특화형 제작";machineButton.image.color=Color.Lerp(Ui.Mint,Ui.Gold,(Mathf.Sin(Time.unscaledTime*4)+1)*.5f);salvageText.color=Ui.Background;}
            machineFill.rectTransform.sizeDelta=new Vector2(155*Mathf.Clamp01(combat.Salvage/(float)combat.MachineCost),3);
            for(int i=0;i<2;i++){wingDrones[i].gameObject.SetActive(combat.SkillEvolved(3));wingDrones[i].anchoredPosition=new Vector2(combat.Position.x+(i==0?-75:75)-10,-combat.Position.y+54);}
            objectiveBearing.text="";
            if(combat.TaskActive&&!InBattleView(combat.TaskPosition,0))
            {
                var delta=combat.TaskPosition-combat.Position;
                string direction=BattleBearing(combat.TaskPosition);
                objectiveBearing.text=direction+" 현장 임무 · "+Mathf.RoundToInt(delta.Length)+"m";
            }
            if(combat.MachinesBuilt>trackedMachines){FeedbackSound(CombatSoundBank.Get(11));
                var burst=CombatRing.Create(identityLayer,320,Ui.Mint,.018f);burst.anchoredPosition=new Vector2(combat.Position.x,-combat.Position.y);burst.gameObject.AddComponent<CombatBurst>().WithClock(combat).Configure(.65f,.3f,1.2f);
                Track("combat_hook","machine",combat.MachinesBuilt,combat.Elapsed);PulseHaptics(35);trackedMachines=combat.MachinesBuilt;}
            if(combat.WaveNumber>trackedWaves){Track("combat_pacing","wave",combat.WaveNumber,combat.Elapsed);trackedWaves=combat.WaveNumber;}
            if(combat.WeaponEvolution!=trackedEvolution){Track("combat_build","evolution",combat.WeaponEvolution,combat.Elapsed);trackedEvolution=combat.WeaponEvolution;}
            if(combat.EvolutionMask!=trackedSkillEvolutions){Track("combat_build","attack_evolutions",combat.EvolutionMask,combat.Elapsed);trackedSkillEvolutions=combat.EvolutionMask;}
            UpdateBattleMusic();
        }
    }
}
