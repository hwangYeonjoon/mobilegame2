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
            settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=music?(System.Environment.GetEnvironmentVariable("SCRAP_AUDIO_BASELINE")=="1"?.5f:.25f):.7f;
            if(music&&System.Environment.GetEnvironmentVariable("SCRAP_AUDIO_BASELINE")!="1"){settings.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;settings.sampleRateOverride=32000;}
            else settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;importer.defaultSampleSettings=settings;
        }
    }
}
