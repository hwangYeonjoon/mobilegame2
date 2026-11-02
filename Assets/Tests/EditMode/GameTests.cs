using System;
using System.IO;
using NUnit.Framework;
using ScrapGuard.Core;
using UnityEngine;

namespace ScrapGuard.Tests
{
    public class GameTests
    {
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
        public void AllNineCharacterArtVariantsAreAvailable()
        {
            var distinct = new System.Collections.Generic.HashSet<Rect>();
            for (int hero = 0; hero < 3; hero++) for (int skin = 0; skin < 3; skin++)
            {
                var sprite = CharacterArt.Get(hero,skin); Assert.That(sprite,Is.Not.Null);
                Assert.That(sprite.rect.xMin,Is.GreaterThanOrEqualTo(0)); Assert.That(sprite.rect.yMin,Is.GreaterThanOrEqualTo(0));
                Assert.That(sprite.rect.xMax,Is.LessThanOrEqualTo(sprite.texture.width)); Assert.That(sprite.rect.yMax,Is.LessThanOrEqualTo(sprite.texture.height));
                distinct.Add(sprite.rect);
            }
            Assert.That(distinct.Count,Is.EqualTo(9));
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
            Assert.That(Resources.Load<Texture2D>("Art/Refinery-v1"),Is.Not.Null);
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
        [Test]
        public void FontContainsKoreanGlyphs()
        {
            var font = Resources.Load<Font>("Fonts/GowunDodum-Regular");
            Assert.That(font, Is.Not.Null); Assert.That(font.HasCharacter('고'), Is.True); Assert.That(font.HasCharacter('석'), Is.True);
        }
        [Test]
        public void RoleAndBossArtHasAllFortyFourStates()
        {
            for(int set=0;set<2;set++)
            {
                var rects=new System.Collections.Generic.HashSet<Rect>();
                for(int role=0;role<(set==0?6:5);role++)for(int pose=0;pose<4;pose++)
                {
                    var sprite=BattleArt.Role(role,pose,set==1);Assert.That(sprite,Is.Not.Null);
                    Assert.That(sprite.rect.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(sprite.rect.yMin,Is.GreaterThanOrEqualTo(0));
                    Assert.That(sprite.rect.xMax,Is.LessThanOrEqualTo(sprite.texture.width+.01f));Assert.That(sprite.rect.yMax,Is.LessThanOrEqualTo(sprite.texture.height+.01f));
                    rects.Add(sprite.rect);
                }
                Assert.That(rects.Count,Is.EqualTo(set==0?24:20));
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
            for(int hero=0;hero<3;hero++)for(int skin=3;skin<8;skin++)Assert.That(CharacterArt.Get(hero,skin),Is.Not.Null);
        }
    }
}
