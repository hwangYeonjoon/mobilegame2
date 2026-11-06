using NUnit.Framework;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ScrapGuard.Tests
{
    public class BattlePresentationSpecification
    {
        [Test]
        public void EveryCampaignLayoutUsesTheSimulationFootprints()
        {
            for(int stage=0;stage<Campaign.Count;stage++)
            {
                var root=new GameObject("arena",typeof(RectTransform));
                try
                {
                    var scenery=ArenaScenery.Build(root.transform,stage,false);var definition=Campaign.Get(stage);
                    var tags=scenery.GetComponentsInChildren<ArenaGeometryTag>();
                    Assert.That(tags.Length,Is.EqualTo(Campaign.WorldGeometry(Campaign.Blocks(definition)).Length+Campaign.WorldGeometry(Campaign.Zones(definition)).Length));
                    int solids=0,zones=0;
                    foreach(var tag in tags)
                    {
                        var expected=tag.Solid?Campaign.WorldGeometry(Campaign.Blocks(definition))[solids++]:Campaign.WorldGeometry(Campaign.Zones(definition))[zones++];
                        Assert.That(tag.Bounds,Is.EqualTo(expected));
                        var rect=(RectTransform)tag.transform;
                        Assert.That(rect.anchoredPosition,Is.EqualTo(new Vector2(expected.X,-expected.Y)));
                        Assert.That(rect.sizeDelta,Is.EqualTo(new Vector2(expected.W,expected.H)));
                    }
                    foreach(var graphic in scenery.GetComponentsInChildren<Graphic>())Assert.That(graphic.raycastTarget,Is.False);
                }
                finally{Object.DestroyImmediate(root);}
            }
        }
        [Test]
        public void ParticleBudgetAndLifetimeStayBounded()
        {
            var root=new GameObject("particles",typeof(RectTransform));
            try
            {
                var particles=CombatParticles.Create(root.transform);
                particles.Burst(new V2(250,500),1000,Color.white);
                Assert.That(particles.Count,Is.EqualTo(CombatParticles.Capacity));
                particles.Advance(0);Assert.That(particles.Count,Is.EqualTo(CombatParticles.Capacity));
                particles.Advance(1);Assert.That(particles.Count,Is.Zero);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void EnemyPoolResetsHitAndRageFeedback()
        {
            var root=new GameObject("actors",typeof(RectTransform));
            try
            {
                var actor=EnemyVisual.Create(root.transform).GetComponent<EnemyVisual>();
                var enemy=new Enemy{kind=3,finalBoss=true,hp=40,maxHp=100,flash=.08f};
                actor.Render(enemy,0,0);Assert.That(actor.JustHit,Is.True);Assert.That(actor.JustRaged,Is.True);
                actor.Render(enemy,0,0);Assert.That(actor.JustHit,Is.False);Assert.That(actor.JustRaged,Is.False);
                enemy.windup=1;actor.Render(enemy,0,0);Assert.That(actor.Pose,Is.EqualTo(1));
                enemy.windup=0;enemy.attackFlash=1;actor.Render(enemy,0,0);Assert.That(actor.Pose,Is.EqualTo(2));
                actor.gameObject.SetActive(false);actor.ResetState();actor.gameObject.SetActive(true);actor.Render(enemy,0,0);
                Assert.That(actor.JustRaged,Is.True);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void ChallengeHasDistinctFloorAndNoCampaignObstacles()
        {
            var root=new GameObject("challenge",typeof(RectTransform));
            try
            {
                var scenery=ArenaScenery.Build(root.transform,4,true);
                Assert.That(scenery.GetComponentsInChildren<ArenaGeometryTag>(),Is.Empty);
                var seen=new System.Collections.Generic.HashSet<Rect>();
                for(int region=0;region<8;region++){var sprite=ArenaArt.Floor(region);Assert.That(sprite,Is.Not.Null);seen.Add(sprite.rect);Assert.That(ArenaArt.Prop(region),Is.Not.Null);Assert.That(ArenaArt.Landmark(region),Is.Not.Null);}
                Assert.That(seen.Count,Is.EqualTo(8));
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
