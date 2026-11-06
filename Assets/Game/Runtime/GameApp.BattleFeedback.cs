using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        AudioClip hitSound,breakSound,pickupSound,growthSound;
        float nextHitAudio,nextBreakAudio,nextPickupAudio,lastStreakKill;
        int streak,lastFeedbackLevel,lastFeedbackXp;
        Text streakLabel,combatNotice;
        float noticeUntil,nextGrowthAudio;
        readonly System.Collections.Generic.Queue<string> battleNotices=new System.Collections.Generic.Queue<string>();
        void SetupBattleFeedback()
        {
            nextHitAudio=nextBreakAudio=nextPickupAudio=0;lastStreakKill=-10;streak=0;
            lastFeedbackLevel=1;lastFeedbackXp=0;noticeUntil=nextGrowthAudio=0;battleNotices.Clear();
            combatNotice=Ui.Label(screen,"",55,237,430,45,17,Ui.Mint,TextAnchor.MiddleCenter);
            var shadow=combatNotice.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.85f);shadow.effectDistance=new Vector2(1,-1);
            streakLabel=Ui.Label(screen,"",24,204,492,30,16,Ui.Gold,TextAnchor.MiddleCenter);
            if(hitSound!=null)return;
            hitSound=CombatSoundBank.Get(0);breakSound=CombatSoundBank.Get(1);
            pickupSound=CombatSoundBank.Get(2);growthSound=CombatSoundBank.Get(3);
        }
        void FeedbackSound(AudioClip clip){if(State.sound && audioSource!=null && clip!=null)audioSource.PlayOneShot(clip);}
        void HitFeedback()
        {
            if(combat.Elapsed>=nextHitAudio){nextHitAudio=combat.Elapsed+.07f;FeedbackSound(hitSound);}
        }
        void EventFeedback(BattleEffect effect)
        {
            if(effect.kind==1)PulseHaptics(20);
            if(effect.kind==5)
            {
                streak=combat.Elapsed-lastStreakKill<2.5f?streak+1:1;lastStreakKill=combat.Elapsed;
                if(combat.Elapsed>=nextBreakAudio){nextBreakAudio=combat.Elapsed+.1f;FeedbackSound(breakSound);}
            }
            if(effect.kind==7 || effect.kind==10)
            {
                if(battleNotices.Count<4)battleNotices.Enqueue(effect.text);
                if(combat.Elapsed>=nextGrowthAudio){nextGrowthAudio=combat.Elapsed+.15f;FeedbackSound(growthSound);}
            }
        }
        void RenderBattleFeedback()
        {
            if(combat.Elapsed>=noticeUntil)
            {
                combatNotice.text=battleNotices.Count>0?battleNotices.Dequeue():"";
                if(combatNotice.text!="")noticeUntil=combat.Elapsed+2;
            }
            if(combat.Level>lastFeedbackLevel)FeedbackSound(growthSound);
            else if(combat.Xp>lastFeedbackXp && combat.Elapsed>=nextPickupAudio){nextPickupAudio=combat.Elapsed+.1f;FeedbackSound(pickupSound);}
            lastFeedbackLevel=combat.Level;lastFeedbackXp=combat.Xp;
            streakLabel.text=streak>=3 && combat.Elapsed-lastStreakKill<2.5f?streak+" 연속 처치"+(streak>=10?" · 고철 소나기!":""):"";
        }
    }
    public sealed class RewardReveal : MonoBehaviour
    {
        float started;Text label;string full;Color tint;
        public void Initialize(Text text){label=text;full=text.text;tint=text.color;started=Time.unscaledTime;}
        void Update()
        {
            if(label==null)return;
            float t=Mathf.Clamp01((Time.unscaledTime-started)/.35f);label.color=new Color(tint.r,tint.g,tint.b,tint.a*t);
            label.rectTransform.localScale=Vector3.one*Mathf.Lerp(.9f,1,t);
            if(t>=1){label.text=full;enabled=false;}
        }
    }
}
