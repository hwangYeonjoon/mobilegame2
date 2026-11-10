using System;
using System.Collections.Generic;

namespace ScrapGuard.Core
{
    [Serializable] public class GameCommand
    {
        public string requestId = Guid.NewGuid().ToString("N");
        public long revision;
        public string type, id, other, value, mode, supportId;
        public int hero, title, frame, skin, stage, kills, seed, rulesVersion;
        public float seconds;
        public bool enabled, won, quick;
        public List<ReplayStep> replay;
        public GameCommand() { }
        public GameCommand(string type, string id = null) { this.type = type; this.id = id; rulesVersion = Combat.RulesVersion; }
    }
    [Serializable] public class BattleTicket { public string id; public int stage, seed, rulesVersion; public bool quick; public string mode, week; public bool hasSupport; public SupportSnapshot support; public SupportSnapshot GetSupport() { return hasSupport ? support : null; } }
    [Serializable] public class ServerReply
    {
        public bool success;
        public string message, playerId, serverTime, code;
        public ReleasePolicy release;
        public ContentCatalog content;
        public bool metricsConsent;
        public long revision;
        public GameState state;
        public BattleTicket ticket;
    }
    [Serializable] public class GuestRequest { public string secret, inviteCode; }
    [Serializable] public class ReplayStep
    {
        public int x, y, frames, encounter, assembly, robot, robotPlan, skill = -1;
        public bool ultimate, support, revive, machine, robotUpgrade;
    }
    public static class CommandDispatcher
    {
        public static CommandResult Apply(Economy e, GameCommand c, bool development)
        {
            switch (c.type)
            {
                case "attendance": return e.Attend();
                case "missions": return e.ClaimMissions();
                case "growth_claim": return e.ClaimGrowthPass(c.stage,c.enabled);
                case "buy": return e.Buy(c.value);
                case "equip": return e.Equip(c.id);
                case "lock": return e.Lock(c.id);
                case "upgrade": return e.Upgrade(c.id);
                case "transfer": return e.Transfer(c.id, c.other);
                case "sell": return e.Sell(c.id);
                case "hero_recruit": return e.RecruitHero(c.hero);
                case "hero_select": return e.SelectHero(c.hero);
                case "hero_upgrade": return e.UpgradeHero();
                case "rename": return Social.Rename(e.State, c.value);
                case "customize": return Social.Customize(e.State, c.hero, c.title, c.frame, c.skin);
                case "showcase_theme": return ShowcaseThemes.Equip(e.State,c.skin);
                case "outfit_clear": return Outfits.Clear(e.State,c.hero);
                case "outfit_target": return Outfits.Target(e.State,c.skin);
                case "outfit_equip": return Outfits.Equip(e.State,c.skin);
                case "guide_seen": e.State.guideIntroSeen=true; return CommandResult.Ok("안내는 정비소에서 다시 볼 수 있어요.");
                case "cosmetic_buy": return Social.Buy(e.State, c.value);
                case "robot_showcase": return RobotAssembly.Showcase(e.State,c.stage);
                case "showcase_equipment": e.State.showcaseItems = new List<string>(e.State.equipped); return CommandResult.Ok("현재 장비를 전시했어요.");
                case "visibility": e.State.publicProfile = c.enabled; return CommandResult.Ok(c.enabled ? "프로필을 공개했어요." : "프로필을 비공개로 바꿨어요.");
                case "sound": e.State.sound = c.enabled; return CommandResult.Ok("효과음 설정 저장");
                case "region_target": return e.SetCollectionTarget(c.stage);
                case "region_craft": return e.CraftRegionSkin(c.stage);
                case "core_exchange": return e.ExchangeCore(c.value);
                case "salvage_claim": return e.ClaimSalvage();
                case "robot_week_claim": return e.ClaimRobotWeek(c.value);
                case "weekly_claim": return e.ClaimWeekly(c.value,c.stage);
                case "battle_cancel": e.State.activeRun = e.State.activeMode = e.State.activeWeek = ""; return CommandResult.Ok("중단된 작전을 종료했어요.");
                case "battle_start":
                    if (!development) return CommandResult.Fail("서버 작전 발급이 필요해요.");
                    if(c.mode==BossTraining.Mode)return e.BeginBossPractice(c.id,c.stage);
                    if (Endgame.IsChallenge(c.mode)) return e.BeginChallenge(c.id,c.mode);
                    if (!string.IsNullOrEmpty(c.mode)) return CommandResult.Fail("없는 전투 모드예요.");
                    if (c.quick) e.State.testProgress = true;
                    return e.BeginRun(c.id, c.stage);
                case "battle_finish":
                    if (!development) return CommandResult.Fail("전투 입력 검증이 필요해요.");
                    return e.FinishRun(c.id, c.stage, c.won, c.kills, c.seconds);
                case "ad_skip_reward":
                    if(!e.State.adSkip)return CommandResult.Fail("광고 스킵 권리가 필요해요.");
                    return e.ClaimDemoReward(c.value,c.id);
                case "demo_reward":
                    if (!development) return CommandResult.Fail("실제 광고 보상은 아직 연결되지 않았어요.");
                    e.State.testProgress = true; return e.ClaimDemoReward(c.value, c.id);
                case "demo_gems":
                    if (!development) return CommandResult.Fail("서버 계정에는 테스트 재화를 지급할 수 없어요.");
                    e.State.testProgress = true; e.State.gems += 1200; return CommandResult.Ok("테스트 보석 +1,200");
                case "demo_materials":
                    if (!development) return CommandResult.Fail("서버 계정에는 테스트 재화를 지급할 수 없어요.");
                    e.State.testProgress = true; e.State.gold += 10000; e.State.parts += 100; return CommandResult.Ok("테스트 재화 지급 완료");
                case "demo_skip":
                    if (!development) return CommandResult.Fail("테스트 전용 기능이에요.");
                    e.State.testProgress = true; e.State.demoAdSkip = c.enabled; return CommandResult.Ok("광고 스킵 체험 설정 저장");
                default: return CommandResult.Fail("지원하지 않는 요청이에요.");
            }
        }
    }
    public sealed class ReplayRecorder
    {
        public const float Step = 1f / 30f;
        public readonly List<ReplayStep> Steps = new List<ReplayStep>();
        public V2 Move(V2 move)
        {
            int x = (int)Math.Round(Math.Max(-1, Math.Min(1, move.x)) * 100);
            int y = (int)Math.Round(Math.Max(-1, Math.Min(1, move.y)) * 100);
            var last = Steps.Count > 0 ? Steps[Steps.Count - 1] : null;
            if (last != null && last.robotPlan==0 && !last.machine && !last.revive && !last.ultimate && !last.support && last.encounter==0 && last.assembly==0 && last.skill == -1 && last.x == x && last.y == y && last.frames < 120) last.frames++;
            else Steps.Add(new ReplayStep { x = x, y = y, frames = 1, skill = -1 });
            return new V2(x / 100f, y / 100f);
        }
        public void RobotPlan(int design,bool upgrade){Steps.Add(new ReplayStep{robotPlan=design,robotUpgrade=upgrade});}
        public void Machine(int design=0){Steps.Add(new ReplayStep{machine=true,robot=design});}
        public void Revive(){Steps.Add(new ReplayStep{revive=true});}
        public void Assemble(int module){Steps.Add(new ReplayStep{assembly=module});}
        public void Encounter(int choice){Steps.Add(new ReplayStep{encounter=choice});}
        public void Support() { Steps.Add(new ReplayStep { support = true }); }
        public void Ultimate() { Steps.Add(new ReplayStep { ultimate = true }); }
        public void Choose(int skill) { Steps.Add(new ReplayStep { skill = skill }); }
        public static Combat Verify(GameState snapshot, int stage, string id, int seed, List<ReplayStep> steps, double elapsedSeconds, string mode = "", SupportSnapshot support = null, bool reviveAllowed = false)
        {
            if (steps == null || steps.Count > 18000) throw new ArgumentException("전투 기록 크기가 올바르지 않아요.");
            int frames = 0;
            foreach (var step in steps)
            {
                if (step == null || step.robotPlan<0 || step.robotPlan>9 || (step.robotUpgrade&&step.robotPlan==0) || step.robot<0 || step.robot>9 || (!step.machine&&step.robot!=0) || step.assembly<0 || step.assembly>4 || step.encounter<0 || step.encounter>2 || step.skill < -1 || step.skill > 11 || step.frames < 0 || step.frames > 120 || Math.Abs((long)step.x) > 100 || Math.Abs((long)step.y) > 100)
                    throw new ArgumentException("전투 입력이 올바르지 않아요.");
                if(step.robotPlan!=0&&(step.machine||step.robot!=0||step.revive||step.assembly!=0||step.encounter!=0||step.support||step.ultimate||step.skill!=-1||step.frames!=0||step.x!=0||step.y!=0))throw new ArgumentException("잘못된 로봇 설계 입력이에요.");
                if(step.machine && (step.revive || step.assembly!=0 || step.encounter!=0 || step.support || step.ultimate || step.skill!=-1 || step.frames!=0 || step.x!=0 || step.y!=0))throw new ArgumentException("잘못된 합체 입력이에요.");
                if(step.revive && (step.assembly!=0 || step.encounter!=0 || step.support || step.ultimate || step.skill!=-1 || step.frames!=0 || step.x!=0 || step.y!=0))throw new ArgumentException("잘못된 부활 입력이에요.");
                if(step.assembly!=0 && (step.encounter!=0 || step.support || step.ultimate || step.skill!=-1 || step.frames!=0 || step.x!=0 || step.y!=0))throw new ArgumentException("잘못된 개조 선택이에요.");
                if(step.encounter!=0 && (step.support || step.ultimate || step.skill!=-1 || step.frames!=0 || step.x!=0 || step.y!=0))throw new ArgumentException("잘못된 현장 선택이에요.");
                if (step.support && (step.ultimate || step.skill != -1 || step.frames != 0 || step.x != 0 || step.y != 0)) throw new ArgumentException("잘못된 지원 호출이에요.");
                if (step.ultimate && (step.skill != -1 || step.frames != 0 || step.x != 0 || step.y != 0)) throw new ArgumentException("잘못된 필살기 입력이에요.");
                if (step.skill >= 0 && (step.frames != 0 || step.x != 0 || step.y != 0)) throw new ArgumentException("잘못된 스킬 입력이에요.");
                if (step.robotPlan==0 && !step.machine && !step.revive && !step.ultimate && !step.support && step.encounter==0 && step.assembly==0 && step.skill == -1 && step.frames < 1) throw new ArgumentException("비어 있는 전투 입력이에요.");
                frames += step.frames;
            }
            if (frames > 18000 || frames * Step > elapsedSeconds + 2) throw new ArgumentException("실제 경과 시간보다 긴 전투 기록이에요.");
            var combat = new Combat(snapshot, stage, id, seed, false, mode, support);
            foreach (var step in steps)
            {
                if (combat.Ended && !step.revive) throw new ArgumentException("종료된 전투 뒤에 입력이 있어요.");
                if(step.robotPlan!=0){if(!combat.PlanRobot(step.robotPlan,step.robotUpgrade))throw new ArgumentException("부품이 부족하거나 선택할 수 없는 설계예요.");}
                else if(step.machine){if(!combat.ActivateMachine(step.robot))throw new ArgumentException("합체 준비가 되지 않았어요.");}
                else if(step.revive){if(!reviveAllowed || !combat.Revive())throw new ArgumentException("확인된 부활 권리가 없거나 부활할 수 없는 상태예요.");}
                else if(step.assembly!=0){if(!combat.Assemble(step.assembly))throw new ArgumentException("장착할 수 없는 현장 개조예요.");}
                else if(step.encounter!=0){if(!combat.ChooseTask(step.encounter))throw new ArgumentException("선택할 수 없는 현장 임무예요.");}
                else if (step.support) { if (!combat.UseSupport()) throw new ArgumentException("사용할 수 없는 지원 호출이에요."); }
                else if (step.ultimate) { if (!combat.UseUltimate()) throw new ArgumentException("충전되지 않은 필살기 입력이에요."); }
                else if (step.skill >= 0) { if (!combat.Choose(step.skill)) throw new ArgumentException("선택할 수 없는 스킬이에요."); }
                else for (int i = 0; i < step.frames; i++)
                {
                    if (combat.Ended || combat.Choosing) throw new ArgumentException("전투 상태와 입력이 일치하지 않아요.");
                    combat.Tick(Step, new V2(step.x / 100f, step.y / 100f));
                }
            }
            if (!combat.Ended) combat.Abandon();
            return combat;
        }
    }
}
