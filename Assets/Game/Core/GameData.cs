using System;
using System.Collections.Generic;

namespace ScrapGuard.Core
{
    [Serializable] public class Item
    {
        public string id;
        public int definition;
        public int rarity;
        public int level = 1;
        public bool locked;
    }
    [Serializable] public class HeroProgress { public int id; public int level = 1; public bool unlocked; }
    [Serializable] public class Receipt { public string id; public string kind; public int gold; public int parts; public int gems;
        public int paidGems,gemDebt;
        public bool adSkip; public bool won; public int kills; public float seconds; public int score; public string week; public int blueprints; public BattleReport report; }
    [Serializable] public class GameState
    {
        public int version = 1;
        public int gold = 1800;
        public int parts = 30;
        public int gems;
        public int paidGems,gemDebt;
        public bool adSkip;
        public int coreShards;
        public int showcaseRobot;
        public List<int> robotStyles=new List<int>();
        public List<RobotRecord> robotRecords=new List<RobotRecord>();
        public int collectionVersion, enemyDiscoveries, bossDiscoveries;
        public List<RegionProgress> regions = new List<RegionProgress>();
        public int selectedHero;
        public int unlockedStage;
        public int attendanceCount;
        public string attendanceDate = "";
        public string serviceDate = "";
        public int dailyRuns;
        public int dailyKills;
        public bool missionClaimed;
        public int dailyItemPurchases;
        public int dailyPartsPurchases;
        public int dailyGemPartsPurchases;
        public int dailySalvageWins; public bool salvageClaimed;
        public int dailySupplySeconds;
        public int dailyGrowthKits, growthPassFree, growthPassPremium;
        public int dailyRewardGold;
        public int dailyRewardParts;
        public bool demoAdSkip;
        public bool sound = true;
        public bool tutorialCompleted;
        public string activeMode = "", activeWeek = "";
        public int dailySupportStarts;
        public List<WeeklyProgress> weekly = new List<WeeklyProgress>();
        public List<WeeklyProgress> skillWeekly = new List<WeeklyProgress>();
        public string activeRun = "", activeGuild = "";
        public int activeStage;
        public string lastResultId = "";
        public int lastResultGold;
        public string lastResultDate = "";
        public bool lastResultBonus;
        public int totalRuns;
        public int totalKills;
        public string displayName = "새내기 정비사";
        public int showcaseHero;
        public int showcaseTitle;
        public int showcaseTheme;
        public int showcaseFrame;
        public int selectedSkin, selectedOutfit;
        public List<int> heroOutfits;
        public int outfitTarget;
        public int collectionTarget=-1;
        public int guideVersion;
        public bool guideIntroSeen, guideMoved, guideUltimate, guideUpgrade, guideMod;
        public bool publicProfile;
        public bool testProgress;
        public List<string> showcaseItems = new List<string>();
        public List<string> ownedCosmetics = new List<string>();
        public List<int> clearedStages = new List<int>();
        public List<int> purchasedRare = new List<int>();
        public List<Item> items = new List<Item>();
        public List<string> equipped = new List<string>();
        public List<HeroProgress> heroes = new List<HeroProgress>();
        public List<Receipt> receipts = new List<Receipt>();
        public static GameState New()
        {
            var state = new GameState { guideVersion = 1 };
            for (int i = 0; i < 3; i++)
            {
                var item = new Item { id = Guid.NewGuid().ToString("N"), definition = i == 0 ? 0 : i == 1 ? 6 : 9 };
                state.items.Add(item); state.equipped.Add(item.id);

            }
            for (int i = 0; i < HeroCatalog.Count; i++) state.heroes.Add(new HeroProgress { id = i, unlocked = i == 0 });
            return state;
        }
    }
    public class ItemDefinition
    {
        public int Id, Slot;
        public string Name, Description, Icon;
        public ItemDefinition(int id, int slot, string name, string description, string icon)
        { Id = id; Slot = slot; Name = name; Description = description; Icon = icon; }
    }
    public static class Catalog
    {
        public static readonly string[] HeroNames = Array.ConvertAll(HeroCatalog.All, h => h.Name);
        public static readonly string[] HeroRoles = Array.ConvertAll(HeroCatalog.All, h => h.Passive);
        public static readonly string[] StageNames = Array.ConvertAll(Campaign.Stages, s => s.Name);
        public static readonly string[] StageDescriptions = Array.ConvertAll(Campaign.Stages, s => s.Description);
        public static readonly string[] RarityNames = { "일반", "고급", "희귀" };
        public static readonly string[] SkillNames = { "리벳 탄막", "회전 톱날", "충격 코일", "지원 드론", "고밀도 탄환", "신속 장전", "응급 수리", "자력 수집", "곡사 박격포", "십자 절단기", "지뢰 살포기", "회수 엔진" };
        public static readonly string[] SkillDescriptions = { "탄환 추가 · 정면을 꿰뚫는 화력", "주위를 도는 톱날로 근접 적 공격", "주기적인 전기 충격으로 주변 정리", "드론이 추적탄으로 적을 공격", "모든 공격 피해 20% 증가", "기본 공격 간격 12% 감소", "최대 체력 15% 증가 · 체력 회복", "경험치 수집 반경 증가", "가장 가까운 적에게 포격 · 폭발 범위 성장", "네 방향 관통 칼날 · 근접 포위 돌파", "지나간 자리에 지뢰 설치 · 추격하는 적 유인", "이동 속도 증가 · 고철 회수량 2배" };
        public static readonly ItemDefinition[] Items = {
            new ItemDefinition(0, 0, "리벳건", "빠르고 정확한 단일 사격", "R"),
            new ItemDefinition(1, 0, "회전 절단기", "가까운 적을 향한 산탄 사격", "S"),
            new ItemDefinition(2, 0, "충격 발사기", "관통하는 고전압 탄환", "E"),
            new ItemDefinition(3, 0, "화염 분사기", "짧은 거리의 빠른 연속 사격", "F"),
            new ItemDefinition(4, 0, "드론 런처", "먼 거리에서 강력한 탄환 발사", "D"),
            new ItemDefinition(5, 0, "자력 폭탄", "느리지만 강력한 폭발 공격", "B"),
            new ItemDefinition(6, 1, "작업 조끼", "기본 체력 증가", "V"),
            new ItemDefinition(7, 1, "강철 외골격", "체력과 접촉 피해 저항", "A"),
            new ItemDefinition(8, 1, "경량 보호복", "체력과 이동속도 증가", "L"),
            new ItemDefinition(9, 2, "수집 모듈", "경험치 수집 반경 증가", "M"),
            new ItemDefinition(10, 2, "드론 모듈", "지원 드론 공격력 증가", "D"),
            new ItemDefinition(11, 2, "조준 모듈", "치명타 확률 증가", "C")
        };
        public static int Attack(GameState s,bool fixedChallenge=false)
        {
            var weapon = s.items.Find(i => i.id == s.equipped[0]);
            return AttackFor(s.selectedHero,s.heroes[s.selectedHero].level,weapon,fixedChallenge);
        }
        public static int Health(GameState s,bool fixedChallenge=false)
        {
            var armor = s.items.Find(i => i.id == s.equipped[1]);
            return HealthFor(s.selectedHero,s.heroes[s.selectedHero].level,armor,fixedChallenge);
        }
        public static int AttackFor(int hero,int level,Item weapon,bool fixedChallenge=false)
        { return (int)(HeroCatalog.All[hero].Attack*(100+3*(level-1))/100f*(fixedChallenge||weapon==null?1:1+.065f*(weapon.level-1))+(weapon==null?0:30*Multiplier(weapon,fixedChallenge))); }
        public static int HealthFor(int hero,int level,Item armor,bool fixedChallenge=false)
        { return (int)(HeroCatalog.All[hero].Health*(100+3*(level-1))/100f*(fixedChallenge||armor==null?1:1+.045f*(armor.level-1))+(armor==null?0:180*Multiplier(armor,fixedChallenge))); }
        public static float Multiplier(Item item,bool fixedChallenge=false) { return new[] { 1f, 1.15f, 1.35f }[item.rarity] * (1 + (fixedChallenge?.05f:.12f) * (item.level - 1)); }
        public static string ServiceDate(DateTimeOffset utc) { return utc.ToOffset(TimeSpan.FromHours(9)).AddHours(-5).ToString("yyyy-MM-dd"); }
    }
    public struct CommandResult
    {
        public bool Success;
        public string Message;
        public CommandResult(bool success, string message) { Success = success; Message = message; }
        public static CommandResult Ok(string text) { return new CommandResult(true, text); }
        public static CommandResult Fail(string text) { return new CommandResult(false, text); }
    }
}
