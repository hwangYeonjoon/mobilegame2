using UnityEngine;
namespace ScrapGuard
{
    public static class NativeShare
    {
        public static void Image(string path)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var provider = new AndroidJavaClass("androidx.core.content.FileProvider"))
            using (var file = new AndroidJavaObject("java.io.File", path))
            using (var uri = provider.CallStatic<AndroidJavaObject>("getUriForFile", activity, activity.Call<string>("getPackageName") + ".showcase", file))
            using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND"))
            using (var intents = new AndroidJavaClass("android.content.Intent"))
            using (var clips = new AndroidJavaClass("android.content.ClipData"))
            {
                intent.Call<AndroidJavaObject>("setType", "image/png").Dispose();
                intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.STREAM", uri).Dispose();
                intent.Call<AndroidJavaObject>("addFlags", 1).Dispose();
                using (var clip = clips.CallStatic<AndroidJavaObject>("newRawUri", "고철 수호대 프로필", uri)) intent.Call("setClipData", clip);
                using (var chooser = intents.CallStatic<AndroidJavaObject>("createChooser", intent, "내 수호대 공유")) activity.Call("startActivity", chooser);
            }
#endif
        }
    }
}
