using System;
using System.Collections.Generic;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ScrapGuard
{
    public partial class GameApp
    {
        Combat combat;
        ArenaScenery scenery;
        CombatParticles particles;
        float presentationTime;
        int presentedBoss;
        RectTransform bossCue;
        RectTransform arena, enemyLayer, pickupLayer, bulletLayer, hazardLayer, effectLayer, player, drone, joystick, stick;
        readonly List<RectTransform> saws = new List<RectTransform>();
        readonly Dictionary<int, RectTransform> enemyViews = new Dictionary<int, RectTransform>(), bulletViews = new Dictionary<int, RectTransform>(), pickupViews = new Dictionary<int, RectTransform>(), hazardViews = new Dictionary<int, RectTransform>();
        readonly HashSet<int> alive = new HashSet<int>();
        readonly List<int> stale = new List<int>();
        readonly Stack<RectTransform> enemyPool = new Stack<RectTransform>(), bulletPool = new Stack<RectTransform>(), pickupPool = new Stack<RectTransform>(), hazardPool = new Stack<RectTransform>();
        readonly List<FloatingEffect> floating = new List<FloatingEffect>();
        readonly List<BattleEffect> frameBattleEffects=new List<BattleEffect>();
        Image hpFill, xpFill, bossFill, bossBackdrop;
        Text hpText, timeText, killText, levelText, bossText, skillText;
        readonly Image[] skillHudIcons=new Image[12];
        readonly Text[] skillHudLevels=new Text[12];
        RectTransform playerLocator;
        Text dangerHint;
        bool battlePaused, choicesShown, resultShown, dragging;
        Vector2 dragStart;
        class FloatingEffect { public RectTransform root; public float life, total; public bool text; public int pooledType=-1; }
        public Combat CurrentCombat { get { return combat; } }
        public void StartBattle(int stage, bool quick = false, string mode = "", string supportId = null)
        {
            if(UpdateBlocksPlay){ShowUpdatePanel();return;}
            if (networkBusy) return;
            if (Online)
            {
                if (quick) { Toast("서버 계정은 일반 작전만 가능해요."); return; }
                StartCoroutine(SubmitCloud(new GameCommand("battle_start") { stage = stage, mode=mode, supportId=supportId }, reply =>
                {
                    if (reply.success && reply.ticket != null && reply.ticket.rulesVersion == Combat.RulesVersion) OpenBattle(reply.ticket.stage, reply.ticket.id, reply.ticket.seed, false, reply.ticket.mode, reply.ticket.GetSupport());
                    else { Toast(reply.message); AccountPanel(); }
                }));
                return;
            }
            string id = Guid.NewGuid().ToString("N");
            var result = Store.Change(e => CommandDispatcher.Apply(e, new GameCommand("battle_start", id) { stage = stage, quick = quick, mode=mode }, true));
            if (!result.Success) { Toast(result.Message); return; }
            OpenBattle(Endgame.IsChallenge(mode)?4:stage, id, Endgame.IsChallenge(mode)?Endgame.Seed(State.activeWeek):Environment.TickCount, quick, mode);
        }
        ReplayRecorder recorder;
        float battleAccumulator;
        GameCommand finishCommand;
        void OpenBattle(int stage, string id, int seed, bool quick, string mode = "", SupportSnapshot support = null)
        {
            combat = new Combat(State, stage, id, seed, quick, mode, support);
            Track("battle_start",string.IsNullOrEmpty(mode)?"campaign":mode,stage);frameSamples=slowFrames=peakEnemies=0;frameSeconds=0;Array.Clear(frameHistogram,0,frameHistogram.Length);
            recorder = new ReplayRecorder(); battleAccumulator = 0; finishCommand = null;
            battlePaused = choicesShown = resultShown = dragging = false;reviveDeclined=revivePrompt=false;
            CloseModal(); Ui.Clear(screen); enemyViews.Clear(); bulletViews.Clear(); pickupViews.Clear(); hazardViews.Clear(); floating.Clear(); saws.Clear();
            enemyPool.Clear(); bulletPool.Clear(); pickupPool.Clear(); hazardPool.Clear();
            var viewport=Ui.Rect(screen,"Battle viewport",0,-menuExtra/2,540,MenuHeight-176);
            viewport.gameObject.AddComponent<RectMask2D>();
            arena = Ui.Rect(viewport, "Arena", 0, menuExtra/2, 540, 960);
            scenery=ArenaScenery.Build(arena,stage,combat.Challenge);
            presentationTime=0;presentedBoss=-1;bossCue=null;
            pickupLayer = Ui.Rect(arena, "Experience", 0, 0, 540, 960);
            hazardLayer = Ui.Rect(arena, "Warnings", 0, 0, 540, 960);
            enemyLayer = Ui.Rect(arena, "Enemies", 0, 0, 540, 960);
            bulletLayer = Ui.Rect(arena, "Projectiles", 0, 0, 540, 960);
            playerShadow = Ui.Disc(arena,0,0,40,new Color(0,0,0,.32f)).rectTransform;
            playerShadow.sizeDelta = new Vector2(40,13);
            player = Ui.Robot(arena, 0, 0, StylePresentation.PlayerScale, combat.Hero, skin: State.selectedSkin, outfit: Outfits.Active(State,combat.Hero));
            playerArt = player.GetComponentInChildren<Image>();
            playerArt.rectTransform.pivot = new Vector2(.5f,.5f);
            playerArt.rectTransform.anchoredPosition = new Vector2(40,-50);
            var outline=playerArt.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.025f,.045f,.07f,.9f);outline.effectDistance=new Vector2(1.5f,-1.5f);
            drone = Ui.Robot(arena, 0, 0, .24f, 0);
            for (int i = 0; i < 6; i++)
            {
                var saw=Ui.Rect(arena,"Orbit saw",0,0,27,27);saw.pivot=new Vector2(.5f,.5f);
                var teeth=saw.gameObject.AddComponent<SawGraphic>();teeth.color=Ui.Hex("C9E4E9");teeth.raycastTarget=false;
                Ui.Disc(saw,9,9,9,Ui.Hex("E8B85E"));Ui.Disc(saw,12,12,3,Ui.Panel);
                saws.Add(saw);
            }
            effectLayer = Ui.Rect(arena, "Effects", 0, 0, 540, 960);
            particles=CombatParticles.Create(effectLayer);
            hazardLayer.SetAsLastSibling();
            playerLocator=CombatRing.Create(arena,58,Ui.Mint,.055f);
            SetupCombatEffects();SetupBattleClarity();turretArt=twinTurretArt=null;magnetField=null;
            Ui.Box(screen,12,12,516,76,new Color(.02f,.04f,.06f,.8f),true);
            Ui.Box(screen,0,824,540,136,new Color(.02f,.04f,.06f,1),false);
            Ui.Button(screen, "Ⅱ", 451, 23, 65, 50, PauseBattle, Ui.Raised, true, 22);
            timeText = Ui.Label(screen, "00:00", 24, 17, 350, 34, 27);
            killText = Ui.Label(screen, "처치 0", 24, 828, 135, 24, 14, Ui.Muted, TextAnchor.MiddleRight);
            Ui.Box(screen, 24, 57, 492, 15, Ui.Raised);
            hpFill = Ui.Box(screen, 24, 57, 492, 15, Ui.Mint);
            hpText = Ui.Label(screen, "", 24, 49, 492, 28, 13, Ui.Background, TextAnchor.MiddleCenter);
            Ui.Box(screen, 24, 80, 492, 5, Ui.Raised);
            xpFill = Ui.Box(screen, 24, 80, 0, 5, Ui.Blue);
            levelText = Ui.Label(screen, "Lv.1", 24, 790, 72, 28, 17, Ui.Mint);
            for(int i=0;i<12;i++)
            {
                float x=84+i*36;

                skillHudIcons[i]=InventoryArt.Draw(screen,InventoryArt.Skill(i),x,787,30);
                skillHudLevels[i]=Ui.Label(screen,"",x+19,808,17,23,17,Ui.Ink,TextAnchor.MiddleCenter);
            }
            skillText=Ui.Label(screen,"",24,960,492,22,12,Ui.Muted,TextAnchor.MiddleCenter);
            guideBackdrop=Ui.Box(screen,32,123,476,43,new Color(.035f,.055f,.08f,.92f));
            guideHint=Ui.Label(screen,"",44,125,452,39,15,Ui.Ink,TextAnchor.MiddleCenter);
            bossBackdrop=Ui.Box(screen,72,92,396,38,new Color(.035f,.055f,.08f,.78f),true);
            bossText = Ui.Label(bossBackdrop.transform, "", 8, 2, 380, 25, 14, Ui.Red, TextAnchor.MiddleCenter);
            bossFill = Ui.Box(bossBackdrop.transform, 8, 30, 0, 5, Ui.Red);
            joystick = Ui.Disc(screen, 64, 846, 95, new Color(.5f, .8f, .8f, .12f)).rectTransform;
            stick = Ui.Disc(joystick, 30, 30, 35, new Color(.6f, 1, .8f, .35f)).rectTransform;

            ultimateButton = Ui.Button(screen,"",345,899,171,49,TriggerUltimate,Ui.Raised,true,17);
            ultimateLabel = ultimateButton.GetComponentInChildren<Text>();
            Ui.Box(screen,351,943,158,3,Ui.Background,false);
            ultimateFill = Ui.Box(screen,351,943,79,3,Ui.Mint,false);
            dangerHint=Ui.Label(screen,"",24,804,492,27,15,Ui.Gold,TextAnchor.MiddleCenter);
            var dangerShadow=dangerHint.gameObject.AddComponent<Shadow>();dangerShadow.effectColor=Ui.Background;dangerShadow.effectDistance=new Vector2(1,-1);
            SetupSupportButton();SetupFieldTask();SetupBattleFeedback();SetupOperationPresentation();SetupAssaultPresentation();
            foreach(RectTransform child in screen)
            {
                if(child==arena.parent)continue;
                if(child.anchoredPosition.y>-240)child.anchoredPosition+=Vector2.up*(menuExtra/2);
                else if(child.anchoredPosition.y<-700)child.anchoredPosition+=Vector2.down*(menuExtra/2);
            }
            RenderBattle();
        }
        Vector2 PointerPosition()
        {
            Vector2 p = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
            Vector2 local; RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, p, null, out local);
            return new Vector2(local.x + 270, 480 - local.y);
        }
        V2 Movement()
        {
            V2 move = new V2();
            if (Keyboard.current != null)
            {
                var k = Keyboard.current;
                move.x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
                move.y = (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0) - (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0);
            }
            bool pressed = Pointer.current != null && Pointer.current.press.isPressed;
            bool began = Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
            if (began)
            {
                Vector2 p = PointerPosition();
                if (p.y > 90-menuExtra/2 && p.y < 940+menuExtra/2 && p.x >= 0 && p.x <= 540 && !(p.x > 175 && p.y > 824+menuExtra/2))
                {
                    dragging = true; dragStart = p;
                    joystick.anchoredPosition = new Vector2(p.x - 47.5f, -(p.y - 47.5f));
                }
            }
            if (!pressed) dragging = false;
            if (dragging)
            {
                Vector2 delta = Vector2.ClampMagnitude(PointerPosition() - dragStart, 42);
                stick.anchoredPosition = new Vector2(30 + delta.x, -30 - delta.y);
                move = new V2(delta.x / 42, delta.y / 42);
            }
            else { joystick.anchoredPosition = new Vector2(64, -846); stick.anchoredPosition = new Vector2(30, -30); }
            return move;
        }
        void UpdateBattle()
        {
            UpdateBattleMusic();
            if (smokeCapturing || networkBusy || soakDriving) return;
            if (combat.Ended) { if (!resultShown && !OfferRevive()) FinishBattle(); return; }
            if (battlePaused) return;
            if (combat.Choosing) { if (!choicesShown) ShowChoices(); return; }
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) TriggerUltimate();
            if (combat.Ended) { if(!OfferRevive())FinishBattle(); return; }
            if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame) TriggerSupport();
            if(Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame){if(Keyboard.current.shiftKey.isPressed)RobotAssembler(robotTargetDesign);else TriggerMachine();}
            if(battlePaused)return;
            V2 movement = smokeActive ? SmokeMovement() : Movement();
            battleAccumulator += Mathf.Min(Time.deltaTime, .1f);
            frameBattleEffects.Clear();
            while (battleAccumulator >= ReplayRecorder.Step && !combat.Ended && !combat.Choosing)
            {
                battleAccumulator -= ReplayRecorder.Step;
                if(PlayerPrefs.GetInt("scrapguard.machine.auto",0)==1&&combat.CanActivateMachine){combat.ActivateMachine();recorder.Machine();}
                combat.Tick(ReplayRecorder.Step, recorder.Move(movement));
                frameBattleEffects.AddRange(combat.Effects);
            }
            combat.Effects.Clear();combat.Effects.AddRange(frameBattleEffects);
            RenderBattle();
            if (combat.Ended) {if(!OfferRevive())FinishBattle();} else if (combat.Choosing) ShowChoices();
        }
        void RenderBattle()
        {
            float presentationDelta=Mathf.Max(0,combat.Elapsed-presentationTime);presentationTime=combat.Elapsed;
            UpdateVisualBudget();scenery.Render(combat.Elapsed,combat.BossArenaActive);particles.Advance(presentationDelta);
            RenderPlayerMotion();
            drone.gameObject.SetActive(combat.Skills[3] > 0 || combat.Hero==0);
            drone.anchoredPosition = new Vector2(combat.DronePosition.x - 10, -(combat.DronePosition.y - 14));
            for (int i = 0; i < saws.Count; i++)
            {
                bool active = combat.Skills[1] > 0 && i < 1 + combat.Skills[1]; saws[i].gameObject.SetActive(active);
                if (active) { V2 p = combat.SawPosition(i); saws[i].anchoredPosition = new Vector2(p.x, -p.y); saws[i].localEulerAngles = new Vector3(0, 0, combat.Elapsed * 250);saws[i].localScale=Vector3.one*(combat.SkillEvolved(1)?1.5f:1);saws[i].GetComponent<Graphic>().color=combat.SkillEvolved(1)?Ui.Mint:Ui.Gold; }
            }
            hpFill.rectTransform.sizeDelta = new Vector2(492 * Mathf.Clamp01(combat.Hp / combat.MaxHp), 15);
            hpFill.color = combat.Hp < combat.MaxHp * .3f ? Ui.Red : Ui.Mint;
            int hp=Mathf.CeilToInt(combat.Hp),maximum=Mathf.CeilToInt(combat.MaxHp);bool shield=combat.Shield>0;
            if(hp!=displayedHp||maximum!=displayedMaxHp||shield!=displayedShield)
            {hpText.text=hp+" / "+maximum+(shield?"  보호막":"");displayedHp=hp;displayedMaxHp=maximum;displayedShield=shield;}
            xpFill.rectTransform.sizeDelta = new Vector2(492 * Mathf.Clamp01((float)combat.Xp / combat.NeededXp), 5);
            if(displayedSeconds!=(int)combat.Elapsed){displayedSeconds=(int)combat.Elapsed;timeText.text=TimeLabel(combat.Elapsed)+" / "+TimeLabel(combat.Duration);}
            int count=combat.Challenge?combat.CoreScore:combat.Kills;
            if(displayedKills!=count){displayedKills=count;killText.text=combat.Challenge?count.ToString("N0")+"점":"처치 "+count;}
            if(levelText.text!="Lv."+combat.Level)levelText.text="Lv."+combat.Level;
            skillText.text=combat.FieldModule>0?FieldEngineering.Name(combat.FieldModule,combat.Hero):"";
            int activeSkills=0,skillSlot=0;foreach(int value in combat.Skills)if(value>0)activeSkills++;
            for(int i=0;i<12;i++)
            {
                bool learned=combat.Skills[i]>0;skillHudIcons[i].gameObject.SetActive(learned);skillHudLevels[i].gameObject.SetActive(learned);
                if(learned){float x=100+skillSlot++*Mathf.Min(48,410f/Mathf.Max(1,activeSkills));skillHudIcons[i].rectTransform.anchoredPosition=new Vector2(x,-787-menuExtra/2);skillHudLevels[i].rectTransform.anchoredPosition=new Vector2(x+20,-808-menuExtra/2);}
                bool evolved=combat.SkillEvolved(i)||i==0&&combat.WeaponEvolution>0;
                int displayState=combat.Skills[i]+(evolved?100:0);
                if(displayedSkills[i]==displayState)continue;
                displayedSkills[i]=displayState;
                skillHudIcons[i].color=combat.Skills[i]>0?Color.white:new Color(.45f,.5f,.55f,.45f);
                skillHudLevels[i].text=evolved?"★":combat.Skills[i]>0?combat.Skills[i].ToString():"–";
            }
            alive.Clear(); Enemy boss = null;
            foreach (var e in combat.Enemies)
            {
                if (e.finalBoss || e.core) boss=e;
                if(!InBattleView(e.position))continue;
                alive.Add(e.id); RectTransform view;
                if (!enemyViews.TryGetValue(e.id, out view))
                {
                    if (enemyPool.Count > 0) { view = enemyPool.Pop(); view.GetComponent<EnemyVisual>().ResetState();view.gameObject.SetActive(true); }
                    else view=EnemyVisual.Create(enemyLayer);
                    enemyViews[e.id] = view;
                }
                var actor=view.GetComponent<EnemyVisual>();actor.Render(e,combat.Definition.Region,combat.Elapsed);
                if(actor.JustHit){particles.Burst(e.position,3,Ui.Gold,65,2);HitFeedback();}
                if((e.finalBoss||e.core) && presentedBoss!=e.id)
                {
                    presentedBoss=e.id;
                    PresentBossCue(e,combat.Challenge?"코어 균열 활성화":combat.Definition.Boss+" 출현",false);
                }
                if(actor.JustRaged)PresentBossCue(e,"위험 단계 · 공격 예고에 집중하세요",true);
                if (e.finalBoss || e.core) boss = e;
            }
            Recycle(enemyViews, enemyPool);
            bossBackdrop.gameObject.SetActive(boss!=null);
            bossText.text = boss == null ? "" : combat.Challenge ? FieldEngineering.TrialObjective(combat.Mode) : combat.Definition.Boss + " · " + (!InBattleView(boss.position,0)?BattleBearing(boss.position)+" 보스 위치":boss.hp<boss.maxHp*.5f?"위험 단계":"위험 예고를 피하세요");
            bossFill.rectTransform.sizeDelta = new Vector2(boss == null ? 0 : 380 * Mathf.Clamp01(combat.Challenge ? combat.CoreScore/30000f : boss.hp / boss.maxHp), 7);
            alive.Clear();
            foreach (var b in combat.Bullets)
            {
                if(!InBattleView(b.position))continue;
                alive.Add(b.id); RectTransform view;
                if (!bulletViews.TryGetValue(b.id, out view))
                {
                    if (bulletPool.Count > 0) view = bulletPool.Pop();
                    else view=BulletVisual.Create(bulletLayer);
                    view.gameObject.SetActive(true); bulletViews[b.id] = view;
                }
                view.GetComponent<BulletVisual>().Render(b,combat.Skills[0],battleOutfit,combat.Elapsed,visualBudget.Compact,b.machine?RobotStyles.Active(State,RobotAssembly.Frame(b.robotDesign)):0);
            }
            Recycle(bulletViews, bulletPool); alive.Clear();
            foreach (var p in combat.Pickups)
            {
                if(!InBattleView(p.position))continue;
                alive.Add(p.id); RectTransform view;
                if (!pickupViews.TryGetValue(p.id, out view))
                {
                    view = pickupPool.Count > 0 ? pickupPool.Pop() : Ui.Box(pickupLayer, 0, 0, 9, 9, Ui.Mint).rectTransform;
                    view.gameObject.SetActive(true); pickupViews[p.id] = view;
                }
                view.anchoredPosition = new Vector2(p.position.x - 4, -p.position.y + 4); view.localEulerAngles = new Vector3(0, 0, 45);
                view.localScale = Vector3.one * (p.part>=0?2.6f:p.heal ? 1.5f : p.xp > 2 ? 1.3f : 1);
                var pickupImage=view.GetComponent<Image>();pickupImage.color=p.part>=0?Color.white:p.heal?Ui.Red:Ui.Mint;pickupImage.sprite=p.part>=0?InventoryArt.Equipment(p.part==0?0:p.part==1?1:6):null;if(p.part>=0)view.localEulerAngles=Vector3.zero;
            }
            Recycle(pickupViews, pickupPool); alive.Clear();
            foreach (var h in combat.Hazards)
            {
                alive.Add(h.id); RectTransform view;
                if (!hazardViews.TryGetValue(h.id, out view))
                {
                    if (hazardPool.Count > 0) view = hazardPool.Pop();
                    else
                    {
                        view = Ui.Box(hazardLayer,0,0,100,100,new Color(1,.25f,.2f,.25f),false).rectTransform;
                        var outline=Ui.Rect(view,"Damage boundary",0,0,100,100);
                        var newBoundary=outline.gameObject.AddComponent<WarningShape>();newBoundary.color=Ui.Hex("FF735B");newBoundary.raycastTarget=false;
                        Ui.Label(view,"!",0,0,100,100,24,Ui.Hex("FFE6A0"),TextAnchor.MiddleCenter);
                    }
                    view.gameObject.SetActive(true); hazardViews[h.id] = view;
                }
                float width=h.width>0?h.width:h.radius*2,height=h.width>0?h.height:h.radius*2;
                view.sizeDelta=new Vector2(width,height);
                view.anchoredPosition = new Vector2(h.position.x-width/2, -(h.position.y-height/2));
                var warning=view.GetComponent<Image>();
                warning.sprite=h.width>0?null:Ui.Circle;warning.type=Image.Type.Simple;
                warning.color = new Color(1, .18f, .12f, .10f + .10f * Mathf.Abs(Mathf.Sin(h.delay * 12)));
                var boundary=view.GetComponentInChildren<WarningShape>();boundary.rectTransform.sizeDelta=new Vector2(width,height);boundary.SetCircle(h.width<=0);
                boundary.color=h.delay<.65f?Ui.Hex("FFF0B0"):Ui.Hex("FF735B");
                var text=view.GetComponentInChildren<Text>();text.rectTransform.sizeDelta=new Vector2(width,height);text.text=h.delay<.65f?"!":Mathf.CeilToInt(h.delay).ToString();
            }
            Recycle(hazardViews, hazardPool);
            bool inDanger=false;
            foreach(var hazard in combat.Hazards)
            {
                var delta=combat.Position-hazard.position;
                if(hazard.width>0?Mathf.Abs(delta.x)<=hazard.width/2&&Mathf.Abs(delta.y)<=hazard.height/2:delta.Length<=hazard.radius){inDanger=true;break;}
            }
            playerLocator.anchoredPosition=new Vector2(combat.Position.x,-combat.Position.y);
            playerLocator.GetComponent<Graphic>().color=inDanger?Ui.Gold: new Color(.65f,1,.9f,.65f);
            playerInDanger=inDanger;
            dangerHint.text=inDanger?"!  위험 예고 · 표시된 영역 밖으로 이동하세요":"";
            foreach (var effect in combat.Effects)
            {
                EventFeedback(effect);
                if(effect.kind==10)particles.Burst(effect.position,18,Ui.Mint,90,4);
                if(effect.kind==7 || effect.kind==10)continue;
                if(effect.kind==3 && boss!=null && effect.text==combat.Definition.Boss+" 출현!")continue;
                if(effect.kind==5)particles.Burst(effect.position,effect.radius>=23?10:5,Ui.Hex("FFF2B0"),130,5);
                int robotStyle=effect.robot?RobotStyles.Active(State,RobotAssembly.Frame(combat.MachineDesign)):0;
                if(effect.kind==2||effect.kind>=11)particles.Burst(effect.position,12,robotStyle==1?Ui.Gold:robotStyle==2?Ui.Mint:Ui.Hex("FFBF72"),130,robotStyle==1?5:3);
                if (floating.Count >= visualBudget.FloatingLimit) continue;
                RectTransform visual;
                if(effect.kind==8)
                {
                    visual=TakeEffect(2);
                    var arc=visual.GetComponent<LightningArc>();arc.color=Ui.Hex("A5EFFF");
                    arc.From=new Vector2(effect.position.x,-effect.position.y);arc.To=new Vector2(effect.end.x,-effect.end.y);
                }
                else if (!string.IsNullOrEmpty(effect.text))
                {
                    visual=TakeEffect(1);visual.anchoredPosition=new Vector2(effect.position.x-70,-effect.position.y+30);
                    var text=visual.GetComponent<Text>();text.text=effect.text;text.fontSize=effect.kind==3?21:16;
                    text.color=effect.kind==1?Ui.Red:effect.kind==6?Ui.Mint:Ui.Gold;text.alignment=TextAnchor.MiddleCenter;
                }
                else
                {
                    visual=TakeEffect(0);visual.sizeDelta=Vector2.one*Mathf.Max(8,effect.radius*2);
                    var ring=visual.GetComponent<CombatRing>();ring.color=effect.kind>=11?new Color(.65f,1,.95f,combat.FinalBossActive?.18f:.6f):effect.kind==4?new Color(.4f,.86f,1,combat.FinalBossActive?.1f:.3f):new Color(1,.72f,.3f,.25f);
                    if(robotStyle>0){var tint=robotStyle==1?Ui.Gold:Ui.Mint;tint.a=combat.FinalBossActive?.12f:.4f;ring.color=tint;}
                    ring.Thickness=effect.kind==4?.022f:.04f;ring.SetVerticesDirty();
                    visual.anchoredPosition=new Vector2(effect.position.x,-effect.position.y);
                }
                floating.Add(new FloatingEffect { root = visual, life = .55f, total = .55f, text = !string.IsNullOrEmpty(effect.text),pooledType=effect.kind==8?2:!string.IsNullOrEmpty(effect.text)?1:0 });
            }
            combat.Effects.Clear();
            RenderModEffects();
            RenderUltimateHud();
            RenderGuideHint();
            RenderSupport();RenderFieldTask();RenderBattleFeedback();RenderOperationPresentation();RenderAssaultPresentation();RenderUnifiedNotice();hazardLayer.SetAsLastSibling();
            for (int i = floating.Count - 1; i >= 0; i--)
            {
                var f = floating[i]; f.life -= presentationDelta;
                if (f.life <= 0) { ReturnEffect(f); floating.RemoveAt(i); continue; }
                if (f.text) f.root.anchoredPosition += Vector2.up * (30 * presentationDelta);
                var graphic = f.root.GetComponent<Graphic>(); Color color = graphic.color; color.a = Mathf.Min(color.a, f.life / f.total); graphic.color = color;
            }
        }
        void PresentBossCue(Enemy enemy,string label,bool rage)
        {
            if(bossCue!=null)
            {
                for(int i=floating.Count-1;i>=0;i--)if(floating[i].root==bossCue)floating.RemoveAt(i);
                Destroy(bossCue.gameObject);bossCue=null;
            }
            if(floating.Count>=34)return;
            var text=Ui.Label(effectLayer,label,24,236,492,28,17,rage?Ui.Hex("FFD7C0"):Ui.Gold,TextAnchor.MiddleCenter).rectTransform;
            var shadow=text.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.8f);shadow.effectDistance=new Vector2(1,-1);
            bossCue=text;
            floating.Add(new FloatingEffect{root=text,life=2.2f,total=2.2f,text=false});
            var ring=CombatRing.Create(effectLayer,enemy.Radius*3.5f,rage?Ui.Red:Ui.Gold,.025f);
            ring.anchoredPosition=new Vector2(enemy.position.x,-enemy.position.y);
            floating.Add(new FloatingEffect{root=ring,life=1,total=1,text=false});
            particles.Burst(enemy.position,18,rage?Ui.Red:Ui.Gold,115,3);
        }
        void Recycle(Dictionary<int, RectTransform> views, Stack<RectTransform> pool)
        {
            stale.Clear(); foreach (var pair in views) if (!alive.Contains(pair.Key)) stale.Add(pair.Key);
            foreach (int id in stale) { var view = views[id]; view.gameObject.SetActive(false); pool.Push(view); views.Remove(id); }
        }
        string TimeLabel(float seconds) { return ((int)seconds / 60).ToString("00") + ":" + ((int)seconds % 60).ToString("00"); }
        void ShowChoices()
        {
            choicesShown = true; dragging = false;
            var card=Dialog("한 단계 더 강하게","Lv."+(combat.Level+1)+" · 이번 전투에 적용할 스킬을 선택하세요.",862);
            for(int i=0;i<combat.Choices.Length;i++)
            {
                int skill=combat.Choices[i];float y=140+i*234;
                var row=Ui.Button(card,"선택",24,y,444,220,()=>ChooseBattleSkill(skill),Ui.Raised).transform;
                row.GetComponentInChildren<Text>().text="";
                InventoryArt.Draw(row,InventoryArt.Skill(skill),12,14,78);
                Ui.Label(row,Catalog.SkillNames[skill],106,15,320,34,25,skill<4||skill>=8&&skill<=10?Ui.Mint:Ui.Gold);
                Ui.Label(row,combat.Skills[skill]==0?"새 스킬  →  Lv.1":"Lv."+combat.Skills[skill]+"  →  Lv."+(combat.Skills[skill]+1),106,56,320,31,21);
                Ui.Label(row,Catalog.SkillDescriptions[skill],16,101,412,43,19,Ui.Ink);
                Ui.Label(row,ModHint(skill).Trim(),16,151,308,57,17,Ui.Muted);
                Ui.Box(row,334,164,94,42,Ui.Mint);
                Ui.Label(row,"선택",334,164,94,42,20,Ui.Background,TextAnchor.MiddleCenter);
            }
            // A skill selection cannot be dismissed without making a selection.
            card.GetChild(2).gameObject.SetActive(false);
        }
        void PauseBattle()
        {
            if (combat == null || combat.Ended || combat.Choosing) return;
            battlePaused = true; dragging = false;
            var card = Dialog("잠시 정비 중", "작전은 일시정지되어 있어요.", 700);
            Ui.Label(card,combat.Challenge?FieldEngineering.TrialTitle(combat.Mode):OperationConditions.Names[combat.Condition]+"\n"+OperationConditions.Details[combat.Condition],24,138,444,78,16,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Button(card, "계속하기", 24, 235, 444, 62, () => { battlePaused = false; CloseModal(); });
            Ui.Button(card, combat.ApexBattle?"극한 종료 · 기록 저장":combat.BossPractice?"보상 없이 연습 종료":combat.Challenge ? "도전 종료하고 기록 저장" : "작전 종료하고 보상 받기", 24, 318, 444, 58, () => { battlePaused = false; combat.Abandon(); CloseModal(); FinishBattle(); }, Ui.Raised, true, 17);
            Ui.Button(card,"전투 화면 · 효과 설정",24,401,444,64,BattleDisplayOptions,Ui.Raised,true,20);
            Ui.Button(card,"소리 · 진동 · 합체 발동 설정",24,484,444,64,AudioOptions,Ui.Raised,true,20);
            Ui.Button(card,"로봇 설계 · 조합 변경",24,565,444,54,()=>RobotAssembler(robotTargetDesign),Ui.Gold,combat.RobotEnabled&&combat.MachineTime<=0,18);
            card.GetChild(2).gameObject.SetActive(false);
        }
        void ChooseBattleSkill(int skill)
        {
            if (!combat.Choose(skill)) return;
            Track("combat_build","skill_"+skill,combat.Skills[skill],combat.Elapsed);PulseHaptics(18);
            recorder.Choose(skill); battleAccumulator = 0; CloseModal(); choicesShown = false;
        }
        void FinishBattle()
        {
            if (resultShown) return;
            resultShown = true;
            if (finishCommand == null) finishCommand = new GameCommand("battle_finish", combat.RunId)
            { stage = combat.Stage, won = combat.Won, kills = combat.Kills, seconds = combat.Elapsed, replay = recorder.Steps };
            int beforeGems=State.gems;
            Action<CommandResult> complete = result =>
            {
                if (result.Success) { if(State.gems!=beforeGems)Track("economy","gems",State.gems-beforeGems,State.gems);Track("guide_progress","step",StarterGuide.Next(State));Track("battle_end",(string.IsNullOrEmpty(combat.Mode)?"campaign":combat.Mode)+(combat.Won?"_win":"_loss"),combat.Stage,combat.Elapsed); RecordCombatQuality(); Track("performance","battle",slowFrames,frameSamples==0?0:(float)(frameSamples/frameSeconds)); selectedStage = State.unlockedStage; ShowResult(); return; }
                var card = Dialog("보상을 저장하지 못했어요", result.Message, 420);
                Ui.Button(card, "같은 기록으로 다시 저장", 24, 210, 444, 59, () => { resultShown = false; FinishBattle(); });
                Ui.Button(card, "정비소로 이동 · 계정에서 동기화", 24, 293, 444, 59, () => { combat = null; Show("home"); AccountPanel(); }, Ui.Raised, true, 16);
                card.GetChild(2).gameObject.SetActive(false);
            };
            if (Online) StartCoroutine(SubmitCloud(finishCommand, reply => complete(new CommandResult(reply.success, reply.message))));
            else complete(Store.Change(e => combat.ApexBattle ? e.FinishApex(combat) : combat.BossPractice ? e.FinishBossPractice(combat) : combat.Challenge ? e.FinishChallenge(combat) : e.FinishCampaign(combat)));
        }
        void ShowResult()
        {
            if(combat.ApexBattle){ApexResult();return;}
            if(combat.BossPractice){BossPracticeResult();return;}
            if (combat.Challenge) { ChallengeResult(); return; }
            var receipt = State.receipts.Find(r => r.id == combat.RunId);
            bool won = receipt != null ? receipt.won : combat.Won;
            var card = Dialog(won ? "구역 정리 완료!" : "다음 작전을 준비해요", Catalog.StageNames[combat.Stage] + " · 다음 작전을 준비하세요.", 745);
            Ui.Label(card, won ? "작전 성공" : "정비하고 다시 도전해요", 24, 134, 444, 40, 20, won ? Ui.Mint : Ui.Gold, TextAnchor.MiddleCenter);
            Ui.Label(card, TimeLabel(receipt != null ? receipt.seconds : combat.Elapsed) + " 생존    " + (receipt != null ? receipt.kills : combat.Kills) + "마리 처치    Lv." + combat.Level, 24, 184, 444, 40, 17, Ui.Muted, TextAnchor.MiddleCenter);
            Ui.Box(card, 24, 249, 444, 82, Ui.Raised);
            if(receipt==null)Ui.Label(card,"보상 저장 완료",39,258,414,65,19,Ui.Gold,TextAnchor.MiddleCenter);
            else
            {
                int[] rewards={receipt.gold,receipt.gems,receipt.parts};
                for(int i=0;i<3;i++)
                {
                    CurrencyArt.Draw(card,i,30+i*146,259,44);
                    Ui.Label(card,CurrencyArt.Names[i],80+i*146,253,91,24,12,Ui.Muted);
                    var amount=Ui.Label(card,"+"+rewards[i].ToString("N0"),80+i*146,278,91,30,19,CurrencyArt.Tint(i));
                    amount.gameObject.AddComponent<RewardReveal>().Initialize(amount);
                }
                if(receipt.blueprints>0)Ui.Label(card,"지역 설계도 +"+receipt.blueprints,36,309,420,20,12,Ui.Blue,TextAnchor.MiddleCenter);
            }
            if(receipt!=null && receipt.report!=null && receipt.report.taskCompleted)
                Ui.Label(card,"현장 임무 보상 포함 · 골드 +"+(receipt.report.taskChoice==2?400:200)+" / 부품 +"+(receipt.report.taskChoice==2?4:2),24,334,444,25,12,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Button(card,"전투 분석",24,360,combat.Challenge?444:214,51,BattleAnalysis,Ui.Mint,true,18);
            if(!combat.Challenge)Ui.Button(card,"조립 로봇 · "+combat.RobotReport().Count+"종",254,360,214,51,()=>RobotHangar(true),Ui.Gold,true,18);
            bool canBonus = !State.lastResultBonus && State.lastResultGold > 0 && State.dailyRewardGold < 3 && State.lastResultDate == State.serviceDate;
            Ui.Button(card, State.lastResultBonus ? "추가 보상 수령 완료" : "추가 골드 +50%  ·  " + (State.demoAdSkip || State.adSkip ? "광고 없이 받기" : Online?"광고 보고 받기":"체험 안내"), 24, 433, 444, 57,
                () => RequestReward("result"), Ui.Raised, canBonus, 17);
            bool showSupport=combat.Support!=null && combat.SupportUsed && combat.Support.showIdentity && Online;
            if(showSupport)
                Ui.Button(card,"지원해 준 동료의 전시실",24,591,444,34,()=>{Track("profile_source","support_result");StartCoroutine(LoadPublic(combat.Support.owner));},Ui.Raised,true,14);
            int stage = combat.Stage;
            Ui.Button(card, won&&stage==Campaign.Count-1?"주간 로봇 도전":"다시 출격", 24, 519, 214, 62, () => { combat = null;if(won&&stage==Campaign.Count-1)Show("weekly");else StartBattle(stage); });
            Ui.Button(card, Growth.CanUpgrade(State)?"강화하고 재도전":"성장 경로 확인", 254, 519, 214, 62, () => { combat = null; GrowthPanel(); }, Ui.Raised);
            if(!showSupport && StarterGuide.Next(State)<5)Ui.Button(card,"다음 목표 · "+StarterGuide.Names[StarterGuide.Next(State)],24,591,444,34,()=>{combat=null;Show("home");GuidePanel();},Ui.Raised,true,14);
            Ui.Button(card, "정비소로 돌아가기", 24, 632, 444, 51, () => { combat = null; Show("home"); }, Ui.Panel, true, 16);
            Ui.Button(card,ratedRun==combat.RunId?"플레이 의견 저장 완료":"플레이 의견 남기기 · 선택",24,693,444,32,CombatOpinion,Ui.Panel,ratedRun!=combat.RunId,15);
            card.GetChild(2).gameObject.SetActive(false);
        }
    }
}
