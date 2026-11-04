using System;
using System.Collections.Generic;
using ScrapGuard.Core;
namespace ScrapGuard.Tests
{
    public static partial class CoreSpecification
    {
        public static void WardrobeMigrationAndTargets()
        {
            var s=GameState.New();s.heroes[1].unlocked=true;s.ownedCosmetics.Add(Outfits.RailKey);s.selectedOutfit=2;
            Social.Normalize(s);Check(Outfits.Active(s,1)==2 && s.heroOutfits.Count==HeroCatalog.Count,"legacy selected outfit migrates once");
            s.gems=1200;Check(Outfits.Target(s,3).Success,"locked hero outfit can be a saving goal");
            Check(!Social.Buy(s,Outfits.Product(3)).Success && s.gems==1200,"locked hero cannot be charged");
            s.heroes[3].unlocked=true;Check(Social.Buy(s,Outfits.Product(3)).Success && s.gems==600 && s.outfitTarget==0,"buy fulfills goal at exact price");
            Check(Outfits.Equip(s,3).Success && Outfits.Active(s,1)==2 && Outfits.Active(s,3)==3,"independent hero wardrobe");
            Check(Outfits.Clear(s,3).Success && Outfits.Active(s,3)==0 && Outfits.Active(s,1)==2,"clearing one hero preserves others");
            Check(!Outfits.Target(s,3).Success && !Outfits.Target(s,999).Success && !Outfits.Clear(s,-1).Success && !Outfits.Equip(s,999).Success,"invalid target and selection rejected");
            s.heroOutfits[0]=2;s.heroOutfits[4]=3;s.heroOutfits.Add(999);Social.Normalize(s);
            Check(s.heroOutfits.Count==HeroCatalog.Count && Outfits.Active(s,0)==0 && Outfits.Active(s,4)==0,"foreign outfit cannot be assigned to wrong hero");
            s.ownedCosmetics.Remove(Outfits.RailKey);Social.Normalize(s);Check(Outfits.Active(s,1)==0,"lost ownership removes equipped display");
        }
        public static void NewOutfitsHaveNoPower()
        {
            for(int id=1;id<=Outfits.Count;id++)
            {
                var s=GameState.New();s.selectedHero=s.showcaseHero=Outfits.Hero(id);s.heroes[s.selectedHero].unlocked=true;s.gems=600;
                int attack=Catalog.Attack(s),health=Catalog.Health(s);
                Check(Social.Buy(s,Outfits.Product(id)).Success && Outfits.Equip(s,id).Success && Social.Build(s).outfit==id,"new outfit purchase and public appearance");
                Check(!Social.Buy(s,Outfits.Product(id)).Success && s.gems==0 && attack==Catalog.Attack(s)&&health==Catalog.Health(s),"duplicate cannot charge or change stats");
                var dressed=new Combat(s,1,"cosmetic",71);Outfits.Clear(s,s.selectedHero);var plain=new Combat(s,1,"cosmetic",71);
                for(int i=0;i<900&&!dressed.Ended;i++)
                {
                    if(dressed.Choosing){int skill=dressed.Choices[0];dressed.Choose(skill);plain.Choose(skill);}
                    var move=V2.Angle(i*.02f);dressed.Tick(ReplayRecorder.Step,move);plain.Tick(ReplayRecorder.Step,move);
                }
                Check(dressed.Hp==plain.Hp&&dressed.Kills==plain.Kills&&dressed.Position.x==plain.Position.x,"cosmetic replay parity for "+id);
            }
        }
        public static void RemainingHeroEngineering()
        {
            Combat Ready(int hero,int module)
            {
                var s=GameState.New();s.selectedHero=hero;s.heroes[hero].unlocked=true;var c=new Combat(s,1,"new-module");
                c.Elapsed=12;c.Position=c.TaskPosition;c.ChooseTask(1);c.Assemble(module);
                c.Enemies.Add(new Enemy{id=999,position=c.Position+new V2(70,0),hp=100000,maxHp=100000});return c;
            }
            var bolt=Ready(2,3);var enemy=bolt.Enemies[0];bolt.Tick(.05f,new V2());
            Check(bolt.EngineeringPulses==1 && Math.Abs(bolt.Shield-bolt.MaxHp*.12f)<.001f && V2.Distance(enemy.position,bolt.Position)>100,"Bolt adds shield and actual knockback");
            var spark=Ready(3,1);for(int i=0;i<50;i++){if(spark.Choosing)spark.Choose(spark.Choices[0]);spark.Tick(.05f,new V2());}
            Check(spark.EngineeringPulses==1,"Spark stationary charge discharges");
            for(int i=0;i<50;i++)spark.Tick(.05f,new V2());Check(spark.EngineeringPulses==1,"Spark cannot chain forever without moving");
            spark.Tick(.05f,new V2(0,1));for(int i=0;i<45;i++)spark.Tick(.05f,new V2());Check(spark.EngineeringPulses==2,"movement rearms Spark charge");
            var iris=Ready(4,2);enemy=iris.Enemies[0];iris.Enemies.Add(new Enemy{id=1000,position=iris.Position+new V2(0,70),hp=100000,maxHp=100000,finalBoss=true});iris.Tick(.05f,new V2());
            Check(enemy.freezeTime>0 && iris.Enemies[1].freezeTime==0 && iris.Enemies[1].slowTime>0,"Iris freezes ordinary enemy but only slows boss");
        }
        public static void ConditionsAndNewModuleReplay()
        {
            for(int seed=0;seed<9;seed++)
            {
                var s=GameState.New();s.selectedHero=2+seed%3;s.heroes[s.selectedHero].unlocked=true;s.heroes[s.selectedHero].level=30;s.tutorialCompleted=true;
                foreach(var item in s.items){item.rarity=2;item.level=20;}
                var c=new Combat(s,7,"conditions",seed);var r=new ReplayRecorder();
                for(int i=0;i<1200&&!c.Ended;i++)
                {
                    if(c.Choosing){int skill=c.Choices[0];c.Choose(skill);r.Choose(skill);}
                    if(c.CanChooseTask){c.ChooseTask(1);r.Encounter(1);}
                    if(c.CanAssemble){int module=c.Hero==2?3:c.Hero==3?1:2;c.Assemble(module);r.Assemble(module);}
                    c.Tick(ReplayRecorder.Step,r.Move((c.TaskPosition-c.Position).Normal));
                }
                c.Abandon();var copy=ReplayRecorder.Verify(s,7,"conditions",seed,r.Steps,45);
                Check(c.FieldModule>0 && c.Hp==copy.Hp && c.Kills==copy.Kills && c.EngineeringPulses==copy.EngineeringPulses && c.ConditionPulses==copy.ConditionPulses,"new module and condition replay "+seed);
                Check(c.Condition==seed/3,"three independent seeded conditions");
                if(c.Condition==2)Check(c.ConditionPulses>=4,"electrical warning emits periodically");
                Check(new Combat(s,0,"intro",seed).Condition==0 && new Combat(s,7,"fair",seed,false,SkillTrial.Mode).Condition==0,"intro and ranked trial exclude conditions");
            }
            var warning=new Combat(GameState.New(),7,"warning",6);warning.Elapsed=8;
            for(int i=0;i<161;i++){if(warning.Choosing)warning.Choose(warning.Choices[0]);warning.Tick(.05f,new V2());}
            Check(warning.Hazards.Exists(h=>h.cause==DamageCause.Electricity && h.delay>1),"hazard has readable delay before impact");
        }
        public static void MemorialThresholds()
        {
            Check(GuildRestoration.Honor(29)==0&&GuildRestoration.Honor(30)==1&&GuildRestoration.Honor(60)==2&&GuildRestoration.Honor(120)==3,"permanent memorial thresholds");
            Check(GuildRestoration.NextGoal(29)==30&&GuildRestoration.NextGoal(30)==60&&GuildRestoration.NextGoal(60)==120&&GuildRestoration.NextGoal(120)==0,"next objective after basic construction");
        }
    }
}
