using System;
using System.Collections.Generic;
namespace ScrapGuard.Core
{
    // Shared, deterministic previews. A preview never spends currency or changes equipment.
    public sealed class GrowthStep
    {
        public string id,name;public int slot,level,gold,parts,attack,health;public bool affordable;
    }
    public sealed class GrowthKitOffer
    {
        public readonly string id,name;public readonly int clear,gold,parts;
        public GrowthKitOffer(string id,string name,int clear,int gold,int parts){this.id=id;this.name=name;this.clear=clear;this.gold=gold;this.parts=parts;}
    }
    public sealed class GrowthKitQuote
    {
        public GrowthKitOffer offer;public GrowthPath before,after;
        public int ExtraLevels {get{return after.to-before.to;}}
    }
    public static class Growth
    {
        public const int KitPrice=100,KitGold=4500,KitParts=40,PassPrice=900;
        public const string PassKey="operation_growth_pass_1";
        // Public fixed catalog: progression unlocks a larger pack, never personalized prices.
        public static readonly GrowthKitOffer[] Kits={
            new GrowthKitOffer("growth_kit","정비 보급",0,4500,40),
            new GrowthKitOffer("growth_kit_field","전선 보급",12,9000,80),
            new GrowthKitOffer("growth_kit_frontier","심부 보급",30,15000,120)};
        public static GrowthKitOffer FindKit(string id){foreach(var k in Kits)if(k.id==id)return k;return null;}
        public static bool KitUnlocked(GameState s,GrowthKitOffer k){return k!=null&&(k.clear==0||s.clearedStages.Contains(k.clear-1));}
        public static GrowthKitOffer CurrentKit(GameState s){for(int i=Kits.Length-1;i>=0;i--)if(KitUnlocked(s,Kits[i]))return Kits[i];return Kits[0];}
        public static GrowthKitQuote KitQuote(GameState s,string id,int slot)
        {
            var k=FindKit(id);if(!KitUnlocked(s,k))return null;
            var before=GrowthPaths.Quote(s,slot);var after=GrowthPaths.Quote(s,slot,30,k.gold,k.parts);
            return before==null||after==null?null:new GrowthKitQuote{offer=k,before=before,after=after};
        }
        public static bool KitAddsGrowth(GameState s,string id)
        {for(int i=0;i<4;i++)if((KitQuote(s,id,i)?.ExtraLevels??0)>0)return true;return false;}
        public static readonly int[] PassStages={3,6,9,12};
        public static List<GrowthStep> Steps(GameState s,int extraGold=0,int extraParts=0)
        {
            var result=new List<GrowthStep>();int hero=s.heroes[s.selectedHero].level;
            for(int slot=0;slot<4;slot++)
            {
                var item=slot==3?null:s.items.Find(i=>i.id==s.equipped[slot]);int level=slot==3?hero:item.level;
                if(level>=(slot==3?30:20))continue;
                int gold=level*(slot==3?500:300),parts=level*(slot==3?3:2);
                var weapon=s.items.Find(i=>i.id==s.equipped[0]);var armor=s.items.Find(i=>i.id==s.equipped[1]);
                var next=item==null?null:new Item{id=item.id,definition=item.definition,rarity=item.rarity,level=level+1};
                result.Add(new GrowthStep{id=item==null?"":item.id,slot=slot,name=slot==3?"대원":Catalog.Items[item.definition].Name,
                    level=level,gold=gold,parts=parts,affordable=(long)s.gold+extraGold>=gold&&(long)s.parts+extraParts>=parts,
                    attack=Catalog.AttackFor(s.selectedHero,slot==3?hero+1:hero,slot==0?next:weapon),
                    health=Catalog.HealthFor(s.selectedHero,slot==3?hero+1:hero,slot==1?next:armor)});
            }
            return result;
        }
        public static string ModuleEffect(Item item,int levels=0)
        {int n=item.level+levels-1;return item.definition==9?"회수 반경 +"+(25+n*3):item.definition==10?"드론 피해 +"+(20+n*2.5f).ToString("0.#")+"%":"치명타 확률 +"+(5+n*.4f).ToString("0.#")+"%";}
        public static bool CanUpgrade(GameState s){return Steps(s).Exists(p=>p.affordable);}
        public static int RepairGold(GameState s){return Math.Min(2400,600+100*s.clearedStages.Count);}
        public static int RepairParts(GameState s){return Math.Min(24,8+2*(s.clearedStages.Count/3));}
        public static int SalvageStage(GameState s)
        {
            foreach(int stage in s.clearedStages)if(Campaign.Get(stage).Mission==Mission.Survive)return stage;
            return s.clearedStages.Contains(0)?0:-1;
        }
        public static int FarmStage(GameState s)
        {
            int stage=-1;foreach(int cleared in s.clearedStages)if(cleared>stage)stage=cleared;return stage;
        }
        public static bool PassOwned(GameState s){return s.ownedCosmetics!=null&&s.ownedCosmetics.Contains(PassKey);}
        public static bool PassReady(GameState s,int tier,bool premium)
        {
            return tier>=0&&tier<PassStages.Length&&s.clearedStages.Contains(PassStages[tier]-1)&&(!premium||PassOwned(s))&&
                ((premium?s.growthPassPremium:s.growthPassFree)&(1<<tier))==0;
        }
        public static int PassGold(int tier,bool premium){return premium?3000+1000*tier:1000+500*tier;}
        public static int PassParts(int tier,bool premium){return premium?30+10*tier:10+5*tier;}
    }
    public sealed partial class Economy
    {
        public CommandResult BuyGrowthKit(string offer="growth_kit")
        {
            RefreshDay();var k=Growth.FindKit(offer);
            if(!Growth.KitUnlocked(State,k))return CommandResult.Fail("보급의 작전 완료 조건을 확인하세요.");
            if(!Growth.KitAddsGrowth(State,offer))return CommandResult.Fail("이 보급으로 추가되는 강화가 없어요. 보유 재료를 먼저 사용하거나 다른 성장 대상을 선택하세요.");
            if(State.dailyGrowthKits>=1)return CommandResult.Fail("모든 성장 보급을 합쳐 하루 1회 구매할 수 있어요.");
            if(State.gold>int.MaxValue-k.gold||State.parts>int.MaxValue-k.parts)return CommandResult.Fail("보유 재료를 먼저 사용하세요.");
            if(!PremiumWallet.Spend(State,Growth.KitPrice))return CommandResult.Fail("보석 잔액 또는 환불 조정 내역을 확인해 주세요.");
            State.gold+=k.gold;State.parts+=k.parts;State.dailyGrowthKits++;
            return CommandResult.Ok(k.name+" · 골드 "+k.gold+" · 부품 "+k.parts+"개 획득");
        }
        public CommandResult BuyGrowthKitAndUpgrade(string offer,int slot,int count,string expectedId,int expectedLevel,int expectedHero)
        {
            RefreshDay();var q=Growth.KitQuote(State,offer,slot);
            // Validate every field before spending gems or granting materials. Server revision
            // additionally protects the full wallet/loadout preview against concurrent changes.
            if(q==null||q.ExtraLevels<=0||q.after.count!=count||q.after.from!=expectedLevel||q.after.id!=expectedId||State.selectedHero!=expectedHero)
                return CommandResult.Fail("강화 대상이나 보급 효과가 바뀌었어요. 다시 확인하세요.");
            var bought=BuyGrowthKit(offer);if(!bought.Success)return bought;
            State.gold-=q.after.gold;State.parts-=q.after.parts;
            if(slot==3)State.heroes[State.selectedHero].level=q.after.to;else State.items.Find(i=>i.id==q.after.id).level=q.after.to;
            State.guideUpgrade=true;return CommandResult.Ok(q.after.name+" Lv."+q.after.from+" → "+q.after.to+" · 구매와 강화 완료");
        }
        public CommandResult BuyGrowthPass()
        {
            if(Growth.PassOwned(State))return CommandResult.Fail("이미 작전 패스를 보유하고 있어요.");
            if(!PremiumWallet.Spend(State,Growth.PassPrice))return CommandResult.Fail("보석 잔액 또는 환불 조정 내역을 확인해 주세요.");
            if(State.ownedCosmetics==null)State.ownedCosmetics=new List<string>();
            State.ownedCosmetics.Add(Growth.PassKey);return CommandResult.Ok("작전 패스 해금 · 완료한 단계도 수령할 수 있어요.");
        }
        public CommandResult ClaimGrowthPass(int tier,bool premium)
        {
            if(!Growth.PassReady(State,tier,premium))return CommandResult.Fail("작전 달성 조건과 수령 내역을 확인해 주세요.");
            State.gold+=Growth.PassGold(tier,premium);State.parts+=Growth.PassParts(tier,premium);
            if(premium)
            {
                State.growthPassPremium|=1<<tier;
                if(tier==3)
                {
                    if(Outfits.Owned(State,1)){State.gold+=6000;State.parts+=60;}
                    else{State.ownedCosmetics.Add(Outfits.Key);Outfits.Normalize(State);}
                }
            }
            else State.growthPassFree|=1<<tier;
            return CommandResult.Ok("작전 패스 보상을 받았어요.");
        }
    }
}
