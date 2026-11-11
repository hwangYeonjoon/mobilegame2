using NUnit.Framework;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard.Tests
{
    public class PresentationTests
    {
        [Test] public void RegionalAtlasAndPooledVisualsUseCorrectFamily()
        {
            var root=new GameObject("regional-fixture",typeof(RectTransform));
            try
            {
                var v=EnemyVisual.Create(root.transform).GetComponent<EnemyVisual>();
                var sprites=new System.Collections.Generic.HashSet<Sprite>();
                for(int region=0;region<10;region++)for(int role=0;role<6;role++)
                {
                    var sprite=BattleArt.Regional(region,(EnemyRole)role);
                    Assert.That(sprite,Is.Not.Null);Assert.That(sprites.Add(sprite),Is.True);
                    Assert.That(sprite.rect.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(sprite.rect.yMin,Is.GreaterThanOrEqualTo(0));
                    Assert.That(sprite.rect.xMax,Is.LessThanOrEqualTo(sprite.texture.width));Assert.That(sprite.rect.yMax,Is.LessThanOrEqualTo(sprite.texture.height));
                    var enemy=new Enemy{role=(EnemyRole)role,position=new V2(200,300),hp=100,maxHp=100};string before=JsonUtility.ToJson(enemy);
                    v.Render(enemy,region,0);Assert.That(JsonUtility.ToJson(enemy),Is.EqualTo(before));
                    Assert.That(System.Array.Exists(v.GetComponentsInChildren<Image>(),i=>i.sprite==sprite),Is.True);
                    v.ResetState();
                }
                Assert.That(BattleArt.Regional(-1,EnemyRole.Artillery),Is.SameAs(BattleArt.Role(2,0)));
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void PooledProjectilesResetAndPreserveSpecialAttackIdentity()
        {
            var root=new GameObject("fixture",typeof(RectTransform));
            try
            {
                var visual=BulletVisual.Create(root.transform).GetComponent<BulletVisual>();
                for(int outfit=1;outfit<=Outfits.Count;outfit++)
                {
                    var bullet=new Bullet{velocity=new V2(1,0),damage=17,life=2,pierce=1};string before=JsonUtility.ToJson(bullet);
                    visual.Render(bullet,1,outfit,0);Assert.That(visual.Signature,Is.EqualTo(outfit));Assert.That(JsonUtility.ToJson(bullet),Is.EqualTo(before));
                    bullet.mod=true;visual.Render(bullet,1,outfit,0);Assert.That(visual.Signature,Is.Zero);
                    bullet.mod=false;bullet.chilling=true;visual.Render(bullet,1,outfit,0);Assert.That(visual.Signature,Is.EqualTo(outfit));
                    Assert.That(visual.GetComponentInChildren<OutfitMark>().color,Is.EqualTo(Ui.Hex("91EAFF")));
                    bullet.chilling=false;bullet.drone=true;visual.Render(bullet,1,outfit,0);Assert.That(visual.Signature,Is.Zero);
                    visual.gameObject.SetActive(false);visual.gameObject.SetActive(true);
                    bullet.drone=false;visual.Render(bullet,1,0,0);Assert.That(visual.Signature,Is.Zero);
                    foreach(var graphic in root.GetComponentsInChildren<Graphic>())Assert.That(graphic.raycastTarget,Is.False);
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void AppearanceComparisonRestoresBaseSpriteAndRemovesOrnaments()
        {
            var root=new GameObject("fixture",typeof(RectTransform));
            try
            {
                for(int id=1;id<=Outfits.Count;id++)
                {
                    int hero=Outfits.Hero(id);var image=CharacterArt.Draw(root.transform,0,0,96,100,hero,0,id);
                    var mark=image.GetComponent<OutfitAttachment>().Mark;Assert.That(mark.enabled,Is.True);
                    Assert.That(mark.transform.GetSiblingIndex(),Is.LessThan(image.transform.GetSiblingIndex()));
                    StylePresentation.SetLook(image,hero,0,0,0);Assert.That(mark.enabled,Is.False);Assert.That(image.sprite,Is.SameAs(CharacterArt.Get(hero,0)));
                    StylePresentation.SetLook(image,hero,0,id,3);Assert.That(mark.enabled,Is.True);Assert.That(mark.Outfit,Is.EqualTo(id));
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
