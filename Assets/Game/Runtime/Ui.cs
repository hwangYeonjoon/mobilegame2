using System;
using UnityEngine;
using UnityEngine.UI;

namespace ScrapGuard
{
    public static class Ui
    {
        public static readonly Color Background = Hex("101C29"), Panel = Hex("1C2D3D"), Raised = Hex("263B4C"), Muted = Hex("92A8B9"), Ink = Hex("EAF3F2"), Mint = Hex("65E4BC"), Gold = Hex("FFC86C"), Red = Hex("FF817D"), Blue = Hex("80B9FF");
        public static Font Font;
        static Sprite rounded, circle;
        public static Color Hex(string hex) { Color c; ColorUtility.TryParseHtmlString("#" + hex, out c); return c; }
        public static Sprite Rounded { get { if (rounded == null) rounded = Shape(false); return rounded; } }
        public static Sprite Circle { get { if (circle == null) circle = Shape(true); return circle; } }
        static Sprite Shape(bool round)
        {
            const int size = 64; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false); texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = round ? x - 31.5f : Math.Max(0, Math.Abs(x - 31.5f) - 19);
                float dy = round ? y - 31.5f : Math.Max(0, Math.Abs(y - 31.5f) - 19);
                float distance = Mathf.Sqrt(dx * dx + dy * dy), radius = round ? 31 : 12;
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(radius - distance + .5f)));
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
        }
        public static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); return rect;
        }
        public static Image Box(Transform parent, float x, float y, float w, float h, Color color, bool soft = true, string name = "Panel")
        {
            var image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
            if (soft) { image.sprite = Rounded; image.type = Image.Type.Sliced; }
            return image;
        }
        public static Image Disc(Transform parent, float x, float y, float size, Color color)
        {
            var image = Box(parent, x, y, size, size, color); image.sprite = Circle; image.type = Image.Type.Simple; return image;
        }
        public static Text Label(Transform parent, string value, float x, float y, float w, float h, int size = 18, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var text = Rect(parent, "Text", x, y, w, h).gameObject.AddComponent<Text>();
            text.font = Font; text.text = value; text.fontSize = size; text.color = color ?? Ink; text.alignment = align;
            text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = .92f;
            text.supportRichText = false; return text;
        }
        public static Button Button(Transform parent, string label, float x, float y, float w, float h, Action clicked, Color? color = null, bool enabled = true, int size = 18)
        {
            var image = Box(parent, x, y, w, h, color ?? Mint); image.raycastTarget = true; image.name = label;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.interactable = enabled;
            var colors = button.colors; colors.highlightedColor = new Color(.9f, 1, .96f); colors.pressedColor = new Color(.7f, .8f, .8f); colors.disabledColor = new Color(.36f, .43f, .48f); button.colors = colors;
            Label(image.transform, label, 8, 0, w - 16, h, size, color == null || color == Mint || color == Gold ? Background : Ink, TextAnchor.MiddleCenter);
            if (clicked != null) button.onClick.AddListener(() => clicked()); return button;
        }
        public static RectTransform Scroll(Transform parent, float x, float y, float w, float h, float contentHeight)
        {
            var viewport = Rect(parent, "Scroll", x, y, w, h);
            var bg = viewport.gameObject.AddComponent<Image>(); bg.color = new Color(0, 0, 0, .001f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect(viewport, "Content", 0, 0, w, contentHeight);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 28; scroll.decelerationRate = .12f;
            return content;
        }
        public static InputField Input(Transform parent, string value, string hint, float x, float y, float w, int limit = 100)
        {
            var box = Box(parent, x, y, w, 52, Background); box.raycastTarget = true;
            var input = box.gameObject.AddComponent<InputField>(); input.targetGraphic = box;
            input.textComponent = Label(box.transform, "", 12, 0, w - 24, 52, 17);
            input.placeholder = Label(box.transform, hint, 12, 0, w - 24, 52, 16, Muted);
            input.characterLimit = limit; input.text = value; input.lineType = InputField.LineType.SingleLine;
            return input;
        }
        public static RectTransform Robot(Transform parent, float x, float y, float scale, int hero, bool enemy = false, int skin = 0)
        {
            var root = Rect(parent, "Robot", x, y, 80, 100); root.localScale = Vector3.one * scale;
            if (!enemy && CharacterArt.Get(hero,skin) != null)
            {
                CharacterArt.Draw(root,-8,0,96,100,hero,skin);
                return root;
            }
            Color c = enemy ? Red : skin == 1 ? Gold : skin == 2 ? Hex("B7E8FF") : hero == 0 ? Mint : hero == 1 ? Gold : Blue;
            Disc(root, 8, 74, 65, new Color(0, 0, 0, .24f)).rectTransform.sizeDelta = new Vector2(65, 20);
            Box(root, 20, 67, 16, 24, Raised); Box(root, 47, 67, 16, 24, Raised);
            Box(root, 13, 34, 55, 43, c); Box(root, 20, 42, 40, 24, Panel);
            Disc(root, 32, 46, 16, c); Box(root, 6, 43, 13, 32, c); Box(root, 63, 40, 14, 33, c);
            Box(root, 19, 8, 43, 30, c); Box(root, 25, 15, 31, 13, Background);
            Box(root, 29, 18, 8, 6, enemy ? Gold : Ink, false); Box(root, 44, 18, 8, 6, enemy ? Gold : Ink, false);
            Box(root, 38, 0, 5, 10, Raised); Disc(root, 35, -4, 11, Gold);
            Box(root, 67, 49, 20, 9, Muted); Box(root, 80, 44, 10, 19, Panel);
            return root;
        }
        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--) { var child = parent.GetChild(i).gameObject; child.SetActive(false); UnityEngine.Object.Destroy(child); }
        }
    }
}
