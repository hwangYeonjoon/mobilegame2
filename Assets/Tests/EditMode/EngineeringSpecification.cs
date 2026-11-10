using System;
using System.Collections.Generic;
using ScrapGuard.Core;
namespace ScrapGuard.Tests
{
    public static partial class CoreSpecification
    {
        public static void AssemblyGuardsAndReplay()
        {
            var s=GameState.New();s.heroes[0].level=30;foreach(var item in s.items){item.level=20;item.rarity=2;}
            var c=new Combat(s,1,"assembly",71);var r=new ReplayRecorder();
            Check(!c.Assemble(1),"unfinished task cannot grant module");
            for(int frame=0;frame<1200 && !c.Ended;frame++)
            {
                if(c.Choosing){int skill=c.Choices[0];c.Choose(skill);r.Choose(skill);}
                if(c.CanChooseTask){c.ChooseTask(1);r.Encounter(1);}
                if(c.CanAssemble){Check(!c.Assemble(0)&&!c.Assemble(5),"assembly allowlist");Check(c.Assemble(1),"recovered parts grant one choice");r.Assemble(1);Check(!c.Assemble(2),"one choice per run");}
                c.Tick(ReplayRecorder.Step,r.Move((c.TaskPosition-c.Position).Normal));
            }
            c.Abandon();var copy=ReplayRecorder.Verify(s,1,"assembly",71,r.Steps,45);
            Check(c.FieldModule==1 && copy.FieldModule==1 && c.Hp==copy.Hp && c.Kills==copy.Kills && c.Mods==copy.Mods,"assembly participates in deterministic replay");
            foreach(var step in new[]{new ReplayStep{assembly=1},new ReplayStep{assembly=4},new ReplayStep{assembly=1,frames=1},new ReplayStep{assembly=1,encounter=1},new ReplayStep{assembly=1,support=true},new ReplayStep{assembly=1,ultimate=true},new ReplayStep{assembly=1,skill=0}})
            {bool rejected=false;try{ReplayRecorder.Verify(s,1,"bad",71,new List<ReplayStep>{step},30);}catch(ArgumentException){rejected=true;}Check(rejected,"forged assembly rejected");}
            Check(new Combat(s,1,"new").FieldModule==0 && s.items.Count==3,"module does not persist as paid equipment");
            var fair=new Combat(s,4,"fair",71,false,SkillTrial.Mode);Check(!fair.Assemble(1) && fair.FieldModule==0,"fixed trial cannot assemble");
        }
        public static void HeroEngineeringMechanics()
        {
            Combat Ready(int hero,int module)
            {
                var s=GameState.New();s.selectedHero=hero;s.heroes[hero].unlocked=true;
                var c=new Combat(s,1,"module");c.Elapsed=12;c.Position=c.TaskPosition;c.ChooseTask(1);c.Assemble(module);
                c.Enemies.Add(new Enemy{id=999,position=c.Position+new V2(190,0),hp=100000,maxHp=100000});return c;
            }
            var luke=Ready(0,1);luke.Tick(.05f,new V2());var ray=Ready(1,1);ray.Tick(.05f,new V2());
            Check(luke.Bullets.FindAll(b=>b.mod).Count==2 && ray.Bullets.FindAll(b=>b.mod).Count==1,"Luke field turret has two firing origins");
            ray=Ready(1,2);for(int i=0;i<80&&!ray.Ended;i++){if(ray.Choosing)ray.Choose(ray.Choices[0]);ray.Tick(.05f,new V2(0,i<40?-1:1));}
            Check(ray.RailShots>0 && ray.Skills[1]>=1 && ray.HasMod(0),"Ray movement charges piercing rail shot and saw");
            var stationary=Ready(1,2);for(int i=0;i<30;i++)stationary.Tick(.05f,new V2());Check(stationary.RailShots==0,"standing still cannot charge movement shot");
            var repair=Ready(2,3);repair.Hp=repair.MaxHp*.5f;repair.Tick(.05f,new V2());Check(repair.Hp>repair.MaxHp*.5f && repair.Shield>0,"recovered repair module has actual effect");
        }
        public static void FieldRouteAndCollectionTarget()
        {
            var s=GameState.New();var positions=new HashSet<float>();
            for(int seed=0;seed<3;seed++)
            {
                var c=new Combat(s,7,"route",seed);positions.Add(c.TaskPosition.x);
                Check(!c.IsBlocked(c.TaskPosition,15),"variant target is reachable");
                Check(c.TaskPosition.x==new Combat(s,7,"same",seed).TaskPosition.x,"route stable for replay seed");
            }
            Check(positions.Count==3,"three field routes beyond first region");
            var e=New();Check(e.SetCollectionTarget(6).Success && e.State.collectionTarget==6,"target selection");
            Check(!e.SetCollectionTarget(Campaign.Regions.Length).Success && e.State.collectionTarget==6,"invalid target cannot overwrite");
            Check(e.SetCollectionTarget(-1).Success && e.State.collectionTarget==-1,"target removal");
            Check(new Combat(s,0,"intro").Task==FieldTask.Salvage,"first operation teaches recovery");
        }
        public static void RailOutfitOwnershipAndParity()
        {
            var s=GameState.New();s.gems=600;
            Check(!Social.Buy(s,Outfits.RailKey).Success && s.gems==600,"cannot buy outfit for locked Ray");
            s.heroes[1].unlocked=true;s.selectedHero=s.showcaseHero=1;
            int attack=Catalog.Attack(s),hp=Catalog.Health(s);
            Check(Social.Buy(s,Outfits.RailKey).Success && s.gems==0 && Outfits.Equip(s,2).Success,"exact outfit purchase and equip");
            Check(Outfits.Active(s,1)==2 && Outfits.Active(s,0)==0 && Social.Build(s).outfit==2,"hero-specific public outfit");
            Check(Catalog.Attack(s)==attack && Catalog.Health(s)==hp && !Social.Buy(s,Outfits.RailKey).Success,"cosmetic parity and duplicate guard");
            var dressed=new Combat(s,1,"parity",33);s.selectedOutfit=0;var plain=new Combat(s,1,"parity",33);
            for(int i=0;i<1000&&!dressed.Ended;i++)
            {
                if(dressed.Choosing){int choice=dressed.Choices[0];dressed.Choose(choice);plain.Choose(choice);}
                var move=new V2((float)Math.Sin(i*.02),(float)Math.Cos(i*.02));dressed.Tick(ReplayRecorder.Step,move);plain.Tick(ReplayRecorder.Step,move);
            }
            Check(dressed.Hp==plain.Hp && dressed.Kills==plain.Kills && dressed.Elapsed==plain.Elapsed,"new outfit does not change combat outcome");
        }
        public static void ModeCopyAndRestorationStages()
        {
            Check(FieldEngineering.TrialObjective(SkillTrial.Mode).Contains("재화 보상 없음"),"skill trial must not promise rewards");
            Check(FieldEngineering.TrialObjective(Endgame.PracticeMode).Contains("저장되지"),"practice explains no saved record");
            Check(GuildRestoration.Level(5)==0 && GuildRestoration.Level(6)==1 && GuildRestoration.Level(15)==2 && GuildRestoration.Level(30)==3,"restoration stages match public thresholds");
        }
    }
}
