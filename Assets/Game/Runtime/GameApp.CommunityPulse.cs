using System;
using System.Collections;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    [Serializable] public sealed class CommunitySeen { public string week="",guild="";public int likes,uses,wins; }
    public partial class GameApp
    {
        bool activityLoading,activityShowPending;Button activityButton;
        string ActivityKey { get { return "scrapguard.activity."+cloud.Session.endpoint+"."+cloud.Session.playerId; } }
        CommunitySeen ReadActivity(){try{return JsonUtility.FromJson<CommunitySeen>(PlayerPrefs.GetString(ActivityKey,"{}"))??new CommunitySeen();}catch{return new CommunitySeen();}}
        void OpenActivity(){if(!Online||!cloudReady){ActivityPanel(null,null);return;}StartCoroutine(RefreshActivity(true));}
        IEnumerator RefreshActivity(bool show)
        {
            if(activityLoading){activityShowPending|=show;yield break;}
            if(networkBusy){if(show)Toast("계정 정보를 확인 중이에요. 잠시 후 다시 눌러 주세요.");yield break;}
            activityShowPending=show;
            activityLoading=true;string owner=ActivityKey;PublicProfile profile=null;GuildView guild=null;
            yield return cloud.Get("/v1/profiles/"+cloud.Session.playerId,(body,status)=>{if(status==200)profile=JsonUtility.FromJson<PublicProfile>(body);});
            yield return cloud.Get("/v1/guild",(body,status)=>{if(status==200)guild=JsonUtility.FromJson<GuildView>(body);});
            activityLoading=false;show=activityShowPending;activityShowPending=false;if(owner!=ActivityKey||!Online)yield break;
            if(profile==null||guild==null){if(show)Toast("활동 기록을 불러오지 못했어요. 다시 시도해 주세요.");yield break;}
            var seen=ReadActivity();var me=guild.members.Find(m=>m.self);
            bool fresh=profile.likes>seen.likes||me!=null&&(seen.guild!=guild.id||seen.week!=guild.week?me.supportUses>0:me.supportUses>seen.uses);
            if(activityButton!=null)activityButton.GetComponentInChildren<Text>().text=fresh?"새 응원 · 지원 활약 보기":"응원 · 지원 활약 보기";
            if(show)ActivityPanel(profile,guild);
        }
        void ActivityPanel(PublicProfile profile,GuildView guild)
        {
            var card=Dialog("내 수호대의 활약",profile==null?"서버 계정을 연결하면 실제 응원과 지원 기록을 확인해요.":"현재 응원과 이번 주 지원 기록 · 구매 보상과 무관해요.",800);
            if(profile==null)
            {
                Ui.Label(card,"응원을 받으면 이곳에서 확인할 수 있어요.\n길드에 대원을 등록하면 동료를 도운 기록이 남아요.",24,180,444,120,23,Ui.Ink,TextAnchor.MiddleCenter);
                Ui.Button(card,"계정 연결",24,350,444,64,AccountPanel,Ui.Mint,true,22);
            }
            else
            {
                var old=ReadActivity();var me=guild.members.Find(m=>m.self);bool same=old.week==guild.week&&old.guild==guild.id;
                string delta=PlayerPrefs.HasKey(ActivityKey)?"지난 확인 대비 +"+Math.Max(0,profile.likes-old.likes):"첫 확인 · 현재 총 응원";
                Ui.Label(card,"받은 응원 "+profile.likes+"개\n"+delta,24,151,444,96,26,Ui.Mint,TextAnchor.MiddleCenter);
                int uses=me?.supportUses??0,wins=me?.supportWins??0;
                Ui.Label(card,"이번 주 지원 "+uses+"회 · 승리 "+wins+"회\n도운 동료 "+(me?.helpedMembers??0)+"명",24,277,444,91,24,Ui.Ink,TextAnchor.MiddleCenter);
                Ui.Label(card,same?"지난 확인 후 지원 +"+Math.Max(0,uses-old.uses)+"회":"이번 주 기록 · 이전 주와 합산하지 않아요.",24,378,444,42,18,Ui.Muted,TextAnchor.MiddleCenter);
                bool featured=me!=null&&guild.featuredId==me.id;
                Ui.Label(card,featured?"이번 주 길드 지원 대원으로 소개됐어요!":"동료와 함께 쌓은 기록이 내 전시의 이야기가 됩니다.",24,442,444,67,21,Ui.Gold,TextAnchor.MiddleCenter);
                PlayerPrefs.SetString(ActivityKey,JsonUtility.ToJson(new CommunitySeen{likes=profile.likes,week=guild.week,guild=guild.id,uses=uses,wins=wins}));PlayerPrefs.Save();
                Ui.Button(card,"길드 · 지원 대원 관리",24,536,444,64,OpenGuild,Ui.Raised,true,21);
            }
            Ui.Button(card,"내 전시실 꾸미기",24,625,444,64,()=>Show("showcase"),Ui.Mint,true,22);
            Ui.Label(card,"응원 취소·주간 변경으로 수치가 달라질 수 있어요.\n실제 기록만 표시하며 자동으로 메시지를 보내지 않아요.",24,711,444,58,16,Ui.Muted,TextAnchor.MiddleCenter);
        }
    }
}
