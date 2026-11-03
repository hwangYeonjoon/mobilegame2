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
            if(UnityEditor.PlayerSettings.bundleVersion!=ScrapGuard.Core.AppRelease.Version || UnityEditor.PlayerSettings.Android.bundleVersionCode!=ScrapGuard.Core.AppRelease.Build)
                throw new BuildFailedException("App release constants and PlayerSettings must match. Run Configure Project before building.");
            if ((report.summary.options & BuildOptions.Development) == 0 && !(ProjectBuilder.InternalTestBuild && report.summary.platform==BuildTarget.Android && EditorUserBuildSettings.buildAppBundle && PlayerSettings.Android.useCustomKeystore))
                throw new BuildFailedException("This prototype only supports Development builds. Complete production authentication, live billing verification and ad reward verification before preparing a production build.");
        }
    }
}
