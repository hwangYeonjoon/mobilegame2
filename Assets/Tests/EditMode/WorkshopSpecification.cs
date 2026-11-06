using System;
using ScrapGuard.Core;

namespace ScrapGuard.Tests
{
    public static partial class CoreSpecification
    {
        public static void ModGeometry()
        {
            var c=new Combat(GameState.New(),0,"pull");c.Skills[1]=3;c.Skills[7]=1;
            Check(!c.HasMod(0),"requires both components");c.Skills[7]=2;
            var e=new Enemy {id=999,position=c.Position+new V2(120,0),hp=10000,maxHp=10000};c.Enemies.Add(e);
            c.Tick(.05f,new V2());Check(V2.Distance(e.position,c.Position)<120,"magnet saw pulls target");
            c=new Combat(GameState.New(),0,"turret");c.Skills[0]=3;c.Skills[5]=3;
            c.Enemies.Add(new Enemy {id=999,position=c.Position+new V2(180,0),hp=10000,maxHp=10000});c.Tick(.05f,new V2());
            Check(c.Bullets.Exists(b=>b.mod && b.pierce==1),"turret creates separate piercing fire");
            c=new Combat(GameState.New(),0,"chain");c.Skills[2]=4;c.Skills[3]=1;
            for(int i=0;i<3;i++)c.Enemies.Add(new Enemy {id=990+i,position=c.Position+new V2(0,-110-i*60),hp=10000,maxHp=10000});
            c.Tick(.05f,new V2());Check(c.Effects.FindAll(f=>f.kind==8).Count==3,"chain visits three distinct targets once");
            Check(c.Enemies.FindAll(x=>x.id>=990 && x.hp<10000).Count==3,"chain damages distinct targets");
        }
        public static void ModReplay()
        {
            var s=GameState.New();s.heroes[0].level=30;foreach(var item in s.items){item.level=20;item.rarity=2;}
            var c=new Combat(s,0,"mods",41);var r=new ReplayRecorder();
            for(int i=0;i<12000 && !c.Ended;i++)
            {
                if(c.Choosing)
                {
                    int choice=c.Choices[0];foreach(int skill in new[]{1,7,0,5,2,3,4,6})if(Array.IndexOf(c.Choices,skill)>=0){choice=skill;break;}
                    r.Choose(choice);c.Choose(choice);
                }
                if(c.CanUltimate){r.Ultimate();c.UseUltimate();if(c.Ended)break;}
                var target=new V2(270+(float)Math.Cos(c.Elapsed*.4f)*160,490+(float)Math.Sin(c.Elapsed*.4f)*210);
                if(c.Pickups.Count>0){float near=float.MaxValue;foreach(var pickup in c.Pickups){float d=V2.Distance(pickup.position,c.Position);if(d<near){near=d;target=pickup.position;}}}
                c.Tick(ReplayRecorder.Step,r.Move((target-c.Position).Normal));
            }
            if(!c.Ended)c.Abandon();var verified=ReplayRecorder.Verify(s,0,"mods",41,r.Steps,500);
            Check(c.Mods!=0,"replay actually exercises an earned mod");
            Check(c.Mods==verified.Mods && c.Kills==verified.Kills && c.Hp==verified.Hp && c.Won==verified.Won,"mod battle is deterministic");
            Check(c.Report().cause==verified.Report().cause,"report cause matches replay");
        }
        public static void BattleDamageReport()
        {
            var c=new Combat(GameState.New(),12,"damage");c.Position=c.Zones[0].Center;c.Shield=50;c.Hp=20;
            c.Tick(.05f,new V2());var report=c.Report();
            Check(c.Ended && report.cause==(int)DamageCause.Poison,"fatal source recorded");
            Check(report.damage[(int)DamageCause.Poison]==20,"only actual HP lost, no shield or overkill");
            c.Abandon();Check(c.Report().cause==(int)DamageCause.Poison,"abandon cannot overwrite already ended cause");
            c=new Combat(GameState.New(),0,"boss");c.Enemies.Add(new Enemy{id=999,kind=3,finalBoss=true,hp=250,maxHp=1000});
            c.Elapsed=c.Duration-31;c.Tick(.05f,new V2());c.Abandon();
            Check(c.Report().cause==(int)DamageCause.Abandoned,"voluntary exit distinguished");
        }
        public static void CollectionMigration()
        {
            var s=GameState.New();s.clearedStages.AddRange(new[]{0,1,2,3,4,5});s.regions=null;s.collectionVersion=0;
            new Economy(s,()=>Now);var p=Workshop.Region(s,0);
            Check(p.wins==6 && p.blueprints==7 && p.bosses==1,"one-time credit from six known clears");
            Workshop.Normalize(s);Check(p.blueprints==7,"migration never repeats credit");
            Check(!Social.HasSkin(s,3),"migration does not give uncrafted skin");
        }
        public static void RegionRewardsAndCraft()
        {
            var e=New();e.State.unlockedStage=5;
            for(int i=0;i<4;i++)
            {
                string id="boss"+i;e.BeginRun(id,5);var c=new Combat(e.State,5,id);c.Enemies.Add(new Enemy{id=999,kind=3,finalBoss=true,hp=0,maxHp=100});c.Tick(.05f,new V2());
                Check(e.FinishCampaign(c).Success,"verified simulation reward");
                Check(!e.FinishCampaign(c).Success,"duplicate simulation cannot pay twice");
            }
            var p=Workshop.Region(e.State,0);Check(p.blueprints==8 && p.wins==4 && p.bosses==4,"regional final grants two per win");
            Check((e.State.bossDiscoveries&1)!=0 && (e.State.enemyDiscoveries&1)!=0,"codex records actual defeated roles and boss");
            Check(!e.CraftRegionSkin(1).Success && !e.CraftRegionSkin(-1).Success,"region isolation and bounds");
            int attack=Catalog.Attack(e.State);Check(e.CraftRegionSkin(0).Success && p.blueprints==0 && Social.HasSkin(e.State,3),"craft consumes eight and grants once");
            Check(!e.CraftRegionSkin(0).Success && p.blueprints==0,"duplicate crafting rejected");
            Check(Social.Customize(e.State,0,0,0,3).Success && Catalog.Attack(e.State)==attack,"region paint is cosmetic only");
            Check(!e.Buy("region_skin:0").Success,"regional exclusive unavailable in cash/gem shop");
        }
    }
}
