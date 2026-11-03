namespace ScrapGuard.Core
{
    public static class Outfits
    {
        public const string Key = "outfit_heavy_engineer", Name = "중장비 정비사";
        public const int Price = 600;
        public static bool Owned(GameState s) { return s.ownedCosmetics != null && s.ownedCosmetics.Contains(Key); }
        public static int Active(GameState s, int hero) { return hero == 0 && s.selectedOutfit == 1 && Owned(s) ? 1 : 0; }
        public static CommandResult Equip(GameState s, int id)
        {
            if (id < 0 || id > 1 || id == 1 && !Owned(s)) return CommandResult.Fail("보유한 의상만 장착할 수 있어요.");
            s.selectedOutfit = id;
            return CommandResult.Ok(id == 0 ? "의상을 해제하고 기존 도색을 표시해요." : "루크 전용 의상 장착 · 전투 능력치는 같아요.");
        }
    }
    public static class StarterGuide
    {
        public static readonly string[] Names = { "이동하며 생존하기", "필살기 사용하기", "첫 장비 강화", "첫 개조 완성", "첫 지역 정복" };
        public static readonly string[] Details = { "전투 화면을 드래그해 이동하세요. 공격은 자동이에요.", "적을 처치해 충전하고 필살기 버튼을 누르세요.", "장비에서 무기를 골라 골드와 부품으로 강화하세요.", "전투 중 리벳 탄막 Lv.3 + 신속 장전 Lv.2를 모아보세요.", "작전 6의 지역 보스를 처치하고 다음 지역으로 가세요." };
        public static bool Done(GameState s, int step)
        {
            switch(step) { case 0:return s.guideMoved; case 1:return s.guideUltimate; case 2:return s.guideUpgrade; case 3:return s.guideMod; case 4:return s.clearedStages.Contains(5); default:return true; }
        }
        public static int Next(GameState s) { for(int i=0;i<Names.Length;i++)if(!Done(s,i))return i;return Names.Length; }
        public static void Normalize(GameState s)
        {
            if(s.guideVersion>0)return;
            s.guideVersion=1;
            // Older experienced accounts keep their progress and are not forced through an introduction.
            s.guideIntroSeen=s.totalRuns>0 || s.tutorialCompleted;
            s.guideUpgrade=s.items.Exists(i=>i.level>1);
            s.guideMod=s.receipts.Exists(r=>r.report!=null && r.report.mods!=0);
        }
        public static void Record(GameState s, Combat combat)
        {
            s.guideMoved |= combat.DistanceMoved >= 120;
            s.guideUltimate |= combat.UltimateUses > 0;
            s.guideMod |= combat.Mods != 0;
        }
    }
}
