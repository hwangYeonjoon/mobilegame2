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
            Ui.Button(card,"작전 패스 · 달성 보상",24,606,444,51,GrowthPassPanel,Ui.Gold,true,18);
            int farm=Growth.FarmStage(State);
            Ui.Button(card,"보석 없이 재료 모으기 · 회수 임무",24,671,444,52,SalvagePanel,Ui.Raised,true,17);
            Ui.Button(card,"보스 패턴 무료 연습",24,736,214,52,BossTrainingPanel,Ui.Raised,true,16);
            Ui.Button(card,"출격 준비",254,736,214,52,()=>{combat=null;Show("home");},Ui.Mint,true,18);
            Ui.Label(card,"반복 작전은 최초 보상 제외 · 연습은 보상 없음",24,802,444,28,14,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void GrowthSources(GrowthStep p)
        {
            var card=Dialog("강화 재료 모으기",p.name+" · "+GrowthDelta(p),590);
            Ui.Label(card,"부족한 골드 "+Math.Max(0,p.gold-State.gold)+"\n부족한 부품 "+Math.Max(0,p.parts-State.parts),24,137,444,73,25,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Button(card,"무료 회수 임무 · 재료와 소요 시간",24,238,444,53,SalvagePanel,Ui.Mint,true,18);
            Ui.Button(card,"선택 광고 · 골드 "+Growth.RepairGold(State)+" + 부품 "+Growth.RepairParts(State),24,308,444,53,()=>RequestReward("parts"),Ui.Raised,State.dailyRewardParts<2,17);
            Ui.Button(card,"성장 보급 구성 보기 · 구매 아님",24,378,444,53,GrowthKitPanel,Ui.Raised,true,17);
            Ui.Button(card,"돌아가기",24,478,444,51,GrowthPanel,Ui.Panel,true,18);
        }
        void GrowthKitPanel()
        {
            var card=Dialog("성장 보급 · 하루 1회","골드 4,500 + 공용 부품 40개 · 확정 구성",800);
            Ui.Label(card,"100 보석 사용 · 보유 "+State.gems+"개",24,139,444,37,23,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Label(card,"구매 후 가능한 강화 1회씩의 미리보기\n각 행은 별도 선택이며 모두 동시 적용되는 것은 아니에요.",24,194,444,58,16,Ui.Muted,TextAnchor.MiddleCenter);
            var steps=Growth.Steps(State,Growth.KitGold,Growth.KitParts);
            for(int i=0;i<steps.Count;i++)
            {
                var p=steps[i];Ui.Label(card,p.name+" · "+GrowthDelta(p)+"\n"+(p.affordable?"구매 후 강화 가능":"추가 재료 필요")+" · 골드 "+p.gold+" / 부품 "+p.parts,24,273+i*66,444,60,16,p.affordable?Ui.Mint:Ui.Muted);
            }
            bool can=State.dailyGrowthKits<1&&State.gems>=Growth.KitPrice&&State.gemDebt==0;
            Ui.Button(card,State.dailyGrowthKits>=1?"오늘 구매 완료":"100 보석으로 구매",24,564,444,58,()=>Change(new GameCommand("buy"){value="growth_kit"},confirmed:()=>GrowthPanel("보급 수령 · 골드 +4,500 / 부품 +40\n원하는 강화를 선택해 바로 사용할 수 있어요.")),Ui.Gold,can,20);
            Ui.Button(card,"보석을 아끼고 같은 재료 모으기",24,637,444,51,SalvagePanel,Ui.Raised,true,17);
            Ui.Button(card,"취소 · 성장 화면",24,705,444,51,GrowthPanel,Ui.Panel,true,17);
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
