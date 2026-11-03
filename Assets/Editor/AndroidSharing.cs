#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;
namespace ScrapGuard.Editor
{
    public sealed class AndroidSharing : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder { get { return 100; } }
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            const string ns = "http://schemas.android.com/apk/res/android";
            string manifest = Path.Combine(path, "src/main/AndroidManifest.xml");
            var xml = new XmlDocument(); xml.Load(manifest); var app = xml.SelectSingleNode("/manifest/application");
            // Do not allow Android's automatic backup to copy bearer credentials and cached balances.
            ((XmlElement)app).SetAttribute("allowBackup", ns, "false");
            foreach (XmlNode existing in app.SelectNodes("provider"))
                if (((XmlElement)existing).GetAttribute("authorities", ns) == "${applicationId}.showcase") app.RemoveChild(existing);
            var provider = xml.CreateElement("provider");
            provider.SetAttribute("name", ns, "androidx.core.content.FileProvider");
            provider.SetAttribute("authorities", ns, "${applicationId}.showcase");
            provider.SetAttribute("exported", ns, "false"); provider.SetAttribute("grantUriPermissions", ns, "true");
            var meta = xml.CreateElement("meta-data"); meta.SetAttribute("name", ns, "android.support.FILE_PROVIDER_PATHS"); meta.SetAttribute("resource", ns, "@xml/showcase_paths");
            provider.AppendChild(meta); app.AppendChild(provider); xml.Save(manifest);
            string resource = Path.Combine(path, "src/main/res/xml"); Directory.CreateDirectory(resource);
            File.WriteAllText(Path.Combine(resource,"showcase_paths.xml"), "<paths xmlns:android=\"http://schemas.android.com/apk/res/android\"><cache-path name=\"showcase\" path=\"showcase/\"/><external-cache-path name=\"showcase_external\" path=\"showcase/\"/></paths>");
            string gradle = Path.Combine(path,"build.gradle");
            if (!File.ReadAllText(gradle).Contains("androidx.core:core:1.15.0"))
                File.AppendAllText(gradle, "\ndependencies { implementation 'androidx.core:core:1.15.0' }\n");
            if (!File.ReadAllText(gradle).Contains("androidx.credentials:credentials:1.5.0"))
                File.AppendAllText(gradle,"\ndependencies { implementation 'androidx.credentials:credentials:1.5.0'; implementation 'androidx.credentials:credentials-play-services-auth:1.5.0'; implementation 'com.google.android.libraries.identity.googleid:googleid:1.1.1' }\n");
            if (!File.ReadAllText(gradle).Contains("org.jetbrains.kotlin:kotlin-bom:1.8.22"))
                File.AppendAllText(gradle, "\ndependencies { implementation platform('org.jetbrains.kotlin:kotlin-bom:1.8.22') }\n");
        }
    }
}
#endif
