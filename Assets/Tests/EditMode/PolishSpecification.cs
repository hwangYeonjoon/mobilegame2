using System;
using ScrapGuard.Core;
namespace ScrapGuard.Tests
{
    public static partial class CoreSpecification
    {
        public static void ShowcaseThemeOwnership()
        {
            var s=GameState.New();s.gems=900;int damage=Catalog.Attack(s),hp=Catalog.Health(s);
            Check(!ShowcaseThemes.Equip(s,1).Success&&!ShowcaseThemes.Equip(s,-1).Success,"unowned or invalid theme rejected");
            Check(Social.Buy(s,"theme_orbit").Success&&s.gems==500,"theme has exact cost");
            Check(!Social.Buy(s,"theme_orbit").Success&&s.gems==500,"theme cannot be charged twice");
            Check(ShowcaseThemes.Equip(s,1).Success&&Social.Build(s).theme==1,"theme appears in public DTO");
            Check(!ShowcaseThemes.Equip(s,3).Success,"earned background requires real progression");
            s.clearedStages.Add(5);Check(ShowcaseThemes.Equip(s,3).Success&&s.gems==500,"earned theme is free");
            s.gemDebt=1;Check(!Social.Buy(s,"theme_abyss").Success&&s.gems==500,"refund debt blocks theme spend");
            Check(damage==Catalog.Attack(s)&&hp==Catalog.Health(s),"themes do not change power");
            s.showcaseTheme=99;Social.Normalize(s);Check(s.showcaseTheme==0,"invalid legacy theme safely normalized");
        }
        public static void NewOperationGoals()
        {
            foreach(int stage in new[]{2,4,8})
            {
                var c=new Combat(GameState.New(),stage,"operation");c.Elapsed=12;
                for(int i=0;i<2600&&!c.TaskCompleted;i++)
                {
                    c.Enemies.Clear();c.Bullets.Clear();c.Hazards.Clear();c.Pickups.Clear();c.Hp=c.MaxHp;
                    if(c.Choosing)c.Choose(c.Choices[0]);c.Position=c.TaskPosition;c.Tick(.05f,new V2());
                }
                Check(c.TaskCompleted&&c.CanAssemble,"operation completes by its actual objective "+stage);
                Check(c.Assemble(4)&&!c.Assemble(1),"recovered operation grants exactly one build");
                Check(c.TaskBonusGold==0,"unfinished main mission has no operation payout");
            }
            var distant=new Combat(GameState.New(),2,"distant");distant.Elapsed=12;distant.Position=new V2(480,220);
            for(int i=0;i<30;i++){distant.Enemies.Clear();distant.Tick(.05f,new V2());}
            Check(distant.TaskProgress==0,"remote player cannot recover robot pieces");
            var relay=new Combat(GameState.New(),4,"relay");relay.Elapsed=12;
            for(int i=0;i<110;i++){relay.Enemies.Clear();relay.Position=relay.TaskPosition;if(relay.Choosing)relay.Choose(relay.Choices[0]);relay.Tick(.05f,new V2());}
            relay.Position=relay.OperationNode(0);Check(relay.TaskNode>0&&relay.PoweredSafety,"restored node offers local safety");
            relay.Position=new V2(500,230);Check(!relay.PoweredSafety,"power safety is local");
            for(int i=0;i<350&&!relay.TaskCompleted;i++){relay.Enemies.Clear();relay.Hp=relay.MaxHp;relay.Position=relay.TaskPosition;if(relay.Choosing)relay.Choose(relay.Choices[0]);relay.Tick(.05f,new V2());}
            for(int i=0;i<3;i++){relay.Position=relay.OperationNode(i);Check(relay.TaskCompleted&&relay.PoweredSafety,"each restored relay remains protective");}
        }
        public static void TacticalBuilds()
        {
            Combat Ready(int module){var c=new Combat(GameState.New(),1,"build");c.Elapsed=12;c.Position=c.TaskPosition;c.ChooseTask(1);Check(c.Assemble(module),"build accepted");return c;}
            var turret=Ready(1);var anchor=turret.FieldAnchor;turret.Position=new V2(100,650);
            Check(turret.TurretPosition.x==anchor.x&&!turret.InTurretZone,"turret remains at deployed position");
            for(int i=0;i<25;i++){turret.Enemies.Clear();if(turret.Choosing)turret.Choose(turret.Choices[0]);turret.Tick(.05f,new V2());}
            Check(turret.InTurretZone&&turret.BuildUses>0,"stopping redeploys distant turret");
            var rail=Ready(4);rail.Enemies.Add(new Enemy{id=9999,position=new V2(300,500),hp=100000,maxHp=100000});
            for(int i=0;i<100;i++){if(rail.Choosing)rail.Choose(rail.Choices[0]);rail.Tick(.05f,new V2(1,0));}
            Check(rail.BuildUses>0,"movement charges piercing build");
            var magnet=Ready(2);var e=new Enemy{id=9999,position=magnet.Position+new V2(140,0),hp=100000,maxHp=100000};magnet.Enemies.Add(e);magnet.Tick(.05f,new V2());
            Check(V2.Distance(e.position,magnet.Position)<120&&magnet.BuildUses==1,"magnet actively pulls close targets");
        }
        public static void HeroIdentityActions()
        {
            for(int hero=0;hero<HeroCatalog.Count;hero++)
            {
                var s=GameState.New();s.selectedHero=hero;s.heroes[hero].unlocked=true;
                var c=new Combat(s,1,"identity");var enemy=new Enemy{id=9000,hp=1000000,maxHp=1000000};
                for(int i=0;i<240;i++)
                {
                    c.Enemies.Clear();enemy.position=c.Position+new V2(hero==2?10:90,0);c.Enemies.Add(enemy);c.Hp=c.MaxHp;
                    if(hero==2)c.Shield=100;if(c.Choosing)c.Choose(c.Choices[0]);
                    c.Tick(.05f,hero==1?new V2(i%80<40?1:-1,0):new V2());
                }
                Check(c.IdentityUses>0,"hero has distinct base action "+hero);
            }
            var supported=new Combat(GameState.New(),0,"cold-support");
            var target=new Enemy{id=9200,hp=10000,maxHp=10000,position=supported.Position+new V2(90,0)};
            supported.Enemies.Add(target);supported.Bullets.Add(new Bullet{id=9201,position=target.position,life=1,damage=1,chilling=true,drone=true});
            supported.Tick(.05f,new V2());Check(target.slowTime>0&&target.freezeTime==0,"Iris support preserves slow for a different active hero");
        }
        public static void PolishReplayCompatibility()
        {
            foreach(int stage in new[]{1,2,4,8})for(int hero=0;hero<HeroCatalog.Count;hero++)
            {
                var state=GameState.New();state.selectedHero=hero;state.heroes[hero].unlocked=true;state.heroes[hero].level=30;
                foreach(var item in state.items)item.level=20;
                var c=new Combat(state,stage,"new-replay",91);var r=new ReplayRecorder();
                for(int i=0;i<1500&&!c.Ended;i++)
                {
                    if(c.Choosing){int choice=c.Choices[0];c.Choose(choice);r.Choose(choice);}
                    if(c.CanChooseTask){c.ChooseTask(1);r.Encounter(1);}
                    if(c.CanAssemble){c.Assemble(4);r.Assemble(4);}
                    var move=c.TaskActive?(c.TaskPosition-c.Position).Normal:V2.Angle(i*.03f);
                    if(c.TaskActive&&V2.Distance(c.TaskPosition,c.Position)<15)move=new V2();
                    c.Tick(ReplayRecorder.Step,r.Move(move));
                }
                var copy=ReplayRecorder.Verify(state,stage,"new-replay",91,r.Steps,60);
                Check(c.Hp==copy.Hp&&c.Kills==copy.Kills&&c.TaskNode==copy.TaskNode&&c.TaskProgress==copy.TaskProgress&&c.IdentityUses==copy.IdentityUses&&c.BuildUses==copy.BuildUses,"new identities and objectives replay identically "+stage+"/"+hero);
            }
        }
    }
}
