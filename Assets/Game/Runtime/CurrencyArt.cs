using UnityEngine;
using UnityEngine.UI;

namespace ScrapGuard
{
    public static class CurrencyArt
    {
        static readonly Sprite[] icons = new Sprite[3];
        public static readonly string[] Names = { "골드", "보석", "부품" };
        public static Color Tint(int kind) { return kind == 0 ? Ui.Gold : kind == 1 ? Ui.Mint : Ui.Blue; }
        public static Sprite Icon(int kind)
        {
            if (icons[kind] != null) return icons[kind];
            var atlas = Resources.Load<Texture2D>("Art/CurrencyIcons-v1");
            if (atlas == null) return null;
            float width = atlas.width / 3f;
            for (int i = 0; i < 3; i++)
                icons[i] = Sprite.Create(atlas, new Rect(width * i, 0, width, atlas.height), new Vector2(.5f,.5f), 100);
            return icons[kind];
        }
        public static Image Draw(Transform parent, int kind, float x, float y, float size)
        {
            var image = Ui.Rect(parent, Names[kind] + " 아이콘", x, y, size, size).gameObject.AddComponent<Image>();
            image.sprite = Icon(kind); image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }
        public static Text Balance(Transform parent, int kind, float x, float y, float width)
        {
            var panel = Ui.Box(parent, x, y, width, 52, Ui.Panel).transform;
            Draw(panel, kind, 6, 7, 38);
            Ui.Label(panel, Names[kind], 49, 3, width-54, 19, 11, Ui.Muted);
            return Ui.Label(panel, "0", 49, 20, width-54, 28, 17, Tint(kind));
        }
    }
}
