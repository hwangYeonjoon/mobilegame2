using ScrapGuard.Core;
namespace ScrapGuard.Tests
{
    public static partial class CoreSpecification
    {
        public static void OutfitOwnership()
        {
            var e=New();var s=e.State;int attack=Catalog.Attack(s),health=Catalog.Health(s);
            Check(!Outfits.Equip(s,1).Success && !Outfits.Equip(s,2).Success,"unowned and unknown outfit rejected");
            s.gems=599;Check(!Social.Buy(s,Outfits.Key).Success && s.gems==599,"price enforced atomically");
            s.gems=900;Check(Social.Buy(s,Outfits.Key).Success && s.gems==300,"exact price and ownership");
            Check(!Social.Buy(s,Outfits.Key).Success && s.gems==300,"duplicate purchase rejected");
            Check(Outfits.Equip(s,1).Success && Outfits.Active(s,0)==1 && Outfits.Active(s,1)==0,"Luke only");
            Check(Social.Build(s).outfit==1 && Catalog.Attack(s)==attack && Catalog.Health(s)==health,"public appearance without stat boost");
            s.selectedSkin=0;Check(Outfits.Equip(s,0).Success && s.selectedSkin==0 && Outfits.Owned(s),"unequip preserves paint and ownership");
            s.selectedOutfit=1;s.ownedCosmetics.Clear();Social.Normalize(s);Check(s.selectedOutfit==0,"corrupt selection cannot grant outfit");
        }
        public static void OutfitReplayEquality()
        {
            var plain=GameState.New();var dressed=GameState.New();dressed.ownedCosmetics.Add(Outfits.Key);dressed.selectedOutfit=1;
            var a=new Combat(plain,0,"equal",42);var b=new Combat(dressed,0,"equal",42);
            for(int i=0;i<1600 && !a.Ended;i++)
            {
                if(a.Choosing){int skill=a.Choices[0];a.Choose(skill);b.Choose(skill);}
                if(a.CanUltimate){a.UseUltimate();b.UseUltimate();}
                var move=new V2((float)System.Math.Cos(i*.02),(float)System.Math.Sin(i*.02));a.Tick(ReplayRecorder.Step,move);b.Tick(ReplayRecorder.Step,move);
            }
            Check(a.Hp==b.Hp && a.Kills==b.Kills && a.Elapsed==b.Elapsed && a.Position.x==b.Position.x && a.Mods==b.Mods && a.UltimateUses==b.UltimateUses,"outfit does not alter simulation");
        }
        public static void GuideProofAndMigration()
        {
            var e=New();Check(StarterGuide.Next(e.State)==0 && !e.State.guideIntroSeen,"new guide starts at movement");
            Check(!e.Upgrade("missing").Success && !e.State.guideUpgrade,"failed upgrade gives no progress");
            Check(e.Upgrade(e.State.equipped[0]).Success && e.State.guideUpgrade,"actual upgrade recorded");
            e.BeginRun("guide",0);var c=new Combat(e.State,0,"guide");
            for(int i=0;i<35;i++)c.Tick(.05f,new V2(1,0));
            Check(c.DistanceMoved>=120 && !e.State.guideMoved,"movement stays uncommitted during battle");
            Check(!e.FinishCampaign(c).Success && !e.State.guideMoved,"unfinished run cannot advance guide");
            for(int i=0;i<25;i++){c.Enemies.Add(new Enemy{id=900+i,hp=0,maxHp=1});c.Tick(.001f,new V2());}
            Check(c.UseUltimate(),"ultimate exercised");c.Skills[0]=3;c.Skills[5]=2;c.Abandon();
            Check(e.FinishCampaign(c).Success && e.State.guideMoved && e.State.guideUltimate && e.State.guideMod,"settled simulation advances guide");
            Check(!e.FinishCampaign(c).Success,"duplicate settlement rejected");
            var old=GameState.New();old.guideVersion=0;old.totalRuns=4;old.items[0].level=3;old.gems=73;new Economy(old,()=>Now);
            Check(old.guideIntroSeen && old.guideUpgrade && !old.guideMoved && old.gems==73,"legacy saves not forced or credited with unobserved actions");
            var edge=new Combat(GameState.New(),0,"edge");edge.Position=new V2(514,490);for(int i=0;i<10;i++)edge.Tick(.05f,new V2(1,0));
            Check(edge.DistanceMoved==0,"pushing boundary is not actual movement");
        }
    }
}
