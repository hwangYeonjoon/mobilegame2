using System;
using System.Collections.Generic;

namespace ScrapGuard.Core
{
    public enum DamageCause { None, Contact, Charge, Artillery, Explosion, Poison, Rail, Laser, Boss, Timeout, Abandoned }
    [Serializable] public sealed class BattleReport
    {
        public float[] damage = new float[9];
        public int cause, mods;
        public int[] skills = new int[8];
        public float bossRemaining = -1;
    }
    [Serializable] public sealed class RegionProgress
    {
        public int region, wins, blueprints, bosses;
    }
    public static class Workshop
    {
        public const int BlueprintCost=8;
        public static readonly string[] ModNames={"자력 분쇄기","연쇄 전격 드론","회전 리벳 포탑"};
        public static readonly int[] MainSkills={1,2,0}, SubSkills={7,3,5};
        public static readonly string[] ModDetails={"톱날이 주변 적을 끌어당겨 분쇄", "드론에서 적 사이로 전격이 연쇄", "플레이어 주위를 돌며 자동 사격하는 포탑"};
        public static readonly string[] CauseNames={"피해 없음","접촉","돌진","포격","자폭","오염 지대","철로 포격","레이저","보스 공격","시간 초과","작전 중단"};
        public static readonly string[] Advice={"스킬 조합을 완성해 다음 작전에 도전하세요.","적에게 둘러싸이기 전에 빈 공간으로 이동하세요.","돌진 준비 표시가 보이면 옆으로 피하세요.","포격 예고 원 밖으로 이동한 뒤 공격하세요.","자폭 준비 중인 적에게서 거리를 벌리세요.","초록색 오염 지대 밖에서 적을 유인하세요.","붉게 표시된 철로를 벗어나세요.","레이저가 예고된 가로 띠 밖으로 이동하세요.","보스 예고 범위를 먼저 피하고 공격하세요.","공격 스킬 조합을 완성해 보스 화력을 높여 보세요.","다음 출격에서 진행을 이어가세요."};
        public static int ModMask(int[] skills)
        {
            int mask=0;for(int i=0;i<3;i++)if(skills[MainSkills[i]]>=3 && skills[SubSkills[i]]>=2)mask|=1<<i;return mask;
        }
        public static int MainCause(BattleReport report)
        {
            if(report==null)return 0;
            if(report.cause>0)return Math.Min(report.cause,CauseNames.Length-1);
            int cause=0;float max=0;
            if(report.damage!=null)for(int i=1;i<Math.Min(report.damage.Length,9);i++)if(report.damage[i]>max){max=report.damage[i];cause=i;}
            return cause;
        }
        public static void Normalize(GameState state)
        {
            if(state.ownedCosmetics==null)state.ownedCosmetics=new List<string>();
            if(state.regions==null)state.regions=new List<RegionProgress>();
            for(int region=0;region<Campaign.Regions.Length;region++)if(!state.regions.Exists(p=>p.region==region))state.regions.Add(new RegionProgress {region=region});
            if(state.collectionVersion>=1)return;
            // Credit only known unique clears once; never invent historical repeat wins.
            foreach(int stage in new HashSet<int>(state.clearedStages))if(stage>=0 && stage<Campaign.Count)
            {
                var definition=Campaign.Get(stage);var progress=Region(state,definition.Region);
                progress.wins++;progress.blueprints++;
                if(definition.Layout==5){progress.bosses++;progress.blueprints++;}
            }
            state.collectionVersion=1;
        }
        public static RegionProgress Region(GameState state,int region) { return state.regions.Find(p=>p.region==region); }
        public static string SkinKey(int region){return "region_skin:"+region;}
    }
    public sealed partial class Economy
    {
        // Both app-local settlement and server replay use the same report and collection logic.
        public CommandResult FinishCampaign(Combat combat)
        {
            if(combat==null || !combat.Ended || combat.Challenge)return CommandResult.Fail("작전 결과가 올바르지 않아요.");
            var result=FinishRun(combat.RunId,combat.Stage,combat.Won,combat.Kills,combat.Elapsed);
            if(!result.Success)return result;
            var receipt=State.receipts.Find(r=>r.id==combat.RunId);receipt.report=combat.Report();
            State.enemyDiscoveries|=combat.DefeatedRoles;
            if(combat.Won)
            {
                var region=Workshop.Region(State,combat.Definition.Region);
                region.wins++;receipt.blueprints=combat.Definition.Layout==5?2:1;region.blueprints+=receipt.blueprints;
                if(combat.Definition.Layout==5)region.bosses++;
                if(combat.Definition.Mission==Mission.Eliminate)State.bossDiscoveries|=1<<combat.Definition.Region;
            }
            return result;
        }
        public CommandResult CraftRegionSkin(int region)
        {
            if(region<0 || region>=Campaign.Regions.Length)return CommandResult.Fail("없는 지역이에요.");
            string key=Workshop.SkinKey(region);
            if(State.ownedCosmetics.Contains(key))return CommandResult.Fail("이미 제작한 도색이에요.");
            var progress=Workshop.Region(State,region);
            if(progress.blueprints<Workshop.BlueprintCost)return CommandResult.Fail("이 지역 설계도 8개가 필요해요.");
            progress.blueprints-=Workshop.BlueprintCost;State.ownedCosmetics.Add(key);
            return CommandResult.Ok(Social.Skins[region+3]+" 제작 완료 · 전시실에서 적용하세요.");
        }
    }
}
