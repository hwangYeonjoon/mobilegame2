using System;
using ScrapGuard.Core;
namespace ScrapGuard.Tests
{
    public static partial class CoreSpecification
    {
        public static void FairTrialIgnoresGrowth()
        {
            var low=GameState.New();var high=GameState.New();high.selectedHero=3;high.heroes[3].unlocked=true;high.heroes[3].level=30;
            foreach(var item in high.items){item.rarity=2;item.level=20;}
            var a=new Combat(low,4,"a",67,false,SkillTrial.Mode);var b=new Combat(high,4,"b",67,false,SkillTrial.Mode);var recorder=new ReplayRecorder();
            Check(a.Hero==0&&b.Hero==0&&a.MaxHp==b.MaxHp,"fixed hero and health despite paid growth");
            for(int i=0;i<2700&&!a.Ended;i++)
            {
                if(a.Choosing){int skill=a.Choices[0];Check(b.Choices[0]==skill,"identical growth options");a.Choose(skill);b.Choose(skill);recorder.Choose(skill);}
                var move=recorder.Move(new V2((float)Math.Sin(i*.01f),0));a.Tick(ReplayRecorder.Step,move);b.Tick(ReplayRecorder.Step,move);
            }
            if(!a.Ended)a.Abandon();if(!b.Ended)b.Abandon();
            Check(a.Hp==b.Hp&&a.Kills==b.Kills&&a.CoreScore==b.CoreScore,"identical skill outcome from identical inputs");
            var replay=ReplayRecorder.Verify(high,4,"a",67,recorder.Steps,100,SkillTrial.Mode);
            Check(replay.CoreScore==a.CoreScore,"server replay uses fixed loadout");
            Check(high.selectedHero==3&&high.heroes[3].level==30,"fair trial never mutates real progression");
        }
        public static void FairTrialNoRewardLeak()
        {
            var e=New();Check(!e.BeginChallenge("locked",SkillTrial.Mode).Success,"skill mode respects unlock");e.State.clearedStages.Add(4);
            Check(e.BeginChallenge("skill",SkillTrial.Mode).Success,"unlocked skill trial");var c=new Combat(e.State,4,"skill",42,false,SkillTrial.Mode);c.Hp=c.MaxHp=100000;
            for(int i=0;i<1000&&!c.Ended;i++){if(c.Choosing)c.Choose(c.Choices[0]);c.Tick(ReplayRecorder.Step,new V2());}c.Abandon();
            int gold=e.State.gold;Check(e.FinishChallenge(c).Success,"skill trial settles");
            Check(e.State.skillWeekly.Count==1&&e.State.weekly.Count==0&&e.State.dailyRuns==0&&e.State.gold==gold,"separate history no farming");
            Check(!e.ClaimWeekly(e.State.skillWeekly[0].week,0).Success,"cannot claim growth rewards from skill score");
        }
        public static void PremiumWalletRules()
        {
            var s=GameState.New();s.gems=50;PremiumWallet.Grant(s,600);Check(PremiumWallet.Spend(s,100)&&s.gems==550&&s.paidGems==550,"free first spending");
            s.gemDebt=30;Check(!PremiumWallet.Spend(s,1),"unresolved refund stops premium spend");
            Check(PremiumWallet.Grant(s,20)==0&&s.gemDebt==10&&s.gems==550,"partial recovery");
            Check(PremiumWallet.Grant(s,100)==90&&s.paidGems==640&&s.gemDebt==0,"remaining recovery preserves wallet");
        }
        public static void PriceCatalogRules()
        {
            Check(CommerceCatalog.SmallestPack(600).Gems==600&&CommerceCatalog.SmallestPack(1200).Gems==1200,"major products match one pack");
            Check(CommerceCatalog.SmallestPack(0)==null&&CommerceCatalog.SmallestPack(3000)==null,"no misleading pack suggestions");
            decimal previous=decimal.MaxValue;foreach(var offer in CommerceCatalog.Offers)if(offer.Consumable){decimal unit=(decimal)offer.ProposedWon/offer.Gems;Check(unit<=previous,"larger pack not worse value");previous=unit;}
        }
    }
}
