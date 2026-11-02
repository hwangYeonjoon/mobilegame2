using UnityEngine;
using UnityEngine.UI;

namespace ScrapGuard
{
    public static class CharacterArt
    {
        static Texture2D atlas;
        static bool loaded;
        static readonly Sprite[,] sprites = new Sprite[3,3];
        public static Sprite Get(int hero, int skin)
        {
            if (hero < 0 || hero > 2 || skin < 0 || skin >= ScrapGuard.Core.Social.Skins.Length) return null;
            if(skin>=3)skin=0;
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
            if(skin<3 || skin>7)return Color.white;
            return new[]{Ui.Hex("FFD1A4"),Ui.Hex("FFBA83"),Ui.Hex("A8F7B4"),Ui.Hex("B8E6FF"),Ui.Hex("E9B4FF")}[skin-3];
        }
        public static Image Draw(Transform parent, float x, float y, float width, float height, int hero, int skin)
        {
            var image = Ui.Rect(parent,"GuardianArt",x,y,width,height).gameObject.AddComponent<Image>();
            image.sprite = Get(hero,skin); image.color=Tint(skin); image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }
    }
}
