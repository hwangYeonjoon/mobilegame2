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
            PlayerSettings.bundleVersion = ScrapGuard.Core.AppRelease.Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ScrapGuard.Core.AppRelease.Package);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, ScrapGuard.Core.AppRelease.Package);
            PlayerSettings.Android.bundleVersionCode = ScrapGuard.Core.AppRelease.Build;
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
            ServiceBuildSettings.Apply();
            AssetDatabase.SaveAssets();
            Debug.Log("SCRAP_GUARD_CONFIGURED");
        }
        [MenuItem("Scrap Guard/Build Android APK")]
        public static void BuildAndroid()
        {
            Configure(); EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, "Builds/Android/ScrapGuard-prototype.apk");
        }
        public static void BuildAndroidSoakQA(){BuildAndroidQA(true);}
        public static void BuildAndroidPolishQA(){BuildAndroidQA(false);}
        static void BuildAndroidQA(bool soak)
        {
            Configure();EditorUserBuildSettings.buildAppBundle=false;
            string previous=PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),previousName=PlayerSettings.productName;
            try
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,ScrapGuard.Core.AppRelease.Package+".qa");
                PlayerSettings.productName="고철 수호대 QA";
                Build(BuildTarget.Android,"Builds/Android/ScrapGuard-qa.apk",BuildOptions.Development,soak?new[]{"SCRAP_QA_SOAK"}:new[]{"SCRAP_QA_SOAK","SCRAP_QA_UI"});
            }
            finally{PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,previous);PlayerSettings.productName=previousName;AssetDatabase.SaveAssets();}
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
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))
                throw new BuildFailedException("Android build support is unavailable. Check the editor module and licensing initialization, then restart the build.");
            Configure();
            bool oldBundle=EditorUserBuildSettings.buildAppBundle,oldExport=EditorUserBuildSettings.exportAsGoogleAndroidProject;
            EditorUserBuildSettings.buildAppBundle=true;EditorUserBuildSettings.exportAsGoogleAndroidProject=false;
            bool oldCustom=PlayerSettings.Android.useCustomKeystore;string oldName=PlayerSettings.Android.keystoreName,oldAlias=PlayerSettings.Android.keyaliasName;
            try
            {
                PlayerSettings.Android.useCustomKeystore=true;PlayerSettings.Android.keystoreName=Path.GetFullPath(key);PlayerSettings.Android.keyaliasName=alias;
                PlayerSettings.Android.keystorePass=storePass;PlayerSettings.Android.keyaliasPass=aliasPass;InternalTestBuild=true;
                Build(BuildTarget.Android,"Builds/Android/ScrapGuard-internal-test.aab",BuildOptions.None);
                if(!File.Exists("Builds/Android/ScrapGuard-internal-test.aab"))
                    throw new BuildFailedException("Unity did not produce the internal-test AAB. Check the Android build log even if the report says Success.");
            }
            finally
            {
                InternalTestBuild=false;EditorUserBuildSettings.buildAppBundle=oldBundle;EditorUserBuildSettings.exportAsGoogleAndroidProject=oldExport;
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
        static void Build(BuildTarget target, string path,BuildOptions options=BuildOptions.Development,string[] extraDefines=null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, target = target, locationPathName = path, options = options,extraScriptingDefines=extraDefines });
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Build failed: " + report.summary.result);
            Debug.Log("BUILD_READY " + Path.GetFullPath(path));
        }
    }
}
