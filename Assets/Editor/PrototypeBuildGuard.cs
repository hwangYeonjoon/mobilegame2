using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace ScrapGuard.Editor
{
    // The prototype economy and development grants must never be mistaken for a store-ready app.
    public sealed class PrototypeBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }
        public void OnPreprocessBuild(BuildReport report)
        {
            ValidateWardrobe();
            if(UnityEditor.PlayerSettings.bundleVersion!=ScrapGuard.Core.AppRelease.Version || UnityEditor.PlayerSettings.Android.bundleVersionCode!=ScrapGuard.Core.AppRelease.Build)
                throw new BuildFailedException("App release constants and PlayerSettings must match. Run Configure Project before building.");
            if ((report.summary.options & BuildOptions.Development) == 0 && !(ProjectBuilder.InternalTestBuild && report.summary.platform==BuildTarget.Android && EditorUserBuildSettings.buildAppBundle && PlayerSettings.Android.useCustomKeystore))
                throw new BuildFailedException("This prototype only supports Development builds. Complete production authentication, live billing verification and ad reward verification before preparing a production build.");
        }
        public static void ValidateWardrobe()
        {
            int expected=ScrapGuard.Core.Outfits.Count+1;
            foreach(var values in new[]{ScrapGuard.Core.Outfits.Keys,ScrapGuard.Core.Outfits.Labels,ScrapGuard.Core.Outfits.Assets,ScrapGuard.Core.Outfits.Details,ScrapGuard.Core.Outfits.Effects,ScrapGuard.Core.Outfits.Arrivals})
                if(values.Length!=expected)throw new BuildFailedException("Outfit catalog metadata is incomplete.");
            var keys=new System.Collections.Generic.HashSet<string>();
            for(int id=1;id<expected;id++)
            {
                if(string.IsNullOrWhiteSpace(ScrapGuard.Core.Outfits.Product(id)) || !keys.Add(ScrapGuard.Core.Outfits.Product(id)) || !ScrapGuard.Core.HeroCatalog.Valid(ScrapGuard.Core.Outfits.Hero(id)))
                    throw new BuildFailedException("Invalid outfit product or hero: "+id);
                var sheet=UnityEngine.Resources.Load<UnityEngine.Texture2D>("Art/"+ScrapGuard.Core.Outfits.Assets[id]);
                if(sheet==null || sheet.width%2!=0 || sheet.height%2!=0)
                    throw new BuildFailedException("Missing or invalid 2x2 outfit atlas: "+id);
            }
        }
    }
}
