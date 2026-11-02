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
        RectTransform arena, enemyLayer, pickupLayer, bulletLayer, hazardLayer, effectLayer, player, drone, joystick, stick;
        readonly List<RectTransform> saws = new List<RectTransform>();
        readonly Dictionary<int, RectTransform> enemyViews = new Dictionary<int, RectTransform>(), bulletViews = new Dictionary<int, RectTransform>(), pickupViews = new Dictionary<int, RectTransform>(), hazardViews = new Dictionary<int, RectTransform>();
        readonly HashSet<int> alive = new HashSet<int>();
        readonly List<int> stale = new List<int>();
        readonly Stack<RectTransform> enemyPool = new Stack<RectTransform>(), bulletPool = new Stack<RectTransform>(), pickupPool = new Stack<RectTransform>(), hazardPool = new Stack<RectTransform>();
        readonly List<FloatingEffect> floating = new List<FloatingEffect>();
        Image hpFill, xpFill, bossFill;
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
            recorder = new ReplayRecorder(); battleAccumulator = 0; finishCommand = null;
            battlePaused = choicesShown = resultShown = dragging = false;
            CloseModal(); Ui.Clear(screen); enemyViews.Clear(); bulletViews.Clear(); pickupViews.Clear(); hazardViews.Clear(); floating.Clear(); saws.Clear();
            enemyPool.Clear(); bulletPool.Clear(); pickupPool.Clear(); hazardPool.Clear();
            arena = Ui.Rect(screen, "Arena", 0, 0, 540, 960);
            arena.gameObject.AddComponent<RectMask2D>().padding = new Vector4(0,128,0,177);
            BattleArt.Floor(arena,stage);
            pickupLayer = Ui.Rect(arena, "Experience", 0, 0, 540, 960);
            hazardLayer = Ui.Rect(arena, "Warnings", 0, 0, 540, 960);
            enemyLayer = Ui.Rect(arena, "Enemies", 0, 0, 540, 960);
            bulletLayer = Ui.Rect(arena, "Projectiles", 0, 0, 540, 960);
            playerShadow = Ui.Disc(arena,0,0,40,new Color(0,0,0,.32f)).rectTransform;
            playerShadow.sizeDelta = new Vector2(40,13);
            player = Ui.Robot(arena, 0, 0, .48f, State.selectedHero, skin: State.selectedSkin);
            playerArt = player.GetComponentInChildren<Image>();
            playerArt.rectTransform.pivot = new Vector2(.5f,.5f);
            playerArt.rectTransform.anchoredPosition = new Vector2(40,-50);
            drone = Ui.Robot(arena, 0, 0, .24f, 0);
            for (int i = 0; i < 6; i++)
            {
                var saw = Ui.Disc(arena, 0, 0, 23, Ui.Muted).rectTransform;
                Ui.Box(saw, 8, -4, 7, 31, Ui.Muted, false); Ui.Box(saw, -4, 8, 31, 7, Ui.Muted, false); Ui.Disc(saw, 7, 7, 9, Ui.Panel);
                saws.Add(saw);
            }
            effectLayer = Ui.Rect(arena, "Effects", 0, 0, 540, 960);
            SetupCombatEffects();
            Ui.Label(screen, combat.Challenge ? "코어 균열 / " + (mode==Endgame.PracticeMode?"연습":"주간 도전") : "작전 0" + (stage + 1) + "  /  " + Catalog.StageNames[stage], 24, 20, 406, 28, 16, Ui.Mint);
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
            bossText = Ui.Label(screen, "", 80, 187, 380, 30, 16, Ui.Red, TextAnchor.MiddleCenter);
            bossFill = Ui.Box(screen, 80, 221, 0, 7, Ui.Red);
            joystick = Ui.Disc(screen, 64, 846, 95, new Color(.5f, .8f, .8f, .12f)).rectTransform;
            stick = Ui.Disc(joystick, 30, 30, 35, new Color(.6f, 1, .8f, .35f)).rectTransform;
            if(combat.Support==null)Ui.Label(screen, "드래그 이동\n자동 사격", 188, 881, 135, 51, 15, Ui.Muted);
            ultimateButton = Ui.Button(screen,"",344,876,172,66,TriggerUltimate,Ui.Raised,true,17);
            ultimateLabel = ultimateButton.GetComponentInChildren<Text>();
            Ui.Box(screen,351,934,158,4,Ui.Background,false);
            ultimateFill = Ui.Box(screen,351,934,79,4,Ui.Mint,false);
            SetupSupportButton();
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
            while (battleAccumulator >= ReplayRecorder.Step && !combat.Ended && !combat.Choosing)
            {
                battleAccumulator -= ReplayRecorder.Step;
                combat.Tick(ReplayRecorder.Step, recorder.Move(movement));
            }
            RenderBattle();
            if (combat.Ended) FinishBattle(); else if (combat.Choosing) ShowChoices();
        }
        void RenderBattle()
        {
            RenderPlayerMotion();
            drone.gameObject.SetActive(combat.Skills[3] > 0);
            drone.anchoredPosition = new Vector2(combat.DronePosition.x - 10, -(combat.DronePosition.y - 14));
            for (int i = 0; i < saws.Count; i++)
            {
                bool active = combat.Skills[1] > 0 && i < 1 + combat.Skills[1]; saws[i].gameObject.SetActive(active);
                if (active) { V2 p = combat.SawPosition(i); saws[i].anchoredPosition = new Vector2(p.x - 11, -p.y + 11); saws[i].localEulerAngles = new Vector3(0, 0, combat.Elapsed * 250); }
            }
            hpFill.rectTransform.sizeDelta = new Vector2(492 * Mathf.Clamp01(combat.Hp / combat.MaxHp), 20);
            hpFill.color = combat.Hp < combat.MaxHp * .3f ? Ui.Red : Ui.Mint;
            hpText.text = Mathf.CeilToInt(combat.Hp) + " / " + Mathf.CeilToInt(combat.MaxHp) + (combat.Shield > 0 ? "  보호막" : "");
            xpFill.rectTransform.sizeDelta = new Vector2(492 * Mathf.Clamp01((float)combat.Xp / combat.NeededXp), 7);
            timeText.text = TimeLabel(combat.Elapsed) + " / " + TimeLabel(combat.Duration);
            killText.text = combat.Challenge ? combat.CoreScore.ToString("N0")+"점" : "처치 " + combat.Kills; levelText.text = "Lv." + combat.Level;
            skillText.text = "탄막 " + combat.Skills[0] + "  ·  톱날 " + combat.Skills[1] + "  ·  코일 " + combat.Skills[2] + "  ·  드론 " + combat.Skills[3];
            alive.Clear(); Enemy boss = null;
            foreach (var e in combat.Enemies)
            {
                alive.Add(e.id); RectTransform view;
                if (!enemyViews.TryGetValue(e.id, out view))
                {
                    if (enemyPool.Count > 0) { view = enemyPool.Pop(); view.gameObject.SetActive(true); }
                    else
                    {
                        var art = Ui.Box(enemyLayer,0,0,48,48,Color.white,false);
                        art.preserveAspect = true; view = art.rectTransform;
                        view.pivot = new Vector2(.5f,.5f);
                    }
                    enemyViews[e.id] = view;
                }
                float size = e.Radius * (e.kind == 3 ? 3.5f : 3.8f);
                view.sizeDelta = new Vector2(size,size);
                view.localScale = Vector3.one;
                view.anchoredPosition = new Vector2(e.position.x,-e.position.y);
                view.localEulerAngles = new Vector3(0,0,Mathf.Sin(combat.Elapsed*8+e.id)*(e.kind == 3 ? 1.5f : 3));
                var enemyImage = view.GetComponent<Image>();
                enemyImage.sprite = BattleArt.Enemy(e.kind,((int)(combat.Elapsed*(e.kind == 1 ? 12 : 8))+e.id)%4);
                enemyImage.color = e.flash > 0 ? new Color(1,.55f,.45f) : Color.white;
                if (e.finalBoss || e.core) boss = e;
            }
            Recycle(enemyViews, enemyPool);
            bossText.text = boss == null ? "" : combat.Challenge ? "최고 보상 목표 · 코어 피해 30,000점" : "폭주 코어  ·  위험 구역에서 벗어나세요";
            bossFill.rectTransform.sizeDelta = new Vector2(boss == null ? 0 : 380 * Mathf.Clamp01(combat.Challenge ? combat.CoreScore/30000f : boss.hp / boss.maxHp), 7);
            alive.Clear();
            foreach (var b in combat.Bullets)
            {
                alive.Add(b.id); RectTransform view;
                if (!bulletViews.TryGetValue(b.id, out view))
                {
                    if (bulletPool.Count > 0) view = bulletPool.Pop();
                    else
                    {
                        view = Ui.Box(bulletLayer,0,0,20,6,Ui.Gold).rectTransform;
                        view.pivot = new Vector2(.5f,.5f);
                        Ui.Box(view,-5,1,11,4,Color.white,false);
                    }
                    view.gameObject.SetActive(true); bulletViews[b.id] = view;
                }
                view.anchoredPosition = new Vector2(b.position.x, -b.position.y);
                view.localEulerAngles = new Vector3(0, 0, -Mathf.Atan2(b.velocity.y, b.velocity.x) * Mathf.Rad2Deg);
                view.GetComponent<Image>().color = b.drone ? Ui.Mint : Ui.Gold;
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
                        view = Ui.Disc(hazardLayer,0,0,h.radius*2,new Color(1,.25f,.2f,.25f)).rectTransform;
                        var rim = CombatRing.Create(view,h.radius*2,Ui.Red,.025f);
                        rim.anchoredPosition = new Vector2(h.radius,-h.radius);
                        Ui.Label(view,"!",h.radius-12,h.radius-20,24,40,28,Ui.Red,TextAnchor.MiddleCenter);
                    }
                    view.gameObject.SetActive(true); hazardViews[h.id] = view;
                }
                view.anchoredPosition = new Vector2(h.position.x - h.radius, -(h.position.y - h.radius));
                view.GetComponent<Image>().color = new Color(1, .25f, .2f, .15f + .3f * Mathf.Abs(Mathf.Sin(h.delay * 12)));
            }
            Recycle(hazardViews, hazardPool);
            foreach (var effect in combat.Effects)
            {
                if (floating.Count >= 36) break;
                RectTransform visual;
                if (!string.IsNullOrEmpty(effect.text))
                    visual = Ui.Label(effectLayer, effect.text, effect.position.x - 70, effect.position.y - 30, 140, 35, effect.kind == 3 ? 21 : 16,
                        effect.kind == 1 ? Ui.Red : effect.kind == 6 ? Ui.Mint : Ui.Gold, TextAnchor.MiddleCenter).rectTransform;
                else
                    visual = Ui.Disc(effectLayer, effect.position.x - effect.radius, effect.position.y - effect.radius, effect.radius * 2,
                        effect.kind == 4 ? new Color(.3f, .8f, 1, .20f) : new Color(1, .7f, .3f, .22f)).rectTransform;
                floating.Add(new FloatingEffect { root = visual, life = .55f, total = .55f, text = !string.IsNullOrEmpty(effect.text) });
            }
            combat.Effects.Clear();
            RenderUltimateHud();
            RenderSupport();
            for (int i = floating.Count - 1; i >= 0; i--)
            {
                var f = floating[i]; f.life -= Time.deltaTime;
                if (f.life <= 0) { Destroy(f.root.gameObject); floating.RemoveAt(i); continue; }
                if (f.text) f.root.anchoredPosition += Vector2.up * (30 * Time.deltaTime);
                var graphic = f.root.GetComponent<Graphic>(); Color color = graphic.color; color.a = Mathf.Min(color.a, f.life / f.total); graphic.color = color;
            }
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
                Ui.Label(row, Catalog.SkillDescriptions[skill], 15, 51, 300, 58, 15, Ui.Muted);
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
            Action<CommandResult> complete = result =>
            {
                if (result.Success) { selectedStage = State.unlockedStage; ShowResult(); return; }
                var card = Dialog("보상을 저장하지 못했어요", result.Message, 420);
                Ui.Button(card, "같은 기록으로 다시 저장", 24, 210, 444, 59, () => { resultShown = false; FinishBattle(); });
                Ui.Button(card, "정비소로 이동 · 계정에서 동기화", 24, 293, 444, 59, () => { combat = null; Show("home"); AccountPanel(); }, Ui.Raised, true, 16);
                card.GetChild(2).gameObject.SetActive(false);
            };
            if (Online) StartCoroutine(SubmitCloud(finishCommand, reply => complete(new CommandResult(reply.success, reply.message))));
            else complete(Store.Change(e => combat.Challenge ? e.FinishChallenge(combat) : CommandDispatcher.Apply(e, finishCommand, true)));
        }
        void ShowResult()
        {
            if (combat.Challenge) { ChallengeResult(); return; }
            var receipt = State.receipts.Find(r => r.id == combat.RunId);
            bool won = receipt != null ? receipt.won : combat.Won;
            var card = Dialog(won ? "구역 정리 완료!" : "다음 작전을 준비해요", Catalog.StageNames[combat.Stage] + " · 장비를 강화하고 다시 도전하세요.", 620);
            Ui.Label(card, won ? "MISSION CLEAR" : "BACK TO THE WORKSHOP", 24, 134, 444, 40, 20, won ? Ui.Mint : Ui.Gold, TextAnchor.MiddleCenter);
            Ui.Label(card, TimeLabel(receipt != null ? receipt.seconds : combat.Elapsed) + " 생존    " + (receipt != null ? receipt.kills : combat.Kills) + "마리 처치    Lv." + combat.Level, 24, 184, 444, 40, 17, Ui.Muted, TextAnchor.MiddleCenter);
            Ui.Box(card, 24, 249, 444, 92, Ui.Raised);
            Ui.Label(card, receipt == null ? "보상 저장 완료" : "골드 +" + receipt.gold + "   부품 +" + receipt.parts + (receipt.gems > 0 ? "\n최초 클리어 보석 +" + receipt.gems + " · 장비 보상" : ""), 39, 258, 414, 74, 19, Ui.Gold, TextAnchor.MiddleCenter);
            bool canBonus = !Online && !State.lastResultBonus && State.lastResultGold > 0 && State.dailyRewardGold < 3 && State.lastResultDate == State.serviceDate;
            Ui.Button(card, State.lastResultBonus ? "추가 보상 수령 완료" : "추가 골드 +50%  ·  " + (State.demoAdSkip ? "스킵" : "광고 체험"), 24, 367, 444, 57,
                () => RequestReward("result"), Ui.Raised, canBonus, 17);
            int stage = combat.Stage;
            Ui.Button(card, "다시 출격", 24, 451, 214, 62, () => { combat = null; StartBattle(stage); });
            Ui.Button(card, "장비 정비", 254, 451, 214, 62, () => { combat = null; Show("gear"); }, Ui.Raised);
            Ui.Button(card, "정비소로 돌아가기", 24, 536, 444, 51, () => { combat = null; Show("home"); }, Ui.Panel, true, 16);
            card.GetChild(2).gameObject.SetActive(false);
        }
    }
}
