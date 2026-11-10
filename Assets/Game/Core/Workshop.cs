using System;
using System.Collections.Generic;

namespace ScrapGuard.Core
{
    public enum DamageCause { None, Contact, Charge, Artillery, Explosion, Poison, Rail, Laser, Boss, Timeout, Abandoned, Electricity, Heat }
    [Serializable] public sealed class BattleReport
    {
        public float[] damage = new float[13];
        public int lastCause,lastSource; public float lastHitTime,lastHitDamage;
        public int cause, mods, task, taskChoice, fieldModule, hero;
        public bool taskCompleted, revived;
        public int machines, waves, evolution, skillEvolutions;
        public List<RobotRecord> robots=new List<RobotRecord>();
        public int[] skills = new int[12];
        public float bossRemaining = -1;
    }
    [Serializable] public sealed class RegionProgress
    {
        public int region, wins, blueprints, bosses;
    }
    public static class Workshop
    {
        public const int BlueprintCost=8;
        public static readonly string[] ModNames={"자력 분쇄기","연쇄 전격 드론","회전 리벳 포탑","파열 리벳","수리 드론","냉각 자력장"};
        public static readonly int[] MainSkills={1,2,0,0,3,2}, SubSkills={7,3,5,4,6,7};
        public static readonly int[] MainLevels={3,4,3,5,2,4}, SubLevels={2,1,3,2,3,2};
        public static readonly string[] ModDetails={"톱날이 주변 적을 끌어당겨 분쇄", "드론에서 적 사이로 전격이 연쇄", "플레이어 주위를 돌며 자동 사격하는 포탑","기본 탄환 적중 시 주변에 폭발 피해","10초마다 체력 3% 수리 · 체력 8% 보호막 보충","코일 범위 +40 · 범위 내 적 1.8초 둔화"};
        public static readonly string[] CauseNames={"피해 없음","접촉","돌진","포격","자폭","오염 지대","철로 포격","레이저","보스 공격","시간 초과","작전 중단","전기 격자","용광로 분출"};
        public static readonly string[] Advice={"스킬 조합을 완성해 다음 작전에 도전하세요.","적에게 둘러싸이기 전에 빈 공간으로 이동하세요.","돌진 준비 표시가 보이면 옆으로 피하세요.","포격 예고 원 밖으로 이동한 뒤 공격하세요.","자폭 준비 중인 적에게서 거리를 벌리세요.","초록색 오염 지대 밖에서 적을 유인하세요.","붉게 표시된 철로를 벗어나세요.","레이저가 예고된 가로 띠 밖으로 이동하세요.","보스 예고 범위를 먼저 피하고 공격하세요.","공격 스킬 조합을 완성해 보스 화력을 높여 보세요.","다음 출격에서 진행을 이어가세요.","전기 격자 예고가 뜨면 옆 통로로 이동하세요.","용광로 분출이 예고된 통로를 벗어나세요."};
        public static int ModMask(int[] skills)
        {
            int mask=0;for(int i=0;i<ModNames.Length;i++)if(skills[MainSkills[i]]>=MainLevels[i] && skills[SubSkills[i]]>=SubLevels[i])mask|=1<<i;return mask;
        }
        public static int MainCause(BattleReport report)
        {
            if(report==null)return 0;
            if(report.cause>0)return Math.Min(report.cause,CauseNames.Length-1);
            int cause=0;float max=0;
            if(report.damage!=null)for(int i=1;i<Math.Min(report.damage.Length,CauseNames.Length);i++)if(report.damage[i]>max){max=report.damage[i];cause=i;}
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
                if(Campaign.IsRegionFinal(definition)){progress.bosses++;progress.blueprints++;}
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
            if(combat==null || !combat.Ended || combat.Challenge || combat.BossPractice || combat.ApexBattle)return CommandResult.Fail("작전 결과가 올바르지 않아요.");
            var result=FinishRun(combat.RunId,combat.Stage,combat.Won,combat.Kills,combat.Elapsed);
            if(!result.Success)return result;
            var receipt=State.receipts.Find(r=>r.id==combat.RunId);receipt.report=combat.Report();
            RobotAssembly.Record(State,receipt.report.robots,combat.Stage);
            if(combat.TaskBonusGold>0)
            {
                receipt.gold+=combat.TaskBonusGold;receipt.parts+=combat.TaskBonusParts;
                State.gold+=combat.TaskBonusGold;State.parts+=combat.TaskBonusParts;
                State.lastResultGold+=combat.TaskBonusGold;
            }
            StarterGuide.Record(State,combat);
            State.enemyDiscoveries|=combat.DefeatedRoles;
            if(combat.Won)
            {
                var region=Workshop.Region(State,combat.Definition.Region);
                region.wins++;receipt.blueprints=Campaign.IsRegionFinal(combat.Definition)?2:1;region.blueprints+=receipt.blueprints;
                if(Campaign.IsRegionFinal(combat.Definition))region.bosses++;
                if(combat.Definition.Mission==Mission.Eliminate)State.bossDiscoveries|=1<<combat.Definition.Region;
            }
            return result;
        }
        public CommandResult SetCollectionTarget(int region)
        {
            if(region < -1 || region>=Campaign.Regions.Length)return CommandResult.Fail("없는 지역이에요.");
            State.collectionTarget=region;return CommandResult.Ok(region<0?"수집 목표를 해제했어요.":Campaign.Regions[region]+" 도색을 수집 목표로 정했어요.");
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
