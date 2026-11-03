using System;
using System.Collections.Generic;

namespace ScrapGuard.Core
{
    public sealed class HeroDefinition
    {
        public readonly string Name, Passive, Ultimate, SupportName, SupportDetail;
        public readonly int Attack, Health, RecruitStage;
        public readonly float Speed;
        public HeroDefinition(string name,string passive,string ultimate,int attack,int health,float speed,int recruitStage,string supportName,string supportDetail)
        { Name=name;Passive=passive;Ultimate=ultimate;Attack=attack;Health=health;Speed=speed;RecruitStage=recruitStage;SupportName=supportName;SupportDetail=supportDetail; }
    }
    // IDs are append-only: saves, guilds and public profiles refer to these indices.
    public static class HeroCatalog
    {
        public static readonly HeroDefinition[] All={
            new HeroDefinition("정비사 루크","드론 피해 +15%","고철 폭풍",100,1000,160,-1,"현장 수리","체력 25% 수리 + 8초 사격 지원"),
            new HeroDefinition("돌격수 레이","치명타 +5% · 빠른 이동","광자 관통",110,850,174,-1,"관통 지원","8초간 빠른 관통 사격 지원"),
            new HeroDefinition("수호자 볼트","20초마다 보호막 생성","요새 강하",90,1200,150,-1,"방호 지원","체력 40% 보호막 + 8초 사격 지원"),
            new HeroDefinition("전격공 스파크","코일 Lv.1 시작 · 코일 간격 -20%","과부하 방전",95,950,163,11,"전격 지원","8초간 관통 2회 전격탄 지원"),
            new HeroDefinition("냉각사 아이리스","기본 탄환 적중 시 30% 둔화","절대 영도",100,1000,158,23,"냉각 지원","8초간 사격 · 적중 시 30% 둔화")
        };
        public static int Count { get { return All.Length; } }
        public static bool Valid(int id) { return id>=0 && id<Count; }
        public static void Normalize(GameState state)
        {
            if(state.heroes==null)throw new ArgumentException("Missing hero progress");
            var seen=new HashSet<int>();
            foreach(var hero in state.heroes)
                if(hero==null || !Valid(hero.id) || !seen.Add(hero.id) || hero.level<1 || hero.level>30)
                    throw new ArgumentException("Invalid hero progress");
            // Only newly introduced heroes may be missing. Do not repair corrupt legacy ownership.
            for(int i=0;i<3;i++)if(!seen.Contains(i))throw new ArgumentException("Missing legacy hero");
            for(int i=3;i<Count;i++)if(!seen.Contains(i))state.heroes.Add(new HeroProgress{id=i});
            state.heroes.Sort((a,b)=>a.id.CompareTo(b.id));
        }
        public static bool CanRecruit(GameState state,int id)
        { return Valid(id) && All[id].RecruitStage>=0 && state.clearedStages.Contains(All[id].RecruitStage) && !state.heroes[id].unlocked; }
    }
    public sealed partial class Economy
    {
        public CommandResult RecruitHero(int id)
        {
            if(!HeroCatalog.Valid(id) || HeroCatalog.All[id].RecruitStage<0)return CommandResult.Fail("작전으로 모집할 수 없는 대원이에요.");
            if(State.heroes[id].unlocked)return CommandResult.Fail("이미 합류한 대원이에요.");
            if(!HeroCatalog.CanRecruit(State,id))return CommandResult.Fail("작전 "+(HeroCatalog.All[id].RecruitStage+1).ToString("D2")+"를 완료해 주세요.");
            State.heroes[id].unlocked=true;
            return CommandResult.Ok(HeroCatalog.All[id].Name+" 합류! 재화 소모 없이 모집했어요.");
        }
    }
}
