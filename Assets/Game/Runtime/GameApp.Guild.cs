using System;
using System.Collections;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        GuildMember selectedGuildSupport;
        Button supportButton;Text supportText;RectTransform supportArt;
        static readonly string[] SupportNames={"현장 수리","관통 지원","방호 지원"};
        static readonly string[] SupportDetails={"체력 25% 수리 + 8초 사격 지원","8초간 빠른 관통 사격 지원","체력 40% 보호막 + 8초 사격 지원"};
        void OpenGuild()
        {
            if(Online && cloudReady){StartCoroutine(LoadGuild());return;}
            var card=Dialog("함께 싸우는 길드","길드 동료의 대원을 빌려 일반 구역을 공략하세요.",650);
            for(int i=0;i<3;i++)
            {
                CharacterArt.Draw(card,28,145+i*117,80,95,i,0);
                Ui.Label(card,SupportNames[i],123,150+i*117,328,33,22,Ui.Mint);
                Ui.Label(card,SupportDetails[i],123,192+i*117,328,49,16,Ui.Muted);
            }
            Ui.Label(card,"하루 지원 출격 3회 · 전투마다 한 번 호출\n길드 생성·가입은 서버 계정에서 이용할 수 있어요.",24,501,444,55,15,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,"계정 · 서버 연결",24,579,444,50,AccountPanel);
        }
        IEnumerator LoadGuild()
        {
            if(networkBusy)yield break;SetNetworkBusy(true);GuildView guild=null;
            yield return cloud.Get("/v1/guild",(body,status)=>{if(status==200)guild=JsonUtility.FromJson<GuildView>(body);});SetNetworkBusy(false);
            if(guild==null){Toast("길드 정보를 불러오지 못했어요.");yield break;}
            GuildPanel(guild);
        }
        void GuildPanel(GuildView guild)
        {
            if(string.IsNullOrEmpty(guild.id))
            {
                selectedGuildSupport=null;
                var card=Dialog("길드와 함께하기","최대 20명 · 지원 대원을 등록하고 함께 성장하세요.",740);
                Ui.Label(card,"새 길드 만들기",24,144,444,31,21,Ui.Mint);
                var name=Ui.Input(card,"","길드 이름 2~12자",24,194,444,12);
                Ui.Button(card,"무료 창설",24,261,444,53,()=>GuildChange(new GameCommand("guild_create"){value=name.text}));
                Ui.Label(card,"초대 코드로 가입",24,353,444,31,21,Ui.Mint);
                var code=Ui.Input(card,"","길드 초대 코드 16자리",24,402,444,16);
                Ui.Button(card,"길드 가입",24,470,444,53,()=>GuildChange(new GameCommand("guild_join"){value=code.text}),Ui.Raised);
                Ui.Label(card,"길드원에게 닉네임이 표시됩니다.\n보유 대원은 직접 등록해야 지원 목록에 올라가요.\n일반 구역에서 하루 3회 지원을 빌릴 수 있어요.",24,575,444,106,16,Ui.Muted);
                return;
            }
            if(selectedGuildSupport!=null && !guild.members.Exists(m=>m.id==selectedGuildSupport.id&&m.hero>=0))selectedGuildSupport=null;
            var panel=Dialog(guild.name,"지원 출격 오늘 "+guild.remaining+"/3회 남음 · 정원 20명\n동료 대원의 소유권과 성장 기록은 그대로 유지돼요.",900);
            Ui.Button(panel,"초대 코드 복사",24,140,214,45,()=>{GUIUtility.systemCopyBuffer=guild.code;Toast("길드 초대 코드를 복사했어요.");},Ui.Raised,true,15);
            Ui.Button(panel,"내 지원 대원 등록",254,140,214,45,SupportRegistration,Ui.Mint,true,15);
            var list=Ui.Scroll(panel,24,204,444,491,Math.Max(491,guild.members.Count*113));
            for(int i=0;i<guild.members.Count;i++)
            {
                var m=guild.members[i];float y=i*113;Ui.Box(list,0,y,444,103,Ui.Raised);
                if(m.hero>=0)CharacterArt.Draw(list,7,y+10,78,83,m.hero,m.skin);
                Ui.Label(list,m.name+(m.self?" (나)":""),96,y+8,229,29,19);
                Ui.Label(list,m.hero<0?"지원 대원 미등록":SupportNames[m.hero]+" · Lv."+m.level+"\n"+SupportDetails[m.hero],96,y+41,226,53,12,Ui.Muted);
                Ui.Button(list,m.self?"등록 변경":selectedGuildSupport!=null&&selectedGuildSupport.id==m.id?"선택됨":"동행 선택",332,y+27,101,49,()=>
                {
                    if(m.self)SupportRegistration();else {selectedGuildSupport=m;Toast(m.name+"의 대원을 선택했어요.");GuildPanel(guild);}
                },Ui.Panel,m.self || m.hero>=0 && guild.remaining>0,14);
            }
            Ui.Label(panel,selectedGuildSupport==null?"동료의 대원을 선택하면 출격 전에 확인해요.":"선택: "+selectedGuildSupport.name+" · "+SupportNames[selectedGuildSupport.hero],24,712,444,40,15,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Button(panel,"지원 없이 출격하기",24,762,214,48,()=>{selectedGuildSupport=null;CloseModal();Toast("지원 선택을 해제했어요.");},Ui.Raised,true,14);
            Ui.Button(panel,"길드 탈퇴",254,762,214,48,()=>ConfirmGuildLeave(guild.leader),Ui.Raised,true,15);
            Ui.Label(panel,"일반 구역 전용 · 출격 시 1회 차감 · 중단 시 반환 없음\n주간 코어 균열에서는 동료 지원을 사용할 수 없어요.",24,829,444,47,12,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void GuildChange(GameCommand command)
        {
            if(networkBusy)return;
            StartCoroutine(SubmitCloud(command,reply=>{Toast(reply.message);if(reply.success)StartCoroutine(LoadGuild());}));
        }
        void SupportRegistration()
        {
            var card=Dialog("내 대원 지원 등록","등록한 대원을 길드 동료가 빌려 쓸 수 있어요.",650);
            for(int i=0;i<3;i++)
            {
                int hero=i;float y=144+i*118;CharacterArt.Draw(card,24,y,80,96,i,State.selectedSkin);
                Ui.Label(card,Catalog.HeroNames[i],120,y+1,330,34,21);
                Ui.Label(card,SupportDetails[i],120,y+39,330,27,14,Ui.Muted);
                Ui.Button(card,State.heroes[i].unlocked?"지원 대원으로 등록":"대원 미보유",120,y+76,330,34,()=>GuildChange(new GameCommand("guild_support"){hero=hero,enabled=true}),Ui.Raised,State.heroes[i].unlocked,15);
            }
            Ui.Button(card,"지원 등록 해제",24,555,444,54,()=>GuildChange(new GameCommand("guild_support"){enabled=false}),Ui.Raised,true,16);
        }
        void ConfirmGuildLeave(bool leader)
        {
            var card=Dialog("길드에서 나갈까요?",leader?"다른 길드원이 있으면 길드장이 자동으로 위임돼요.\n마지막 길드원이 나가면 길드가 해산됩니다.":"등록한 지원 대원이 길드 목록에서 내려갑니다.",350);
            Ui.Button(card,"탈퇴",24,158,444,56,()=>{selectedGuildSupport=null;GuildChange(new GameCommand("guild_leave"));},Ui.Raised);
            Ui.Button(card,"계속 함께하기",24,237,444,56,OpenGuild);
        }
        void CampaignLaunch()
        {
            if(!Online || selectedGuildSupport==null){StartBattle(selectedStage);return;}
            var member=selectedGuildSupport;var card=Dialog("길드 동료와 출격",member.name+"의 "+Catalog.HeroNames[member.hero],458);
            CharacterArt.Draw(card,24,142,106,121,member.hero,member.skin);
            Ui.Label(card,SupportDetails[member.hero]+"\n전투 시작 8초 후 한 번 호출할 수 있어요.",150,149,310,100,17,Ui.Mint);
            Ui.Label(card,"출격 시 하루 이용 횟수 1회가 차감돼요.\n전투를 중단해도 반환되지 않아요.",24,278,444,53,15,Ui.Muted);
            Ui.Button(card,"지원 없이",24,359,214,57,()=>StartBattle(selectedStage),Ui.Raised,true,17);
            Ui.Button(card,"동료와 출격",254,359,214,57,()=>StartBattle(selectedStage,false,"",member.id),null,true,17);
        }
        void SetupSupportButton()
        {
            supportButton=null;supportArt=null;
            if(combat.Support==null)return;
            supportButton=Ui.Button(screen,"",183,876,146,66,TriggerSupport,Ui.Blue,true,15);supportText=supportButton.GetComponentInChildren<Text>();
            supportArt=CharacterArt.Draw(effectLayer,0,0,44,52,combat.Support.hero,combat.Support.skin).rectTransform;
        }
        void TriggerSupport()
        {
            if(combat==null || battlePaused || networkBusy || !combat.UseSupport())return;
            recorder.Support();dragging=false;
            var label=Ui.Label(effectLayer,SupportNames[combat.Support.hero]+"!",80,290,380,44,26,Ui.Blue,TextAnchor.MiddleCenter);
            label.gameObject.AddComponent<CombatBurst>().Configure(1.3f,1,1);RenderBattle();
        }
        void RenderSupport()
        {
            if(supportButton==null)return;
            supportButton.interactable=combat.CanSupport;
            supportText.text=combat.SupportUsed?"지원 완료":combat.Elapsed<8?"지원 준비\n"+Mathf.CeilToInt(8-combat.Elapsed)+"초":SupportNames[combat.Support.hero]+"\n호출 · Q";
            supportArt.gameObject.SetActive(combat.SupportTime>0);
            supportArt.anchoredPosition=new Vector2(combat.Position.x-54,-combat.Position.y+76+Mathf.Sin(combat.Elapsed*9)*2);
        }
    }
}
