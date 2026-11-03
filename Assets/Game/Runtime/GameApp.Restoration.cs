using System;
using System.Linq;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        void RestorationPanel(GuildView guild)
        {
            int level=GuildRestoration.Level(guild.restorationPoints);
            var card=Dialog("길드 공동 정비소",guild.name+" · "+guild.week+" 주간 복구",890);
            var stage=Ui.Box(card,24,140,444,250,Ui.Background).transform;
            var art=Ui.Box(stage,112,39,220,180,level>0?Color.white:new Color(.28f,.32f,.35f),false);art.sprite=ArenaArt.Landmark(level>=2?0:7);art.preserveAspect=true;
            for(int i=0;i<3;i++)Ui.Box(stage,69+i*111,221,84,6,level>i?Ui.Mint:Ui.Raised,false);
            if(level>=3)CharacterArt.Draw(stage,10,109,110,120,State.showcaseHero,State.selectedSkin,Outfits.Active(State,State.showcaseHero));
            Ui.Label(stage,GuildRestoration.Names[level],12,6,420,32,24,Ui.Gold,TextAnchor.MiddleCenter);
            Ui.Label(card,"공동 복구 "+guild.restorationPoints+"점 · 오늘 내 기여 "+guild.restorationToday+" / 3",24,408,444,35,20,Ui.Mint,TextAnchor.MiddleCenter);
            Ui.Label(card,"현장 임무를 완료하고 일반 작전에서 승리하면 +1점\n30초 이상 전투 · 계정당 하루 3점 · 테스트 진행 제외\n30점 복구 → 60점 협력 → 120점 개척 기념장",24,456,444,85,15,Ui.Muted,TextAnchor.MiddleCenter);
            var members=guild.members.Where(m=>m.restorationPoints>0).OrderByDescending(m=>m.restorationPoints).ThenBy(m=>m.id,StringComparer.Ordinal).ToArray();
            var list=Ui.Scroll(card,24,561,444,166,Math.Max(166,members.Length*57));
            if(members.Length==0)Ui.Label(list,"첫 복구 기여자를 기다리고 있어요.",10,50,424,45,18,Ui.Muted,TextAnchor.MiddleCenter);
            for(int i=0;i<members.Length;i++)
            {
                var member=members[i];Ui.Label(list,member.name+" · "+member.restorationPoints+"점",12,i*57,310,44,18);
                Ui.Button(list,"대원 보기",323,i*57,109,43,()=>GuildPortrait(member,guild),Ui.Raised,member.hero>=0,14);
            }
            int next=GuildRestoration.NextGoal(guild.restorationPoints);
            Ui.Label(card,next>0?"다음 기념장까지 "+Math.Max(0,next-guild.restorationPoints)+"점 · 주간 진행은 초기화돼요.":"이번 주 최고 기념장 완성 · 달성 기록은 남아요.",24,740,444,37,14,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,"영구 기념관 · "+guild.memorialWeeks+"주 달성",24,789,444,43,()=>GuildMemorialPanel(guild),Ui.Gold,true,17);
            Ui.Label(card,"기념장은 길드에 영구 보관 · 보석·능력치 보상 없음",24,843,444,25,12,Ui.Muted,TextAnchor.MiddleCenter);
        }
        void GuildMemorialPanel(GuildView guild)
        {
            var card=Dialog("길드 영구 기념관",guild.name+" · 총 "+guild.memorialWeeks+"주 달성\n주간 초기화 후에도 기념장은 남습니다.",880);
            var list=Ui.Scroll(card,24,145,444,550,Math.Max(550,guild.memorials.Count*112));
            if(guild.memorials.Count==0)Ui.Label(list,"이번 주 30점을 모아 첫 기념장을 남겨 보세요.\n60점·120점에 도달하면 같은 주의 기념장이 승급해요.",16,100,412,100,18,Ui.Muted,TextAnchor.MiddleCenter);
            for(int i=0;i<guild.memorials.Count;i++)
            {
                var m=guild.memorials[i];var row=Ui.Box(list,0,i*112,444,100,Ui.Background).transform;
                var tint=m.tier>=3?Ui.Gold:m.tier==2?Ui.Blue:Ui.Mint;
                var seal=CombatRing.Create(row,62,tint,.09f);seal.anchoredPosition=new Vector2(49,-49);
                Ui.Label(row,m.tier.ToString(),25,29,48,40,25,tint,TextAnchor.MiddleCenter);
                Ui.Label(row,GuildRestoration.HonorNames[Mathf.Clamp(m.tier-1,0,2)],97,13,330,35,23,tint);
                Ui.Label(row,m.week+" 주 · 달성 기록 "+m.points+"점",97,56,330,28,15,Ui.Muted);
            }
            Ui.Label(card,"최근 52주를 표시 · 전체 달성 횟수는 계속 보관\n길드 공용 기록으로 개인 탈퇴·주간 초기화 후에도 유지\n길드가 해산되면 삭제 · 구매로 점수를 올릴 수 없어요.",24,713,444,78,14,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,"이번 주 공동 복구",24,808,444,47,()=>RestorationPanel(guild),Ui.Raised,true,17);
        }

    }
}
