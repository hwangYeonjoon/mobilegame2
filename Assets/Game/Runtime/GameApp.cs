using System;
using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ScrapGuard
{
    public partial class GameApp : MonoBehaviour
    {
        public ProfileStore Store { get; private set; }
        public GameState State { get { return Store.State; } }
        public RectTransform Root { get; private set; }
        RectTransform screen, modal, toastRoot;
        Text toastLabel;
        readonly Text[] walletAmounts = new Text[3];
        float toastTime;
        string page = "home";
        int selectedStage;
        bool demoRewardPending;
        AudioSource audioSource;
        AudioClip clickClip;
        public static GameApp Instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (FindFirstObjectByType<GameApp>() == null) new GameObject("Scrap Guard").AddComponent<GameApp>(); }
        void Awake()
        {
            Instance = this;
            Debug.developerConsoleEnabled = false;
            Application.targetFrameRate = 60; Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Ui.Font = Resources.Load<Font>("Fonts/GowunDodum-Regular");
            string profile = Path.Combine(Application.persistentDataPath, "scrap-guard-v1.json");
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-profilePath") profile = args[i + 1];
#if SCRAP_QA_SOAK
            if(!Application.identifier.EndsWith(".qa",StringComparison.Ordinal))throw new InvalidOperationException("QA soak requires isolated package");
            profile=Path.Combine(Application.persistentDataPath,"qa-profile.json");
#if SCRAP_QA_MANUAL
            args=Array.Empty<string>();
#elif SCRAP_QA_ASSAULT
            args=new[]{"-smoke","-assaultSmoke","-withNatural","-smokeOutput",Path.Combine(Application.persistentDataPath,"AssaultUI")};
#elif SCRAP_QA_BOSS
            args=new[]{"-smoke","-naturalCombat","-bossProgression","-smokeOutput",Path.Combine(Application.persistentDataPath,"BossProgression")};
#elif SCRAP_QA_REGIONAL
            args=new[]{"-smoke","-growthSmoke","-regionalRuntime","-smokeOutput",Path.Combine(Application.persistentDataPath,"RegionalRuntime")};
#elif SCRAP_QA_GROWTH
            args=new[]{"-smoke","-growthSmoke","-smokeOutput",Path.Combine(Application.persistentDataPath,"GrowthUI")};
#elif SCRAP_QA_UI
            args=new[]{"-smoke","-polishSmoke","-smokeOutput",Path.Combine(Application.persistentDataPath,"PolishUI")};
#else
            args=new[]{"-smoke","-polishSoak","-smokeOutput",Path.Combine(Application.persistentDataPath,"PolishSoak")};
#endif
#endif
#if SCRAP_QA_GROWTH || SCRAP_QA_REGIONAL
            profile=Path.Combine(Application.persistentDataPath,"qa-growth-"+Guid.NewGuid().ToString("N")+".json");
#endif
            Store = new ProfileStore(profile);
            SetupCloud(profile);
#if SCRAP_QA_SOAK
            cloud.Session.enabled=false;cloud.Session.guestRegistrationPending=false;cloud.Session.localPlaySelected=true;Store=localStore;
#endif
            SetupResearch(profile);
            SetupServices();
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var system = new GameObject("Input", typeof(EventSystem));
                system.AddComponent<InputSystemUIInputModule>();
            }
            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Root = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>(); Root.SetParent(canvas.transform, false);
            Root.anchorMin = Root.anchorMax = Root.pivot = new Vector2(.5f, .5f); Root.sizeDelta = new Vector2(540, 960);
            FitScreen();
            Ui.Box(Root, 0, 0, 540, 960, Ui.Background, false);
            screen = Ui.Rect(Root, "Screen", 0, 0, 540, 960);
            modal = Ui.Rect(Root, "Modal", 0, 0, 540, 960);
            toastRoot = Ui.Rect(Root, "Toast", 32, 806, 476, 48);
            Ui.Box(toastRoot, 0, 0, 476, 48, Ui.Ink);
            toastLabel = Ui.Label(toastRoot, "", 12, 0, 452, 48, 15, Ui.Background, TextAnchor.MiddleCenter); toastRoot.gameObject.SetActive(false);
            audioSource = gameObject.AddComponent<AudioSource>();
            clickClip = AudioClip.Create("Soft click", 2205, 1, 22050, false);
            var wave = new float[2205]; for (int i = 0; i < wave.Length; i++) wave[i] = Mathf.Sin(i * .18f) * .08f * (1 - (float)i / wave.Length);
            clickClip.SetData(wave, 0);
            selectedStage = State.unlockedStage; Show("home");
            if (!string.IsNullOrEmpty(Store.Notice)) Toast(Store.Notice);
            ShowTitleScreen();
            SetupSmoke(args);
            SetupUpdates();
            if (Online) StartCoroutine(ReconnectCloud());
        }
        void FitScreen()
        {
            Rect safe = Screen.safeArea; float scale = Mathf.Min(safe.width / 540, safe.height / 960);
            Root.localScale = Vector3.one * scale;
            Root.anchoredPosition = safe.center - new Vector2(Screen.width / 2f, Screen.height / 2f);
            FitMenu(scale);
        }
        void Update()
        {
            FitScreen();
            PollUpdateNotice();
            if(updateOverlay!=null){updateOverlay.SetAsLastSibling();return;}
            MeasureFrame();
            if (titleScreen != null && titleScreen.gameObject.activeSelf)
            {
                if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) StartFromTitle();
                else if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) DismissTitleScreen();
                return;
            }
            if (toastTime > 0) { toastTime -= Time.unscaledDeltaTime; if (toastTime <= 0) toastRoot.gameObject.SetActive(false); }
            if (combat != null) UpdateBattle();
            if(menuLayoutDirty&&combat==null&&modal.childCount==0){menuLayoutDirty=false;Show(page);}
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (demoRewardPending || networkBusy) return;
                if (combat != null) { if (!combat.Ended && !combat.Choosing) PauseBattle(); }
                else if (modal.childCount > 0) CloseModal(); else Show("home");
            }
            UpdateSmoke();
        }
        void OnApplicationPause(bool paused)
        {
            if(!paused && !smokeActive && cloud!=null && Root!=null && Time.unscaledTime-lastReleaseCheck>2)StartCoroutine(CheckAppUpdate(false));
            if (paused && updateOverlay==null && !smokeActive && combat != null && !combat.Ended && !combat.Choosing) PauseBattle();
        }
        public void Toast(string text)
        {
            toastLabel.text = text; toastRoot.gameObject.SetActive(true); toastRoot.SetAsLastSibling(); toastTime = 3.2f;
            if (State.sound && audioSource != null) audioSource.PlayOneShot(clickClip);
        }
        void Change(GameCommand command, Action after = null, bool redraw = true, Action confirmed = null)
        {
            if(UpdateBlocksPlay){ShowUpdatePanel();return;}
            if(command.type!=null&&command.type.StartsWith("demo_",StringComparison.Ordinal)&&!Debug.isDebugBuild&&!Application.isEditor){Toast("개발 빌드 전용 기능이에요.");return;}
            if (networkBusy) { Toast("서버 요청을 처리 중이에요."); return; }
            int previousGems=State.gems;
            Action<CommandResult> complete = result =>
            {
                if(result.Success){Track("command",command.type);if(State.gems!=previousGems)Track("economy","gems",State.gems-previousGems,State.gems);if(command.type=="cosmetic_buy")Track("cosmetic_purchase",command.value);}
                Toast(result.Message);
                if (redraw && combat == null) Show(page); else RefreshWallet();
                after?.Invoke();
                if(result.Success)confirmed?.Invoke();
            };
            if (!Online) { complete(Store.Change(e => CommandDispatcher.Apply(e, command, true))); return; }
            StartCoroutine(SubmitCloud(command, reply => complete(new CommandResult(reply.success, reply.message))));
        }
        public void Show(string target)
        {
            FitScreen();menuLayoutDirty=false;
            Track("screen",target);
            UpdateBattleMusic();
            page = target; if (!Online) Store.Economy.RefreshDay(); Ui.Clear(screen); CloseModal();
            Ui.Label(screen, "SCRAP GUARD", 24, 20, 300, 25, 14, Ui.Mint);
            Ui.Label(screen, target == "home" ? "고철 수호대" : target == "gear" ? "장비 정비" : target == "heroes" ? "수호대원" : target == "weekly" ? "주간 로봇 도전 · 반복 보상" : target == "showcase" ? "나의 전시실" : "보급 상점", 24, 50, 396, 40, 30);
            Ui.Button(screen, "설정", 432, 34, 84, 60, Settings, Ui.Raised, true, 16);
            for(int i=0;i<3;i++)walletAmounts[i]=CurrencyArt.Balance(screen,i,24+i*166,104,160);
            RefreshWallet();
            switch (target) { case "gear": Gear(); break; case "heroes": Heroes(); break; case "shop": Shop(); break; case "showcase": Showcase(); break; case "weekly": WeeklyHub(); break; default: Home(); break; }
            string[] names = { "상점", "장비", "출격", "대원", "전시실" }, pages = { "shop", "gear", "home", "heroes", "showcase" };
            Ui.Box(screen, 16, MenuHeight-98, 508, 80, Ui.Panel);
            for (int i = 0; i < 5; i++)
            {
                string p = pages[i]; bool active = p == page;
                Ui.Button(screen, names[i], 24 + i * 100, MenuHeight-94, 92, 72, () => Show(p), active ? Ui.Mint : Ui.Panel, true, 19);
            }
            Ui.Label(screen, Online ? "서버에 저장됨" : "로컬 체험 · 기기에 저장됨", 0, MenuHeight-20, 540, 20, 12, Ui.Muted, TextAnchor.MiddleCenter);
        }
        void RefreshWallet()
        {
            int[] amounts={State.gold,State.gems,State.parts};
            for(int i=0;i<3;i++)if(walletAmounts[i]!=null)
            {
                walletAmounts[i].text=amounts[i].ToString("N0");
                walletAmounts[i].resizeTextForBestFit=true;walletAmounts[i].resizeTextMinSize=14;walletAmounts[i].resizeTextMaxSize=21;
            }
        }
        void Home()
        {
            float height=MenuBottom-358;
            var room=Ui.Box(screen,24,178,492,height,Ui.Panel).rectTransform;
            float displayHeight=Mathf.Min(520,height-40),displayY=(height-displayHeight)/2;
            WorkshopStage.Draw(room,0,displayY,492,displayHeight,Ui.Mint);
            var art=CharacterArt.Draw(room,81,displayY+42,330,displayHeight-90,State.selectedHero,State.selectedSkin,Outfits.Active(State,State.selectedHero));
            art.rectTransform.pivot=new Vector2(.5f,0);art.rectTransform.anchoredPosition=new Vector2(246,-displayY-displayHeight+58);
            art.gameObject.AddComponent<ShowroomMotion>();
            Ui.Label(room,"부수고 · 회수하고 · 합체하라",18,12,456,28,19,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Label(room,Catalog.HeroNames[State.selectedHero]+"  Lv."+State.heroes[State.selectedHero].level,18,height-51,456,35,25,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Label(screen,(selectedStage+1).ToString("D2")+" · "+Catalog.StageNames[selectedStage],24,MenuBottom-168,492,32,23,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Button(screen,State.clearedStages.Count==Campaign.Count?"주간 로봇 도전 →":"출격하기  →",24,MenuBottom-124,310,78,()=>{if(State.clearedStages.Count==Campaign.Count)Show("weekly");else CampaignLaunch();},Ui.Mint,true,24);
            Ui.Button(screen,Growth.CanUpgrade(State)?"강화 가능 ●":"성장하기",348,MenuBottom-124,168,78,GrowthPanel,Growth.CanUpgrade(State)?Ui.Gold:Ui.Raised,true,21);
            Ui.Button(screen,"작전 지도 · 출석 · 더 보기",24,MenuBottom-36,492,35,HomeActivities,Ui.Panel,true,17);
        }
        void HomeActivities()
        {
            var card=Dialog("정비소 활동","출격과 성장은 홈에서 바로, 다른 활동은 여기서 확인해요.",830);
            string[] labels={"작전 지도","출석 · "+(Store.Economy.CanAttend?"보상 받기":"수령 완료"),"오늘의 임무 · "+Math.Min(3,State.dailyRuns)+"/3","성장 · 수집 목표","초보 안내","주간 로봇 도전 · 반복 보상","길드 · 지원 대원"};
            Action[] actions={()=>CampaignMap(),Attendance,Missions,ProgressGoals,GuidePanel,()=>Show("weekly"),OpenGuild};
            for(int i=0;i<labels.Length;i++)Ui.Button(card,labels[i],24,135+i*91,444,70,actions[i],Ui.Raised,true,21);
        }
        RectTransform Dialog(string title, string subtitle, float height = 500)
        {
            CloseModal();
            var shade = Ui.Box(modal, 0, -menuExtra/2, 540, MenuHeight, new Color(0, 0, 0, .78f), false); shade.raycastTarget = true;
            var card = Ui.Box(modal, 24, (960 - height) / 2, 492, height, Ui.Panel).rectTransform;
            Ui.Label(card, title, 24, 21, 340, 43, 25);
            Ui.Label(card, subtitle, 24, 70, 444, 54, 15, Ui.Muted);
            Ui.Button(card, "닫기", 390, 10, 78, 60, () => { if (combat != null && combat.Ended) ShowResult(); else CloseModal(); }, Ui.Raised, true, 17);
            return card;
        }
        void CloseModal() { if (!demoRewardPending) Ui.Clear(modal); }
        void Attendance()
        {
            var card = Dialog("매일 쌓이는 보급품", "누적 출석 · 결석해도 이어서 받을 수 있어요\n매일 오전 5시 초기화", 604);
            string[] rewards = { "골드\n1,000", "부품\n20", "보석\n50", "골드\n2,000", "부품\n40", "보석\n100", State.attendanceCount < 7 ? "볼트\n합류" : "부품\n60" };
            int progress = State.attendanceCount % 7;
            if (!Store.Economy.CanAttend && progress == 0 && State.attendanceCount > 0) progress = 7;
            for (int i = 0; i < 7; i++)
            {
                float x = 24 + i % 4 * 113, y = 147 + i / 4 * 143;
                bool done = i < progress, current = i == progress && Store.Economy.CanAttend;
                Ui.Box(card, x, y, 105, 129, current ? Ui.Mint : Ui.Raised);
                Ui.Label(card, (i + 1) + "일차", x + 4, y + 8, 97, 28, 14, current ? Ui.Background : Ui.Muted, TextAnchor.MiddleCenter);
                Ui.Label(card, done ? "수령 완료" : rewards[i], x + 4, y + 38, 97, 75, 19, current ? Ui.Background : done ? Ui.Muted : Ui.Ink, TextAnchor.MiddleCenter);
            }
            Ui.Button(card, Store.Economy.CanAttend ? "오늘의 보상 받기" : "오늘 출석 완료", 24, 478, 444, 61, () => Change(new GameCommand("attendance"), Attendance), null, Store.Economy.CanAttend);
            Ui.Label(card, "현재까지 " + State.attendanceCount + "일 출석", 24, 549, 444, 30, 14, Ui.Muted, TextAnchor.MiddleCenter);
        }
        void Missions()
        {
            var card = Dialog("오늘의 정비 일지", "전투 3회와 적 500마리를 처치하세요.\n30초 이상 진행한 전투가 횟수에 포함돼요.", 490);
            Ui.Label(card, "전투 완료     " + Math.Min(3, State.dailyRuns) + " / 3", 24, 155, 444, 45, 22);
            Ui.Label(card, "로봇 처치     " + Math.Min(500, State.dailyKills) + " / 500", 24, 216, 444, 45, 22);
            Ui.Label(card, "골드 2,000  ·  부품 20  ·  보석 30", 24, 292, 444, 42, 17, Ui.Gold);
            Ui.Button(card, State.missionClaimed ? "수령 완료" : "모두 완료하고 보상 받기", 24, 380, 444, 62, () => Change(new GameCommand("missions"), Missions), null, !State.missionClaimed && State.dailyRuns >= 3 && State.dailyKills >= 500);
        }
        void Settings()
        {
            var card = Dialog("설정", Online ? "진행 상황은 연결한 서버에 저장됩니다." : "진행 상황은 이 기기에 저장됩니다.", 880);
            Ui.Button(card, "소리 · 음악 · 진동", 24, 134, 444, 53, AudioOptions, Ui.Raised);
            Ui.Button(card,"Play 상점 · 구매 복원",24,202,444,53,ConnectPlayBilling,Ui.Raised,true,17);
            Ui.Button(card, "계정 · 서버 연결", 24, 270, 444, 53, AccountPanel, Ui.Raised);
            Ui.Button(card, "테스트 도구", 24, 337, 444, 53, DemoTools, Ui.Raised, !Online && (Debug.isDebugBuild || Application.isEditor));
            Ui.Label(card, "이동: 화면 드래그 또는 WASD / 방향키\n공격은 자동 · 레벨업하면 스킬 선택\n일시정지: 전투 상단 버튼 또는 ESC", 24, 407, 444, 98, 16, Ui.Muted);
            Ui.Button(card,"전투 화면 · 효과 설정",24,526,444,48,BattleDisplayOptions,Ui.Raised,true,17);
            Ui.Button(card,"앱 "+AppRelease.Version+" · 업데이트 확인",24,596,444,48,()=>StartCoroutine(CheckAppUpdate(true)),Ui.Raised,true,17);
            Ui.Button(card,"광고 보상 · 개인정보 · 고객지원",24,660,444,48,ServicePanel,Ui.Raised,true,16);
            Ui.Button(card,"플레이 테스트 · 기록 설정",24,724,444,60,ResearchPanel,Ui.Raised,true,19);
            Ui.Label(card,"고철 수호대 · 회수하고 개조하는 생존 게임",24,812,444,25,14,Ui.Muted);
        }
        void Info(string title, string text)
        {
            var card = Dialog(title, "", 340); Ui.Label(card, text, 24, 115, 444, 120, 18);
            Ui.Button(card, "확인", 24, 260, 444, 55, CloseModal);
        }
        void DemoTools()
        {
            var card = Dialog("프로토타입 테스트", "현금 결제와 광고 송출은 발생하지 않습니다.\n테스트 재화와 권리는 이 기기에만 적용돼요.", 510);
            Ui.Button(card, "테스트 보석 1,200 지급", 24, 147, 444, 56, () => Change(new GameCommand("demo_gems"), DemoTools), Ui.Raised);
            Ui.Button(card, "테스트 골드 10,000 · 부품 100", 24, 217, 444, 56, () => Change(new GameCommand("demo_materials"), DemoTools), Ui.Raised);
            Ui.Button(card, State.demoAdSkip ? "광고 스킵 체험 끄기" : "광고 스킵 체험 켜기", 24, 287, 444, 56, () => Change(new GameCommand("demo_skip") { enabled = !State.demoAdSkip }, DemoTools), Ui.Raised);
            Ui.Button(card, "45초 빠른 전투 테스트", 24, 372, 444, 60, () => StartBattle(0, true));
            Ui.Label(card, "정식 출시에서는 테스트 도구를 제거합니다.", 24, 445, 444, 35, 13, Ui.Muted, TextAnchor.MiddleCenter);
        }
        void RequestReward(string placement)
        {
            if (demoRewardPending || networkBusy) return;
            if(Online){StartCoroutine(RequestOnlineReward(placement));return;}
            string claim = Guid.NewGuid().ToString("N");
            if (State.demoAdSkip)
            {
                Change(new GameCommand("demo_reward", claim) { value = placement }, redraw: combat == null);
                if (combat != null && combat.Ended) ShowResult(); return;
            }
            var card = Dialog("보상 광고 체험", "실제 광고가 아닌 3초 테스트 화면입니다.\n완료 버튼을 누르면 보상이 지급됩니다.", 360);
            var status = Ui.Label(card, "3", 24, 140, 444, 68, 38, Ui.Mint, TextAnchor.MiddleCenter);
            var done = Ui.Button(card, "체험 완료 후 받기", 24, 253, 444, 63, () =>
            {
                Change(new GameCommand("demo_reward", claim) { value = placement }, redraw: combat == null);
                if (combat != null && combat.Ended) ShowResult(); else CloseModal();
            }, null, false);
            StartCoroutine(RewardCountdown(status, done));
        }
        IEnumerator RewardCountdown(Text status, Button button)
        {
            for (int i = 3; i > 0; i--) { if (status == null) yield break; status.text = i.ToString(); yield return new WaitForSecondsRealtime(1); }
            if (status != null && button != null) { status.text = "체험 완료"; button.interactable = true; }
        }
    }
}
