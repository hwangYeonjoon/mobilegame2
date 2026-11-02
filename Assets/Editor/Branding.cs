using System.IO;
using UnityEditor;
using UnityEngine;

namespace ScrapGuard.Editor
{
    public static class Branding
    {
        public static void Apply()
        {
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = Ui.Background;
            const string path = "Assets/Art/AppIcon.png";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory("Assets/Art");
                var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
                {
                    Color c = Ui.Background;
                    if (Disc(x, y, 128, 120, 106)) c = Ui.Panel;
                    if (Rect(x,y,77,38,38,43) || Rect(x,y,141,38,38,43)) c = Ui.Raised;
                    if (Rect(x,y,66,75,124,89) || Rect(x,y,40,83,26,62) || Rect(x,y,190,83,26,62)) c = Ui.Mint;
                    if (Rect(x,y,85,92,86,48)) c = Ui.Background;
                    if (Disc(x,y,128,116,18)) c = Ui.Gold;
                    if (Rect(x,y,77,170,102,55)) c = Ui.Mint;
                    if (Rect(x,y,89,185,78,27)) c = Ui.Background;
                    if (Rect(x,y,98,192,20,12) || Rect(x,y,138,192,20,12)) c = Ui.Ink;
                    if (Rect(x,y,124,222,8,16)) c = Ui.Raised;
                    if (Disc(x,y,128,239,9)) c = Ui.Gold;
                    texture.SetPixel(x,y,c);
                }
                texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { AssetDatabase.LoadAssetAtPath<Texture2D>(path) });
        }
        static bool Rect(int x,int y,int left,int bottom,int width,int height) { return x >= left && x < left + width && y >= bottom && y < bottom + height; }
        static bool Disc(int x,int y,int cx,int cy,int radius) { return (x-cx)*(x-cx)+(y-cy)*(y-cy)<radius*radius; }
    }
}
