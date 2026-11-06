using System;
using System.Collections.Generic;
using ScrapGuard.Core;
namespace ScrapGuard
{
    public sealed class ProgressTarget
    {
        public string Kind,Title,Detail;
        public int Id,Current,Required;
        public bool Ready;
    }
    // Derived from authoritative saved progress. Viewing goals never grants rewards or edits a save.
    public static class ProgressTargets
    {
        public static List<ProgressTarget> Build(GameState state)
        {
            var result=new List<ProgressTarget>();
            int next=0;while(next<Campaign.Count&&state.clearedStages.Contains(next))next++;
            if(next<Campaign.Count)result.Add(new ProgressTarget{Kind="stage",Id=next,Title="다음 작전 "+(next+1).ToString("D2")+" · "+Campaign.Get(next).Name,
                Detail="최초 완료 보석 50개 · 보상은 작전 상세에서 확인",Current=state.clearedStages.Count,Required=Campaign.Count});
            for(int i=1;i<HeroCatalog.Count;i++)
            {
                if(state.heroes[i].unlocked)continue;
                int stage=HeroCatalog.All[i].RecruitStage;
                int current=i==1?state.gems:i==2?state.attendanceCount:state.clearedStages.FindAll(s=>s<=stage).Count;
                int required=i==1?1200:i==2?7:stage+1;
                bool ready=i==1?current>=required&&state.gemDebt==0:i==2?false:HeroCatalog.CanRecruit(state,i);
                result.Add(new ProgressTarget{Kind="hero",Id=i,Title=Catalog.HeroNames[i]+" 모집",Current=current,Required=required,Ready=ready,
                    Detail=i==1?"보유 보석 / 1,200 · 무료 보석 사용 가능":i==2?"누적 출석 / 7일 · 출석 보상 수령 시 합류":"작전 "+(stage+1).ToString("D2")+" 완료 후 무료 모집"});
            }
            int region=state.collectionTarget;
            if(region<0||region>=Campaign.Regions.Length||ScrapGuard.Core.Social.HasSkin(state,region+3))
            {region=-1;for(int r=0;r<Campaign.Regions.Length;r++)if(!ScrapGuard.Core.Social.HasSkin(state,r+3)){region=r;break;}}
            if(region>=0)
            {
                int blueprints=state.regions?.Find(p=>p.region==region)?.blueprints??0;
                result.Add(new ProgressTarget{Kind="region",Id=region,Title=Campaign.Regions[region]+" 전용 도색",Current=blueprints,Required=Workshop.BlueprintCost,Ready=blueprints>=Workshop.BlueprintCost,
                    Detail="지역 설계도 / 8개 · 성공 1개, 지역 마지막 작전 2개"});
            }
            int outfit=state.outfitTarget;
            if(!Outfits.Valid(outfit)||Outfits.Owned(state,outfit))
            {outfit=0;for(int id=1;id<=Outfits.Count;id++)if(Outfits.Hero(id)==state.selectedHero&&!Outfits.Owned(state,id)){outfit=id;break;}}
            if(outfit>0)result.Add(new ProgressTarget{Kind="outfit",Id=outfit,Title=(outfit==state.outfitTarget?"내 수집 목표 · ":"의상 둘러보기 · ")+Outfits.Label(outfit),Current=state.gems,Required=Outfits.Price,
                Ready=state.gems>=Outfits.Price&&state.gemDebt==0&&state.heroes[Outfits.Hero(outfit)].unlocked,
                Detail=!state.heroes[Outfits.Hero(outfit)].unlocked?Outfits.HeroName(outfit)+" 모집 필요 · 의상은 능력치 변화 없음":"보유 보석 / 600 · 외형 전용 · 판매 여부는 의상실에서 확인"});
            return result;
        }
    }
}
