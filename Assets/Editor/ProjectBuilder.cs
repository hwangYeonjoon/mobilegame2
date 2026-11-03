using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ScrapGuard.Editor
{
    [InitializeOnLoad]
    public static class ProjectBuilder
    {
        const string ScenePath = "Assets/Scenes/Workshop.unity";
        static ProjectBuilder() { EditorApplication.delayCall += EnsureProject; }
        static void EnsureProject()
        {
            if (!File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode) Configure();
        }
        [MenuItem("Scrap Guard/Configure Project")]
        public static void Configure()
        {
            PlayerSettings.companyName = "ScrapGuard"; PlayerSettings.productName = "고철 수호대";
            PlayerSettings.bundleVersion = "0.12.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.scrapguard.mobilegame2.prototype");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.scrapguard.mobilegame2.prototype");
            PlayerSettings.Android.bundleVersionCode = 12;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultScreenWidth = 540; PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
            QualitySettings.vSyncCount = 0;
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler");
            if (input != null) { input.intValue = 1; settings.ApplyModifiedPropertiesWithoutUndo(); }
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = new GameObject("Camera").AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Ui.Background; camera.orthographic = true; camera.transform.position = new Vector3(0, 0, -10);
                camera.gameObject.AddComponent<AudioListener>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Branding.Apply();
            AssetDatabase.SaveAssets();
            Debug.Log("SCRAP_GUARD_CONFIGURED");
        }
        [MenuItem("Scrap Guard/Build Android APK")]
        public static void BuildAndroid()
        {
            Configure(); EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, "Builds/Android/ScrapGuard-prototype.apk");
        }
        public static bool InternalTestBuild {get;private set;}
        [MenuItem("Scrap Guard/Build signed internal-test AAB")]
        public static void BuildAndroidBundle()
        {
            string key=System.Environment.GetEnvironmentVariable("SCRAP_KEYSTORE_PATH");
            string alias=System.Environment.GetEnvironmentVariable("SCRAP_KEY_ALIAS");
            string storePass=System.Environment.GetEnvironmentVariable("SCRAP_KEYSTORE_PASSWORD");
            string aliasPass=System.Environment.GetEnvironmentVariable("SCRAP_KEY_PASSWORD");
            if(string.IsNullOrWhiteSpace(key)||!File.Exists(key)||string.IsNullOrWhiteSpace(alias)||string.IsNullOrEmpty(storePass)||string.IsNullOrEmpty(aliasPass))
                throw new BuildFailedException("Set the four SCRAP signing environment variables locally. No signing secret is stored in the repository.");
            Configure();EditorUserBuildSettings.buildAppBundle=true;
            bool oldCustom=PlayerSettings.Android.useCustomKeystore;string oldName=PlayerSettings.Android.keystoreName,oldAlias=PlayerSettings.Android.keyaliasName;
            try
            {
                PlayerSettings.Android.useCustomKeystore=true;PlayerSettings.Android.keystoreName=Path.GetFullPath(key);PlayerSettings.Android.keyaliasName=alias;
                PlayerSettings.Android.keystorePass=storePass;PlayerSettings.Android.keyaliasPass=aliasPass;InternalTestBuild=true;
                Build(BuildTarget.Android,"Builds/Android/ScrapGuard-internal-test.aab",BuildOptions.None);
            }
            finally
            {
                InternalTestBuild=false;EditorUserBuildSettings.buildAppBundle=false;
                PlayerSettings.Android.keystorePass=PlayerSettings.Android.keyaliasPass="";
                PlayerSettings.Android.useCustomKeystore=oldCustom;PlayerSettings.Android.keystoreName=oldName;PlayerSettings.Android.keyaliasName=oldAlias;
                AssetDatabase.SaveAssets();
            }
        }
        [MenuItem("Scrap Guard/Build macOS Preview")]
        public static void BuildMac()
        {
            Configure(); Build(BuildTarget.StandaloneOSX, "Builds/macOS/ScrapGuard.app");
        }
        public static void BuildAll()
        {
            BuildMac(); BuildAndroid();
        }
        static void Build(BuildTarget target, string path,BuildOptions options=BuildOptions.Development)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, target = target, locationPathName = path, options = options });
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Build failed: " + report.summary.result);
            Debug.Log("BUILD_READY " + Path.GetFullPath(path));
        }
    }
}
