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
            Ui.Label(card,"현장 임무를 완료하고 일반 작전에서 승리하면 +1점\n30초 이상 전투 · 계정당 하루 3점 · 테스트 진행 제외\n6점 전력 → 15점 작업대 → 30점 전시관",24,456,444,85,15,Ui.Muted,TextAnchor.MiddleCenter);
            var members=guild.members.Where(m=>m.restorationPoints>0).OrderByDescending(m=>m.restorationPoints).ThenBy(m=>m.id,StringComparer.Ordinal).ToArray();
            var list=Ui.Scroll(card,24,561,444,166,Math.Max(166,members.Length*57));
            if(members.Length==0)Ui.Label(list,"첫 복구 기여자를 기다리고 있어요.",10,50,424,45,18,Ui.Muted,TextAnchor.MiddleCenter);
            for(int i=0;i<members.Length;i++)
            {
                var member=members[i];Ui.Label(list,member.name+" · "+member.restorationPoints+"점",12,i*57,310,44,18);
                Ui.Button(list,"대원 보기",323,i*57,109,43,()=>GuildPortrait(member,guild),Ui.Raised,member.hero>=0,14);
            }
            Ui.Label(card,"복구 외형은 길드 공용 · 다음 주에 새로 시작해요.\n보석·전투 능력치 보상이나 유료 기여는 없습니다.",24,744,444,52,14,Ui.Muted,TextAnchor.MiddleCenter);
            Ui.Button(card,"길드로 돌아가기",24,818,444,46,()=>GuildPanel(guild),Ui.Raised,true,17);
        }
    }
}
