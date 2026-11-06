using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        AudioSource musicSource;
        float nextHaptic;
        void StartBattleMusic()
        {
            if(musicSource==null){musicSource=gameObject.AddComponent<AudioSource>();musicSource.loop=true;musicSource.playOnAwake=false;musicSource.clip=Resources.Load<AudioClip>("Audio/SalvageDrive");}
            UpdateBattleMusic();
        }
        void UpdateBattleMusic()
        {
            if(musicSource==null)return;
            bool playing=combat!=null&&!combat.Ended&&!battlePaused&&!combat.Choosing&&!adBusy&&PlayerPrefs.GetInt("scrapguard.music",1)==1;
            musicSource.volume=.28f;
            if(playing&&!musicSource.isPlaying){musicSource.UnPause();if(!musicSource.isPlaying)musicSource.Play();}
            if(!playing&&musicSource.isPlaying)musicSource.Pause();
        }
        void PulseHaptics(int milliseconds)
        {
            if(smokeActive||smokeCapturing||PlayerPrefs.GetInt("scrapguard.haptics",1)==0||Time.unscaledTime<nextHaptic)return;
            nextHaptic=Time.unscaledTime+.25f;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using(var vibrator=activity.Call<AndroidJavaObject>("getSystemService","vibrator"))
                using(var effect=new AndroidJavaClass("android.os.VibrationEffect"))
                {
                    if(vibrator!=null&&vibrator.Call<bool>("hasVibrator"))
                    using(var pulse=effect.CallStatic<AndroidJavaObject>("createOneShot",(long)milliseconds,-1))vibrator.Call("vibrate",pulse);
                }
            }
            catch(AndroidJavaException){Handheld.Vibrate();}
#endif
        }
        void AudioOptions()
        {
            var card=Dialog("소리 · 진동","효과음은 계정에, 음악과 진동은 이 기기에 저장돼요.",520);
            Ui.Button(card,State.sound?"효과음 켜짐":"효과음 꺼짐",24,142,444,64,()=>Change(new ScrapGuard.Core.GameCommand("sound"){enabled=!State.sound},AudioOptions),Ui.Raised);
            Ui.Button(card,PlayerPrefs.GetInt("scrapguard.music",1)==1?"배경음악 켜짐":"배경음악 꺼짐",24,226,444,64,()=>{PlayerPrefs.SetInt("scrapguard.music",1-PlayerPrefs.GetInt("scrapguard.music",1));PlayerPrefs.Save();UpdateBattleMusic();AudioOptions();},Ui.Raised);
            Ui.Button(card,PlayerPrefs.GetInt("scrapguard.haptics",1)==1?"진동 켜짐":"진동 꺼짐",24,310,444,64,()=>{PlayerPrefs.SetInt("scrapguard.haptics",1-PlayerPrefs.GetInt("scrapguard.haptics",1));PlayerPrefs.Save();PulseHaptics(25);AudioOptions();},Ui.Raised);
            Ui.Label(card,"피격·성장·필살기·고철 합체에 짧은 진동을 사용해요.\n진동 지원 Android 기기에서 동작합니다.",24,402,444,70,17,Ui.Muted);
        }
    }
}
