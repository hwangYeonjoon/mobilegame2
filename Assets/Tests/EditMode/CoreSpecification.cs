using System;
using ScrapGuard.Core;

namespace ScrapGuard.Tests
{
    public static partial class CoreSpecification
    {
        public static readonly string[] Names = { "AttendanceBoundary", "AttendanceCannotRepeat", "AttendanceSkipAndCycle", "ClockBackwards", "InsufficientFunds", "ShopLimitsAndReset", "RareLifetimeLimit", "SellGuards", "TransferConservesLevels", "HeroUnlock", "DuplicateBattle", "WrongStage", "FirstClearOnlyOnce", "AbandonNoDailyCredit", "RewardCaps", "RewardReplay", "OldResultCannotFarm", "MissionClaimOnce", "CombatPausesForChoices", "InvalidSkillRejected", "CharacterStats", "CombatDeath", "BossVictory", "SpawnBudget", "ShowcaseMigration", "CosmeticsDoNotAffectStats", "ShowcaseOwnership", "NicknameValidation", "ShowcaseSoldItem", "ReplayRoundTrip", "ReplayRejectsInvalidInput", "UltimateRules", "UltimateHeroGeometry", "UltimateReplay", "UltimateRejectsForgery", "WeeklyBoundary", "ChallengeReplay", "WeeklyRewardRules", "CoreExchangeRules", "SupportReplay", "SupportGuards", "CampaignProgression", "CampaignMigration", "CampaignReplay", "CampaignTerrain", "CampaignEnemyRoles", "ModGeometry", "ModReplay", "BattleDamageReport", "CollectionMigration", "RegionRewardsAndCraft", "ExpansionMigration", "ExpansionRecruitment", "ExpansionHeroCombat", "ExpansionReplay", "ExpansionTerrainAndRewards", "OutfitOwnership", "OutfitReplayEquality", "GuideProofAndMigration", "EarlyBattleProgression", "FieldTaskChoiceGuards", "FieldTaskReplayAndRewards", "FieldTaskDefenseAndEscort", "ExpandedModMechanics", "FairTrialIgnoresGrowth", "FairTrialNoRewardLeak", "PremiumWalletRules", "PriceCatalogRules", "AssemblyGuardsAndReplay", "HeroEngineeringMechanics", "FieldRouteAndCollectionTarget", "RailOutfitOwnershipAndParity", "ModeCopyAndRestorationStages", "WardrobeMigrationAndTargets", "NewOutfitsHaveNoPower", "RemainingHeroEngineering", "ConditionsAndNewModuleReplay", "MemorialThresholds", "ReleaseCompatibilityRules", "ReleaseValidationAndStoreLink", "UpdatePreservesPendingTransactions", "ShowcaseThemeOwnership", "NewOperationGoals", "TacticalBuilds", "HeroIdentityActions", "PolishReplayCompatibility", "ExpandedWorldAndHordes", "ArsenalAndMachineAssembly", "EvolutionChoices", "AssaultReplayRoundTrip", "ReviveReplayAuthorization", "SpatialCollisionAcrossCells", "CrowdSpacingAndSharedQueries", "AttackEvolutionMechanics", "MachineInputGuards", "MachineRecoveryAndTelemetry", "BossArenaSafety", "EarlyEvolutionAndGrowthTail", "DamageSourceAccountingAndStackGuard", "OpeningThreatBudget", "MachineRescueAndBurst", "EquipmentGrowthAndFixedChallenge", "IntroBossScopeAndLearning", "HealingSourcesAndLowHealthTime", "RobotPartsAndDistinctFrames", "RobotReplayAndCollection", "IntroRobotRecoveryScope", "EarlyBossLessonsAndDamageAccounting", "EarlyBossReplayRoundTrip", "ContinuousBossCurve", "BossAttackCoordination", "GrowthPreviewAndCampaignImpact", "ModuleUpgradeHasCombatEffect", "GrowthKitAndDailyLimits", "GrowthPassRightsAndClaims", "FreeGrowthRecovery", "VectorGridPrecision", "BossPracticeIsolation", "SalvageSupplyGuards", "WeeklyRobotReplayAndRewards", "LastDamageCoaching", "RobotStyleCommerceAndGrowth", "Expansion034BossReplay" };
        public static void Run(string name) { typeof(CoreSpecification).GetMethod(name).Invoke(null, null); }
        static DateTimeOffset Now = new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.Zero);
        static Economy New() { return new Economy(GameState.New(), () => Now); }
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        public static void CampaignProgression()
        {
            var e=New();Check(Campaign.Count==60,"60 operations");
            Check(!e.BeginRun("locked",1).Success,"sequential unlock");
            for(int stage=0;stage<Campaign.Count;stage++)
            {
                var d=Campaign.Get(stage);Check(d.Id==stage && d.Duration<500,"stable IDs and replay duration budget");
                Check(e.BeginRun("clear"+stage,stage).Success,"operation starts");
                Check(e.FinishRun("clear"+stage,stage,true,20,d.Duration).Success,"operation settles");
            }
            Check(e.State.clearedStages.Count==Campaign.Count && e.State.gems==Campaign.Count*50 && e.State.unlockedStage==Campaign.Count-1,"all rewards and final bound");
            int gems=e.State.gems,count=e.State.items.Count;
            e.BeginRun("repeat",29);e.FinishRun("repeat",29,true,20,270);
            Check(e.State.gems==gems && e.State.items.Count==count,"repeat cannot duplicate first rewards");
            Check(!e.BeginRun("overflow",Campaign.Count).Success && !e.BeginRun("negative",-1).Success,"stage bounds");
        }
        public static void CampaignMigration()
        {
            var state=GameState.New();state.unlockedStage=4;state.clearedStages.AddRange(new[]{0,1,2,3,4});state.gems=250;
            var e=new Economy(state,()=>Now);Check(state.unlockedStage==5 && state.gems==250 && state.clearedStages.Count==5,"old clears preserved without reward replay");
            Campaign.Normalize(state);Check(state.unlockedStage==5 && Endgame.Unlocked(state) && Social.HasTitle(state,4),"idempotent migration and earned rights retained");
            Check(e.BeginRun("six",5).Success,"legacy account continues");
        }
        public static void CampaignReplay()
        {
            for(int stage=0;stage<Campaign.Count;stage++)
            {
                var state=GameState.New();state.heroes[0].level=30;foreach(var item in state.items)item.level=20;
                var combat=new Combat(state,stage,"campaign",913);var recorder=new ReplayRecorder();
                for(int i=0;i<1200 && !combat.Ended;i++)
                {
                    if(combat.Choosing){recorder.Choose(combat.Choices[0]);combat.Choose(combat.Choices[0]);}
                    if(combat.CanUltimate){recorder.Ultimate();combat.UseUltimate();}
                    combat.Tick(ReplayRecorder.Step,recorder.Move(new V2((float)Math.Sin(i*.02),(float)Math.Cos(i*.02))));
                }
                if(!combat.Ended)combat.Abandon();
                var replay=ReplayRecorder.Verify(state,stage,"campaign",913,recorder.Steps,50);
                Check(replay.Hp==combat.Hp && replay.Kills==combat.Kills && replay.Position.x==combat.Position.x && replay.Position.y==combat.Position.y,"deterministic region replay "+stage);
            }
        }
        public static void CampaignTerrain()
        {
            var s=GameState.New();
            for(int stage=0;stage<Campaign.Count;stage++)
            {
                var c=new Combat(s,stage,"bounds");Check(!c.IsBlocked(c.Position),"safe spawn");
                var block=c.Blocks[0];c.Position=new V2(block.X+block.W+15,block.Y+block.H/2);c.Tick(.05f,new V2(-1,0));Check(!c.IsBlocked(c.Position),"solid collision");
            }
            var toxic=new Combat(s,12,"toxic");toxic.Position=toxic.Zones[0].Center;float hp=toxic.Hp;toxic.Tick(.05f,new V2());Check(toxic.Hp<hp,"poison zone damages");
            var ice=new Combat(s,18,"ice");ice.Tick(.05f,new V2(1,0));float x=ice.Position.x;ice.Tick(.05f,new V2());Check(ice.Position.x>x,"ice carries momentum");
            var belt=new Combat(s,24,"belt");belt.Position=belt.Zones[0].Center;float y=belt.Position.y;belt.Tick(.05f,new V2());Check(belt.Position.y<y,"conveyor displacement");
            var rail=new Combat(s,6,"rail");rail.Elapsed=5;for(int i=0;i<102;i++){rail.Enemies.Clear();rail.Tick(.05f,new V2());}
            Check(rail.Hazards.Exists(h=>h.width>0 && h.delay>0),"rail has warning before damage");
            var survival=new Combat(s,2,"survival");survival.Elapsed=survival.Duration-.01f;survival.Tick(.05f,new V2());Check(survival.Ended && survival.Won,"survival mission clears without boss");
            var rift=new Combat(s,4,"rift",1,false,Endgame.PracticeMode);Check(rift.Blocks.Length==0 && rift.Zones.Length==0,"rift isolated from campaign terrain");
        }
        public static void CampaignEnemyRoles()
        {
            foreach(EnemyRole role in Enum.GetValues(typeof(EnemyRole)))
            {
                var c=new Combat(GameState.New(),24,"role");c.Enemies.Clear();
                var enemy=new Enemy {id=900,kind=0,role=role,position=c.Position+new V2(70,0),hp=100000,maxHp=100000,speed=30,cooldown=0};c.Enemies.Add(enemy);
                c.Tick(.05f,new V2());
                if(role==EnemyRole.Charger)Check(enemy.windup>0,"charge telegraphs");
                if(role==EnemyRole.Artillery || role==EnemyRole.Bomber)Check(c.Hazards.Count>0,"attack warning exists");
                if(role==EnemyRole.Summoner)Check(c.Enemies.Count>=2,"summoner produces unit");
            }
        }
        public static void ShowcaseMigration()
        {
            var s = GameState.New(); s.displayName = null; s.ownedCosmetics = null; s.showcaseItems = null; s.showcaseHero = 99;
            Social.Normalize(s); Check(s.displayName == "새내기 정비사" && s.showcaseItems.Count == 3 && s.showcaseHero == 0, "old profile migration");
        }
        public static void CosmeticsDoNotAffectStats()
        {
            var s = GameState.New(); s.gems = 600; int attack = Catalog.Attack(s), health = Catalog.Health(s);
            Check(Social.Buy(s,"frame_solar").Success && Social.Buy(s,"skin_amber").Success && s.gems == 50, "exact cosmetic price");
            Check(!Social.Buy(s,"frame_solar").Success && s.gems == 50, "no duplicate charge");
            Check(Social.Customize(s,0,0,3,1).Success && Catalog.Attack(s) == attack && Catalog.Health(s) == health, "no combat advantage");
        }
        public static void ShowcaseOwnership()
        {
            var s = GameState.New();
            Check(!Social.Customize(s,1,0,0,0).Success && !Social.Customize(s,0,4,0,0).Success && !Social.Customize(s,0,0,3,1).Success, "locked selection");
            s.attendanceCount = 7; Check(Social.Customize(s,0,2,2,0).Success,"earned selection");
            var p = Social.Build(s); Check(p.attack == Catalog.Attack(s) && p.health == Catalog.Health(s),"showcase displayed stats");
        }
        public static void NicknameValidation()
        {
            var s = GameState.New(); Check(Social.Rename(s,"  고철  장인  ").Success && s.displayName == "고철 장인", "normalize name");
            Check(!Social.Rename(s,"<b>관리자</b>").Success && !Social.Rename(s,"a").Success && !Social.Rename(s,new string('a',13)).Success, "reject markup and lengths");
        }
        public static void ShowcaseSoldItem()
        {
            var e = New(); var item = e.AddItem(1,0); Social.Normalize(e.State); e.State.showcaseItems[0] = item.id;
            Check(e.Sell(item.id).Success,"sell display equipment"); var p = Social.Build(e.State);
            Check(p.items[0].definition == e.State.items[0].definition && e.State.showcaseItems[0] == e.State.equipped[0],"safe display fallback");
        }
        public static void ReplayRoundTrip()
        {
            var s = GameState.New(); var c = new Combat(s,0,"replay",57); var r = new ReplayRecorder();
            int frames = 0;
            while (frames < 2700 && !c.Ended)
            {
                if (c.Choosing) { int skill = c.Choices[0]; Check(c.Choose(skill),"choose"); r.Choose(skill); }
                c.Tick(ReplayRecorder.Step,r.Move(new V2((float)Math.Sin(frames*.01), (float)Math.Cos(frames*.01)))); frames++;
            }
            if (!c.Ended) c.Abandon(); var verified = ReplayRecorder.Verify(s,0,"replay",57,r.Steps,100);
            Check(verified.Kills == c.Kills && verified.Elapsed == c.Elapsed && verified.Won == c.Won && verified.Level == c.Level,"deterministic replay");
        }
        public static void ReplayRejectsInvalidInput()
        {
            bool failed = false;
            try { ReplayRecorder.Verify(GameState.New(),0,"bad",1,new System.Collections.Generic.List<ReplayStep>{new ReplayStep {x = 101,frames = 1}},3); }
            catch (ArgumentException) { failed = true; } Check(failed,"invalid axes");
            failed = false;
            try { ReplayRecorder.Verify(GameState.New(),0,"bad",1,new System.Collections.Generic.List<ReplayStep>{new ReplayStep {frames = 120}},0); }
            catch (ArgumentException) { failed = true; } Check(failed,"time bound");
        }
        static Combat Charged(int hero)
        {
            var state=GameState.New();state.selectedHero=hero;
            var c=new Combat(state,0,"ultimate",23);
            for(int i=0;i<760;i++) { if(c.Choosing)c.Choose(c.Choices[0]);c.Enemies.Clear();c.Bullets.Clear();c.Pickups.Clear();c.Tick(ReplayRecorder.Step,new V2()); }
            c.Enemies.Clear(); Check(c.CanUltimate,"charge over time"); return c;
        }
        public static void UltimateRules()
        {
            var empty=new Combat(GameState.New(),0,"empty");Check(!empty.UseUltimate() && empty.UltimateUses==0,"reject uncharged");
            var c=Charged(2);c.Choosing=true;Check(!c.UseUltimate(),"no ultimate during selection");c.Choosing=false;
            Check(c.UseUltimate() && c.UltimateUses==1 && c.UltimateCooldown==16 && c.UltimateCharge==0,"consume charge once");
            Check(!c.UseUltimate() && Math.Abs(c.Shield-c.MaxHp*.35f)<.001f,"no repeated activation and shield awarded");
            for(int i=0;i<155;i++) { c.Enemies.Clear();c.Bullets.Clear();c.Tick(ReplayRecorder.Step,new V2()); }
            Check(c.Shield==0,"ultimate shield expires after five seconds");
            c=Charged(0);c.Abandon();Check(!c.UseUltimate(),"no ultimate after battle");
        }
        public static void UltimateHeroGeometry()
        {
            for(int hero=0;hero<3;hero++)
            {
                var c=Charged(hero);
                var near=new Enemy {id=900,position=c.Position+new V2(80,0),hp=100000};
                var front=new Enemy {id=901,position=c.Position+new V2(230,0),hp=100000};
                var side=new Enemy {id=902,position=c.Position+new V2(0,130),hp=100000};
                var behind=new Enemy {id=903,position=c.Position+new V2(-90,0),hp=100000};
                c.Enemies.AddRange(new[]{near,front,side,behind});Check(c.UseUltimate(),"hero activation");
                Check(near.hp<100000,"near hit");
                Check((front.hp<100000)==(hero!=2),"radius vs beam length");
                Check((side.hp<100000)==(hero!=1) && (behind.hp<100000)==(hero!=1),"beam has directional width");
            }
        }
        public static void UltimateReplay()
        {
            for(int hero=0;hero<3;hero++)
            {
                var state=GameState.New();state.selectedHero=hero;
                var c=new Combat(state,0,"ultimate-replay",57);var r=new ReplayRecorder();int frames=0;
                while(frames<2400 && !c.Ended)
                {
                    if(c.Choosing) { int skill=c.Choices[0];c.Choose(skill);r.Choose(skill); }
                    if(c.CanUltimate) { Check(c.UseUltimate(),"activation");r.Ultimate();if(c.Ended)break; }
                    c.Tick(ReplayRecorder.Step,r.Move(new V2((float)Math.Sin(frames*.01),(float)Math.Cos(frames*.01))));frames++;
                }
                if(!c.Ended)c.Abandon();var verified=ReplayRecorder.Verify(state,0,"ultimate-replay",57,r.Steps,100);
                Check(c.UltimateUses>0 && verified.UltimateUses==c.UltimateUses && verified.Kills==c.Kills && verified.Hp==c.Hp && verified.Elapsed==c.Elapsed,"ultimate replay parity for hero "+hero);
            }
            var recorder=new ReplayRecorder();recorder.Move(new V2());recorder.Ultimate();recorder.Move(new V2());
            Check(recorder.Steps.Count==3 && recorder.Steps[1].frames==0,"movement compression preserves activation boundary");
        }
        public static void UltimateRejectsForgery()
        {
            foreach(var step in new[]{new ReplayStep{ultimate=true},new ReplayStep{ultimate=true,frames=1},new ReplayStep{ultimate=true,skill=0},new ReplayStep{ultimate=true,x=1}})
            {
                bool rejected=false;
                try { ReplayRecorder.Verify(GameState.New(),0,"forged",57,new System.Collections.Generic.List<ReplayStep>{step},100); }
                catch(ArgumentException) { rejected=true; }
                Check(rejected,"reject impossible or mixed ultimate input");
            }
        }
        public static void WeeklyBoundary()
        {
            var boundary=new DateTimeOffset(2026,11,1,20,0,0,TimeSpan.Zero);
            Check(Endgame.Week(boundary.AddSeconds(-1))=="2026-10-26" && Endgame.Week(boundary)=="2026-11-02","Monday KST 05 reset");
            Check(Endgame.Week(boundary.AddDays(6))=="2026-11-02","Sunday retains week");
            Check(Endgame.Seed("2026-11-02")==Endgame.Seed("2026-11-02") && Endgame.Seed("2026-11-09")!=Endgame.Seed("2026-11-02"),"stable weekly seed");
        }
        static Combat SimulateChallenge(GameState state,string mode,out ReplayRecorder recorder)
        {
            var c=new Combat(state,4,"core",Endgame.Seed("2026-11-02"),false,mode);recorder=new ReplayRecorder();
            for(int f=0;f<3000 && !c.Ended;f++)
            {
                if(c.Choosing){int skill=c.Choices[0];c.Choose(skill);recorder.Choose(skill);}
                if(c.CanUltimate){c.UseUltimate();recorder.Ultimate();}
                c.Tick(ReplayRecorder.Step,recorder.Move(new V2((float)Math.Sin(f*.013),(float)Math.Cos(f*.013))));
            }
            if(!c.Ended)c.Abandon();return c;
        }
        public static void ChallengeReplay()
        {
            var state=GameState.New();state.heroes[0].level=30;foreach(var item in state.items)item.level=20;
            ReplayRecorder recorder;var c=SimulateChallenge(state,Endgame.WeeklyMode,out recorder);
            var replay=ReplayRecorder.Verify(state,4,"core",Endgame.Seed("2026-11-02"),recorder.Steps,120,Endgame.WeeklyMode);
            Check(c.CoreScore>0 && c.CoreScore==replay.CoreScore && c.Hp==replay.Hp && c.Elapsed==replay.Elapsed,"weekly score deterministic");
            Check(c.Duration==90 && c.Elapsed<=90,"bounded challenge duration");
        }
        public static void WeeklyRewardRules()
        {
            var now=Now;var state=GameState.New();var e=new Economy(state,()=>now);
            Check(!e.BeginChallenge("locked",Endgame.WeeklyMode).Success,"campaign completion gate");
            Check(e.BeginChallenge("core",Endgame.PracticeMode).Success,"practice before unlock");
            ReplayRecorder r;var c=SimulateChallenge(state,Endgame.PracticeMode,out r);int gold=state.gold;
            Check(e.FinishChallenge(c).Success && state.weekly.Count==0 && state.gold==gold && state.coreShards==0,"practice never grants record or rewards");
            state.clearedStages.Add(4);state.heroes[0].level=30;foreach(var item in state.items)item.level=20;
            Check(e.BeginChallenge("real",Endgame.WeeklyMode).Success,"unlock formal challenge");
            // Exercise settlement against a simulated run with the exact issued ID.
            var real=new Combat(state,4,"real",1,false,Endgame.WeeklyMode);
            for(int i=0;i<950 && !real.Ended;i++){if(real.Choosing)real.Choose(real.Choices[0]);real.Tick(ReplayRecorder.Step,new V2((float)Math.Sin(i*.013),(float)Math.Cos(i*.013)));}
            real.Abandon();now=now.AddDays(7);
            Check(e.FinishChallenge(real).Success && Endgame.Find(state,"2026-11-02")!=null,"boundary finish belongs to start week");
            Check(!e.FinishChallenge(real).Success,"duplicate settlement rejected");
            Check(e.ClaimWeekly("2026-11-02",0).Success && state.coreShards==1,"previous-week claim remains available");
            Check(!e.ClaimWeekly("2026-11-02",0).Success && !e.ClaimWeekly("unknown",0).Success && !e.ClaimWeekly("2026-11-02",-1).Success,"duplicate and forged tiers rejected");
        }
        public static void CoreExchangeRules()
        {
            var e=New();Check(!e.ExchangeCore("frame_core").Success && !Social.Buy(e.State,"frame_core").Success,"exclusive reward cannot use gems");
            int attack=Catalog.Attack(e.State);e.State.coreShards=32;
            Check(e.ExchangeCore("frame_core").Success && e.ExchangeCore("title_rift").Success && e.State.coreShards==0,"exact shard exchange");
            Check(!e.ExchangeCore("frame_core").Success && !e.ExchangeCore("anything").Success,"duplicate or unknown exchange rejected");
            Check(Social.Customize(e.State,0,5,4,0).Success && Catalog.Attack(e.State)==attack,"earned cosmetics do not alter combat power");
        }
        public static void SupportReplay()
        {
            for(int hero=0;hero<3;hero++)
            {
                var state=GameState.New();var helper=new SupportSnapshot {hero=hero,power=200,owner="friend"};var c=new Combat(state,0,"aid",23,false,"",helper);var r=new ReplayRecorder();
                for(int f=0;f<1000 && !c.Ended;f++)
                {
                    if(c.Choosing){int skill=c.Choices[0];c.Choose(skill);r.Choose(skill);}
                    if(c.CanSupport){Check(c.UseSupport(),"support activation");r.Support();}
                    c.Tick(ReplayRecorder.Step,r.Move(new V2((float)Math.Sin(f*.01),(float)Math.Cos(f*.01))));
                }
                if(!c.Ended)c.Abandon();var replay=ReplayRecorder.Verify(state,0,"aid",23,r.Steps,40,"",helper);
                Check(c.SupportUsed && replay.SupportUsed && c.Hp==replay.Hp && c.Kills==replay.Kills,"support replay hero "+hero);
            }
        }
        public static void SupportGuards()
        {
            var c=new Combat(GameState.New(),0,"none");Check(!c.UseSupport(),"no unissued support");
            foreach(var step in new[]{new ReplayStep {support=true},new ReplayStep {support=true,frames=1},new ReplayStep {support=true,ultimate=true}})
            {
                bool rejected=false;try {ReplayRecorder.Verify(GameState.New(),0,"bad",1,new System.Collections.Generic.List<ReplayStep>{step},20);}catch(ArgumentException){rejected=true;}
                Check(rejected,"invalid support input rejected");
            }
            bool weeklyRejected=false;
            try {new Combat(GameState.New(),4,"weekly",1,false,Endgame.WeeklyMode,new SupportSnapshot {hero=0,power=100});}catch(ArgumentException){weeklyRejected=true;}
            Check(weeklyRejected,"weekly mode cannot borrow support");
            var r=new ReplayRecorder();r.Move(new V2());r.Support();r.Move(new V2());Check(r.Steps.Count==3,"support event is not movement-compressed");
        }
        public static void AttendanceBoundary()
        {
            Check(Catalog.ServiceDate(new DateTimeOffset(2026, 11, 1, 19, 59, 59, TimeSpan.Zero)) == "2026-11-01", "before reset");
            Check(Catalog.ServiceDate(new DateTimeOffset(2026, 11, 1, 20, 0, 0, TimeSpan.Zero)) == "2026-11-02", "after reset");
        }
        public static void AttendanceCannotRepeat()
        {
            var e = New(); int gold = e.State.gold; Check(e.Attend().Success, "first claim"); Check(!e.Attend().Success, "duplicate claim"); Check(e.State.gold == gold + 1000 && e.State.attendanceCount == 1, "single reward");
        }
        public static void AttendanceSkipAndCycle()
        {
            var time = Now; var e = new Economy(GameState.New(), () => time);
            for (int i = 0; i < 14; i++) { Check(e.Attend().Success, "claim " + i); time = time.AddDays(i == 2 ? 3 : 1); }
            Check(e.State.heroes[2].unlocked && e.State.attendanceCount == 14 && e.State.gems == 300 && e.State.parts == 210, "cycle rewards");
        }
        public static void ClockBackwards()
        {
            var time = Now; var e = new Economy(GameState.New(), () => time); e.Attend(); time = time.AddDays(-1);
            Check(!e.Attend().Success, "clock rollback must not reopen attendance");
        }
        public static void InsufficientFunds()
        {
            var e = New(); int before = e.State.gold; Check(!e.Buy("normal:1").Success, "insufficient"); Check(e.State.items.Count == 3 && e.State.gold == before, "no mutation");
            Check(!e.Buy("normal:999").Success && !e.Buy("unknown:1").Success, "unregistered offers");
        }
        public static void ShopLimitsAndReset()
        {
            var time = Now; var e = new Economy(GameState.New(), () => time); e.State.gold = 20000;
            Check(e.Buy("normal:1").Success && e.Buy("normal:2").Success && !e.Buy("normal:3").Success, "daily limit");
            time = time.AddDays(1); Check(e.Buy("normal:3").Success, "reset");
        }
        public static void RareLifetimeLimit()
        {
            var e = New(); e.State.gems = 2000; Check(e.Buy("rare:2").Success, "purchase");
            var rare = e.State.items.Find(i => i.definition == 2); Check(e.Sell(rare.id).Success, "sell");
            Check(!e.Buy("rare:2").Success && e.State.gems == 1400, "lifetime limit survives sale");
        }
        public static void SellGuards()
        {
            var e = New(); Check(!e.Sell(e.State.equipped[0]).Success, "equipped");
            var item = e.AddItem(1, 0); item.locked = true; Check(!e.Sell(item.id).Success, "locked");
            item.locked = false; item.level = 2; Check(!e.Sell(item.id).Success, "upgraded");
            item.level = 1; Check(e.Sell(item.id).Success && !e.Sell(item.id).Success, "duplicate sale");
        }
        public static void TransferConservesLevels()
        {
            var e = New(); var source = e.State.items[0]; source.level = 7; var target = e.AddItem(1, 2);
            Check(e.Transfer(source.id, target.id).Success && source.level == 1 && target.level == 7, "transfer");
            Check(!e.Transfer(target.id, e.State.items[1].id).Success, "cross slot");
            Check(!e.Transfer(source.id, target.id).Success, "no duplication");
        }
        public static void HeroUnlock()
        {
            var e = New(); Check(!e.SelectHero(1).Success, "locked"); e.State.gems = 1200;
            Check(e.Buy("hero").Success && e.SelectHero(1).Success, "unlock"); Check(!e.Buy("hero").Success && e.State.gems == 0, "one unlock");
        }
        public static void DuplicateBattle()
        {
            var e = New(); e.BeginRun("run", 0); Check(e.FinishRun("run", 0, true, 60, 180).Success, "first"); int gold = e.State.gold;
            Check(!e.FinishRun("run", 0, true, 60, 180).Success && e.State.gold == gold, "duplicate result");
        }
        public static void WrongStage()
        {
            var e = New(); e.BeginRun("run", 0); Check(!e.FinishRun("run", 4, true, 60, 180).Success, "stage must match");
            Check(!e.FinishRun("run", 0, true, 1, float.PositiveInfinity).Success, "invalid duration");
        }
        public static void FirstClearOnlyOnce()
        {
            var e = New(); e.BeginRun("a", 0); e.FinishRun("a", 0, true, 60, 180); e.BeginRun("b", 0); e.FinishRun("b", 0, true, 60, 180);
            Check(e.State.gems == 50 && e.State.items.Count == 4 && e.State.unlockedStage == 1, "first clear reward");
        }
        public static void AbandonNoDailyCredit()
        {
            var e = New(); e.BeginRun("a", 0); e.FinishRun("a", 0, false, 0, 0);
            Check(e.State.dailyRuns == 0 && e.State.gold == 1800 && e.State.parts == 30, "instant abandon");
        }
        public static void RewardCaps()
        {
            var e = New(); Check(e.ClaimDemoReward("parts", "a").Success && e.ClaimDemoReward("parts", "b").Success, "two claims");
            Check(!e.ClaimDemoReward("parts", "c").Success && e.State.parts == 46 && e.State.gold == 3000, "cap");
        }
        public static void RewardReplay()
        {
            var e = New(); e.BeginRun("run", 0); e.FinishRun("run", 0, true, 50, 180);
            Check(e.ClaimDemoReward("result", "bonus").Success, "first bonus"); int gold = e.State.gold;
            Check(!e.ClaimDemoReward("result", "bonus").Success && !e.ClaimDemoReward("result", "another").Success && e.State.gold == gold, "exactly one bonus per result");
        }
        public static void OldResultCannotFarm()
        {
            var time = Now; var e = new Economy(GameState.New(), () => time); e.BeginRun("a", 0); e.FinishRun("a", 0, true, 50, 180);
            time = time.AddDays(1); Check(!e.ClaimDemoReward("result", "bonus").Success, "old result");
        }
        public static void MissionClaimOnce()
        {
            var e = New(); e.State.dailyRuns = 3; e.State.dailyKills = 500;
            Check(e.ClaimMissions().Success && !e.ClaimMissions().Success && e.State.gems == 30, "one mission payout");
        }
        public static void CombatPausesForChoices()
        {
            var c = new Combat(GameState.New(), 0, "a"); c.Xp = 100; c.Tick(.02f, new V2());
            Check(c.Choosing, "skill choices offered"); float time = c.Elapsed; c.Tick(.05f, new V2(1, 0)); Check(time == c.Elapsed, "paused");
            Check(c.Choose(c.Choices[0]) && c.Level == 2, "choice applies");
        }
        public static void InvalidSkillRejected()
        {
            var c = new Combat(GameState.New(), 0, "a"); Check(!c.Choose(7), "outside level-up"); c.Xp = 100; c.Tick(.02f, new V2()); Check(!c.Choose(99), "invalid skill");
        }
        public static void CharacterStats()
        {
            var s = GameState.New(); int attack = Catalog.Attack(s), hp = Catalog.Health(s); s.selectedHero = 1;
            Check(Catalog.Attack(s) > attack && Catalog.Health(s) < hp, "hero tradeoff"); s.items[0].level = 10; Check(Catalog.Attack(s) > attack + 10, "gear affects combat");
        }
        public static void CombatDeath()
        {
            var c = new Combat(GameState.New(), 0, "a"); c.Hp = 1;
            c.Enemies.Add(new Enemy { id = 999, hp = 1000, maxHp = 1000, position = c.Position });
            c.Tick(.02f, new V2()); Check(c.Ended && !c.Won && c.Hp == 0, "death ends run");
        }
        public static void BossVictory()
        {
            var c = new Combat(GameState.New(), 0, "a");
            c.Enemies.Add(new Enemy { id = 999, hp = 0, maxHp = 1000, finalBoss = true, kind = 3 });
            c.Tick(.02f, new V2()); Check(c.Ended && c.Won, "boss kill clears stage");
        }
        public static void SpawnBudget()
        {
            var c = new Combat(GameState.New(), 0, "a");
            for (int i = 0; i < 10000 && !c.Ended; i++)
            {
                c.Hp = c.MaxHp; if (c.Choosing) c.Choose(c.Choices[0]); c.Tick(.05f, new V2());
                Check(c.Enemies.Count <= c.EnemyLimit+2 && c.Pickups.Count <= 230, "bounded entities");
            }
            Check(c.Ended, "run terminates");
        }
    }
}
