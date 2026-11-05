using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        void PurchaseOutfit(int id)
        {
            if(!Outfits.Valid(id) || Outfits.Owned(State,id)){OutfitRoom(id);return;}
            Change(new GameCommand("cosmetic_buy"){value=Outfits.Product(id)},confirmed:()=>{
                if(Outfits.Owned(State,id)){Track("collection_acquired",Outfits.Product(id));OutfitAcquired(id);}
            });
        }
        void PurchaseSupply(string offer,string name)
        {
            var ids=new System.Collections.Generic.HashSet<string>();foreach(var item in State.items)ids.Add(item.id);
            Change(new GameCommand("buy"){value=offer},confirmed:()=>{
                var added=State.items.Find(i=>!ids.Contains(i.id));
                var card=Dialog(offer=="hero"?"새 대원이 합류했어요":"구매한 보급품을 받았어요",name+" · 보유 내역에 저장됐어요.",650);
                if(offer=="hero")
                {
                    CharacterArt.Draw(card,136,128,220,215,1,State.selectedSkin,Outfits.Active(State,1));
                    Ui.Label(card,HeroCatalog.All[1].Passive+"\n필살기 · "+Combat.UltimateNames[1],24,361,444,65,18,Ui.Ink,TextAnchor.MiddleCenter);
                    Ui.Button(card,"레이를 출격 대원으로 선택",24,460,444,54,()=>Change(new GameCommand("hero_select"){hero=1},()=>Show("heroes")),Ui.Mint,true,17);
                }
                else if(added!=null)
                {
                    ItemIcon(card,added,204,146,84);
                    Ui.Label(card,Catalog.Items[added.definition].Name+"\n"+Catalog.RarityNames[added.rarity]+" · 기본 능력치 ×"+Catalog.Multiplier(added).ToString("0.00"),24,266,444,73,23,Rarity(added.rarity),TextAnchor.MiddleCenter);
                    Ui.Label(card,Catalog.Items[added.definition].Description+"\n장착하면 출격에 적용돼요.",24,360,444,65,17,Ui.Muted,TextAnchor.MiddleCenter);
                    Ui.Button(card,"장비 확인 · 장착",24,460,444,54,()=>ItemDetail(added.id),Ui.Mint,true,18);
                }
                else
                {
                    Ui.Label(card,"공용 부품\n현재 "+State.parts.ToString("N0")+"개 보유",24,176,444,133,32,Ui.Gold,TextAnchor.MiddleCenter);
                    Ui.Label(card,"장비와 대원 강화에 사용할 수 있어요.\n강화 화면에서 비용과 결과를 확인하세요.",24,350,444,70,18,Ui.Ink,TextAnchor.MiddleCenter);
                    Ui.Button(card,"장비 강화하러 가기",24,460,444,54,()=>Show("gear"),Ui.Mint,true,18);
                }
                Ui.Button(card,"상점으로 돌아가기",24,550,444,51,()=>Show("shop"),Ui.Raised,true,17);
            });
        }
        void OutfitAcquired(int id)
        {
            if(!Outfits.Owned(State,id))return;
            int hero=Outfits.Hero(id);bool equipped=Outfits.Active(State,hero)==id;
            var card=Dialog(equipped?"장착 완료 · 출격 준비":"새 의상을 수집했어요",Outfits.Label(id)+" · "+Outfits.HeroName(id)+" 전용\n보유 내역에 저장됐어요. 언제든 다시 장착할 수 있어요.",880);
            var stage=Ui.Box(card,24,142,444,361,Ui.Background);stage.gameObject.AddComponent<RectMask2D>();
            Ui.Label(stage.transform,equipped?"수집 완료  →  장착 완료  →  전시하기":"수집 완료  →  장착하고 출격해 보세요",16,14,412,26,14,OutfitVisuals.Accent(id),TextAnchor.MiddleCenter);
            var preview=stage.gameObject.AddComponent<OutfitPreview>();preview.Initialize(id);preview.Ultimate();
            Button size=null;size=Ui.Button(stage.transform,"전투 크기로 보기",121,311,202,38,()=>{preview.ToggleSize();size.GetComponentInChildren<Text>().text=preview.BattleSize?"크게 보기":"전투 크기로 보기";},Ui.Raised,true,14);
            Ui.Label(card,"전용 실루엣 · 사격 · 필살기 · 지원 등장\n외형과 연출이 바뀌며 능력치는 그대로예요.",24,521,444,61,17,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Button(card,"사격",24,600,140,43,preview.Fire,Ui.Raised,true,16);
            Ui.Button(card,"필살기",176,600,140,43,preview.Ultimate,Ui.Gold,true,16);
            Ui.Button(card,"지원 등장",328,600,140,43,preview.Support,Ui.Raised,true,16);
            Ui.Button(card,equipped?"이 모습으로 대표 대원 전시":"전용 대원에 의상 장착",24,669,444,53,()=>{
                if(equipped)Change(new GameCommand("customize"){hero=hero,title=State.showcaseTitle,frame=State.showcaseFrame,skin=State.selectedSkin},()=>Show("showcase"));
                else Change(new GameCommand("outfit_equip"){skin=id},()=>{if(Outfits.Active(State,hero)==id)OutfitAcquired(id);else OutfitRoom(id);});
            },Ui.Mint,true,18);
            Ui.Button(card,"이 모습으로 출격 준비",24,737,214,45,()=>PrepareOutfitHero(id),Ui.Raised,true,16);
            Ui.Button(card,"계속 감상하기",254,737,214,45,()=>OutfitRoom(id),Ui.Raised,true,16);
            Ui.Label(card,equipped?"출격 대원은 대원 메뉴에서 선택할 수 있어요.":"장착할 대원과 전시할 대표 대원을 직접 선택해 주세요.",24,804,444,38,13,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void PrepareOutfitHero(int id)
        {
            int hero=Outfits.Hero(id);
            if(!Outfits.Owned(State,id)||!State.heroes[hero].unlocked){OutfitRoom(id);return;}
            System.Action select=()=>Change(new GameCommand("hero_select"){hero=hero},confirmed:()=>Show("home"));
            if(Outfits.Active(State,hero)==id)select();
            else Change(new GameCommand("outfit_equip"){skin=id},confirmed:select);
        }
        void UpgradeItemWithFeedback(string id)
        {
            var item=State.items.Find(i=>i.id==id);if(item==null)return;
            int level=item.level;float multiplier=Catalog.Multiplier(item);int attack=Catalog.Attack(State),health=Catalog.Health(State);
            Change(new GameCommand("upgrade",id),()=>{
                var updated=State.items.Find(i=>i.id==id);
                string feedback=updated!=null && updated.level>level?"강화 완료 · Lv."+level+" → "+updated.level+"\n배율 "+multiplier.ToString("0.00")+" → "+Catalog.Multiplier(updated).ToString("0.00")+
                    (State.equipped.Contains(id)?" · 공격 +"+(Catalog.Attack(State)-attack)+" / 체력 +"+(Catalog.Health(State)-health):" · 미장착 장비"):null;
                ItemDetail(id,feedback);
            });
        }
        void UpgradeHeroWithFeedback()
        {
            int hero=State.selectedHero,level=State.heroes[hero].level,attack=Catalog.Attack(State),health=Catalog.Health(State);
            Change(new GameCommand("hero_upgrade"),()=>HeroUpgrade(State.heroes[hero].level>level?
                "성장 완료 · 공격 "+attack+" → "+Catalog.Attack(State)+"\n체력 "+health+" → "+Catalog.Health(State):null));
        }
    }
}
