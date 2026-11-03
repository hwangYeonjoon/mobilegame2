using UnityEngine;
using UnityEngine.UI;

namespace ScrapGuard
{
    public static class CharacterArt
    {
        static Texture2D atlas, expansion, outfitAtlas;
        static readonly Sprite[] outfitSprites=new Sprite[4];
        static bool loaded;
        static readonly Sprite[,] sprites = new Sprite[ScrapGuard.Core.HeroCatalog.Count,3];
        public static Sprite Get(int hero, int skin, int outfit = 0, int pose = 0)
        {
            if (!ScrapGuard.Core.HeroCatalog.Valid(hero) || skin < 0 || skin >= ScrapGuard.Core.Social.Skins.Length) return null;
            if(hero==0 && outfit==1)
            {
                if(outfitAtlas==null)outfitAtlas=Resources.Load<Texture2D>("Art/Luke-HeavyEngineer-v1");
                if(outfitAtlas!=null)
                {
                    pose=Mathf.Clamp(pose,0,3);
                    if(outfitSprites[pose]==null)outfitSprites[pose]=Sprite.Create(outfitAtlas,new Rect((pose%2)*outfitAtlas.width/2f,(1-pose/2)*outfitAtlas.height/2f,outfitAtlas.width/2f,outfitAtlas.height/2f),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
                    return outfitSprites[pose];
                }
            }
            if(skin>=3)skin=0;
            if(hero>=3)
            {
                if(expansion==null)expansion=Resources.Load<Texture2D>("Art/Guardians-expansion-v1");
                if(expansion==null)return null;
                if(sprites[hero,skin]==null)
                    sprites[hero,skin]=Sprite.Create(expansion,new Rect((hero-3)*expansion.width/2f,(2-skin)*expansion.height/3f,expansion.width/2f,expansion.height/3f),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
                return sprites[hero,skin];
            }
            if (!loaded) { atlas = Resources.Load<Texture2D>("Art/Guardians-v1"); loaded = true; }
            if (atlas == null) return null;
            if (sprites[hero,skin] != null) return sprites[hero,skin];
            // The guardian's shield extends further left than the nominal atlas cell.
            // These measured column gutters preserve every silhouette without editing the PNG.
            int[] edges = { 0, 418, 816, 1254 };
            float scale = atlas.width / 1254f;
            var rect = new Rect(edges[hero]*scale, (2-skin)*atlas.height/3f,
                (edges[hero+1]-edges[hero])*scale, atlas.height/3f);
            sprites[hero,skin] = Sprite.Create(atlas,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            sprites[hero,skin].name = "Guardian-" + hero + "-" + skin;
            return sprites[hero,skin];
        }
        public static Color Tint(int skin)
        {
            if(skin<3 || skin>=ScrapGuard.Core.Social.Skins.Length)return Color.white;
            return new[]{Ui.Hex("FFD1A4"),Ui.Hex("FFBA83"),Ui.Hex("A8F7B4"),Ui.Hex("B8E6FF"),Ui.Hex("E9B4FF"),Ui.Hex("FFF0A3"),Ui.Hex("FFA59B")}[skin-3];
        }
        public static Image Draw(Transform parent, float x, float y, float width, float height, int hero, int skin, int outfit = 0)
        {
            var image = Ui.Rect(parent,"GuardianArt",x,y,width,height).gameObject.AddComponent<Image>();
            image.sprite = Get(hero,skin,outfit); image.color=hero==0 && outfit==1 ? Color.white : Tint(skin); image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }
    }
}
