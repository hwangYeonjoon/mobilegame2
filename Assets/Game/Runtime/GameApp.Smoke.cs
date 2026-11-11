using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScrapGuard
{
    public partial class GameApp
    {
        bool smokeActive,smokeRecover,smokeWithNatural,smokeBossProgression;
        bool smokeCapturing;
        string smokeOutput;
        readonly List<string> smokeErrors = new List<string>();
        void SetupSmoke(string[] args)
        {
            smokeActive = Array.IndexOf(args, "-smoke") >= 0;smokeWithNatural=Array.IndexOf(args,"-withNatural")>=0;smokeBossProgression=Array.IndexOf(args,"-bossProgression")>=0;
            if (!smokeActive) return;
            Application.runInBackground = true;
            smokeOutput = Path.Combine(Application.persistentDataPath, "Smoke");
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-smokeOutput") smokeOutput = args[i + 1];
            Directory.CreateDirectory(smokeOutput);
            Application.logMessageReceived += (message, stack, type) => { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) smokeErrors.Add(message); };
            StartCoroutine(Array.IndexOf(args,"-regionalSmoke")>=0 ? RegionalSmokeRoutine() : Array.IndexOf(args,"-growthSmoke")>=0 ? GrowthSmokeRoutine(Array.IndexOf(args,"-regionalRuntime")>=0) : Array.IndexOf(args,"-naturalCombat")>=0 ? NaturalCombatRoutine() : Array.IndexOf(args,"-assaultSmoke")>=0 ? AssaultSmokeRoutine() : Array.IndexOf(args,"-polishSoak")>=0 ? PolishSmokeRoutine(true) : Array.IndexOf(args,"-polishSmoke")>=0 ? PolishSmokeRoutine() : Array.IndexOf(args,"-battleClaritySmoke")>=0 ? BattleClaritySmokeRoutine() : Array.IndexOf(args,"-uiCompleteSmoke")>=0 ? UiCompleteSmokeRoutine() : Array.IndexOf(args,"-shopPolishSmoke")>=0 ? ShopPolishSmokeRoutine() : Array.IndexOf(args,"-liveOpsSmoke")>=0 ? LiveOpsSmokeRoutine() : Array.IndexOf(args,"-serviceSmoke")>=0 ? ServiceSmokeRoutine() : Array.IndexOf(args,"-styleValueSmoke")>=0 ? StyleValueSmokeRoutine() : Array.IndexOf(args,"-updateSmoke")>=0 ? UpdateSmokeRoutine() : Array.IndexOf(args,"-retentionSmoke")>=0 ? RetentionSmokeRoutine() : Array.IndexOf(args,"-engineeringSmoke")>=0 ? EngineeringSmokeRoutine() : Array.IndexOf(args,"-commerceSmoke")>=0 ? CommerceSmokeRoutine() : Array.IndexOf(args,"-experienceSmoke")>=0 ? ExperienceSmokeRoutine() : Array.IndexOf(args,"-artSmoke")>=0 ? ArtSmokeRoutine() : Array.IndexOf(args,"-outfitSmoke")>=0 ? OutfitSmokeRoutine() : Array.IndexOf(args,"-expansionSmoke")>=0 ? ExpansionSmokeRoutine() : Array.IndexOf(args,"-workshopSmoke")>=0 ? WorkshopSmokeRoutine() : Array.IndexOf(args,"-campaignSmoke")>=0 ? CampaignSmokeRoutine() : Array.IndexOf(args, "-cloudSmoke") >= 0 ? CloudSmokeRoutine() : Array.IndexOf(args,"-endgameSmoke")>=0 ? EndgameSmokeRoutine() : SmokeRoutine());
        }
        void UpdateSmoke() { }
        V2 SmokeMovement()
        {
            if(smokeRecover && !combat.TaskCompleted)return (combat.TaskPosition-combat.Position).Normal;
            V2 away = new V2();
            foreach (var e in combat.Enemies)
            {
                V2 delta = combat.Position - e.position; float d = delta.Length;
                if (d < 130) away += delta.Normal * ((130 - d) / 90);
            }
            V2 target = new V2(270 + Mathf.Cos(combat.Elapsed * .6f) * 150, 490 + Mathf.Sin(combat.Elapsed * .6f) * 200);
            return ((target - combat.Position).Normal + away).Normal;
        }
        IEnumerator Capture(string name)
        {
            smokeCapturing = true;
            yield return new WaitForSecondsRealtime(.4f);
            toastRoot.gameObject.SetActive(false);
            ValidateButtonTargets();
            yield return new WaitForEndOfFrame();
#if UNITY_ANDROID && !UNITY_EDITOR
            // Android CaptureScreenshot prefixes persistentDataPath even for absolute names.
            var captured=ScreenCapture.CaptureScreenshotAsTexture();
            try{File.WriteAllBytes(Path.Combine(smokeOutput,name+".png"),captured.EncodeToPNG());}
            finally{Destroy(captured);}
#else
            ScreenCapture.CaptureScreenshot(Path.Combine(smokeOutput, name + ".png"));
#endif
            yield return new WaitForSecondsRealtime(.35f);
            smokeCapturing = false;
        }
        void ValidateButtonTargets()
        {
            Transform scope = updateOverlay != null ? updateOverlay : titleScreen != null && titleScreen.gameObject.activeSelf ? titleScreen : modal.childCount > 0 ? (Transform)modal : screen;
            foreach (var button in scope.GetComponentsInChildren<Button>())
            {
                if (!button.isActiveAndEnabled || !button.interactable) continue;
                var rect = (RectTransform)button.transform;
                Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                // Scrolled-out rows are intentionally not hittable.
                bool clipped=false;
                foreach(var mask in button.GetComponentsInParent<RectMask2D>())
                    if(mask.isActiveAndEnabled&&!RectTransformUtility.RectangleContainsScreenPoint(mask.rectTransform,point)){clipped=true;break;}
                if(clipped)continue;
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
                if (hits.Count == 0 || (hits[0].gameObject != button.gameObject && !hits[0].gameObject.transform.IsChildOf(button.transform)))
                    smokeErrors.Add("Button blocked: " + button.name);
            }
        }
        IEnumerator SmokeRoutine()
        {
            yield return Capture("00-title");
            ClickVisibleButton(!Online && !cloud.Session.localPlaySelected?"로컬 체험":"정비소로 들어가기");
            if (titleScreen != null) smokeErrors.Add("Title screen did not dismiss");
            if (Array.IndexOf(Environment.GetCommandLineArgs(),"-titleSmoke") >= 0)
            {
                yield return Capture("01-home");
                File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Title runtime errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
                Application.Quit(smokeErrors.Count==0 ? 0 : 1);yield break;
            }
            yield return Capture("01-home");
            Attendance(); yield return Capture("02-attendance");
            ClickVisibleButton("오늘의 보상 받기");
            if (State.attendanceCount != 1) smokeErrors.Add("Attendance button failed");
            Show("gear"); yield return Capture("03-equipment");
            string weapon = State.equipped[0];
            ItemDetail(weapon); yield return Capture("10-upgrade");
            ClickVisibleButton("장비 강화  +1");
            if (State.items.Find(i => i.id == weapon).level != 2) smokeErrors.Add("Upgrade button failed");
            Show("heroes"); yield return Capture("04-heroes");
            Show("shop"); yield return Capture("05-shop");
            int oldGems = State.gems, oldSkin = State.selectedSkin, oldHero = State.selectedHero;
            string ownedBefore = string.Join(",",State.ownedCosmetics);
            Wardrobe(1); yield return Capture("16-amber-fitting");
            Wardrobe(2,2); yield return Capture("17-glacier-fitting");
            Wardrobe(0,1); yield return Capture("18-locked-hero-preview");
            if (State.gems != oldGems || State.selectedSkin != oldSkin || State.selectedHero != oldHero || ownedBefore != string.Join(",",State.ownedCosmetics)) smokeErrors.Add("Preview changed ownership or wallet");
            CosmeticOffer("skin_amber"); yield return Capture("19-cosmetic-confirmation");
            shopSupplies = true; Show("shop"); yield return Capture("20-supplies");
            shopSupplies = false;
            Show("showcase"); yield return Capture("11-showcase");
            CustomizeProfile(); yield return Capture("12-customize");
            CloseModal();
            Change(new GameCommand("demo_gems"));
            Change(new GameCommand("cosmetic_buy") { value = "frame_solar" });
            Change(new GameCommand("cosmetic_buy") { value = "skin_amber" });
            Change(new GameCommand("customize") { hero = 0, title = 0, frame = 3, skin = 1 });
            Show("showcase"); yield return Capture("13-cosmetic-showcase");
            yield return ExportCard(showcaseCard);
            if (string.IsNullOrEmpty(lastSharedPath) || !File.Exists(lastSharedPath)) smokeErrors.Add("Missing profile export");
            else File.Copy(lastSharedPath, Path.Combine(smokeOutput,"14-share-card.png"),true);
            AccountPanel(); yield return Capture("15-account");
            CloseModal(); StartBattle(0, true);
            float until = Time.realtimeSinceStartup + 120;
            bool capturedChoice = false, capturedBattle = false, capturedBoss = false, capturedUltimate = false;
            while (!combat.Ended && Time.realtimeSinceStartup < until)
            {
                if (combat.Choosing)
                {
                    if (!capturedChoice) { yield return Capture("07-skill-choice"); capturedChoice = true; }
                    int choice = combat.Choices[0];
                    foreach (int skill in new[] { 1, 2, 0, 3, 4, 5, 6, 7 }) if (Array.IndexOf(combat.Choices, skill) >= 0) { choice = skill; break; }
                    ChooseBattleSkill(choice);
                }
                if (combat.CanUltimate && !combat.Choosing)
                {
                    TriggerUltimate();
                    if (!capturedUltimate) { yield return Capture("21-ultimate"); capturedUltimate = true; }
                }
                if (combat.Elapsed >= 8 && !capturedBattle) { yield return Capture("06-combat"); capturedBattle = true; }
                if (combat.FinalBossActive && !capturedBoss && !combat.Choosing) { yield return Capture("09-boss"); capturedBoss = true; }
                yield return null;
            }
            if (!capturedUltimate) smokeErrors.Add("Ultimate was not exercised");
            if (!combat.Ended) combat.Abandon();
            FinishBattle(); yield return Capture("08-result");
            int expectedGold = State.gold, expectedLevel = State.items.Find(i => i.id == weapon).level;
            var restored = new ProfileStore(Path.Combine(Path.GetDirectoryName(SmokeProfilePath()), Path.GetFileName(SmokeProfilePath())));
            if (restored.State.gold != expectedGold || restored.State.items.Find(i => i.id == weapon)?.level != expectedLevel) smokeErrors.Add("Persistence mismatch");
            File.WriteAllText(Path.Combine(smokeOutput, "report.txt"), "Screens captured. Runtime errors: " + smokeErrors.Count + "\n" + string.Join("\n", smokeErrors) + "\nGold=" + expectedGold + " WeaponLevel=" + expectedLevel + "\n");
            Debug.Log("SMOKE_COMPLETE errors=" + smokeErrors.Count);
            Application.Quit(smokeErrors.Count == 0 ? 0 : 1);
        }
        void ClickVisibleButton(string name)
        {
            Transform scope=updateOverlay!=null?updateOverlay:titleScreen!=null&&titleScreen.gameObject.activeSelf?titleScreen:modal.childCount>0?(Transform)modal:screen;
            foreach (var button in scope.GetComponentsInChildren<Button>())
            {
                if (button.name != name || !button.isActiveAndEnabled || !button.interactable) continue;
                ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
                return;
            }
            smokeErrors.Add("Missing button: " + name);
        }
        string SmokeProfilePath()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-profilePath") return args[i + 1];
            return Path.Combine(Application.persistentDataPath, "scrap-guard-v1.json");
        }
    }
}
