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
        bool battlePaused, choicesShown, resultShown, dragging;
        Vector2 dragStart;
        class FloatingEffect { public RectTransform root; public float life, total; public bool text; }
        public Combat CurrentCombat { get { return combat; } }
        public void StartBattle(int stage, bool quick = false, string mode = "", string supportId = null)
        {
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
            Track("battle_start",string.IsNullOrEmpty(mode)?"campaign":mode,stage);frameSamples=slowFrames=0;frameSeconds=0;
            recorder = new ReplayRecorder(); battleAccumulator = 0; finishCommand = null;
            battlePaused = choicesShown = resultShown = dragging = false;
            CloseModal(); Ui.Clear(screen); enemyViews.Clear(); bulletViews.Clear(); pickupViews.Clear(); hazardViews.Clear(); floating.Clear(); saws.Clear();
            enemyPool.Clear(); bulletPool.Clear(); pickupPool.Clear(); hazardPool.Clear();
            arena = Ui.Rect(screen, "Arena", 0, 0, 540, 960);
            arena.gameObject.AddComponent<RectMask2D>().padding = new Vector4(0,128,0,177);
            scenery=ArenaScenery.Build(arena,stage,combat.Challenge);
            presentationTime=0;presentedBoss=-1;bossCue=null;
            pickupLayer = Ui.Rect(arena, "Experience", 0, 0, 540, 960);
            hazardLayer = Ui.Rect(arena, "Warnings", 0, 0, 540, 960);
            enemyLayer = Ui.Rect(arena, "Enemies", 0, 0, 540, 960);
            bulletLayer = Ui.Rect(arena, "Projectiles", 0, 0, 540, 960);
            playerShadow = Ui.Disc(arena,0,0,40,new Color(0,0,0,.32f)).rectTransform;
            playerShadow.sizeDelta = new Vector2(40,13);
            player = Ui.Robot(arena, 0, 0, .64f, combat.Hero, skin: State.selectedSkin, outfit: Outfits.Active(State,combat.Hero));
            playerArt = player.GetComponentInChildren<Image>();
            playerArt.rectTransform.pivot = new Vector2(.5f,.5f);
            playerArt.rectTransform.anchoredPosition = new Vector2(40,-50);
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
            SetupCombatEffects();turretArt=twinTurretArt=null;magnetField=null;
            Ui.Label(screen, combat.Challenge ? FieldEngineering.TrialTitle(mode) : "작전 " + (stage + 1).ToString("D2") + "  /  " + Catalog.StageNames[stage], 24, 20, 406, 28, 16, Ui.Mint);
            Ui.Button(screen, "Ⅱ", 451, 23, 65, 50, PauseBattle, Ui.Raised, true, 22);
            timeText = Ui.Label(screen, "00:00", 24, 56, 250, 43, 30);
            killText = Ui.Label(screen, "처치 0", 278, 65, 155, 30, 17, Ui.Muted, TextAnchor.MiddleRight);
            Ui.Box(screen, 24, 115, 492, 20, Ui.Raised);
            hpFill = Ui.Box(screen, 24, 115, 492, 20, Ui.Mint);
            hpText = Ui.Label(screen, "", 24, 108, 492, 34, 13, Ui.Background, TextAnchor.MiddleCenter);
            Ui.Box(screen, 24, 153, 492, 7, Ui.Raised);
            xpFill = Ui.Box(screen, 24, 153, 0, 7, Ui.Blue);
            levelText = Ui.Label(screen, "Lv.1", 24, 832, 100, 28, 17, Ui.Mint);
            skillText = Ui.Label(screen, "", 130, 832, 386, 40, 12, Ui.Muted, TextAnchor.MiddleRight);
            guideHint=Ui.Label(screen,"",40,190,460,40,14,Ui.Ink,TextAnchor.MiddleCenter);
            bossBackdrop=Ui.Box(screen,72,183,396,49,new Color(.035f,.055f,.08f,.78f),true);
            bossText = Ui.Label(screen, "", 80, 187, 380, 34, 14, Ui.Red, TextAnchor.MiddleCenter);
            bossFill = Ui.Box(screen, 80, 221, 0, 7, Ui.Red);
            joystick = Ui.Disc(screen, 64, 846, 95, new Color(.5f, .8f, .8f, .12f)).rectTransform;
            stick = Ui.Disc(joystick, 30, 30, 35, new Color(.6f, 1, .8f, .35f)).rectTransform;
            if(combat.Support==null)Ui.Label(screen, "드래그 이동\n자동 사격", 188, 881, 135, 51, 15, Ui.Muted);
            ultimateButton = Ui.Button(screen,"",344,876,172,66,TriggerUltimate,Ui.Raised,true,17);
            ultimateLabel = ultimateButton.GetComponentInChildren<Text>();
            Ui.Box(screen,351,934,158,4,Ui.Background,false);
            ultimateFill = Ui.Box(screen,351,934,79,4,Ui.Mint,false);
            SetupSupportButton();SetupFieldTask();SetupBattleFeedback();
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
                if (p.y > 235 && p.y < 940 && p.x >= 0 && p.x <= 540 && !(p.x > 175 && p.y > 860))
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
            if (smokeCapturing || networkBusy) return;
            if (combat.Ended) { if (!resultShown) FinishBattle(); return; }
            if (battlePaused) return;
            if (combat.Choosing) { if (!choicesShown) ShowChoices(); return; }
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) TriggerUltimate();
            if (combat.Ended) { FinishBattle(); return; }
            if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame) TriggerSupport();
            V2 movement = smokeActive ? SmokeMovement() : Movement();
            battleAccumulator += Mathf.Min(Time.deltaTime, .1f);
            frameBattleEffects.Clear();
            while (battleAccumulator >= ReplayRecorder.Step && !combat.Ended && !combat.Choosing)
            {
                battleAccumulator -= ReplayRecorder.Step;
                combat.Tick(ReplayRecorder.Step, recorder.Move(movement));
                frameBattleEffects.AddRange(combat.Effects);
            }
            combat.Effects.Clear();combat.Effects.AddRange(frameBattleEffects);
            RenderBattle();
            if (combat.Ended) FinishBattle(); else if (combat.Choosing) ShowChoices();
        }
        void RenderBattle()
        {
            float presentationDelta=Mathf.Max(0,combat.Elapsed-presentationTime);presentationTime=combat.Elapsed;
            scenery.Render(combat.Elapsed);particles.Advance(presentationDelta);
            RenderPlayerMotion();
            drone.gameObject.SetActive(combat.Skills[3] > 0);
            drone.anchoredPosition = new Vector2(combat.DronePosition.x - 10, -(combat.DronePosition.y - 14));
            for (int i = 0; i < saws.Count; i++)
            {
                bool active = combat.Skills[1] > 0 && i < 1 + combat.Skills[1]; saws[i].gameObject.SetActive(active);
                if (active) { V2 p = combat.SawPosition(i); saws[i].anchoredPosition = new Vector2(p.x, -p.y); saws[i].localEulerAngles = new Vector3(0, 0, combat.Elapsed * 250); }
            }
            hpFill.rectTransform.sizeDelta = new Vector2(492 * Mathf.Clamp01(combat.Hp / combat.MaxHp), 20);
            hpFill.color = combat.Hp < combat.MaxHp * .3f ? Ui.Red : Ui.Mint;
            hpText.text = Mathf.CeilToInt(combat.Hp) + " / " + Mathf.CeilToInt(combat.MaxHp) + (combat.Shield > 0 ? "  보호막" : "");
            xpFill.rectTransform.sizeDelta = new Vector2(492 * Mathf.Clamp01((float)combat.Xp / combat.NeededXp), 7);
            timeText.text = TimeLabel(combat.Elapsed) + " / " + TimeLabel(combat.Duration);
            killText.text = combat.Challenge ? combat.CoreScore.ToString("N0")+"점" : "처치 " + combat.Kills; levelText.text = "Lv." + combat.Level;
            skillText.text = (combat.FieldModule>0?FieldEngineering.Name(combat.FieldModule,combat.Hero)+"\n":"")+"탄막 " + combat.Skills[0] + "  ·  톱날 " + combat.Skills[1] + "  ·  코일 " + combat.Skills[2] + "  ·  드론 " + combat.Skills[3];
            alive.Clear(); Enemy boss = null;
            foreach (var e in combat.Enemies)
            {
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
            bossText.text = boss == null ? "" : combat.Challenge ? FieldEngineering.TrialObjective(combat.Mode) : combat.Definition.Boss + " · " + (boss.hp<boss.maxHp*.5f?"위험 단계":"위험 예고를 피하세요");
            bossFill.rectTransform.sizeDelta = new Vector2(boss == null ? 0 : 380 * Mathf.Clamp01(combat.Challenge ? combat.CoreScore/30000f : boss.hp / boss.maxHp), 7);
            alive.Clear();
            foreach (var b in combat.Bullets)
            {
                alive.Add(b.id); RectTransform view;
                if (!bulletViews.TryGetValue(b.id, out view))
                {
                    if (bulletPool.Count > 0) view = bulletPool.Pop();
                    else view=BulletVisual.Create(bulletLayer);
                    view.gameObject.SetActive(true); bulletViews[b.id] = view;
                }
                view.GetComponent<BulletVisual>().Render(b,combat.Skills[0],Outfits.Active(State,combat.Hero)>0,combat.Elapsed);
            }
            Recycle(bulletViews, bulletPool); alive.Clear();
            foreach (var p in combat.Pickups)
            {
                alive.Add(p.id); RectTransform view;
                if (!pickupViews.TryGetValue(p.id, out view))
                {
                    view = pickupPool.Count > 0 ? pickupPool.Pop() : Ui.Box(pickupLayer, 0, 0, 9, 9, Ui.Mint).rectTransform;
                    view.gameObject.SetActive(true); pickupViews[p.id] = view;
                }
                view.anchoredPosition = new Vector2(p.position.x - 4, -p.position.y + 4); view.localEulerAngles = new Vector3(0, 0, 45);
                view.localScale = Vector3.one * (p.heal ? 1.5f : p.xp > 2 ? 1.3f : 1);
                view.GetComponent<Image>().color = p.heal ? Ui.Red : Ui.Mint;
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
                boundary.color=Color.Lerp(Ui.Hex("FF735B"),Ui.Hex("FFF0B0"),.3f*Mathf.Abs(Mathf.Sin(h.delay*12)));
                var text=view.GetComponentInChildren<Text>();text.rectTransform.sizeDelta=new Vector2(width,height);
            }
            Recycle(hazardViews, hazardPool);
            foreach (var effect in combat.Effects)
            {
                EventFeedback(effect);
                if(effect.kind==10)particles.Burst(effect.position,18,Ui.Mint,90,4);
                if(effect.kind==7 || effect.kind==10)continue;
                if(effect.kind==3 && boss!=null && effect.text==combat.Definition.Boss+" 출현!")continue;
                if(effect.kind==5)particles.Burst(effect.position,effect.radius>=23?10:5,Ui.Hex("DDB482"),90,4);
                if(effect.kind==2)particles.Burst(effect.position,12,Ui.Hex("FFBF72"),130,3);
                if (floating.Count >= 36) continue;
                RectTransform visual;
                if(effect.kind==8)
                {
                    visual=Ui.Rect(effectLayer,"Chain arc",0,0,540,960);
                    var arc=visual.gameObject.AddComponent<LightningArc>();arc.raycastTarget=false;arc.color=Ui.Hex("A5EFFF");
                    arc.From=new Vector2(effect.position.x,-effect.position.y);arc.To=new Vector2(effect.end.x,-effect.end.y);
                }
                else if (!string.IsNullOrEmpty(effect.text))
                    visual = Ui.Label(effectLayer, effect.text, effect.position.x - 70, effect.position.y - 30, 140, 35, effect.kind == 3 ? 21 : 16,
                        effect.kind == 1 ? Ui.Red : effect.kind == 6 ? Ui.Mint : Ui.Gold, TextAnchor.MiddleCenter).rectTransform;
                else
                {
                    visual=CombatRing.Create(effectLayer,Mathf.Max(8,effect.radius*2),effect.kind==4?new Color(.4f,.86f,1,.65f):new Color(1,.72f,.3f,.5f),effect.kind==4?.022f:.04f);
                    visual.anchoredPosition=new Vector2(effect.position.x,-effect.position.y);
                }
                floating.Add(new FloatingEffect { root = visual, life = .55f, total = .55f, text = !string.IsNullOrEmpty(effect.text) });
            }
            combat.Effects.Clear();
            RenderModEffects();
            RenderUltimateHud();
            RenderGuideHint();
            RenderSupport();RenderFieldTask();RenderBattleFeedback();
            for (int i = floating.Count - 1; i >= 0; i--)
            {
                var f = floating[i]; f.life -= presentationDelta;
                if (f.life <= 0) { Destroy(f.root.gameObject); floating.RemoveAt(i); continue; }
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
            var card = Dialog("한 단계 더 강하게", "Lv." + (combat.Level + 1) + " · 이번 전투에 적용할 스킬을 선택하세요.", 604);
            for (int i = 0; i < combat.Choices.Length; i++)
            {
                int skill = combat.Choices[i]; float y = 140 + i * 142;
                var row = Ui.Box(card, 24, y, 444, 126, Ui.Raised).transform;
                Ui.Label(row, Catalog.SkillNames[skill] + "  " + combat.Skills[skill] + " → " + (combat.Skills[skill] + 1), 15, 10, 300, 38, 21, skill < 4 ? Ui.Mint : Ui.Gold);
                Ui.Label(row, Catalog.SkillDescriptions[skill]+ModHint(skill), 15, 49, 300, 73, 13, Ui.Muted);
                Ui.Button(row, "선택", 331, 38, 98, 54, () => { ChooseBattleSkill(skill); }, null, true, 17);
            }
            // A skill selection cannot be dismissed without making a selection.
            card.GetChild(2).gameObject.SetActive(false);
        }
        void PauseBattle()
        {
            if (combat == null || combat.Ended || combat.Choosing) return;
            battlePaused = true; dragging = false;
            var card = Dialog("잠시 정비 중", "작전은 일시정지되어 있어요.", 382);
            Ui.Button(card, "계속하기", 24, 149, 444, 62, () => { battlePaused = false; CloseModal(); });
            Ui.Button(card, combat.Challenge ? "도전 종료하고 기록 저장" : "작전 종료하고 보상 받기", 24, 232, 444, 58, () => { battlePaused = false; combat.Abandon(); CloseModal(); FinishBattle(); }, Ui.Raised, true, 17);
            Ui.Label(card, "30초 미만 전투는 일일 전투 횟수에서 제외돼요.", 24, 310, 444, 43, 13, Ui.Muted);
            card.GetChild(2).gameObject.SetActive(false);
        }
        void ChooseBattleSkill(int skill)
        {
            if (!combat.Choose(skill)) return;
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
                if (result.Success) { if(State.gems!=beforeGems)Track("economy","gems",State.gems-beforeGems,State.gems);Track("guide_progress","step",StarterGuide.Next(State));Track("battle_end",(string.IsNullOrEmpty(combat.Mode)?"campaign":combat.Mode)+(combat.Won?"_win":"_loss"),combat.Stage,combat.Elapsed); Track("performance","battle",slowFrames,frameSamples==0?0:(float)(frameSamples/frameSeconds)); selectedStage = State.unlockedStage; ShowResult(); return; }
                var card = Dialog("보상을 저장하지 못했어요", result.Message, 420);
                Ui.Button(card, "같은 기록으로 다시 저장", 24, 210, 444, 59, () => { resultShown = false; FinishBattle(); });
                Ui.Button(card, "정비소로 이동 · 계정에서 동기화", 24, 293, 444, 59, () => { combat = null; Show("home"); AccountPanel(); }, Ui.Raised, true, 16);
                card.GetChild(2).gameObject.SetActive(false);
            };
            if (Online) StartCoroutine(SubmitCloud(finishCommand, reply => complete(new CommandResult(reply.success, reply.message))));
            else complete(Store.Change(e => combat.Challenge ? e.FinishChallenge(combat) : e.FinishCampaign(combat)));
        }
        void ShowResult()
        {
            if (combat.Challenge) { ChallengeResult(); return; }
            var receipt = State.receipts.Find(r => r.id == combat.RunId);
            bool won = receipt != null ? receipt.won : combat.Won;
            var card = Dialog(won ? "구역 정리 완료!" : "다음 작전을 준비해요", Catalog.StageNames[combat.Stage] + " · 다음 작전을 준비하세요.", 745);
            Ui.Label(card, won ? "작전 성공" : "정비하고 다시 도전해요", 24, 134, 444, 40, 20, won ? Ui.Mint : Ui.Gold, TextAnchor.MiddleCenter);
            Ui.Label(card, TimeLabel(receipt != null ? receipt.seconds : combat.Elapsed) + " 생존    " + (receipt != null ? receipt.kills : combat.Kills) + "마리 처치    Lv." + combat.Level, 24, 184, 444, 40, 17, Ui.Muted, TextAnchor.MiddleCenter);
            Ui.Box(card, 24, 249, 444, 92, Ui.Raised);
            Ui.Label(card, receipt == null ? "보상 저장 완료" : "골드 +" + receipt.gold + "   부품 +" + receipt.parts + (receipt.blueprints>0 ? "\n지역 설계도 +"+receipt.blueprints : "") + (receipt.gems > 0 ? " · 최초 보석 +" + receipt.gems : ""), 39, 258, 414, 74, 19, Ui.Gold, TextAnchor.MiddleCenter);
            foreach(var text in card.GetComponentsInChildren<Text>())if(text.text.StartsWith("골드 +"))text.gameObject.AddComponent<RewardReveal>().Initialize(text);
            if(receipt!=null && receipt.report!=null && receipt.report.taskCompleted)
                Ui.Label(card,"현장 임무 보상 포함 · 골드 +"+(receipt.report.taskChoice==2?400:200)+" / 부품 +"+(receipt.report.taskChoice==2?4:2),24,334,444,25,12,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Button(card,"전투 분석 · 조합 확인",24,360,444,51,BattleAnalysis,Ui.Mint,true,18);
            bool canBonus = (!Online || State.adSkip) && !State.lastResultBonus && State.lastResultGold > 0 && State.dailyRewardGold < 3 && State.lastResultDate == State.serviceDate;
            Ui.Button(card, State.lastResultBonus ? "추가 보상 수령 완료" : "추가 골드 +50%  ·  " + (State.demoAdSkip || State.adSkip ? "광고 없이 받기" : "체험 안내"), 24, 433, 444, 57,
                () => RequestReward("result"), Ui.Raised, canBonus, 17);
            bool showSupport=combat.Support!=null && combat.SupportUsed && combat.Support.showIdentity && Online;
            if(showSupport)
                Ui.Button(card,"지원해 준 동료의 전시실",24,591,444,34,()=>{Track("profile_source","support_result");StartCoroutine(LoadPublic(combat.Support.owner));},Ui.Raised,true,14);
            int stage = combat.Stage;
            Ui.Button(card, "다시 출격", 24, 519, 214, 62, () => { combat = null; StartBattle(stage); });
            Ui.Button(card, "장비 정비", 254, 519, 214, 62, () => { combat = null; Show("gear"); }, Ui.Raised);
            if(!showSupport && StarterGuide.Next(State)<5)Ui.Button(card,"다음 목표 · "+StarterGuide.Names[StarterGuide.Next(State)],24,591,444,34,()=>{combat=null;Show("home");GuidePanel();},Ui.Raised,true,14);
            Ui.Button(card, "정비소로 돌아가기", 24, 632, 444, 51, () => { combat = null; Show("home"); }, Ui.Panel, true, 16);
            card.GetChild(2).gameObject.SetActive(false);
        }
    }
}
