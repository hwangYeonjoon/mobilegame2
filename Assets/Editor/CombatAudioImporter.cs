using UnityEditor;
using UnityEngine;
namespace ScrapGuard.Editor
{
    public sealed class CombatAudioImporter : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if(!assetPath.StartsWith("Assets/Resources/Audio/Licensed/"))return;
            var importer=(AudioImporter)assetImporter;
            bool music=assetPath.EndsWith("Factory.ogg")||assetPath.EndsWith("Overdrive.ogg")||assetPath.EndsWith("Boss.ogg");
            importer.forceToMono=!music;importer.loadInBackground=music;
            var settings=importer.defaultSampleSettings;settings.loadType=music?UnityEngine.AudioClipLoadType.Streaming:UnityEngine.AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=music?.5f:.7f;importer.defaultSampleSettings=settings;
        }
    }
}
