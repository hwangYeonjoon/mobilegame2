using NUnit.Framework;
namespace ScrapGuard.Tests
{
    public class ServiceTests
    {
        [Test] public void UnconfiguredBuildDoesNotRequestAdvertising()
        {
            var settings=new ServiceSettings();
            Assert.IsFalse(settings.adsEnabled);Assert.IsFalse(settings.productionAds);
            Assert.IsFalse(new RewardedAds(settings).Available);
        }
        [Test] public void AdReplyCannotReplaceAnotherAccountOrNewerRevision()
        {
            string folder=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"scrap-ad-client-"+System.Guid.NewGuid().ToString("N"));
            try
            {
                var client=new CloudClient(folder);client.Session.playerId="account-a";client.Session.revision=3;
                var claim=new ScrapGuard.Core.AdClaim{reply=new ScrapGuard.Core.ServerReply{playerId="account-b",revision=4,state=ScrapGuard.Core.GameState.New()}};
                Assert.IsFalse(client.AcceptAd(claim));
                claim.reply.playerId="account-a";claim.reply.revision=2;Assert.IsFalse(client.AcceptAd(claim));
                Assert.AreEqual(3,client.Session.revision);
                claim.reply.revision=4;Assert.IsTrue(client.AcceptAd(claim));Assert.AreEqual(4,client.Session.revision);
            }
            finally {if(System.IO.Directory.Exists(folder))System.IO.Directory.Delete(folder,true);}
        }
        [Test] public void PublicServiceLinksRejectPlaceholdersAndCredentials()
        {
            Assert.IsFalse(ServiceSettings.PublicUrl(""));
            Assert.IsFalse(ServiceSettings.PublicUrl("https://game.example.com/privacy"));
            Assert.IsFalse(ServiceSettings.PublicUrl("http://game.test/privacy"));
            Assert.IsFalse(ServiceSettings.PublicUrl("https://secret@game.test/privacy"));
            Assert.IsTrue(ServiceSettings.PublicUrl("https://game.test/privacy"));
        }
    }
}
