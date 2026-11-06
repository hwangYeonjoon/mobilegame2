using NUnit.Framework;
using ScrapGuard.Core;
using UnityEngine;

namespace ScrapGuard.Tests
{
    public class BattleClaritySpecification
    {
        [Test]
        public void NewEnemyAndBossAtlasesHaveEveryDistinctFrame()
        {
            for(int boss=0;boss<2;boss++)for(int role=0;role<(boss==0?6:8);role++)
            {
                var rects=new System.Collections.Generic.HashSet<Rect>();
                for(int frame=0;frame<4;frame++)
                {
                    var sprite=BattleArt.Role(role,frame,boss==1);Assert.That(sprite,Is.Not.Null);
                    rects.Add(sprite.rect);Assert.That(sprite.rect.width,Is.GreaterThan(80));
                    Assert.That(sprite.rect.xMax,Is.LessThanOrEqualTo(sprite.texture.width));
                    Assert.That(sprite.rect.yMax,Is.LessThanOrEqualTo(sprite.texture.height));
                }
                Assert.That(rects.Count,Is.EqualTo(4));
            }
        }
        [Test]
        public void MaterialSoundsAreCachedAudibleAndDoNotClip()
        {
            var signatures=new System.Collections.Generic.HashSet<string>();
            for(int i=0;i<12;i++)
            {
                var clip=CombatSoundBank.Get(i);Assert.That(CombatSoundBank.Get(i),Is.SameAs(clip));
                var samples=new float[clip.samples];Assert.That(clip.GetData(samples,0),Is.True);
                float peak=0,energy=0;foreach(var sample in samples){Assert.That(float.IsNaN(sample),Is.False);peak=Mathf.Max(peak,Mathf.Abs(sample));energy+=sample*sample;}
                Assert.That(peak,Is.LessThanOrEqualTo(.701f));Assert.That(energy/samples.Length,Is.GreaterThan(.0001f));
                signatures.Add(samples[100].ToString("R")+samples[700].ToString("R"));
            }
            Assert.That(signatures.Count,Is.EqualTo(12));
        }
        [Test]
        public void AutomaticBudgetRecoversOnlyAfterSustainedHealthyFrames()
        {
            var budget=new BattleVisualBudget();
            budget.Sample(.1f,0,0);Assert.That(budget.Compact,Is.False);
            for(int i=0;i<50;i++)budget.Sample(1f/30,0,0);
            Assert.That(budget.Compact,Is.True);
            for(int i=0;i<120;i++)budget.Sample(1f/60,0,0);
            Assert.That(budget.Compact,Is.True);
            for(int i=0;i<240;i++)budget.Sample(1f/60,0,0);
            Assert.That(budget.Compact,Is.False);
            budget.Sample(0,100,0);Assert.That(budget.Compact,Is.True);
            budget.Mode=1;budget.Sample(0,200,300);Assert.That(budget.Compact,Is.False);
            budget.Mode=2;budget.Sample(0,0,0);Assert.That(budget.Compact,Is.True);
        }
        [Test]
        public void CompactParticlesTrimExistingEffectsAndReuseCapacity()
        {
            var root=new GameObject("budget",typeof(RectTransform));
            try
            {
                var particles=CombatParticles.Create(root.transform);particles.Burst(new V2(),1000,Color.white);
                particles.Limit=48;Assert.That(particles.Count,Is.EqualTo(48));
                particles.Burst(new V2(),1000,Color.white);Assert.That(particles.Count,Is.EqualTo(48));
                particles.Advance(1);Assert.That(particles.Count,Is.Zero);
                particles.Limit=160;particles.Burst(new V2(),1000,Color.white);Assert.That(particles.Count,Is.EqualTo(160));
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void AllHeroesHaveMotionFramesAndHurtHasPriority()
        {
            for(int hero=0;hero<HeroCatalog.Count;hero++)for(int skin=0;skin<3;skin++)
            {
                var cells=new System.Collections.Generic.HashSet<Rect>();
                for(int f=0;f<6;f++){var sprite=GuardianMotion.Frame(hero,skin,f);Assert.That(sprite,Is.Not.Null);cells.Add(sprite.rect);}
                Assert.That(cells.Count,Is.EqualTo(6));
                Assert.That(GuardianMotion.Sample(hero,skin,0,.2f,1,true,true,false),Is.SameAs(GuardianMotion.Frame(hero,skin,5)));
                Assert.That(GuardianMotion.Sample(hero,skin,0,.2f,1,true,false,false),Is.SameAs(GuardianMotion.Frame(hero,skin,4)));
                Assert.That(GuardianMotion.Sample(hero,skin,0,.26f,1,false,false,false),Is.SameAs(GuardianMotion.Frame(hero,skin,2)));
            }
            for(int outfit=1;outfit<=Outfits.Count;outfit++)
                Assert.That(GuardianMotion.Sample(Outfits.Hero(outfit),0,outfit,0,0,false,false,true),Is.SameAs(CharacterArt.Get(Outfits.Hero(outfit),0,outfit,3)));
        }
        [Test]
        public void GoalsReflectActualProgressWithoutMutatingTheSave()
        {
            var state=GameState.New();state.gems=1200;state.outfitTarget=1;state.collectionTarget=2;
            for(int i=0;i<=11;i++)state.clearedStages.Add(i);
            var before=JsonUtility.ToJson(state);var goals=ProgressTargets.Build(state);
            Assert.That(JsonUtility.ToJson(state),Is.EqualTo(before));
            Assert.That(goals.Find(g=>g.Kind=="stage").Id,Is.EqualTo(12));
            Assert.That(goals.Find(g=>g.Kind=="hero"&&g.Id==3).Ready,Is.True);
            Assert.That(goals.Find(g=>g.Kind=="hero"&&g.Id==1).Ready,Is.True);
            Assert.That(goals.Find(g=>g.Kind=="region").Id,Is.EqualTo(2));
            Assert.That(goals.Find(g=>g.Kind=="outfit").Id,Is.EqualTo(1));
            state.gemDebt=1;state.heroes[3].unlocked=true;goals=ProgressTargets.Build(state);
            Assert.That(goals.Exists(g=>g.Kind=="hero"&&g.Id==3),Is.False);
            Assert.That(goals.Find(g=>g.Kind=="hero"&&g.Id==1).Ready,Is.False);
            Assert.That(goals.Find(g=>g.Kind=="outfit").Ready,Is.False);
        }
    }
}
