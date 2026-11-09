using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        AudioSource musicSource,musicTail;
        readonly AudioClip[] battleMusic=new AudioClip[3];int musicIndex=-1;
        float nextHaptic;
        void StartBattleMusic()
        {
            if(musicSource==null)
            {
                musicSource=gameObject.AddComponent<AudioSource>();musicTail=gameObject.AddComponent<AudioSource>();
                foreach(var source in new[]{musicSource,musicTail}){source.loop=true;source.playOnAwake=false;source.volume=0;}
                string[] names={"Factory","Overdrive","Boss"};for(int i=0;i<3;i++)battleMusic[i]=Resources.Load<AudioClip>("Audio/Licensed/"+names[i]);
            }
            musicIndex=-1;UpdateBattleMusic();
        }
        void UpdateBattleMusic()
        {
            if(musicSource==null)return;
            bool playing=combat!=null&&!combat.Ended&&!battlePaused&&!combat.Choosing&&!adBusy&&PlayerPrefs.GetInt("scrapguard.music",1)==1;
            if(!playing){if(musicSource.isPlaying)musicSource.Pause();if(musicTail.isPlaying)musicTail.Pause();return;}
            int next=combat.FinalBossActive||combat.Challenge?2:combat.MachineTime>0?1:combat.Definition.Region%2;
            if(next!=musicIndex)
            {
                var previous=musicSource;musicSource=musicTail;musicTail=previous;
                musicSource.Stop();musicSource.clip=battleMusic[next];musicSource.volume=0;musicSource.Play();musicIndex=next;
            }
            if(!musicSource.isPlaying){musicSource.UnPause();if(!musicSource.isPlaying)musicSource.Play();}
            musicSource.volume=Mathf.MoveTowards(musicSource.volume,.22f,Time.unscaledDeltaTime*.18f);
            musicTail.volume=Mathf.MoveTowards(musicTail.volume,0,Time.unscaledDeltaTime*.18f);
            if(musicTail.volume<=0)musicTail.Stop();else if(!musicTail.isPlaying)musicTail.UnPause();
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
            bool fromBattle=combat!=null&&!combat.Ended;if(fromBattle)battlePaused=true;
            var card=Dialog("소리 · 진동","효과음은 계정에, 전투 화면·음악·진동 설정은 이 기기에 저장돼요.",800);
            if(fromBattle){var close=card.GetChild(2).GetComponent<UnityEngine.UI.Button>();close.onClick.RemoveAllListeners();close.onClick.AddListener(PauseBattle);}
            Ui.Button(card,State.sound?"효과음 켜짐":"효과음 꺼짐",24,142,444,64,()=>Change(new ScrapGuard.Core.GameCommand("sound"){enabled=!State.sound},AudioOptions),Ui.Raised);
            Ui.Button(card,PlayerPrefs.GetInt("scrapguard.music",1)==1?"배경음악 켜짐":"배경음악 꺼짐",24,226,444,64,()=>{PlayerPrefs.SetInt("scrapguard.music",1-PlayerPrefs.GetInt("scrapguard.music",1));PlayerPrefs.Save();UpdateBattleMusic();AudioOptions();},Ui.Raised);
            Ui.Button(card,PlayerPrefs.GetInt("scrapguard.haptics",1)==1?"진동 켜짐":"진동 꺼짐",24,310,444,64,()=>{PlayerPrefs.SetInt("scrapguard.haptics",1-PlayerPrefs.GetInt("scrapguard.haptics",1));PlayerPrefs.Save();PulseHaptics(25);AudioOptions();},Ui.Raised);
            Ui.Button(card,PlayerPrefs.GetInt("scrapguard.machine.auto",0)==1?"로봇 · 선택한 설계 자동 탑승":"로봇 · 직접 탑승 / 길게 눌러 조립",24,402,444,64,()=>{PlayerPrefs.SetInt("scrapguard.machine.auto",1-PlayerPrefs.GetInt("scrapguard.machine.auto",0));PlayerPrefs.Save();AudioOptions();},Ui.Raised,true,20);
            Ui.Button(card,PlayerPrefs.GetInt("scrapguard.robot.clear",1)==1?"로봇 표시 · 전투 시야 우선":"로봇 표시 · 큰 기체",24,488,444,58,()=>{int next=1-PlayerPrefs.GetInt("scrapguard.robot.clear",1);PlayerPrefs.SetInt("scrapguard.robot.clear",next);PlayerPrefs.Save();Track("robot_view",next==1?"clear":"large");AudioOptions();},Ui.Raised,true,18);
            Ui.Label(card,"시야 우선: 작게 표시 · 가까운 적이 있으면 반투명\n그림만 바뀌며 공격·피격 판정은 같습니다.",24,557,444,60,15,Ui.Muted);
            Ui.Label(card,"피격·성장·필살기·고철 합체에 짧은 진동을 사용해요.\n진동 지원 Android 기기에서 동작합니다.",24,628,444,65,17,Ui.Muted);
            Ui.Label(card,"음악: Alexander Ehlers · 효과음: Kenney (CC0)\n일반 지역·합체·보스전에 따라 음악이 전환됩니다.",24,704,444,65,16,Ui.Muted);
        }
    }
}
