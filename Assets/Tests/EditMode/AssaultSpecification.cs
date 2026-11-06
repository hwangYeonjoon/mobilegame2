using System;
using ScrapGuard.Core;
namespace ScrapGuard.Tests
{
    public static partial class CoreSpecification
    {
        public static void ExpandedWorldAndHordes()
        {
            var c=new Combat(GameState.New(),0,"world",77);
            c.Position=new V2(270,900);
            for(int i=0;i<100;i++){c.Hp=c.MaxHp;if(c.Choosing)c.Choose(c.Choices[0]);c.Tick(.05f,new V2(1,0));}
            Check(c.Position.x>540,"movement reaches beyond the previous fixed box");
            c.Position=new V2(c.WorldRight-26,1100);float before=c.DistanceMoved;
            for(int i=0;i<5;i++)c.Tick(.05f,new V2(1,0));
            Check(c.DistanceMoved==before,"world boundary never credits movement");
            c=new Combat(GameState.New(),0,"horde",17);
            for(int i=0;i<560;i++){c.Hp=c.MaxHp;if(c.Choosing)c.Choose(c.Choices[0]);c.Tick(.05f,new V2());}
            Check(c.WaveNumber>0&&c.Enemies.Count+c.Kills>40,"early game contains an actual group wave");
            Check(c.Enemies.Count<=c.EnemyLimit+2,"group respects simulation cap");
            var challenge=new Combat(GameState.New(),4,"rank",17,false,SkillTrial.Mode);
            Check(challenge.WorldLeft==0&&challenge.WorldRight==540,"ranked arena retains defined geometry");
        }
        public static void ArsenalAndMachineAssembly()
        {
            var c=new Combat(GameState.New(),0,"machine");
            c.Pickups.Add(new Pickup{id=9000,position=c.Position,xp=40});c.Tick(.05f,new V2());
            if(c.Choosing)c.Choose(c.Choices[0]);c.Tick(.05f,new V2());
            Check(c.MachinesBuilt==1&&c.Salvage==0&&c.MachineTime>11,"collected salvage is spent once on the machine");
            var target=new Enemy{id=9999,hp=10000,maxHp=10000,position=c.Position+new V2(75,0)};c.Enemies.Add(target);
            for(int i=0;i<20;i++){if(c.Choosing)c.Choose(c.Choices[0]);c.Tick(.05f,new V2());}
            Check(target.hp<10000,"assembled machine participates in combat");
            c=new Combat(GameState.New(),0,"arsenal");c.Skills[8]=c.Skills[9]=c.Skills[10]=1;
            c.Enemies.Add(new Enemy{id=999,hp=10000,maxHp=10000,position=c.Position+new V2(160,0)});c.Tick(.05f,new V2());
            Check(c.Effects.Exists(e=>e.kind==2)&&c.Bullets.FindAll(b=>b.mod).Count==4&&c.Mines.Count==1,"mortar, cross blades and placed mine have distinct mechanics");
            var mine=c.Mines[0];c.Enemies.Clear();target=new Enemy{id=998,hp=10000,maxHp=10000,position=mine.position};c.Enemies.Add(target);
            for(int i=0;i<15;i++)c.Tick(.05f,new V2());
            Check(c.Mines.Count==0&&target.hp<10000,"armed mine detonates on a pursuing enemy");
        }
        public static void EvolutionChoices()
        {
            for(int weapon=0;weapon<6;weapon++)foreach(int branch in new[]{1,2})
            {
                var s=GameState.New();s.items.Find(i=>i.id==s.equipped[0]).definition=weapon;
                var c=new Combat(s,0,"evolution");c.Skills[0]=5;c.Skills[branch==1?4:5]=3;c.Tick(.05f,new V2());
                Check(c.WeaponEvolution==branch,"every weapon evolves through the chosen investment path");
                c.Skills[branch==1?5:4]=5;c.Tick(.05f,new V2());Check(c.WeaponEvolution==branch,"later upgrades cannot overwrite chosen evolution");
            }
            Check(Workshop.MainLevels[1]!=Workshop.MainLevels[4]&&Workshop.SubLevels[2]!=Workshop.SubLevels[1],"recipes require different investments");
        }
        public static void AssaultReplayRoundTrip()
        {
            var s=GameState.New();s.heroes[0].level=30;foreach(var item in s.items){item.rarity=2;item.level=20;}
            var c=new Combat(s,0,"assault",91);var recorder=new ReplayRecorder();
            for(int i=0;i<6000&&!c.Ended;i++)
            {
                if(c.Choosing){int skill=c.Choices[0];foreach(int candidate in c.Choices)if(candidate>=8){skill=candidate;break;}c.Choose(skill);recorder.Choose(skill);}
                if(c.CanUltimate){c.UseUltimate();recorder.Ultimate();}
                var target=new V2(270+(float)Math.Cos(c.Elapsed*.06f)*500,490+(float)Math.Sin(c.Elapsed*.06f)*650);
                c.Tick(ReplayRecorder.Step,recorder.Move((target-c.Position).Normal));
            }
            if(!c.Ended)c.Abandon();var copy=ReplayRecorder.Verify(s,0,"assault",91,recorder.Steps,220);
            Check(c.Kills==copy.Kills&&c.Hp==copy.Hp&&c.Position.x==copy.Position.x&&c.MachinesBuilt==copy.MachinesBuilt&&c.WaveNumber==copy.WaveNumber,"expanded world, spatial hits, waves and machines replay identically");
            Check(c.WaveNumber>0&&c.Skills.Length==12,"test includes expanded combat");
        }
        public static void ReviveReplayAuthorization()
        {
            var state=GameState.New();var c=new Combat(state,0,"revive",17);var recorder=new ReplayRecorder();
            for(int i=0;i<12000&&!c.Ended;i++)
            {
                if(c.Choosing){c.Choose(c.Choices[0]);recorder.Choose(c.Choices[0]);}
                c.Tick(ReplayRecorder.Step,recorder.Move(new V2()));
            }
            Check(c.CanRevive,"ordinary combat defeat can offer one revive");
            Check(c.Revive()&&c.Hp==c.MaxHp*.5f,"revive restores half health");recorder.Revive();
            Check(!c.Revive(),"cannot revive a living player twice");
            var protectedDamage=c.Report().damage;float revivedHp=c.Hp;
            for(int i=0;i<60;i++){if(c.Choosing){int skill=c.Choices[0];c.Choose(skill);recorder.Choose(skill);}c.Tick(ReplayRecorder.Step,recorder.Move(new V2()));}
            Check(c.Hp>=revivedHp,"healing pickups may increase health during revive protection");
            var afterProtection=c.Report().damage;
            for(int i=0;i<protectedDamage.Length;i++)Check(afterProtection[i]==protectedDamage[i],"revive protection prevents damage while allowing healing");
            c.Abandon();
            bool rejected=false;try{ReplayRecorder.Verify(state,0,"revive",17,recorder.Steps,500);}catch(ArgumentException){rejected=true;}
            Check(rejected,"unverified revive event rejected");
            var copy=ReplayRecorder.Verify(state,0,"revive",17,recorder.Steps,500,reviveAllowed:true);
            Check(copy.Revived&&copy.Hp==c.Hp&&copy.Kills==c.Kills,"server-authorized revive replays deterministically");
            var challenge=new Combat(state,4,"challenge",17,false,SkillTrial.Mode);challenge.Hp=0;challenge.Ended=true;
            Check(!challenge.Revive(),"no paid or ad revival in equal-condition competition");
            var guard=new Combat(state,0,"guard");guard.Elapsed=30;guard.Hp=1;
            guard.Hazards.Add(new Hazard{position=guard.Position,radius=100,delay=0,damage=10});guard.Tick(.05f,new V2());
            Check(guard.Revive(),"controlled fatal damage enables revive");float protectedHp=guard.Hp;
            for(int i=0;i<58;i++)
            {
                if(guard.Choosing)guard.Choose(guard.Choices[0]);
                guard.Enemies.Clear();guard.Pickups.Clear();guard.Hazards.Clear();
                guard.Hazards.Add(new Hazard{position=guard.Position,radius=100,delay=0,damage=10});guard.Tick(.05f,new V2());
                Check(guard.Hp==protectedHp,"continuous hazards cannot damage during first 2.9 seconds");
            }
            for(int i=0;i<4;i++){if(guard.Choosing)guard.Choose(guard.Choices[0]);guard.Hazards.Add(new Hazard{position=guard.Position,radius=100,delay=0,damage=10});guard.Tick(.05f,new V2());}
            Check(guard.Hp<protectedHp,"damage resumes after three seconds; protection is not permanent");
        }
        public static void SpatialCollisionAcrossCells()
        {
            var c=new Combat(GameState.New(),0,"cells");c.Position=new V2(-200,1000);
            var target=new Enemy{id=999,position=new V2(-79,1000),hp=1000,maxHp=1000};c.Enemies.Add(target);
            c.Bullets.Add(new Bullet{id=9999,position=new V2(-82,1000),velocity=new V2(),damage=80,life=1});
            c.Tick(.05f,new V2());Check(target.hp==920,"negative grid boundary hit preserved");
            Check(!c.Bullets.Exists(b=>b.id==9999),"non-piercing bullet removed after its hit");
        }
    }
}
