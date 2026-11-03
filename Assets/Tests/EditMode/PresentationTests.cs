using NUnit.Framework;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard.Tests
{
    public class PresentationTests
    {
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
