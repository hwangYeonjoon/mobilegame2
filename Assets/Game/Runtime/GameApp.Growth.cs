using System;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        string GrowthDelta(GrowthStep p)
        {
            if(p.slot==2){var item=State.items.Find(i=>i.id==p.id);return Growth.ModuleEffect(item)+" → "+Growth.ModuleEffect(item,1);}
            if(p.slot==0)return "공격 "+Catalog.Attack(State)+" → "+p.attack;
            if(p.slot==1)return "체력 "+Catalog.Health(State)+" → "+p.health;
            return "공격 "+Catalog.Attack(State)+" → "+p.attack+" · 체력 "+Catalog.Health(State)+" → "+p.health;
        }
        void GrowthPanel(){GrowthPanel(null);}
        void GrowthPanel(string feedback)
        {
            var card=Dialog("다음 도전을 위한 성장",feedback??"강화 전후를 확인하고 선택하세요. 같은 조건 실력전은 능력치가 고정돼요.",870);
            var steps=Growth.Steps(State);
            for(int i=0;i<steps.Count;i++)
            {
                var p=steps[i];float y=139+i*112;
                Action select=()=>{
                    if(!p.affordable){GrowthSources(p);return;}
                    string before=GrowthDelta(p);
                    Change(p.slot==3?new GameCommand("hero_upgrade"):new GameCommand("upgrade",p.id),confirmed:()=>GrowthPanel("강화 완료 · "+before));
                };
                var row=Ui.Button(card,"성장 선택 "+p.slot,24,y,444,102,select,Ui.Background).transform;
                row.GetComponentInChildren<UnityEngine.UI.Text>().text="";
                Ui.Label(row,p.name+"  Lv."+p.level+" → "+(p.level+1),12,5,420,28,19);
                Ui.Label(row,GrowthDelta(p),12,35,420,25,16,Ui.Mint);
                Ui.Label(row,"골드 "+p.gold+" · 부품 "+p.parts,12,66,270,26,15,Ui.Muted);
                Ui.Button(row,p.affordable?"강화":"부족분",314,61,118,34,select,p.affordable?Ui.Mint:Ui.Raised,true,16);
            }
            if(steps.Count==0)Ui.Label(card,"장착 장비와 대원이 최대 레벨입니다.",24,210,444,60,22,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Button(card,"집중 강화",24,606,214,51,()=>GrowthPathPanel(),Ui.Mint,true,18);
            Ui.Button(card,"로봇 외형 공방",254,606,214,51,()=>RobotStylePanel(),Ui.Gold,true,18);
            int farm=Growth.FarmStage(State);
            Ui.Button(card,"진행하며 받는 무료 현장 보급",24,671,444,52,SalvagePanel,Ui.Raised,true,17);
            Ui.Button(card,"보스 패턴 무료 연습",24,736,214,52,BossTrainingPanel,Ui.Raised,true,16);
            Ui.Button(card,"출격 준비",254,736,214,52,()=>{combat=null;Show("home");},Ui.Mint,true,18);
            Ui.Label(card,"반복 작전은 최초 보상 제외 · 연습은 보상 없음",24,802,444,28,14,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void GrowthSources(GrowthStep p)
        {
            var card=Dialog("강화 재료 모으기",p.name+" · "+GrowthDelta(p),590);
            Ui.Label(card,"부족한 골드 "+Math.Max(0,p.gold-State.gold)+"\n부족한 부품 "+Math.Max(0,p.parts-State.parts),24,137,444,73,25,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Button(card,"무료 현장 보급 · 전투 5분 합산",24,238,444,53,SalvagePanel,Ui.Mint,true,18);
            Ui.Button(card,"선택 광고 · 골드 "+Growth.RepairGold(State)+" + 부품 "+Growth.RepairParts(State),24,308,444,53,()=>RequestReward("parts"),Ui.Raised,State.dailyRewardParts<2,17);
            Ui.Button(card,"성장 보급 구성 보기 · 구매 아님",24,378,444,53,GrowthKitPanel,Ui.Raised,true,17);
            Ui.Button(card,"돌아가기",24,478,444,51,GrowthPanel,Ui.Panel,true,18);
        }
        void GrowthKitPanel()
        {
            var kit=Growth.CurrentKit(State);
            var card=Dialog(kit.name+" · 성장 경로 선택","골드 "+kit.gold.ToString("N0")+" · 부품 "+kit.parts+" · 100 보석\n모든 보급 합쳐 하루 1회 · 원하는 경로 하나를 선택",850);
            Ui.Label(card,"보유 재료로 가능한 강화와 구매 효과를 비교해요.",24,142,444,32,16,Ui.Muted);
            for(int slot=0;slot<4;slot++)
            {
                int choice=slot;var q=Growth.KitQuote(State,kit.id,slot);float y=190+slot*112;
                string label=q==null?new[]{"공격 · 무기","생존 · 방어구","모듈","대원"}[slot]+" · 최대 레벨":q.after.name+" · 현재 Lv."+q.after.from+"\n보유 재료로 Lv."+q.before.to+" → 구매하면 Lv."+q.after.to+"\n"+(q.ExtraLevels>0?"구매로 추가 "+q.ExtraLevels+"단계 · 효과 확인":"추가 강화 없음 · 보유 재료 먼저 사용");
                Ui.Button(card,label,24,y,444,101,()=>GrowthKitConfirm(kit.id,choice),q!=null&&q.ExtraLevels>0?Ui.Raised:Ui.Background,q!=null&&q.ExtraLevels>0&&State.dailyGrowthKits<1,17);
            }
            Ui.Button(card,State.dailyGrowthKits>0?"오늘 구매 완료 · 보유 재료 강화":"보유 재료로 집중 강화",24,651,444,48,()=>GrowthPathPanel(),Ui.Mint,true,17);
            Ui.Button(card,"무료 보급·구간별 상품 구성",24,718,444,47,GrowthKitCatalog,Ui.Raised,true,17);
            Ui.Button(card,"돌아가기",24,784,444,44,GrowthPanel,Ui.Raised,true,17);
        }
        string KitPathEffect(GrowthPath p)
        {
            if(p.slot==2)return Growth.ModuleEffect(State.items.Find(i=>i.id==p.id),p.to-p.from);
            return p.slot==0?"공격 "+p.attack:p.slot==1?"체력 "+p.health:"공격 "+p.attack+" · 체력 "+p.health;
        }
        void GrowthKitConfirm(string offer,int slot)
        {
            var q=Growth.KitQuote(State,offer,slot);if(q==null||q.ExtraLevels<=0){GrowthKitPanel();return;}
            int hero=State.selectedHero,retry=selectedStage;
            if(combat!=null&&combat.Ended&&!combat.Won&&string.IsNullOrEmpty(combat.Mode))retry=combat.Stage;
            string from=slot==2?Growth.ModuleEffect(State.items.Find(i=>i.id==q.after.id)):slot==0?"공격 "+Catalog.Attack(State):slot==1?"체력 "+Catalog.Health(State):"공격 "+Catalog.Attack(State)+" · 체력 "+Catalog.Health(State);
            string to=KitPathEffect(q.after);
            var card=Dialog("보급 구매와 강화 확인",q.offer.name+" · "+q.after.name+"\n골드 "+q.offer.gold.ToString("N0")+" · 부품 "+q.offer.parts+" 지급",780);
            Ui.Label(card,"보유 재료만 사용: Lv."+q.before.from+" → "+q.before.to+"\n"+KitPathEffect(q.before),24,148,444,65,19,Ui.Muted);
            Ui.Label(card,"구매 후 사용: Lv."+q.after.from+" → "+q.after.to+"\n"+to+"\n이 구매로 추가 강화 "+q.ExtraLevels+"단계",24,237,444,100,21,Ui.Mint);
            Ui.Label(card,"기존 재료와 받은 재료를 합쳐 사용해요.\n강화 비용: 골드 "+q.after.gold.ToString("N0")+" · 부품 "+q.after.parts+"\n강화 후 잔액: 골드 "+(State.gold+q.offer.gold-q.after.gold).ToString("N0")+" · 부품 "+(State.parts+q.offer.parts-q.after.parts),24,362,444,96,17,Ui.Ink);
            Ui.Label(card,"보석 "+State.gems+" → "+(State.gems>=100?(State.gems-100).ToString():"부족")+"\n선택한 경로만 강화 · 승리 보장 없음",24,481,444,60,17,Ui.Muted);
            Ui.Button(card,"100 보석 구매 · "+q.after.count+"단계 강화",24,566,444,56,()=>Change(new GameCommand("growth_kit_upgrade",q.after.id){value=offer,stage=slot,count=q.after.count,level=q.after.from,hero=hero},confirmed:()=>GrowthKitApplied(q,from,to,retry)),Ui.Gold,State.gems>=100&&State.gemDebt==0&&State.dailyGrowthKits<1,18);
            Ui.Button(card,"취소",24,649,214,47,GrowthKitPanel,Ui.Raised,true,17);
            Ui.Button(card,"보석 획득·충전",254,649,214,47,()=>GemSources(100,()=>GrowthKitConfirm(offer,slot)),Ui.Raised,true,16);
        }
        void GrowthKitApplied(GrowthKitQuote q,string from,string to,int retry)
        {
            var card=Dialog("강화 완료 · 다시 도전하세요",q.after.name+" Lv."+q.after.from+" → "+q.after.to,610);
            Ui.Label(card,from+"\n↓\n"+to,24,157,444,115,26,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Label(card,"보급 구매로 추가 "+q.ExtraLevels+"단계 가능해졌어요.\n남은 골드 "+State.gold.ToString("N0")+" · 부품 "+State.parts,24,303,444,65,18,Ui.Muted,TextAnchor.MiddleCenter);
            int stage=Mathf.Clamp(retry,0,State.unlockedStage);
            Ui.Button(card,"이 장비로 "+(stage+1)+"작전 출격",24,412,444,58,()=>{combat=null;CloseModal();StartBattle(stage);},Ui.Gold,true,19);
            Ui.Button(card,"다른 성장 확인",24,495,214,48,()=>GrowthPathPanel(),Ui.Raised,true,17);
            Ui.Button(card,"출격 준비",254,495,214,48,()=>{combat=null;Show("home");},Ui.Mint,true,17);
        }
        void GrowthKitCatalog()
        {
            var card=Dialog("성장 보급 · 공개 구성","가격은 모두 100 보석 · 종류를 합쳐 하루 1회\n작전 진행으로 더 큰 고정 구성이 열립니다.",700);
            for(int i=0;i<Growth.Kits.Length;i++)
            {
                var k=Growth.Kits[i];Ui.Label(card,k.name+" · "+(k.clear==0?"처음부터":k.clear+"작전 완료 후")+"\n골드 "+k.gold.ToString("N0")+" · 부품 "+k.parts,24,148+i*103,444,75,20,Growth.KitUnlocked(State,k)?Ui.Mint:Ui.Muted);
            }
            Ui.Label(card,"무료 현장 보급: 전투 합산 5분, 하루 한 번\n골드 4,500 · 부품 40 · 구매 여부와 무관\n추가 강화가 없으면 구매할 수 없어요.",24,469,444,88,17,Ui.Ink);
            Ui.Button(card,"무료 현장 보급",24,588,214,50,SalvagePanel,Ui.Mint,true,17);
            Ui.Button(card,"보급 효과 비교",254,588,214,50,GrowthKitPanel,Ui.Raised,true,17);
        }
        void ClaimGrowthReward(int tier,bool premium)
        {
            bool had=Outfits.Owned(State,1);
            Change(new GameCommand("growth_claim"){stage=tier,enabled=premium},confirmed:()=>{
                if(premium&&tier==3&&!had&&Outfits.Owned(State,1))OutfitAcquired(1);else GrowthPassPanel();
            });
        }
        void GrowthPassPanel()
        {
            bool owned=Growth.PassOwned(State);
            var card=Dialog("작전 성장 패스","기간 만료 없음 · 작전 3/6/9/12 달성 보상\n무료 보상은 구매 없이 받을 수 있어요.",900);
            var list=Ui.Scroll(card,24,145,444,414,4*143);
            for(int i=0;i<4;i++)
            {
                int tier=i;var row=Ui.Box(list,0,i*143,444,133,Ui.Background).transform;
                Ui.Label(row,"작전 "+Growth.PassStages[i]+" 완료",12,5,420,28,21,Ui.Ink);
                for(int t=0;t<2;t++)
                {
                    bool premium=t==1;float y=39+t*44;
                    string text=(premium?"추가":"무료")+" · 골드 "+Growth.PassGold(i,premium)+" / 부품 "+Growth.PassParts(i,premium);
                    Ui.Label(row,text,12,y,314,30,16,premium?Ui.Gold:Ui.Mint);
                    bool claimed=((premium?State.growthPassPremium:State.growthPassFree)&(1<<i))!=0;
                    Ui.Button(row,claimed?"수령 완료":"받기",333,y,99,33,()=>ClaimGrowthReward(tier,premium),Ui.Raised,Growth.PassReady(State,i,premium),15);
                }
            }
            Ui.Label(card,"추가 트랙 총 골드 18,000 · 부품 180\n12작전 추가 보상: 중장비 정비사 의상·공격·지원 연출\n이미 가진 의상은 골드 6,000·부품 60으로 대체",24,577,444,88,16,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Button(card,"최종 의상 · 연출 미리보기",24,676,444,45,()=>OutfitRoom(1),Ui.Raised,true,17);
            Ui.Button(card,owned?"추가 트랙 보유 중":"추가 트랙 900 보석 · 구성 확인",24,734,444,53,()=>{
                var confirm=Dialog("작전 패스 구매 확인","900 보석 사용 · 현재 "+State.gems+"개 보유\n완료한 단계도 수령 가능 · 보상은 자동 강화되지 않아요.",420);
                Ui.Button(confirm,"900 보석으로 해금",24,172,444,58,()=>Change(new GameCommand("buy"){value="growth_pass"},confirmed:GrowthPassPanel),Ui.Gold,State.gems>=Growth.PassPrice&&State.gemDebt==0,20);
                Ui.Button(confirm,"무료 보석 · 부족분 확인",24,244,444,51,()=>GemSources(Growth.PassPrice,GrowthPassPanel),Ui.Raised,true,17);
                Ui.Button(confirm,"취소",24,309,444,51,GrowthPassPanel,Ui.Panel,true,18);
            },Ui.Gold,!owned,18);
            Ui.Button(card,"받은 재료로 강화하기",24,806,444,51,GrowthPanel,Ui.Mint,true,18);
        }
    }
}
