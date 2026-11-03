using System;

namespace ScrapGuard.Core
{
    // Shared command rules. Live balances are owned by the server repository.
    public sealed partial class Economy
    {
        public GameState State { get; private set; }
        readonly Func<DateTimeOffset> clock;
        public Economy(GameState state, Func<DateTimeOffset> clock, bool refresh = true) { State = state; HeroCatalog.Normalize(state); Campaign.Normalize(state); Endgame.Normalize(state); Workshop.Normalize(state); StarterGuide.Normalize(state); this.clock = clock; if (refresh) RefreshDay(); }
        public void RefreshDay()
        {
            string today = Catalog.ServiceDate(clock());
            // A backwards clock must not reopen an already used daily allowance.
            if (String.CompareOrdinal(today, State.serviceDate) <= 0) return;
            State.serviceDate = today;
            State.dailyRuns = State.dailyKills = State.dailyItemPurchases = State.dailyPartsPurchases = State.dailyGemPartsPurchases = 0;
            State.dailySupportStarts = 0;
            State.dailyRewardGold = State.dailyRewardParts = 0; State.missionClaimed = false;
        }
        public bool CanAttend { get { return String.CompareOrdinal(State.serviceDate, State.attendanceDate) > 0; } }
        public CommandResult Attend()
        {
            RefreshDay();
            if (!CanAttend) return CommandResult.Fail("오늘의 출석 보상을 이미 받았어요.");
            int day = State.attendanceCount % 7;
            string message;
            switch (day)
            {
                case 0: State.gold += 1000; message = "골드 1,000 획득"; break;
                case 1: State.parts += 20; message = "부품 20개 획득"; break;
                case 2: State.gems += 50; message = "보석 50개 획득"; break;
                case 3: State.gold += 2000; message = "골드 2,000 획득"; break;
                case 4: State.parts += 40; message = "부품 40개 획득"; break;
                case 5: State.gems += 100; message = "보석 100개 획득"; break;
                default:
                    if (!State.heroes[2].unlocked) { State.heroes[2].unlocked = true; message = "수호자 볼트 합류!"; }
                    else { State.parts += 60; message = "부품 60개 획득"; } break;
            }
            State.attendanceCount++; State.attendanceDate = State.serviceDate;
            return CommandResult.Ok(message);
        }
        public CommandResult ClaimMissions()
        {
            RefreshDay();
            if (State.missionClaimed) return CommandResult.Fail("오늘의 임무 보상을 이미 받았어요.");
            if (State.dailyRuns < 3 || State.dailyKills < 500) return CommandResult.Fail("전투 3회와 적 500마리 처치를 완료해 주세요.");
            State.gold += 2000; State.parts += 20; State.gems += 30; State.missionClaimed = true;
            return CommandResult.Ok("골드 2,000 · 부품 20 · 보석 30 획득");
        }
        public CommandResult Buy(string offer)
        {
            RefreshDay();
            if (offer == "hero")
            {
                if (State.heroes[1].unlocked) return CommandResult.Fail("이미 합류한 캐릭터예요.");
                if (State.gems < 1200) return CommandResult.Fail("보석 1,200개가 필요해요.");
                State.gems -= 1200; State.heroes[1].unlocked = true; return CommandResult.Ok("돌격수 레이 합류!");
            }
            if (offer == "parts_gold")
            {
                if (State.dailyPartsPurchases >= 1) return CommandResult.Fail("오늘 구매 한도에 도달했어요.");
                if (State.gold < 2000) return CommandResult.Fail("골드 2,000이 필요해요.");
                State.gold -= 2000; State.parts += 20; State.dailyPartsPurchases++; return CommandResult.Ok("부품 20개 획득");
            }
            if (offer == "parts_gems")
            {
                if (State.dailyGemPartsPurchases >= 1) return CommandResult.Fail("오늘 구매 한도에 도달했어요.");
                if (State.gems < 100) return CommandResult.Fail("보석 100개가 필요해요.");
                State.gems -= 100; State.parts += 100; State.dailyGemPartsPurchases++; return CommandResult.Ok("부품 100개 획득");
            }
            string[] split = (offer ?? "").Split(':');
            int definition;
            if (split.Length != 2 || !int.TryParse(split[1], out definition) || definition < 0 || definition >= Catalog.Items.Length)
                return CommandResult.Fail("등록되지 않은 상품이에요.");
            if (State.items.Count >= 80) return CommandResult.Fail("장비 보관함이 가득 찼어요. 장비를 정리해 주세요.");
            if (split[0] == "normal")
            {
                if (State.dailyItemPurchases >= 2) return CommandResult.Fail("일반 장비는 하루 2개까지 구매할 수 있어요.");
                if (State.gold < 3000) return CommandResult.Fail("골드 3,000이 필요해요.");
                State.gold -= 3000; State.dailyItemPurchases++; AddItem(definition, 0);
            }
            else if (split[0] == "rare")
            {
                if (State.purchasedRare.Contains(definition) || State.items.Exists(i => i.definition == definition && i.rarity == 2))
                    return CommandResult.Fail("이미 보유했거나 구매한 희귀 장비예요.");
                if (State.gems < 600) return CommandResult.Fail("보석 600개가 필요해요.");
                State.gems -= 600; State.purchasedRare.Add(definition); AddItem(definition, 2);
            }
            else return CommandResult.Fail("등록되지 않은 상품이에요.");
            return CommandResult.Ok(Catalog.Items[definition].Name + " 획득");
        }
        public Item AddItem(int definition, int rarity)
        {
            var item = new Item { id = Guid.NewGuid().ToString("N"), definition = definition, rarity = rarity };
            State.items.Add(item); return item;
        }
        public CommandResult Upgrade(string id)
        {
            var item = State.items.Find(i => i.id == id);
            if (item == null) return CommandResult.Fail("장비를 찾을 수 없어요.");
            if (item.level >= 20) return CommandResult.Fail("최대 강화 단계예요.");
            int gold = 300 * item.level, parts = 2 * item.level;
            if (State.gold < gold || State.parts < parts) return CommandResult.Fail("강화에 필요한 골드 또는 부품이 부족해요.");
            State.gold -= gold; State.parts -= parts; item.level++; State.guideUpgrade=true;
            return CommandResult.Ok("강화 성공 · Lv." + item.level);
        }
        public CommandResult UpgradeHero()
        {
            var hero = State.heroes[State.selectedHero];
            if (hero.level >= 30) return CommandResult.Fail("최대 레벨이에요.");
            if (State.gold < 500 * hero.level || State.parts < 3 * hero.level) return CommandResult.Fail("레벨업에 필요한 재화가 부족해요.");
            State.gold -= 500 * hero.level; State.parts -= 3 * hero.level; hero.level++;
            return CommandResult.Ok("캐릭터 Lv." + hero.level);
        }
        public CommandResult SelectHero(int id)
        {
            if (id < 0 || id >= HeroCatalog.Count || !State.heroes[id].unlocked) return CommandResult.Fail("먼저 캐릭터를 해금해 주세요.");
            State.selectedHero = id; return CommandResult.Ok(Catalog.HeroNames[id] + " 출격 준비");
        }
        public CommandResult Equip(string id)
        {
            var item = State.items.Find(i => i.id == id);
            if (item == null) return CommandResult.Fail("장비를 찾을 수 없어요.");
            State.equipped[Catalog.Items[item.definition].Slot] = id; return CommandResult.Ok("장비를 장착했어요.");
        }
        public CommandResult Lock(string id)
        {
            var item = State.items.Find(i => i.id == id);
            if (item == null) return CommandResult.Fail("장비를 찾을 수 없어요.");
            item.locked = !item.locked; return CommandResult.Ok(item.locked ? "판매 잠금 설정" : "판매 잠금 해제");
        }
        public CommandResult Transfer(string from, string to)
        {
            var a = State.items.Find(i => i.id == from); var b = State.items.Find(i => i.id == to);
            if (a == null || b == null || a == b || a.level <= 1 || b.level != 1 || Catalog.Items[a.definition].Slot != Catalog.Items[b.definition].Slot)
                return CommandResult.Fail("같은 슬롯의 Lv.1 장비에만 이전할 수 있어요.");
            b.level = a.level; a.level = 1; return CommandResult.Ok("강화 단계를 이전했어요.");
        }
        public CommandResult Sell(string id)
        {
            var item = State.items.Find(i => i.id == id);
            if (item == null) return CommandResult.Fail("이미 판매했거나 없는 장비예요.");
            if (State.equipped.Contains(id) || item.locked || item.level > 1) return CommandResult.Fail("장착·잠금·강화 장비는 판매할 수 없어요.");
            int value = new[] { 300, 600, 1200 }[item.rarity];
            State.items.Remove(item); State.gold += value; return CommandResult.Ok("판매 완료 · 골드 +" + value);
        }
        public CommandResult BeginRun(string id, int stage)
        {
            RefreshDay();
            if (string.IsNullOrEmpty(id) || stage < 0 || stage > State.unlockedStage || stage >= Campaign.Count) return CommandResult.Fail("아직 갈 수 없는 구역이에요.");
            if (State.receipts.Exists(r => r.id == id) || !string.IsNullOrEmpty(State.activeRun)) return CommandResult.Fail("진행 중인 전투가 있어요.");
            State.activeMode = State.activeWeek = "";
            State.activeRun = id; State.activeStage = stage; return CommandResult.Ok("출격!");
        }
        public CommandResult FinishRun(string id, int stage, bool won, int kills, float seconds)
        {
            RefreshDay();
            if (State.activeMode != "") return CommandResult.Fail("도전 전용 정산이 필요해요.");
            if (State.receipts.Exists(r => r.id == id)) return CommandResult.Fail("이미 받은 전투 보상이에요.");
            if (State.activeRun != id || string.IsNullOrEmpty(id) || stage != State.activeStage || stage < 0 || stage >= Campaign.Count || kills < 0 || seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds))
                return CommandResult.Fail("유효하지 않은 전투 결과예요.");
            var definition = Campaign.Get(stage);
            int gold = won ? definition.Gold : Math.Min(1000, (int)Math.Min(seconds, 300) * 4);
            int parts = won ? definition.Parts : Math.Min(8, (int)seconds / 30);
            int gems = 0;
            if (won && !State.clearedStages.Contains(stage))
            {
                gems = 50; State.clearedStages.Add(stage); State.unlockedStage = Math.Min(Campaign.Count - 1, Math.Max(State.unlockedStage, stage + 1));
                if (State.items.Count < 80) AddItem(definition.FirstItem, definition.FirstRarity);
                else parts += 30;
            }
            State.gold += gold; State.parts += parts; State.gems += gems;
            if (seconds >= 30) State.dailyRuns++;
            State.dailyKills += kills; State.totalKills += kills; State.totalRuns++;
            State.tutorialCompleted |= won; State.activeRun = "";
            State.lastResultId = id; State.lastResultGold = gold; State.lastResultDate = State.serviceDate; State.lastResultBonus = false;
            State.receipts.Add(new Receipt { id = id, kind = "battle", gold = gold, parts = parts, gems = gems, won = won, kills = kills, seconds = seconds });
            return CommandResult.Ok("골드 +" + gold + " · 부품 +" + parts + (gems > 0 ? " · 보석 +" + gems : ""));
        }
        public CommandResult ClaimDemoReward(string placement, string claim)
        {
            RefreshDay();
            if (string.IsNullOrEmpty(claim) || State.receipts.Exists(r => r.id == claim)) return CommandResult.Fail("이미 수령한 보상이에요.");
            int gold = 0, parts = 0;
            if (placement == "result")
            {
                if (State.lastResultBonus || State.lastResultGold <= 0 || State.lastResultDate != State.serviceDate || State.dailyRewardGold >= 3)
                    return CommandResult.Fail("수령 가능한 전투 추가 보상이 없어요.");
                gold = State.lastResultGold / 2; State.dailyRewardGold++; State.lastResultBonus = true;
            }
            else if (placement == "parts")
            {
                if (State.dailyRewardParts >= 2) return CommandResult.Fail("오늘 부품 보상을 모두 받았어요.");
                parts = 10; State.dailyRewardParts++;
            }
            else return CommandResult.Fail("없는 보상이에요.");
            State.gold += gold; State.parts += parts;
            State.receipts.Add(new Receipt { id = claim, kind = "demo_reward", gold = gold, parts = parts });
            return CommandResult.Ok(gold > 0 ? "추가 골드 +" + gold : "부품 +10");
        }
    }
}
