using System;
using System.IO;
using NUnit.Framework;
using ScrapGuard.Core;
using UnityEngine;

namespace ScrapGuard.Tests
{
    public class GameTests
    {
        [Test]
        public void RobotStylesRoundTripAndAtlasBounds()
        {
            var s=GameState.New();s.gems=900;var e=new Economy(s,()=>DateTimeOffset.UtcNow);Assert.That(e.BuyRobotStylePack().Success,Is.True);
            var restored=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(s));ScrapGuard.Core.Social.Normalize(restored);
            var seen=new System.Collections.Generic.HashSet<Rect>();
            for(int style=1;style<=2;style++)for(int frame=0;frame<3;frame++)
            {
                var art=ArenaArt.Cell("RobotStyles-v1",3,2,frame,style-1);Assert.That(art,Is.Not.Null);seen.Add(art.rect);
                Assert.That(art.rect.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(art.rect.yMin,Is.GreaterThanOrEqualTo(0));Assert.That(art.rect.xMax,Is.LessThanOrEqualTo(art.texture.width));Assert.That(art.rect.yMax,Is.LessThanOrEqualTo(art.texture.height));
                Assert.That(RobotStyles.Active(restored,frame),Is.EqualTo(1));
            }
            Assert.That(seen.Count,Is.EqualTo(6));
            var command=new GameCommand("upgrade_path","gear"){stage=1,count=3,level=4};var copy=JsonUtility.FromJson<GameCommand>(JsonUtility.ToJson(command));Assert.That(copy.count,Is.EqualTo(3));Assert.That(copy.level,Is.EqualTo(4));
        }
        [Test]
        public void ScheduledContentRoundTripAndBoundaries()
        {
            var now=new DateTimeOffset(2026,11,4,0,0,0,TimeSpan.Zero);
            var release=new OutfitRelease{id=1,minimumBuild=AppRelease.Build,startsAt=now.ToString("O"),endsAt=now.AddHours(1).ToString("O")};
            var reply=new ServerReply{metricsConsent=true,content=new ContentCatalog{serverTime=now.ToString("O")}};reply.content.outfits.Add(release);
            var copy=JsonUtility.FromJson<ServerReply>(JsonUtility.ToJson(reply));Assert.That(copy.metricsConsent,Is.True);
            Assert.That(copy.content.outfits[0].Available(now,AppRelease.Build),Is.True);
            Assert.That(release.Available(now.AddTicks(-1),AppRelease.Build),Is.False);
            Assert.That(release.Available(now.AddHours(1),AppRelease.Build),Is.False);
            Assert.That(release.Available(now,AppRelease.Build-1),Is.False);
            release.paused=true;Assert.That(release.Available(now,AppRelease.Build),Is.False);
        }
        [Test]
        public void UpdateCachePreservesSessionAndPendingJournal()
        {
            string folder=Path.Combine(Path.GetTempPath(),"scrap-update-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            try
            {
                var cloud=new CloudClient(folder);cloud.Session.endpoint="https://game.example.com";cloud.Session.secret=CloudClient.NewSecret();cloud.Session.enabled=true;cloud.SaveSession();
                var pending=new GameCommand("attendance");string json=JsonUtility.ToJson(pending);File.WriteAllText(Path.Combine(folder,"cloud-pending.json"),json);
                var p=new ReleasePolicy{latestBuild=AppRelease.Build+1,minimumBuild=AppRelease.Build+1,latestVersion="0.16.0",combatRules=Combat.RulesVersion,packageName=AppRelease.Package};
                Assert.That(cloud.ObserveRelease(cloud.Session.endpoint,p),Is.True);
                var restored=new CloudClient(folder);Assert.That(restored.UpdateBlocked,Is.True);Assert.That(restored.Pending.requestId,Is.EqualTo(pending.requestId));Assert.That(restored.Session.secret,Is.EqualTo(cloud.Session.secret));
                Assert.That(File.ReadAllText(Path.Combine(folder,"cloud-pending.json")),Is.EqualTo(json));
                p.packageName="foreign";Assert.That(restored.ObserveRelease(restored.Session.endpoint,p),Is.False);Assert.That(new CloudClient(folder).UpdateBlocked,Is.True);
                p.packageName=AppRelease.Package;p.minimumBuild=14;p.latestBuild=14;p.latestVersion=AppRelease.Version;restored.ObserveRelease(restored.Session.endpoint,p);
                Assert.That(new CloudClient(folder).UpdateBlocked,Is.False);Assert.That(new CloudClient(folder).Pending.requestId,Is.EqualTo(pending.requestId));
                restored.Session.endpoint="https://other.example.com";restored.SaveSession();Assert.That(new CloudClient(folder).Release,Is.Null);
            }
            finally{Directory.Delete(folder,true);}
        }
        [Test]
        public void ReleaseWireRoundTrip()
        {
            var reply=new ServerReply{code="client_update_required",release=new ReleasePolicy{latestBuild=AppRelease.Build+1,minimumBuild=AppRelease.Build+1,latestVersion="0.16.0",packageName=AppRelease.Package,combatRules=Combat.RulesVersion,playAvailable=true}};
            var copy=JsonUtility.FromJson<ServerReply>(JsonUtility.ToJson(reply));Assert.That(ReleaseRules.Evaluate(copy.release,14,Combat.RulesVersion),Is.EqualTo(UpdateRequirement.Required));
            Assert.That(copy.code,Is.EqualTo("client_update_required"));
        }
        [Test]
        public void FullWardrobeAndMemorialRoundTrip()
        {
            var s=GameState.New();s.gems=Outfits.Count*Outfits.Price;
            for(int id=1;id<=Outfits.Count;id++)
            {
                int hero=Outfits.Hero(id);s.heroes[hero].unlocked=true;
                Assert.That(ScrapGuard.Core.Social.Buy(s,Outfits.Product(id)).Success,Is.True);Assert.That(Outfits.Equip(s,id).Success,Is.True);
                var cells=new System.Collections.Generic.HashSet<Rect>();
                for(int pose=0;pose<4;pose++){var sprite=CharacterArt.Get(hero,0,id,pose);Assert.That(sprite,Is.Not.Null);Assert.That(sprite.texture.name,Is.EqualTo(Outfits.Assets[id]));cells.Add(sprite.rect);}
                Assert.That(cells.Count,Is.EqualTo(4));
            }
            var copy=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(s));ScrapGuard.Core.Social.Normalize(copy);
            for(int id=1;id<=Outfits.Count;id++)Assert.That(Outfits.Owned(copy,id),Is.True);
            for(int hero=0;hero<HeroCatalog.Count;hero++)Assert.That(Outfits.Active(copy,hero),Is.EqualTo(Outfits.Active(s,hero)));
            Assert.That(Outfits.Active(copy,0),Is.EqualTo(6));Assert.That(Outfits.Active(copy,1),Is.EqualTo(7));Assert.That(Outfits.Active(copy,2),Is.EqualTo(10));
            Assert.That(Outfits.Active(copy,3),Is.EqualTo(8));Assert.That(Outfits.Active(copy,4),Is.EqualTo(9));
            Assert.That(Outfits.Equip(copy,1).Success,Is.True);Assert.That(Outfits.Active(copy,0),Is.EqualTo(1));Assert.That(Outfits.Active(copy,1),Is.EqualTo(7));
            s.heroOutfits=null;var legacy=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(s));
            Assert.That(legacy.heroOutfits==null || legacy.heroOutfits.Count==0,Is.True);legacy.selectedOutfit=2;ScrapGuard.Core.Social.Normalize(legacy);Assert.That(Outfits.Active(legacy,1),Is.EqualTo(2));
            var guild=new GuildView{memorialWeeks=1};guild.memorials.Add(new GuildMemorial{week="2026-11-02",tier=3,points=120});
            var restored=JsonUtility.FromJson<GuildView>(JsonUtility.ToJson(guild));Assert.That(restored.memorials[0].tier,Is.EqualTo(3));
        }
        [Test]
        public void RailOutfitAtlasAndSaveRoundTrip()
        {
            var s=GameState.New();s.heroes[1].unlocked=true;s.gems=600;
            Assert.That(ScrapGuard.Core.Social.Buy(s,Outfits.RailKey).Success,Is.True);Outfits.Equip(s,2);s.collectionTarget=6;
            var copy=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(s));ScrapGuard.Core.Social.Normalize(copy);
            Assert.That(Outfits.Active(copy,1),Is.EqualTo(2));Assert.That(copy.collectionTarget,Is.EqualTo(6));
            var cells=new System.Collections.Generic.HashSet<Rect>();
            for(int pose=0;pose<4;pose++){var art=CharacterArt.Get(1,0,2,pose);Assert.That(art,Is.Not.Null);Assert.That(art.texture.name,Is.EqualTo("Ray-RailRanger-v1"));cells.Add(art.rect);}
            Assert.That(cells.Count,Is.EqualTo(4));Assert.That(CharacterArt.Get(0,0,2),Is.SameAs(CharacterArt.Get(0,0)));
            var step=JsonUtility.FromJson<ReplayStep>(JsonUtility.ToJson(new ReplayStep{assembly=2}));Assert.That(step.assembly,Is.EqualTo(2));Assert.That(step.skill,Is.EqualTo(-1));
        }
        [Test]
        public void ResearchConsentRetentionAndPrivacy()
        {
            string folder=Path.Combine(Path.GetTempPath(),"scrap-research-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            string path=Path.Combine(folder,"journal.json");var now=DateTimeOffset.UtcNow;
            var journal=new PlaytestJournal(path,()=>now);journal.Add("screen","home");Assert.That(journal.Data.events.Count,Is.Zero);
            journal.Consent(true);journal.Add("preview","outfit_heavy_engineer");journal.Add("profile","User Name");
            Assert.That(journal.Data.events.Count,Is.EqualTo(1));
            var restored=new PlaytestJournal(path,()=>now);Assert.That(restored.Data.consent,Is.True);Assert.That(restored.Data.events.Count,Is.EqualTo(1));
            now=now.AddDays(15);restored.Add("session","start");Assert.That(restored.Data.events.Count,Is.EqualTo(1));
            string participant=restored.Data.participant;restored.Clear();Assert.That(restored.Data.participant,Is.Not.EqualTo(participant));Assert.That(restored.Data.events.Count,Is.Zero);
            restored.Consent(false);restored.Add("screen","home");Assert.That(restored.Data.events.Count,Is.Zero);
            Directory.Delete(folder,true);
        }
        [Test]
        public void FieldAndSupportPresentationDataRoundTrip()
        {
            var report=new BattleReport{task=1,taskChoice=2,taskCompleted=true,mods=63};
            var restored=JsonUtility.FromJson<BattleReport>(JsonUtility.ToJson(report));
            Assert.That(restored.taskCompleted && restored.taskChoice==2 && restored.mods==63,Is.True);
            var step=JsonUtility.FromJson<ReplayStep>("{\"encounter\":2,\"skill\":-1}");
            Assert.That(step.encounter,Is.EqualTo(2));Assert.That(step.frames,Is.Zero);
            var guild=new GuildView{week="2026-11-02",featuredId="helper"};guild.members.Add(new GuildMember{id="helper",supportUses=4,supportWins=3,helpedMembers=2});
            var copy=JsonUtility.FromJson<GuildView>(JsonUtility.ToJson(guild));Assert.That(copy.members[0].helpedMembers,Is.EqualTo(2));Assert.That(copy.featuredId,Is.EqualTo("helper"));
        }
        [Test]
        public void OutfitAndGuideRoundTrip()
        {
            var state=GameState.New();state.gems=600;ScrapGuard.Core.Social.Buy(state,Outfits.Key);Outfits.Equip(state,1);
            state.guideMoved=state.guideUltimate=true;
            var restored=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(state));new Economy(restored,()=>DateTimeOffset.UtcNow);
            Assert.That(Outfits.Active(restored,0),Is.EqualTo(1));Assert.That(restored.gems,Is.Zero);Assert.That(restored.guideMoved && restored.guideUltimate,Is.True);
            var rects=new System.Collections.Generic.HashSet<Rect>();
            for(int pose=0;pose<4;pose++)
            {
                var art=CharacterArt.Get(0,0,1,pose);Assert.That(art,Is.Not.Null);Assert.That(art.texture.name,Is.EqualTo("Luke-HeavyEngineer-v1"));
                rects.Add(art.rect);Assert.That(art.rect.xMax,Is.LessThanOrEqualTo(art.texture.width));Assert.That(art.rect.yMax,Is.LessThanOrEqualTo(art.texture.height));
            }
            Assert.That(rects.Count,Is.EqualTo(4));Assert.That(CharacterArt.Get(1,0,1),Is.SameAs(CharacterArt.Get(1,0)));
        }

        [TestCaseSource(typeof(CoreSpecification), nameof(CoreSpecification.Names))]
        public void CoreRules(string name) { CoreSpecification.Run(name); }
        [Test]
        public void SaveRoundTripAndBackupRecovery()
        {
            string directory = Path.Combine(Path.GetTempPath(), "scrap-save-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "profile.json");
            try
            {
                var store = new ProfileStore(path);
                Assert.That(store.Change(e => e.Attend()).Success, Is.True);
                int gold = store.State.gold;
                Assert.That(store.Change(e => e.Upgrade(e.State.equipped[0])).Success, Is.True);
                var restored = new ProfileStore(path);
                Assert.That(restored.State.items[0].level, Is.EqualTo(2));
                Assert.That(restored.State.gold, Is.EqualTo(gold - 300));
                File.WriteAllText(path, "{broken");
                var backup = new ProfileStore(path);
                Assert.That(backup.State.gold, Is.EqualTo(gold));
                Assert.That(backup.Notice, Does.Contain("백업"));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
        [Test]
        public void BrokenSavesArePreserved()
        {
            string directory = Path.Combine(Path.GetTempPath(), "scrap-broken-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory); string path = Path.Combine(directory, "profile.json");
            try
            {
                File.WriteAllText(path, "broken"); var store = new ProfileStore(path);
                Assert.That(store.ReadOnly, Is.True); store.Change(e => e.Attend());
                Assert.That(File.ReadAllText(path), Is.EqualTo("broken"));
            }
            finally { Directory.Delete(directory, true); }
        }
        [Test]
        public void RemoteCacheDoesNotAdvanceServerDay()
        {
            string folder = Path.Combine(Path.GetTempPath(), "scrap-remote-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new ProfileStore(Path.Combine(folder,"cache.json"));
                var state = GameState.New(); state.serviceDate = "2020-01-01"; state.dailyKills = 432;
                store.ReplaceRemote(state);
                Assert.That(store.State.serviceDate, Is.EqualTo("2020-01-01")); Assert.That(store.State.dailyKills, Is.EqualTo(432));
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder,true); }
        }
        [Test]
        public void CloudJournalSurvivesClientRestart()
        {
            string folder = Path.Combine(Path.GetTempPath(),"scrap-journal-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
            try
            {
                var command = new GameCommand("battle_finish","run") { revision = 42, replay = new System.Collections.Generic.List<ReplayStep> { new ReplayStep { frames = 120, x = -85 } } };
                File.WriteAllText(Path.Combine(folder,"cloud-pending.json"),JsonUtility.ToJson(command));
                var restored = new CloudClient(folder).Pending;
                Assert.That(restored.requestId,Is.EqualTo(command.requestId)); Assert.That(restored.revision,Is.EqualTo(42));
                Assert.That(restored.replay[0].skill,Is.EqualTo(-1)); Assert.That(restored.replay[0].frames,Is.EqualTo(120));
            }
            finally { Directory.Delete(folder,true); }
        }
        [Test]
        public void CloudEndpointRequiresHttpsOutsideLoopback()
        {
            Assert.That(CloudClient.ValidEndpoint("https://game.example.com"),Is.True);
            Assert.That(CloudClient.ValidEndpoint("http://game.example.com"),Is.False);
            Assert.That(CloudClient.ValidEndpoint("https://user:secret@game.example.com"),Is.False);
            Assert.That(CloudClient.ValidEndpoint("https://game.example.com?key=x"),Is.False);
        }
        [Test]
        public void AllCharacterArtVariantsAreAvailable()
        {
            var distinct = new System.Collections.Generic.HashSet<string>();
            for (int hero = 0; hero < HeroCatalog.Count; hero++) for (int skin = 0; skin < 3; skin++)
            {
                var sprite = CharacterArt.Get(hero,skin); Assert.That(sprite,Is.Not.Null);
                Assert.That(sprite.rect.xMin,Is.GreaterThanOrEqualTo(0)); Assert.That(sprite.rect.yMin,Is.GreaterThanOrEqualTo(0));
                Assert.That(sprite.rect.xMax,Is.LessThanOrEqualTo(sprite.texture.width)); Assert.That(sprite.rect.yMax,Is.LessThanOrEqualTo(sprite.texture.height));
                distinct.Add(sprite.texture.name+":"+sprite.rect);
            }
            Assert.That(distinct.Count,Is.EqualTo(HeroCatalog.Count*3));
            Assert.That(CharacterArt.Get(0,1),Is.SameAs(CharacterArt.Get(0,1)));
        }
        [Test]
        public void AllSixteenEnemyAnimationFramesAreAvailable()
        {
            var rects = new System.Collections.Generic.HashSet<Rect>();
            for(int kind=0;kind<4;kind++) for(int frame=0;frame<4;frame++)
            {
                var sprite=BattleArt.Enemy(kind,frame); Assert.That(sprite,Is.Not.Null);
                Assert.That(sprite.rect.xMin,Is.GreaterThanOrEqualTo(0)); Assert.That(sprite.rect.yMin,Is.GreaterThanOrEqualTo(0));
                Assert.That(sprite.rect.xMax,Is.LessThanOrEqualTo(sprite.texture.width)); Assert.That(sprite.rect.yMax,Is.LessThanOrEqualTo(sprite.texture.height));
                rects.Add(sprite.rect);
            }
            Assert.That(rects.Count,Is.EqualTo(16));
            Assert.That(Resources.Load<Texture2D>("Art/ContinuousFloor-v1"),Is.Not.Null);
            foreach(string archived in new[]{"ArenaProps-v2","EnemyRoles-v2","EnemyRoles-v3","ExpansionBosses-v1","ExpansionBosses-v2","Landmarks-v1","OutfitConcepts-v1","Refinery-v1","RegionBosses-v1","RegionBosses-v2"})
                Assert.That(Resources.Load<Texture2D>("Art/"+archived),Is.Null,"Archived texture must stay out of Resources: "+archived);
        }
        [Test]
        public void ServerNullSupportDoesNotBecomeAGhostCompanion()
        {
            var ticket=JsonUtility.FromJson<BattleTicket>("{\"id\":\"test\",\"stage\":4,\"mode\":\"practice\",\"hasSupport\":false,\"support\":null}");
            var combat=new Combat(GameState.New(),ticket.stage,ticket.id,1,false,ticket.mode,ticket.GetSupport());
            Assert.That(combat.Support,Is.Null);
            ticket=JsonUtility.FromJson<BattleTicket>("{\"id\":\"test\",\"hasSupport\":true,\"support\":{\"hero\":0,\"power\":130}}");
            combat=new Combat(GameState.New(),0,ticket.id,1,false,"",ticket.GetSupport());
            Assert.That(combat.Support.power,Is.EqualTo(130));
        }
        [TestCase("artillery")]
        [TestCase("walker")]
        [TestCase("fortress")]
        public void RobotArtworkIsLoadableAs2DTexture(string frame)
        {
            var texture=Resources.Load<Texture2D>("Art/RobotFrame-"+frame+"-v1");
            Assert.That(texture,Is.Not.Null,"A cubemap cannot be loaded by the runtime sprite renderer.");
            Assert.That(texture.width,Is.LessThanOrEqualTo(512));
            Assert.That(ArenaArt.Cell("RobotFrame-"+frame+"-v1",1,1,0,0),Is.Not.Null);
        }
        [Test]
        public void FontContainsKoreanGlyphs()
        {
            var font = Resources.Load<Font>("Fonts/GowunDodum-Regular");
            Assert.That(font, Is.Not.Null); Assert.That(font.HasCharacter('고'), Is.True); Assert.That(font.HasCharacter('석'), Is.True);
        }
        [Test]
        public void RoleAndBossArtHasAllStates()
        {
            for(int set=0;set<2;set++)
            {
                var rects=new System.Collections.Generic.HashSet<string>();
                for(int role=0;role<(set==0?6:Campaign.Regions.Length);role++)for(int pose=0;pose<4;pose++)
                {
                    var sprite=BattleArt.Role(role,pose,set==1);Assert.That(sprite,Is.Not.Null);
                    Assert.That(sprite.rect.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(sprite.rect.yMin,Is.GreaterThanOrEqualTo(0));
                    Assert.That(sprite.rect.xMax,Is.LessThanOrEqualTo(sprite.texture.width+.01f));Assert.That(sprite.rect.yMax,Is.LessThanOrEqualTo(sprite.texture.height+.01f));
                    rects.Add(sprite.texture.name+":"+sprite.rect);
                }
                Assert.That(rects.Count,Is.EqualTo(set==0?24:Campaign.Regions.Length*4));
            }
        }
        [Test]
        public void RegionPaintAndReportsSurviveUnityJson()
        {
            var state=GameState.New();var e=new Economy(state,()=>DateTimeOffset.UtcNow);Workshop.Region(state,4).blueprints=8;
            Assert.That(e.CraftRegionSkin(4).Success,Is.True);
            Assert.That(ScrapGuard.Core.Social.Customize(state,0,0,0,7).Success,Is.True);
            state.receipts.Add(new Receipt {id="report",report=new BattleReport {cause=5,mods=7,bossRemaining=.25f}});
            var restored=JsonUtility.FromJson<GameState>(JsonUtility.ToJson(state));new Economy(restored,()=>DateTimeOffset.UtcNow);
            Assert.That(ScrapGuard.Core.Social.HasSkin(restored,7),Is.True);Assert.That(restored.selectedSkin,Is.EqualTo(7));
            Assert.That(Workshop.Region(restored,4).blueprints,Is.Zero);
            Assert.That(restored.receipts[0].report.mods,Is.EqualTo(7));Assert.That(restored.receipts[0].report.bossRemaining,Is.EqualTo(.25f));
            for(int hero=0;hero<HeroCatalog.Count;hero++)for(int skin=3;skin<ScrapGuard.Core.Social.Skins.Length;skin++)Assert.That(CharacterArt.Get(hero,skin),Is.Not.Null);
        }
        [Test]
        public void LegacyThreeHeroSaveMigratesAndPersistsNewRecruit()
        {
            string directory=Path.Combine(Path.GetTempPath(),"scrap-legacy-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            string path=Path.Combine(directory,"profile.json");
            try
            {
                var old=GameState.New();old.heroes.RemoveRange(3,2);old.heroes[1].unlocked=true;old.heroes[1].level=9;old.selectedHero=1;old.clearedStages.Add(11);old.gems=321;
                File.WriteAllText(path,JsonUtility.ToJson(old));var store=new ProfileStore(path);
                Assert.That(store.ReadOnly,Is.False);Assert.That(store.State.heroes.Count,Is.EqualTo(5));Assert.That(store.State.heroes[1].level,Is.EqualTo(9));
                Assert.That(store.Change(e=>e.RecruitHero(3)).Success,Is.True);
                Assert.That(store.Change(e=>e.SelectHero(3)).Success,Is.True);
                var restored=new ProfileStore(path);Assert.That(restored.State.selectedHero,Is.EqualTo(3));Assert.That(restored.State.gems,Is.EqualTo(321));Assert.That(restored.State.heroes[4].unlocked,Is.False);
            }
            finally { Directory.Delete(directory,true); }
        }
    }
}
