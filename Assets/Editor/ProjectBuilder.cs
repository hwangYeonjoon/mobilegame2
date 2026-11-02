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
            PlayerSettings.bundleVersion = "0.4.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.scrapguard.mobilegame2.prototype");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.scrapguard.mobilegame2.prototype");
            PlayerSettings.Android.bundleVersionCode = 4;
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
        [MenuItem("Scrap Guard/Build macOS Preview")]
        public static void BuildMac()
        {
            Configure(); Build(BuildTarget.StandaloneOSX, "Builds/macOS/ScrapGuard.app");
        }
        public static void BuildAll()
        {
            BuildMac(); BuildAndroid();
        }
        static void Build(BuildTarget target, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, target = target, locationPathName = path, options = BuildOptions.Development });
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Build failed: " + report.summary.result);
            Debug.Log("BUILD_READY " + Path.GetFullPath(path));
        }
    }
}
