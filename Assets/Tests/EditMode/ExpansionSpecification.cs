using System;
using System.Collections.Generic;
using ScrapGuard.Core;

namespace ScrapGuard.Tests
{
    public static partial class CoreSpecification
    {
        public static void ExpansionMigration()
        {
            var s=GameState.New();s.heroes.RemoveRange(3,s.heroes.Count-3);s.heroes[1].unlocked=true;s.heroes[1].level=17;
            s.selectedHero=1;s.gems=777;s.collectionVersion=1;
            for(int i=0;i<30;i++)s.clearedStages.Add(i);s.unlockedStage=29;
            new Economy(s,()=>Now);new Economy(s,()=>Now);
            Check(s.heroes.Count==5 && s.heroes[1].level==17 && s.selectedHero==1 && s.gems==777,"legacy progress is preserved exactly");
            Check(!s.heroes[3].unlocked && !s.heroes[4].unlocked && s.unlockedStage==30,"new slots locked, stage 31 unlocked after old final");
            Check(Workshop.Region(s,5).blueprints==0 && Workshop.Region(s,6).blueprints==0,"no retroactive new-region materials");
            s.heroes.Add(new HeroProgress{id=3});bool rejected=false;try{HeroCatalog.Normalize(s);}catch(ArgumentException){rejected=true;}
            Check(rejected,"duplicate hero records are not repaired into ownership");
        }
        public static void ExpansionRecruitment()
        {
            var e=New();int gold=e.State.gold,gems=e.State.gems;
            Check(!e.RecruitHero(3).Success && !e.RecruitHero(4).Success,"locked recruits cannot be claimed");
            e.State.unlockedStage=41;Check(!e.RecruitHero(3).Success,"unlock bound alone is not a clear");
            e.State.clearedStages.Add(11);Check(e.RecruitHero(3).Success && !e.RecruitHero(4).Success,"stage 12 only recruits Spark");
            e.State.clearedStages.Add(23);Check(e.RecruitHero(4).Success,"stage 24 recruits Iris");
            Check(!e.RecruitHero(4).Success && !e.RecruitHero(0).Success && !e.RecruitHero(99).Success,"duplicate and invalid recruits rejected");
            Check(e.State.gold==gold && e.State.gems==gems,"recruitment has no currency cost or bonus");
            for(int hero=3;hero<5;hero++)
            {
                Check(e.SelectHero(hero).Success,"new hero selectable");Check(e.UpgradeHero().Success,"new hero upgrades");
                Check(Social.Customize(e.State,hero,0,0,0).Success,"new hero can be showcased");
                var profile=Social.Build(e.State);Check(profile.attack==Catalog.Attack(e.State) && profile.health==Catalog.Health(e.State),"profile and combat stats agree");
            }
        }
        public static void ExpansionHeroCombat()
        {
            var s=GameState.New();s.selectedHero=3;
            var spark=new Combat(s,0,"spark");Check(spark.Skills[2]==1,"Spark starts with a coil");
            var target=new Enemy{id=999,position=spark.Position+new V2(80,0),hp=100000,maxHp=100000};spark.Enemies.Add(target);
            spark.Tick(.05f,new V2());Check(target.hp<100000 && spark.Effects.Exists(e=>e.kind==4),"starting coil actually attacks");
            for(int i=0;i<105;i++)spark.Tick(.05f,new V2());
            spark.Enemies.Clear();target.position=spark.Position+new V2(300,0);spark.Enemies.Add(target);spark.Elapsed=1; // Elapsed is not used to charge: kills fill charge below.
            for(int i=0;i<25;i++){spark.Enemies.Add(new Enemy{id=800+i,hp=0,maxHp=1});spark.Tick(.001f,new V2());}
            float hp=target.hp;Check(spark.UseUltimate() && target.hp<hp && spark.Effects.Exists(e=>e.kind==8),"electrical ultimate reaches 310 and emits arcs");
            s.selectedHero=4;var iris=new Combat(s,0,"iris");
            target=new Enemy{id=999,position=iris.Position+new V2(50,0),hp=100000,maxHp=100000,speed=0};iris.Enemies.Add(target);
            for(int i=0;i<10;i++)iris.Tick(.05f,new V2());Check(target.slowTime>0,"Iris bullets apply timed slow");
            for(int i=0;i<25;i++){iris.Enemies.Add(new Enemy{id=800+i,hp=0,maxHp=1});iris.Tick(.001f,new V2());}
            Check(iris.UseUltimate() && target.freezeTime>2,"Iris freezes ordinary enemies");
            var before=target.position;target.speed=100;iris.Tick(.05f,new V2());Check(V2.Distance(before,target.position)==0,"frozen enemies stop moving");
            var boss=new Enemy{id=1000,kind=3,finalBoss=true,position=iris.Position+new V2(200,0),hp=100000,maxHp=100000};
            var bossTest=new Combat(s,0,"boss");bossTest.Enemies.Add(boss);
            for(int i=0;i<25;i++){bossTest.Enemies.Add(new Enemy{id=800+i,hp=0,maxHp=1});bossTest.Tick(.001f,new V2());}
            Check(bossTest.UseUltimate() && boss.freezeTime<=.6f && boss.freezeTime>0,"boss freeze is capped");
        }
        public static void ExpansionReplay()
        {
            foreach(int hero in new[]{3,4})foreach(int stage in new[]{30,36})
            {
                var s=GameState.New();s.selectedHero=hero;s.heroes[hero].unlocked=true;s.heroes[hero].level=30;foreach(var item in s.items)item.level=20;
                var support=new SupportSnapshot{hero=hero,power=180};var c=new Combat(s,stage,"expansion",123,false,"",support);var r=new ReplayRecorder();
                for(int i=0;i<1500 && !c.Ended;i++)
                {
                    if(c.Choosing){r.Choose(c.Choices[0]);c.Choose(c.Choices[0]);}
                    if(c.CanUltimate){r.Ultimate();c.UseUltimate();}
                    if(c.CanSupport){r.Support();c.UseSupport();}
                    c.Tick(ReplayRecorder.Step,r.Move(new V2((float)Math.Cos(i*.02),(float)Math.Sin(i*.02))));
                }
                var replay=ReplayRecorder.Verify(s,stage,"expansion",123,r.Steps,60,"",support);
                Check(c.SupportUsed && c.UltimateUses>0,"new support and ultimate exercised");
                Check(replay.Kills==c.Kills && replay.Hp==c.Hp && replay.Position.x==c.Position.x,"expanded combat replays exactly");
            }
        }
        public static void ExpansionTerrainAndRewards()
        {
            for(int region=5;region<7;region++)
            {
                var s=GameState.New();var c=new Combat(s,region*6,"hazard");
                for(int i=0;i<102;i++){c.Enemies.Clear();c.Tick(.05f,new V2());}
                var hazard=c.Hazards.Find(h=>h.width>0);Check(hazard!=null && hazard.delay>0,"new terrain telegraphs before damage");
                Check(hazard.cause==(region==5?DamageCause.Electricity:DamageCause.Heat),"new damage source is identified");
                c.Position=hazard.position;c.Hp=10;hazard.delay=.001f;c.Tick(.05f,new V2());
                Check(c.Ended && c.Report().damage[(int)hazard.cause]==10,"new terrain records actual fatal damage");
                var e=New();e.State.unlockedStage=41;int stage=Campaign.FinalStage(region).Id;
                for(int i=0;i<4;i++)
                {
                    string id="new-region"+i;e.BeginRun(id,stage);c=new Combat(e.State,stage,id);
                    c.Enemies.Add(new Enemy{id=999,kind=3,finalBoss=true,hp=0,maxHp=1});c.Tick(.05f,new V2());
                    Check(e.FinishCampaign(c).Success && !e.FinishCampaign(c).Success,"new final settles once");
                }
                Check(Workshop.Region(e.State,region).blueprints==8 && (e.State.bossDiscoveries&(1<<region))!=0,"new region earns materials and codex");
                Check(e.CraftRegionSkin(region).Success && Social.Customize(e.State,0,0,0,region+3).Success,"new region skin craft and equip");
            }
        }
    }
}
