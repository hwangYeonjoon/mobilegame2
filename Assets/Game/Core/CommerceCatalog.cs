using System;
namespace ScrapGuard.Core
{
    public sealed class StoreOffer
    {
        public readonly string Id, Name;
        public readonly int Gems, ProposedWon;
        public readonly bool Consumable;
        public StoreOffer(string id,string name,int gems,int won,bool consumable=true)
        {Id=id;Name=name;Gems=gems;ProposedWon=won;Consumable=consumable;}
    }
    // Price proposals only. Checkout must display the localized Play price, never this estimate.
    public static class CommerceCatalog
    {
        public const int Version=1;
        public static readonly StoreOffer[] Offers={
            new StoreOffer("gems_300","보석 300개",300,2200),
            new StoreOffer("gems_600","보석 600개",600,3900),
            new StoreOffer("gems_1200","보석 1,200개",1200,7500),
            new StoreOffer("gems_2600","보석 2,600개",2600,14900),
            new StoreOffer("ad_skip_lifetime","영구 광고 스킵",0,9900,false)
        };
        public static StoreOffer Find(string id){return Array.Find(Offers,o=>o.Id==id);}
        public static StoreOffer SmallestPack(int missing)
        {if(missing<=0)return null;return Array.Find(Offers,o=>o.Gems>=missing);}
        public static readonly string[] ConceptIds={"rail_ranger","storm_conductor","aurora_keeper"};
        public static readonly string[] ConceptNames={"레일 레인저","폭풍 지휘자","오로라 수호자"};
        public static readonly string[] ConceptDetails={"레이 · 철도 장갑과 레일건 궤적","스파크 · 전격 날개와 코일 파동","아이리스 · 결정 망토와 눈꽃 장판"};
        public static bool Concept(string id){return Array.IndexOf(ConceptIds,id)>=0;}
    }
    public static class SkillTrial
    {
        public const string Mode="skill";
        public static GameState Loadout()
        {
            var s=GameState.New();s.selectedHero=0;s.heroes[0].level=10;s.tutorialCompleted=true;
            foreach(var i in s.items){i.level=10;i.rarity=1;}
            return s;
        }
    }
}
