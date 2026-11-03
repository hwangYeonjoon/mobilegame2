using System;
using System.Linq;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
using Social=ScrapGuard.Core.Social;
namespace ScrapGuard
{
    public partial class GameApp
    {
        void GuardianGallery(PublicProfile profile,Action back,string caption=null,bool share=true)
        {
            Track("preview","guardian");
            var card=Dialog("수호대원 확대 감상","외형과 동작을 가까이에서 보고 사진 카드로 남겨 보세요.",890);
            var stage=Ui.Box(card,24,138,444,448,Ui.Background).rectTransform;stage.gameObject.AddComponent<RectMask2D>();
            var floor=Ui.Box(stage,0,0,444,448,Color.white,false);floor.sprite=ArenaArt.Floor(Mathf.Clamp(profile.skin-3,0,6));
            Ui.Box(stage,0,0,444,448,new Color(.02f,.04f,.07f,.42f),false);
            Ui.Label(stage,profile.name,18,12,408,37,26,Ui.Ink,TextAnchor.MiddleCenter);
            var lookLabel=Ui.Label(stage,StylePresentation.Look(profile.outfit,profile.skin),18,54,408,30,17,Ui.Gold,TextAnchor.MiddleCenter);
            var preview=stage.gameObject.AddComponent<GuardianPosePreview>();preview.Initialize(profile.hero,profile.skin,profile.outfit);
            Ui.Label(stage,"Lv."+profile.heroLevel+" · "+Catalog.HeroNames[profile.hero],18,365,408,30,19,Ui.Ink,TextAnchor.MiddleCenter);
            Ui.Label(stage,caption??("정복 "+profile.stages+" / "+Campaign.Count+" · "+Social.Titles[Mathf.Clamp(profile.title,0,Social.Titles.Length-1)]),18,401,408,34,13,Ui.Mint,TextAnchor.MiddleCenter);
            string[] poses={"대기","이동","사격","필살기","지원"};
            for(int i=0;i<5;i++){int pose=i;Ui.Button(card,poses[i],24+i*90,605,84,49,()=>preview.SetPose(pose),i==3?Ui.Gold:Ui.Raised,true,15);}
            Ui.Button(card,profile.outfit>0?"이 의상 체험":"방향 바꾸기",24,672,214,48,()=>{if(profile.outfit>0)OutfitRoom(profile.outfit);else preview.Flip();},Ui.Raised,true,15);
            Ui.Button(card,"사진 카드 공유",254,672,214,48,()=>{if(!exportingCard)StartCoroutine(ExportCard(stage));},Ui.Mint,share,15);
            Button compare=null,size=null;
            compare=Ui.Button(card,"기본 외형과 비교",24,734,214,42,()=>{preview.Compare();lookLabel.text=preview.BaseLook?"기본 외형 · 비교 중":StylePresentation.Look(profile.outfit,profile.skin);compare.GetComponentInChildren<Text>().text=preview.BaseLook?"장착 모습으로 돌아가기":"기본 외형과 비교";},Ui.Raised,true,14);
            size=Ui.Button(card,"전투 크기로 보기",254,734,214,42,()=>{preview.ToggleSize();size.GetComponentInChildren<Text>().text=preview.BattleSize?"크게 보기":"전투 크기로 보기";},Ui.Raised,true,14);
            Ui.Label(card,share?"미리보기는 장착·재화·능력치를 바꾸지 않아요.":"길드 내부 감상 · 사진 공유는 공개 프로필에서 가능해요.",24,781,444,19,11,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,"돌아가기",24,803,444,51,back,Ui.Raised,true,17);
        }
        void GuildPortrait(GuildMember member,GuildView guild)
        {
            if(member.hero<0)return;
            var profile=new PublicProfile{name=member.name,hero=member.hero,heroLevel=member.level,skin=member.skin,outfit=member.outfit};
            GuardianGallery(profile,()=>SupportHall(guild),"이번 주 "+member.supportUses+"회 호출 · "+member.helpedMembers+"명 공략 지원",false);
        }
        void SupportHall(GuildView guild)
        {
            var card=Dialog("이번 주 길드 지원 활약",guild.name+" · "+guild.week+"\n실제로 호출한 30초 이상 전투를 정산한 기록입니다.",890);
            var featured=guild.members.Find(m=>m.id==guild.featuredId);
            var panel=Ui.Box(card,24,143,444,239,Ui.Background).transform;
            if(featured==null)
            {
                Ui.Label(panel,"다음 주인공은 당신의 대원",16,44,412,42,23,Ui.Gold,TextAnchor.MiddleCenter);
                Ui.Label(panel,"동료가 지원 대원을 호출하고 작전을 클리어하면\n도움을 받은 동료 수를 기준으로 전시됩니다.",16,106,412,83,16,Ui.Muted,TextAnchor.MiddleCenter);
            }
            else
            {
                CharacterArt.Draw(panel,10,30,160,180,featured.hero,featured.skin,featured.outfit);
                Ui.Label(panel,"이번 주 활약 길드원",182,20,245,30,16,Ui.Gold);
                Ui.Label(panel,"현재 등록 대원",182,47,245,20,11,Ui.Muted);
                Ui.Label(panel,featured.name,182,69,245,37,23,Ui.Ink);
                Ui.Label(panel,featured.helpedMembers+"명 공략 지원\n호출 "+featured.supportUses+"회 · 승리 "+featured.supportWins+"회",182,113,245,60,16,Ui.Mint);
                Ui.Button(panel,"대원 크게 보기",182,183,242,40,()=>GuildPortrait(featured,guild),Ui.Raised,true,15);
            }
            var members=guild.members.OrderByDescending(m=>m.helpedMembers).ThenByDescending(m=>m.supportWins).ThenBy(m=>m.id,StringComparer.Ordinal).ToArray();
            var list=Ui.Scroll(card,24,401,444,324,Mathf.Max(324,members.Length*86));
            for(int i=0;i<members.Length;i++)
            {
                var member=members[i];var row=Ui.Box(list,0,i*86,444,78,Ui.Raised).transform;
                if(member.hero>=0)CharacterArt.Draw(row,6,5,58,68,member.hero,member.skin,member.outfit);
                Ui.Label(row,member.name,76,7,255,27,17);
                Ui.Label(row,"호출 "+member.supportUses+" · 승리 "+member.supportWins+" · 동료 "+member.helpedMembers+"명",76,39,255,26,12,Ui.Muted);
                Ui.Button(row,"감상",348,17,83,43,()=>GuildPortrait(member,guild),Ui.Panel,member.hero>=0,14);
            }
            Ui.Label(card,"도운 동료 수 → 승리 수 순 · 같은 동료는 1명으로 집계\n길드 내부 전시이며 별도 재화 보상은 없습니다.",24,741,444,46,12,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,"길드로 돌아가기",24,811,444,49,()=>GuildPanel(guild),Ui.Raised,true,17);
        }
    }

    public sealed class GuardianPosePreview : MonoBehaviour
    {
        Image actor,shadow;int hero,skin,outfit,pose;bool flipped;float nextShot;
        public int Pose { get { return pose; } }
        public bool BaseLook { get; private set; }
        public bool BattleSize { get; private set; }
        public void Compare(){BaseLook=!BaseLook;SetPose(0);}
        public void ToggleSize(){BattleSize=!BattleSize;}
        public void Initialize(int h,int s,int o)
        {
            hero=h;skin=s;outfit=o;
            shadow=Ui.Disc(transform,127,320,190,new Color(0,0,0,.35f));shadow.rectTransform.sizeDelta=new Vector2(190,30);
            actor=CharacterArt.Draw(transform,82,96,280,260,hero,skin,outfit);
            actor.rectTransform.pivot=new Vector2(.5f,.5f);actor.rectTransform.anchoredPosition=new Vector2(222,-226);
        }
        public void Flip(){flipped=!flipped;}
        public void SetPose(int value)
        {
            pose=Mathf.Clamp(value,0,4);nextShot=0;
            int shown=BaseLook?0:outfit;
            if(pose==4)StylePresentation.Arrival(transform,15,88,"지원 등장 미리보기",hero,BaseLook?0:skin,shown,"동료에게 보이는 장착 모습");
            if(pose==3)
            {
                if(shown>0)OutfitVisuals.Special(transform,new Vector2(222,-246),150,shown);
                else {var ring=CombatRing.Create(transform,360,hero==3?Ui.Gold:Ui.Blue,.025f);ring.anchoredPosition=new Vector2(222,-246);ring.gameObject.AddComponent<CombatBurst>().Configure(.8f,.25f,1);}
            }
        }
        void Update()
        {
            if(actor==null)return;
            float t=Time.unscaledTime;float stride=pose==1?Mathf.Sin(t*12):Mathf.Sin(t*2)*.2f;
            StylePresentation.SetLook(actor,hero,BaseLook?0:skin,BaseLook?0:outfit,pose==1?(stride>0?1:0):Mathf.Min(pose,3));
            actor.rectTransform.anchoredPosition=new Vector2(222+(pose==2?Mathf.Sin(t*20)*2:0),-226+Mathf.Abs(stride)*4);
            actor.rectTransform.sizeDelta=BattleSize?new Vector2(96,100)*StylePresentation.PlayerScale:new Vector2(280,260);
            actor.rectTransform.localScale=new Vector3((flipped?-1:1)*(1+stride*.018f),1-stride*.018f,1);
            shadow.gameObject.SetActive(!BattleSize);
            if(pose==2 && t>=nextShot)
            {
                nextShot=t+.45f;StylePresentation.Shot(transform,new Vector2(222+(flipped?1:-1)*(BattleSize?24:80),-216),BaseLook?0:outfit,!flipped);
            }
        }
    }
}
