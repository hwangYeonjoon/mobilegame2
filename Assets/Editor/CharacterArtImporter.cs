using UnityEditor;
using UnityEngine;
namespace ScrapGuard.Editor
{
    public sealed class CharacterArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Art/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None; importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = assetPath.Contains("SalvageMachine")?512:assetPath.Contains("ContinuousFloor")?1024:2048;
            if(assetPath.Contains("ContinuousFloor")){importer.textureType=TextureImporterType.Default;importer.wrapMode=TextureWrapMode.Mirror;} importer.textureCompression = TextureImporterCompression.Uncompressed;
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true; android.maxTextureSize = importer.maxTextureSize;
            android.format = TextureImporterFormat.ETC2_RGBA8; importer.SetPlatformTextureSettings(android);
        }
    }
}
