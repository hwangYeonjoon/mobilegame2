using System;
using ScrapGuard.Core;
using UnityEngine;

namespace ScrapGuard
{
    public partial class GameApp
    {
        Color Rarity(int rarity) { return rarity == 2 ? Ui.Blue : rarity == 1 ? Ui.Mint : Ui.Muted; }
        void ItemIcon(Transform parent, Item item, float x, float y, float size = 65)
        {
            Ui.Box(parent, x, y, size, size, Rarity(item.rarity));
            Ui.Box(parent, x + 3, y + 3, size - 6, size - 6, Ui.Panel);
            Ui.Label(parent, Catalog.Items[item.definition].Icon, x, y, size, size, 28, Rarity(item.rarity), TextAnchor.MiddleCenter);
        }
        void Gear()
        {
            Ui.Label(screen, "장착 중", 24, 175, 492, 31, 18, Ui.Muted);
            string[] slots = { "무기", "갑옷", "모듈" };
            for (int i = 0; i < 3; i++)
            {
                var item = State.items.Find(a => a.id == State.equipped[i]); string id = item.id; float x = 24 + i * 168;
                var card = Ui.Box(screen, x, 218, 156, 167, Ui.Panel).transform;
                Ui.Label(card, slots[i], 12, 6, 132, 25, 13, Ui.Muted);
                ItemIcon(card, item, 47, 39, 62);
                Ui.Label(card, Catalog.Items[item.definition].Name, 5, 105, 146, 28, 16, Ui.Ink, TextAnchor.MiddleCenter);
                Ui.Button(card, "Lv." + item.level + "  정비", 12, 136, 132, 27, () => ItemDetail(id), Ui.Raised, true, 13);
            }
            Ui.Label(screen, "보관함  " + State.items.Count + "/80", 24, 402, 492, 36, 18, Ui.Muted);
            var list = Ui.Scroll(screen, 24, 450, 492, 392, State.items.Count * 107);
            for (int i = 0; i < State.items.Count; i++)
            {
                var item = State.items[i]; string id = item.id; float y = i * 107;
                var row = Ui.Box(list, 0, y, 492, 97, Ui.Panel).transform; ItemIcon(row, item, 14, 16);
                Ui.Label(row, Catalog.Items[item.definition].Name + "  Lv." + item.level, 94, 12, 270, 36, 19);
                Ui.Label(row, Catalog.RarityNames[item.rarity] + (State.equipped.Contains(item.id) ? " · 장착 중" : "") + (item.locked ? " · 잠금" : ""), 94, 51, 270, 27, 14, Rarity(item.rarity));
                Ui.Button(row, "관리", 395, 23, 82, 51, () => ItemDetail(id), Ui.Raised, true, 16);
            }
        }
        void ItemDetail(string id)
        {
            var item = State.items.Find(i => i.id == id); if (item == null) { CloseModal(); return; }
            var definition = Catalog.Items[item.definition];
            var card = Dialog(definition.Name, Catalog.RarityNames[item.rarity] + " · " + definition.Description, 624);
            ItemIcon(card, item, 24, 137, 84);
            Ui.Label(card, "강화 Lv." + item.level + " / 20", 130, 136, 333, 39, 24);
            Ui.Label(card, "기본 능력치 ×" + Catalog.Multiplier(item).ToString("0.00"), 130, 181, 333, 35, 16, Rarity(item.rarity));
            Ui.Button(card, State.equipped.Contains(id) ? "장착 중" : "장착하기", 24, 243, 214, 53, () => Change(new GameCommand("equip", id), () => ItemDetail(id)), null, !State.equipped.Contains(id));
            Ui.Button(card, item.locked ? "잠금 해제" : "판매 잠금", 254, 243, 214, 53, () => Change(new GameCommand("lock", id), () => ItemDetail(id)), Ui.Raised);
            Ui.Label(card, "강화 비용  골드 " + (item.level * 300).ToString("N0") + " · 부품 " + item.level * 2, 24, 317, 444, 32, 16, Ui.Gold);
            Ui.Button(card, item.level >= 20 ? "최대 강화" : "장비 강화  +1", 24, 362, 444, 56, () => Change(new GameCommand("upgrade", id), () => ItemDetail(id)), null, item.level < 20);
            Ui.Button(card, "강화 이전", 24, 438, 214, 50, () => TransferTargets(id), Ui.Raised, item.level > 1, 16);
            Ui.Button(card, "판매  +" + new[] { 300, 600, 1200 }[item.rarity], 254, 438, 214, 50, () => ConfirmSale(id), Ui.Raised,
                !State.equipped.Contains(id) && !item.locked && item.level == 1, 16);
            Ui.Label(card, "장착·잠금 장비는 판매되지 않아요.\n강화 장비는 강화 단계를 옮긴 뒤 판매하세요.\n보석 구매 장비도 판매 시 골드만 지급돼요.", 24, 510, 444, 88, 14, Ui.Muted);
        }
        void ConfirmSale(string id)
        {
            var item = State.items.Find(i => i.id == id); if (item == null) return;
            var card = Dialog("장비를 판매할까요?", "판매한 장비는 되돌릴 수 없어요.", 342);
            Ui.Label(card, Catalog.Items[item.definition].Name + " → 골드 " + new[] { 300, 600, 1200 }[item.rarity], 24, 140, 444, 54, 22);
            Ui.Button(card, "판매 확정", 24, 247, 444, 63, () => Change(new GameCommand("sell", id)), Ui.Gold);
        }
        void TransferTargets(string id)
        {
            var source = State.items.Find(i => i.id == id); if (source == null) return;
            var targets = State.items.FindAll(i => i.id != id && i.level == 1 && Catalog.Items[i.definition].Slot == Catalog.Items[source.definition].Slot);
            var card = Dialog("강화 이전", "같은 슬롯의 Lv.1 장비를 선택하세요.\n원본 장비는 Lv.1로 돌아갑니다. 비용은 없어요.", 560);
            var list = Ui.Scroll(card, 24, 145, 444, 380, Math.Max(100, targets.Count * 90));
            if (targets.Count == 0) Ui.Label(list, "이전할 수 있는 장비가 없어요.", 0, 20, 444, 60, 19, Ui.Muted, TextAnchor.MiddleCenter);
            for (int i = 0; i < targets.Count; i++)
            {
                string target = targets[i].id;
                Ui.Button(list, Catalog.RarityNames[targets[i].rarity] + " " + Catalog.Items[targets[i].definition].Name, 0, i * 90, 444, 74,
                    () => Change(new GameCommand("transfer", id) { other = target }, () => ItemDetail(target)), Ui.Raised);
            }
        }
        void Heroes()
        {
            var list = Ui.Scroll(screen, 24, 178, 492, 668, HeroCatalog.Count*232);
            for (int i = 0; i < HeroCatalog.Count; i++)
            {
                int hero = i; float y = i * 232; bool unlocked = State.heroes[i].unlocked; bool selected = State.selectedHero == i;
                var row = Ui.Box(list, 0, y, 492, 215, Ui.Panel).transform;
                Ui.Robot(row, 22, 48, .95f, i, skin: State.selectedSkin, outfit: Outfits.Active(State,i));
                Ui.Label(row, Catalog.HeroNames[i] + "  Lv." + State.heroes[i].level, 124, 15, 350, 38, 23);
                Ui.Label(row, Catalog.HeroRoles[i], 124, 59, 350, 34, 15, i == 0 ? Ui.Mint : i == 1 ? Ui.Gold : Ui.Blue);
                Ui.Label(row, "기본 공격 " + HeroCatalog.All[i].Attack + " · 체력 " + HeroCatalog.All[i].Health, 124, 98, 350, 29, 14, Ui.Muted);
                Ui.Label(row, "필살기 · " + Combat.UltimateNames[i]+(i==0?" / 현장 쌍둥이 포탑":i==1?" / 현장 질주 레일건":""),124,124,350,22,12,Ui.Mint);
                if (unlocked)
                {
                    Ui.Button(row, selected ? "선택 중" : "출격 대원 선택", 124, 148, 180, 49, () => Change(new GameCommand("hero_select") { hero = hero }), selected ? Ui.Raised : Ui.Mint, !selected, 15);
                    if (selected) Ui.Button(row, "레벨업", 316, 148, 158, 49, HeroUpgrade, Ui.Raised, true, 16);
                }
                else if(HeroCatalog.All[i].RecruitStage>=0)
                {
                    bool ready=HeroCatalog.CanRecruit(State,i);
                    Ui.Button(row,ready?"무료 모집 · 작전 보상":"작전 "+(HeroCatalog.All[i].RecruitStage+1).ToString("D2")+" 완료로 모집",124,148,350,49,
                        ()=>{if(ready)Change(new GameCommand("hero_recruit"){hero=hero});else CampaignDetails(HeroCatalog.All[hero].RecruitStage);},ready?Ui.Mint:Ui.Raised,true,16);
                }
                else Ui.Button(row, i == 1 ? "◆ 1,200 해금" : "누적 출석 7일 해금", 124, 148, 350, 49,
                    () => { if (hero == 1) ConfirmBuy("hero", "돌격수 레이", "보석 1,200"); else Attendance(); }, Ui.Raised, true, 16);
            }
        }
        void HeroUpgrade()
        {
            var hero = State.heroes[State.selectedHero];
            var card = Dialog("대원 레벨업", Catalog.HeroNames[State.selectedHero] + " · 기본 공격과 체력이 증가해요.", 365);
            Ui.Label(card, "Lv." + hero.level + " → Lv." + Math.Min(30, hero.level + 1), 24, 139, 444, 52, 29, Ui.Mint, TextAnchor.MiddleCenter);
            Ui.Label(card, "골드 " + (hero.level * 500).ToString("N0") + " · 부품 " + hero.level * 3, 24, 202, 444, 40, 18, Ui.Gold, TextAnchor.MiddleCenter);
            Ui.Button(card, hero.level == 30 ? "최대 레벨" : "레벨업", 24, 279, 444, 59, () => Change(new GameCommand("hero_upgrade"), HeroUpgrade), null, hero.level < 30);
        }
        bool shopSupplies;
        void Shop()
        {
            Ui.Button(screen,"스타일 컬렉션",24,176,238,46,() => { shopSupplies = false; Show("shop"); },shopSupplies ? Ui.Raised : Ui.Mint,true,17);
            Ui.Button(screen,"보급 · 장비",278,176,238,46,() => { shopSupplies = true; Show("shop"); },shopSupplies ? Ui.Mint : Ui.Raised,true,17);
            if (!shopSupplies) { StyleShop(); return; }
            var list = Ui.Scroll(screen, 24, 238, 492, 608, 2520);
            Ui.Label(list, "보급품", 0, 0, 492, 32, 22);
            ShopRow(list, 45, "공용 부품 ×20", "매일 1회 · 골드로 성장 재료 보충", "2,000 골드", () => ConfirmBuy("parts_gold", "부품 20개", "골드 2,000"), State.dailyPartsPurchases < 1);
            ShopRow(list, 154, "공용 부품 ×100", "매일 1회 · 확정 수량", "◆ 100", () => ConfirmBuy("parts_gems", "부품 100개", "보석 100"), State.dailyGemPartsPurchases < 1);
            ShopRow(list, 263, "추가 보급 부품 ×10", "선택 보상 · 오늘 " + State.dailyRewardParts + "/2회", State.demoAdSkip || State.adSkip ? "스킵 수령" : "체험 안내", () => RequestReward("parts"), State.dailyRewardParts < 2);
            Ui.Label(list, "확정 장비 · 일반", 0, 391, 492, 36, 22);
            Ui.Label(list, "하루 총 2개 · 구매 " + State.dailyItemPurchases + "/2", 0, 427, 492, 27, 14, Ui.Muted);
            int[] sale = { 1, 2, 3, 4, 5, 7, 8, 10, 11 };
            for (int i = 0; i < sale.Length; i++)
            {
                int definition = sale[i];
                ShopRow(list, 465 + i * 109, Catalog.Items[definition].Name, Catalog.Items[definition].Description, "3,000 골드",
                    () => ConfirmBuy("normal:" + definition, Catalog.Items[definition].Name, "골드 3,000"), State.dailyItemPurchases < 2);
            }
            Ui.Label(list, "희귀 장비 · 확정 구매", 0, 1460, 492, 38, 22);
            ShopRow(list, 1508, "희귀 충격 발사기", "능력치 ×1.35 · 장비별 영구 1회", "◆ 600", () => ConfirmBuy("rare:2", "희귀 충격 발사기", "보석 600"), !State.purchasedRare.Contains(2) && !State.items.Exists(i => i.definition == 2 && i.rarity == 2));
            Ui.Label(list, "Google Play 상품 미리보기", 0, 1636, 330, 38, 22);
            Ui.Button(list,"보석 내역",356,1635,136,38,GemWalletPanel,Ui.Raised,true,14);
            Ui.Label(list, "일반 판매 준비 중 · 이곳에서는 구성과 제안가만 확인해요.", 0, 1677, 492, 38, 13, Ui.Muted);
            for(int i=0;i<CommerceCatalog.Offers.Length;i++)
            {
                var offer=CommerceCatalog.Offers[i];
                ShopRow(list,1730+i*109,offer.Name,"테스트 제안가 "+offer.ProposedWon.ToString("N0")+"원", "혜택 보기",()=>StoreOfferPanel(offer));
            }
            Ui.Button(list,"의상 컬렉션 · 수집 목표와 대원별 장착",0,2300,492,55,ConceptRoom,Ui.Mint,true,18);
            Ui.Label(list,"무료 보상과 보석 상품의 구매 효력은 같아요.\n장비 판매는 장비 → 관리에서 진행하세요.\n보석을 현금으로 환전할 수 없습니다.",0,2380,492,105,16,Ui.Muted);
        }
        void ShopRow(Transform parent, float y, string title, string description, string action, Action clicked, bool enabled = true)
        {
            var row = Ui.Box(parent, 0, y, 492, 98, Ui.Panel).transform;
            Ui.Label(row, title, 16, 12, 310, 37, 20);
            Ui.Label(row, description, 16, 52, 307, 34, 13, Ui.Muted);
            Ui.Button(row, enabled ? action : "완료", 338, 22, 138, 53, clicked, Ui.Raised, enabled, 15);
        }
        void ConfirmBuy(string offer, string name, string price)
        {
            var card = Dialog("구매 확인", "확정 구성 · 화면에 표시된 재화만 사용합니다.", 470);
            Ui.Label(card, name, 24, 128, 444, 45, 25, Ui.Ink, TextAnchor.MiddleCenter);
            Ui.Label(card, price, 24, 182, 444, 40, 21, Ui.Gold, TextAnchor.MiddleCenter);
            int gems=offer=="hero"?1200:offer=="parts_gems"?100:offer.StartsWith("rare:")?600:0;
            Ui.Button(card, "구매하기", 24, 269, 444, 62, () => Change(new GameCommand("buy") { value = offer }),null,gems==0 || State.gems>=gems && State.gemDebt==0);
            if(gems>0)Ui.Button(card,"무료 보석 획득처 · 부족분 확인",24,365,444,51,()=>GemSources(gems,()=>ConfirmBuy(offer,name,price)),Ui.Raised,true,16);
        }
    }
}
