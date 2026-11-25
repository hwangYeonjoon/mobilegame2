#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;

namespace ScrapGuard.Editor
{
    // Match the in-game system-language fallback, without changing package identity.
    public sealed class AndroidLocalization : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 110;
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string resources=Path.GetFullPath(Path.Combine(path,"../launcher/src/main/res"));
            WriteName(Path.Combine(resources,"values/strings.xml"),"Scrap Guard");
            WriteName(Path.Combine(resources,"values-ko/strings.xml"),"고철 수호대");
        }
        static void WriteName(string path,string name)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var xml=new XmlDocument();
            if(File.Exists(path))xml.Load(path);else xml.LoadXml("<resources/>");
            var node=xml.SelectSingleNode("/resources/string[@name='app_name']") as XmlElement;
            if(node==null){node=xml.CreateElement("string");node.SetAttribute("name","app_name");xml.DocumentElement.AppendChild(node);}
            node.InnerText=name;xml.Save(path);
        }
    }
}
#endif
