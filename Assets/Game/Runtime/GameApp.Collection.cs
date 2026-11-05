using System;
using ScrapGuard.Core;
using UnityEngine;

namespace ScrapGuard
{
    public partial class GameApp
    {
        Color Rarity(int rarity) { return InventoryArt.Rarity(rarity); }
        void ItemIcon(Transform parent, Item item, float x, float y, float size = 65)
        { InventoryArt.Item(parent,item,x,y,size); }
        void Gear()
        {
            Ui.Label(screen,"장착 중 · 눌러서 정비",24,176,492,32,21,Ui.Muted);
            for(int i=0;i<3;i++)
            {
                var item=State.items.Find(a=>a.id==State.equipped[i]);string id=item.id;
                var card=Ui.Button(screen,"장착 장비 정비 · "+i,24+i*168,218,156,234,()=>ItemDetail(id),Ui.Panel).transform;
                card.GetComponentInChildren<UnityEngine.UI.Text>().text="";
                Ui.Label(card,InventoryArt.Slot(i),12,6,132,28,18,Ui.Muted);
                ItemIcon(card,item,32,40,92);
                Ui.Label(card,Catalog.Items[item.definition].Name,7,137,142,30,18,Ui.Ink,TextAnchor.MiddleCenter);
                Ui.Button(card,"Lv."+item.level+" 정비",8,174,140,52,()=>ItemDetail(id),Ui.Raised,true,18);
            }
            Ui.Label(screen,"보관함  "+State.items.Count+"/80",24,469,492,35,21,Ui.Muted);
            var list=Ui.Scroll(screen,24,516,492,MenuBottom-516,State.items.Count*125);
            for(int i=0;i<State.items.Count;i++)
            {
                var item=State.items[i];string id=item.id;
                var row=Ui.Box(list,0,i*125,492,113,Ui.Panel).transform;ItemIcon(row,item,12,14,84);
                Ui.Label(row,Catalog.Items[item.definition].Name+"  Lv."+item.level,110,12,260,36,21);
                Ui.Label(row,InventoryArt.Slot(Catalog.Items[item.definition].Slot)+" · "+Catalog.RarityNames[item.rarity],110,49,260,27,18,Rarity(item.rarity));
                Ui.Label(row,State.equipped.Contains(id)?"장착 중":item.locked?"판매 잠금":"보유",110,79,260,24,17,Ui.Muted);
                Ui.Button(row,"관리",386,21,94,72,()=>ItemDetail(id),Ui.Raised,true,20);
            }
        }
        void ItemDetail(string id,string feedback=null)
        {
            var item = State.items.Find(i => i.id == id); if (item == null) { CloseModal(); return; }
            var definition = Catalog.Items[item.definition];
            var card=Dialog(definition.Name,feedback??(Catalog.RarityNames[item.rarity]+" · "+InventoryArt.Slot(definition.Slot)),730);
            ItemIcon(card,item,24,138,144);
            Ui.Label(card,"강화 Lv."+item.level+" / 20",186,141,282,39,26);
            Ui.Label(card,"등급 · 강화 배율 ×"+Catalog.Multiplier(item).ToString("0.00"),186,187,282,34,19,Rarity(item.rarity));
            Ui.Label(card,definition.Description,186,230,282,53,18,Ui.Muted);
            Ui.Button(card,State.equipped.Contains(id)?"장착 중":"장착하기",24,300,214,64,()=>Change(new GameCommand("equip",id),()=>ItemDetail(id)),null,!State.equipped.Contains(id),20);
            Ui.Button(card,item.locked?"잠금 해제":"판매 잠금",254,300,214,64,()=>Change(new GameCommand("lock",id),()=>ItemDetail(id)),Ui.Raised,true,20);
            Ui.Label(card,"강화 비용  골드 "+(item.level*300).ToString("N0")+" · 부품 "+item.level*2,24,384,444,32,19,Ui.Gold);
            Ui.Button(card,item.level>=20?"최대 강화":"장비 강화  +1",24,431,444,64,()=>UpgradeItemWithFeedback(id),null,item.level<20,22);
            Ui.Button(card,"강화 이전",24,511,214,64,()=>TransferTargets(id),Ui.Raised,item.level>1,20);
            Ui.Button(card,"판매  +"+new[]{300,600,1200}[item.rarity],254,511,214,64,()=>ConfirmSale(id),Ui.Raised,!State.equipped.Contains(id)&&!item.locked&&item.level==1,20);
            Ui.Label(card,"장착·잠금 장비는 판매되지 않아요.\n강화 장비는 강화 단계를 옮긴 뒤 판매하세요.\n보석 구매 장비도 판매 시 골드만 지급돼요.",24,596,444,100,18,Ui.Muted);
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
            var list = Ui.Scroll(screen, 24, 178, 492, MenuBottom-178, HeroCatalog.Count*232);
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
        {HeroUpgrade(null);}
        void HeroUpgrade(string feedback)
        {
            var hero = State.heroes[State.selectedHero];
            var card = Dialog("대원 레벨업", feedback??(Catalog.HeroNames[State.selectedHero] + " · 기본 공격과 체력이 증가해요."), 440);
            Ui.Label(card, "Lv." + hero.level + " → Lv." + Math.Min(30, hero.level + 1), 24, 139, 444, 52, 29, Ui.Mint, TextAnchor.MiddleCenter);
            var weapon=State.items.Find(i=>i.id==State.equipped[0]);var armor=State.items.Find(i=>i.id==State.equipped[1]);int next=Mathf.Min(30,hero.level+1);
            Ui.Label(card,"공격 "+Catalog.Attack(State)+" → "+Catalog.AttackFor(State.selectedHero,next,weapon)+"   체력 "+Catalog.Health(State)+" → "+Catalog.HealthFor(State.selectedHero,next,armor),24,204,444,37,16,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Label(card, "골드 " + (hero.level * 500).ToString("N0") + " · 부품 " + hero.level * 3, 24, 255, 444, 40, 18, Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Button(card, hero.level == 30 ? "최대 레벨" : "레벨업", 24, 343, 444, 59, UpgradeHeroWithFeedback, null, hero.level < 30);
        }
        int shopTab;
        bool shopSupplies { get { return shopTab==1; } set { shopTab=value?1:0; } }
        bool shopGear;
        readonly float[] shopOffsets=new float[4];
        void Shop()
        {
            string[] tabs={"의상 · 스타일","성장 · 장비","보석 · 광고"};
            for(int i=0;i<3;i++)
            {
                int tab=i;
                Ui.Button(screen,tabs[i],24+i*166,176,160,72,()=>{shopTab=tab;Show("shop");},shopTab==i?Ui.Mint:Ui.Raised,true,18);
            }
            if(shopTab==0){StyleShop();return;}
            if(shopTab==2){CurrencyShop();return;}
            Ui.Button(screen,"성장 재료",24,258,238,66,()=>{shopGear=false;Show("shop");},shopGear?Ui.Raised:Ui.Mint,true,16);
            Ui.Button(screen,"확정 장비",278,258,238,66,()=>{shopGear=true;Show("shop");},shopGear?Ui.Mint:Ui.Raised,true,16);
            if(!shopGear)
            {
                var panel=Ui.Box(screen,24,340,492,145,Ui.Panel).transform;
                CurrencyArt.Draw(panel,2,16,22,98);
                Ui.Label(panel,"다음 작전을 위한 정비",130,20,338,36,23);
                Ui.Label(panel,"골드와 부품으로 장비·대원을 강화해요.\n구매한 재료는 보유량에 바로 더해져요.",130,65,338,57,16,Ui.Muted);
                var list=ShopScroll(501,MenuBottom-501,386,0);
                ShopRow(list,0,"공용 부품 ×20","매일 1회 · 골드로 성장 재료 보충","2,000 골드",()=>ConfirmBuy("parts_gold","부품 20개","골드 2,000"),State.dailyPartsPurchases<1);
                ShopRow(list,110,"공용 부품 ×100","매일 1회 · 확정 수량","100 보석",()=>ConfirmBuy("parts_gems","부품 100개","보석 100"),State.dailyGemPartsPurchases<1);
                Ui.Button(list,"장비 강화로 이동",0,230,238,53,()=>Show("gear"),Ui.Raised,true,17);
                Ui.Button(list,"대원 성장으로 이동",254,230,238,53,()=>Show("heroes"),Ui.Raised,true,17);
                Ui.Label(list,"무료 부품은 보석 · 광고 탭에서 받을 수 있어요.",0,305,492,43,15,Ui.Muted,TextAnchor.MiddleCenter);
                return;
            }
            var gearList=ShopScroll(340,MenuBottom-340,1670,1);
            Ui.Label(gearList,"일반 장비 · 하루 총 2개",0,0,330,35,22);
            Ui.Label(gearList,"오늘 "+State.dailyItemPurchases+" / 2",340,0,152,35,18,Ui.Muted,TextAnchor.MiddleRight);
            int[] sale={1,2,3,4,5,7,8,10,11};
            for(int i=0;i<sale.Length;i++)GearOfferRow(gearList,48+i*145,sale[i],0,State.dailyItemPurchases<2);
            Ui.Label(gearList,"희귀 장비 · 확정 구매",0,1360,492,35,22);
            GearOfferRow(gearList,1408,2,2,!State.purchasedRare.Contains(2)&&!State.items.Exists(i=>i.definition==2&&i.rarity==2));
            Ui.Button(gearList,"보유 장비 확인 · 판매",0,1568,492,72,()=>Show("gear"),Ui.Raised,true,20);
        }
        void GearOfferRow(Transform parent,float y,int definition,int rarity,bool available)
        {
            Action compare=()=>ConfirmBuy((rarity==2?"rare:":"normal:")+definition,Catalog.Items[definition].Name,rarity==2?"보석 600":"골드 3,000");
            var row=Ui.Button(parent,"장비 비교 · "+definition+" / "+rarity,0,y,492,133,compare,Ui.Panel,available).transform;
            row.GetComponentInChildren<UnityEngine.UI.Text>().text="";
            ItemIcon(row,new Item{definition=definition,rarity=rarity,level=1},12,17,92);
            Ui.Label(row,Catalog.Items[definition].Name,118,10,355,31,22);
            int count=State.items.FindAll(i=>i.definition==definition&&i.rarity==rarity).Count;
            Ui.Label(row,Catalog.RarityNames[rarity]+" · "+InventoryArt.Slot(Catalog.Items[definition].Slot)+" · 보유 "+count,118,44,355,26,18,Rarity(rarity));
            Ui.Button(row,available?(rarity==2?"600 보석 · 비교":"3,000 골드 · 비교"):"구매 완료",118,78,358,46,
                ()=>ConfirmBuy((rarity==2?"rare:":"normal:")+definition,Catalog.Items[definition].Name,rarity==2?"보석 600":"골드 3,000"),Ui.Raised,available,19);
        }
        RectTransform ShopScroll(float y,float height,float contentHeight,int slot)
        {
            var list=Ui.Scroll(screen,24,y,492,height,contentHeight);
            list.anchoredPosition=new Vector2(0,Mathf.Clamp(shopOffsets[slot],0,Mathf.Max(0,contentHeight-height)));
            list.parent.GetComponent<UnityEngine.UI.ScrollRect>().onValueChanged.AddListener(_=>{if(list!=null&&list.gameObject.activeInHierarchy)shopOffsets[slot]=list.anchoredPosition.y;});
            return list;
        }
        void CurrencyShop()
        {
            var list=ShopScroll(258,MenuBottom-258,1365,2);
            var free=Ui.Box(list,0,0,492,146,Ui.Panel).transform;
            CurrencyArt.Draw(free,2,12,24,90);
            Ui.Label(free,"무료 보급 · 부품 10개",116,16,355,34,22);
            Ui.Label(free,"오늘 "+State.dailyRewardParts+" / 2회 · 선택 보상",116,52,355,27,15,Ui.Muted);
            Ui.Button(free,State.dailyRewardParts>=2?"오늘 수령 완료":State.demoAdSkip||State.adSkip?"광고 없이 받기":Online?"광고 보고 받기":"보상 체험 안내",116,91,352,42,()=>RequestReward("parts"),Ui.Mint,State.dailyRewardParts<2,17);
            Ui.Button(list,"무료 보석 획득처",0,161,238,66,()=>GemSources(0,()=>{shopTab=2;Show("shop");}),Ui.Raised,true,16);
            Ui.Button(list,"보석 잔액 · 내역",254,161,238,66,GemWalletPanel,Ui.Raised,true,16);
            Ui.Label(list,"보석 충전 · 상품 구성",0,241,492,38,24);
            Ui.Label(list,"실제 가격과 판매 여부는 Google Play에서 확인해요.",0,287,492,40,17,Ui.Muted);
            for(int i=0;i<CommerceCatalog.Offers.Length;i++)
            {
                var offer=CommerceCatalog.Offers[i];float y=340+i*162;
                var row=Ui.Button(list,"상품 구성 · "+offer.Id,0,y,492,150,()=>StoreOfferPanel(offer),Ui.Panel).transform;
                row.GetComponentInChildren<UnityEngine.UI.Text>().text="";
                InventoryArt.Draw(row,InventoryArt.Pack(i),8,13,110);
                Ui.Label(row,offer.Name,130,12,344,33,24);
                Ui.Label(row,offer.Gems>0?"표시 수량 확정 지급":"보상 횟수 유지 · 광고 시청 생략",130,51,344,29,17,Ui.Muted);
                Ui.Button(row,"구성 · 가격 확인",130,88,344,52,()=>StoreOfferPanel(offer),Ui.Raised,true,20);
            }
            Ui.Button(list,"Google Play 연결 · 구매 복원",0,1170,492,72,ConnectPlayBilling,Ui.Mint,Online,17);
            Ui.Label(list,"무료·유료 보석의 구매 효력은 같아요.\n보석은 현금으로 환전할 수 없어요.",0,1260,492,70,18,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void ShopRow(Transform parent, float y, string title, string description, string action, Action clicked, bool enabled = true)
        {
            var row = Ui.Box(parent, 0, y, 492, 98, Ui.Panel).transform;
            Ui.Label(row, title, 16, 12, 310, 37, 20);
            Ui.Label(row, description, 16, 52, 307, 34, 15, Ui.Muted);
            Ui.Button(row, enabled ? action : "완료", 338, 22, 138, 53, clicked, Ui.Raised, enabled, 15);
        }
        void ConfirmBuy(string offer, string name, string price)
        {
            bool equipment=offer.StartsWith("normal:")||offer.StartsWith("rare:");
            float extra=equipment?280:0;
            var card = Dialog("구매 확인", "확정 구성 · 화면에 표시된 재화만 사용합니다.", 600+extra);
            if(equipment)EquipmentComparison(card,offer,181);
            Ui.Label(card, name, 24, 128, 444, 45, 25, Ui.Ink, TextAnchor.MiddleCenter);
            int gems=offer=="hero"?1200:offer=="parts_gems"?100:offer.StartsWith("rare:")?600:0;
            int cost=gems>0?gems:offer=="parts_gold"?2000:3000;
            int balance=gems>0?State.gems:State.gold;
            PurchaseBalance(card,gems>0?1:0,cost,balance,190+extra);
            bool enough=balance>=cost&&(gems==0||State.gemDebt==0);
            Ui.Button(card, enough?"구매하기":gems>0&&State.gemDebt>0?"환불 조정 내역을 확인해 주세요":"재화가 부족해요", 24, 360+extra, 444, 64, () => PurchaseSupply(offer,name),null,enough);
            if(gems>0)Ui.Button(card,"무료 보석 획득처 · 부족분 확인",24,434+extra,444,60,()=>GemSources(gems,()=>ConfirmBuy(offer,name,price)),Ui.Raised,true,16);
            Ui.Button(card,"취소 · 상점으로",24,505+extra,444,60,()=>Show("shop"),Ui.Raised,true,17);
        }
        void PurchaseBalance(Transform card,int kind,int cost,int balance,float y)
        {
            var panel=Ui.Box(card,24,y,444,139,Ui.Background).transform;
            CurrencyArt.Draw(panel,kind,18,24,82);
            Ui.Label(panel,CurrencyArt.Names[kind]+" "+cost.ToString("N0")+"개 사용",116,16,310,32,23,CurrencyArt.Tint(kind));
            Ui.Label(panel,"현재 보유  "+balance.ToString("N0"),116,57,310,26,17,Ui.Muted);
            Ui.Label(panel,balance>=cost?"구매 후  "+(balance-cost).ToString("N0"):"부족한 수량  "+(cost-balance).ToString("N0"),116,95,310,27,18,balance>=cost?Ui.Ink:Ui.Red);
        }
    }
}
