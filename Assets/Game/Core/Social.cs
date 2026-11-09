using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ScrapGuard.Core
{
    [Serializable] public class ShowcaseItem
    {
        public int definition, rarity, level;
    }
    [Serializable] public class PublicProfile
    {
        public string id, name;
        public int hero, heroLevel, title, frame, skin, outfit, attack, health, kills, stages, attendance, likes;
        public int theme, robotDesign;
        public int coreBest, trophy = -1;
        public bool local, test, liked;
        public List<ShowcaseItem> items = new List<ShowcaseItem>();
    }
    [Serializable] public class ProfileList { public List<PublicProfile> profiles = new List<PublicProfile>(); public string next = "", featuredId = ""; }
    [Serializable] public class BlockList { public List<string> ids = new List<string>(); }
    public static class Social
    {
        public static readonly string[] Titles = { "새내기 정비사", "첫 구역의 수호자", "꾸준한 발걸음", "천 대의 증인", "코어 정복자", "균열의 개척자" };
        public static readonly string[] TitleGoals = { "처음부터 사용 가능", "구역 1개 클리어", "누적 출석 7일", "적 1,000마리 처치", "작전 5개 클리어", "코어 파편 12개 교환" };
        public static readonly string[] Frames = { "작업대", "첫 승리", "푸른 궤적", "태양의 테두리", "코어 궤도" };
        public static readonly string[] Skins = { "기본 도색", "앰버 에디션", "서리 도색", "정비소의 동빛", "철로의 황혼", "정화의 녹빛", "극저온 은빛", "코어의 자주빛", "번개의 황금빛", "용광로의 홍옥빛" };
        public static void Normalize(GameState s)
        {
            HeroCatalog.Normalize(s); Campaign.Normalize(s); Endgame.Normalize(s); Workshop.Normalize(s);
            if (string.IsNullOrWhiteSpace(s.displayName)) s.displayName = "새내기 정비사";
            if (s.ownedCosmetics == null) s.ownedCosmetics = new List<string>();
            if (s.showcaseItems == null) s.showcaseItems = new List<string>();
            if (s.showcaseHero < 0 || s.showcaseHero >= s.heroes.Count || !s.heroes[s.showcaseHero].unlocked) s.showcaseHero = s.selectedHero;
            if (!HasTitle(s, s.showcaseTitle)) s.showcaseTitle = 0;
            if (!HasFrame(s, s.showcaseFrame)) s.showcaseFrame = 0;
            if (!HasSkin(s, s.selectedSkin)) s.selectedSkin = 0;
            Outfits.Normalize(s);
            if(!ShowcaseThemes.Owned(s,s.showcaseTheme))s.showcaseTheme=0;
            if(s.collectionTarget < -1 || s.collectionTarget>=Campaign.Regions.Length)s.collectionTarget=-1;
            StarterGuide.Normalize(s);
            for (int i = 0; i < 3; i++)
            {
                if (s.showcaseItems.Count <= i) s.showcaseItems.Add(s.equipped[i]);
                var item = s.items.Find(x => x.id == s.showcaseItems[i]);
                if (item == null || Catalog.Items[item.definition].Slot != i) s.showcaseItems[i] = s.equipped[i];
            }
            if (s.showcaseItems.Count > 3) s.showcaseItems.RemoveRange(3, s.showcaseItems.Count - 3);
        }
        public static bool HasTitle(GameState s, int id)
        {
            switch (id) { case 0: return true; case 1: return s.clearedStages.Count > 0; case 2: return s.attendanceCount >= 7; case 3: return s.totalKills >= 1000; case 4: return s.clearedStages.Count >= 5; case 5: return s.ownedCosmetics!=null && s.ownedCosmetics.Contains("title_rift"); default: return false; }
        }
        public static bool HasFrame(GameState s, int id)
        {
            return id == 0 || id == 1 && s.clearedStages.Count > 0 || id == 2 && s.attendanceCount >= 7 || id == 3 && s.ownedCosmetics != null && s.ownedCosmetics.Contains("frame_solar") || id == 4 && s.ownedCosmetics!=null && s.ownedCosmetics.Contains("frame_core");
        }
        public static bool HasSkin(GameState s, int id)
        {
            return id == 0 || id == 1 && s.ownedCosmetics != null && s.ownedCosmetics.Contains("skin_amber") || id == 2 && s.totalKills >= 1000 || id>=3 && id<Skins.Length && s.ownedCosmetics!=null && s.ownedCosmetics.Contains(Workshop.SkinKey(id-3));
        }
        public static CommandResult Rename(GameState s, string name)
        {
            name = Regex.Replace((name ?? "").Normalize().Trim(), " +", " ");
            if (!Regex.IsMatch(name, @"^[가-힣a-zA-Z0-9 _-]{2,12}$")) return CommandResult.Fail("닉네임은 한글·영문·숫자 2~12자로 입력해 주세요.");
            s.displayName = name; return CommandResult.Ok("닉네임을 변경했어요.");
        }
        public static CommandResult Customize(GameState s, int hero, int title, int frame, int skin)
        {
            Normalize(s);
            if (hero < 0 || hero >= s.heroes.Count || !s.heroes[hero].unlocked || !HasTitle(s, title) || !HasFrame(s, frame) || !HasSkin(s, skin))
                return CommandResult.Fail("아직 획득하지 않은 대원 또는 장식이에요.");
            s.showcaseHero = hero; s.showcaseTitle = title; s.showcaseFrame = frame; s.selectedSkin = skin;
            return CommandResult.Ok("나만의 전시 프로필을 저장했어요.");
        }
        public static CommandResult Buy(GameState s, string id)
        {
            Normalize(s);
            int outfit=Outfits.Find(id);
            int price = ShowcaseThemes.Find(id)>0?ShowcaseThemes.Price:id == "frame_solar" ? 250 : id == "skin_amber" ? 300 : outfit>0 ? Outfits.Price : -1;
            if (price < 0) return CommandResult.Fail("등록되지 않은 꾸미기 상품이에요.");
            if(outfit>0 && !s.heroes[Outfits.Hero(outfit)].unlocked)return CommandResult.Fail(Outfits.HeroName(outfit)+"를 먼저 모집해 주세요. 의상은 대원을 포함하지 않아요.");
            if (s.ownedCosmetics.Contains(id)) return CommandResult.Fail("이미 가지고 있는 꾸미기 상품이에요.");
            if (s.gems < price) return CommandResult.Fail("보석 " + price + "개가 필요해요.");
            if(!PremiumWallet.Spend(s,price))return CommandResult.Fail("보석 잔액 또는 환불 조정 내역을 확인해 주세요."); s.ownedCosmetics.Add(id);if(outfit>0 && s.outfitTarget==outfit)s.outfitTarget=0;
            return CommandResult.Ok("꾸미기 획득! 전시실에서 적용해 보세요.");
        }
        public static PublicProfile Build(GameState s, string id = "", bool local = true)
        {
            Normalize(s);RobotAssembly.Normalize(s);
            var p = new PublicProfile { id = id, name = s.displayName, hero = s.showcaseHero, heroLevel = s.heroes[s.showcaseHero].level,
                robotDesign=s.showcaseRobot,theme=s.showcaseTheme, title = s.showcaseTitle, frame = s.showcaseFrame, skin = s.selectedSkin, outfit = Outfits.Active(s,s.showcaseHero), kills = s.totalKills, stages = s.clearedStages.Count,
                attendance = s.attendanceCount, local = local, test = s.testProgress };
            for (int slot = 0; slot < 3; slot++)
            {
                var item = s.items.Find(x => x.id == s.showcaseItems[slot]);
                p.items.Add(new ShowcaseItem { definition = item.definition, rarity = item.rarity, level = item.level });
            }
            p.attack=Catalog.AttackFor(p.hero,p.heroLevel,s.items.Find(i=>i.id==s.showcaseItems[0]));
            p.health=Catalog.HealthFor(p.hero,p.heroLevel,s.items.Find(i=>i.id==s.showcaseItems[1]));
            p.trophy=Endgame.Trophy(s);foreach(var week in s.weekly)p.coreBest=Math.Max(p.coreBest,week.bestScore);
            return p;
        }
    }
}
