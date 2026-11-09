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
            if(c.Choosing)c.Choose(c.Choices[0]);
            Check(c.MachinesBuilt==0&&c.CanActivateMachine,"salvage waits for player input");
            Check(c.ActivateMachine()&&!c.ActivateMachine(),"activation consumes once");
            Check(c.MachinesBuilt==1&&c.Salvage==0&&c.MachineTime>9,"collected salvage is spent once on the machine");
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
                if(c.CanActivateMachine){c.ActivateMachine();recorder.Machine();}
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
        public static void CrowdSpacingAndSharedQueries()
        {
            var c=new Combat(GameState.New(),4,"spacing",71,false,SkillTrial.Mode);
            c.Position=new V2(270,690);var a=new Enemy{id=10000,hp=100000,maxHp=100000,position=new V2(260,320)};
            var b=new Enemy{id=10001,hp=100000,maxHp=100000,position=new V2(262,320)};c.Enemies.Add(a);c.Enemies.Add(b);
            float before=V2.Distance(a.position,b.position);
            for(int i=0;i<12;i++)c.Tick(ReplayRecorder.Step,new V2());
            Check(V2.Distance(a.position,b.position)>before+10,"crowd neighbors separate without randomness");
            c.Enemies.Clear();c.Bullets.Clear();c.Enemies.Add(new Enemy{id=20000,hp=10000,maxHp=10000,position=new V2(260,320)});
            for(int i=0;i<100;i++)c.Bullets.Add(new Bullet{id=30000+i,position=new V2(190,320),damage=1,life=1});
            c.Tick(ReplayRecorder.Step,new V2());Check(c.CandidateBuilds<5,"100 bullets in the same cells share ordered candidate lists");
        }
        public static void AttackEvolutionMechanics()
        {
            int[] attacks={1,2,3,8,9,10},supports={7,6,5,4,11,7};
            for(int n=0;n<attacks.Length;n++)
            {
                var c=new Combat(GameState.New(),0,"evo",71);int skill=attacks[n];c.Skills[skill]=5;c.Skills[supports[n]]=2;
                Check(!c.SkillEvolved(skill),"evolution requires its support investment");c.Skills[supports[n]]=3;
                Check(c.SkillEvolved(skill),"every attack has its own evolution");
                c.Enemies.Add(new Enemy{id=10000,hp=100000,maxHp=100000,position=c.Position+new V2(130,0)});c.Tick(.05f,new V2());
                if(skill==1)Check(Math.Abs(V2.Distance(c.SawPosition(0),c.Position)-105)<.1f,"evolved saw changes orbit");
                if(skill==2)Check(c.Enemies.Exists(e=>e.freezeTime>0),"evolved coil freezes nearby mobs");
                if(skill==3)Check(c.Bullets.FindAll(x=>x.drone).Count==3,"drone evolution deploys three guns");
                if(skill==8)Check(c.Effects.FindAll(e=>e.kind==11).Count==3,"mortar evolution paints three impact areas");
                if(skill==9)Check(c.Bullets.FindAll(x=>x.returning&&x.visual==4).Count==4,"blade evolution creates identifiable returning projectiles");
            }
            var mines=new Combat(GameState.New(),4,"chain",71,false,SkillTrial.Mode);mines.Skills[10]=5;mines.Skills[7]=3;
            var first=new Combat.ScrapMine{position=new V2(100,320),arm=0};var second=new Combat.ScrapMine{position=new V2(220,320),arm=0};
            mines.Mines.Add(first);mines.Mines.Add(second);mines.Enemies.Add(new Enemy{id=9000,position=first.position,hp=10000,maxHp=10000});
            mines.Tick(.05f,new V2());mines.Tick(.05f,new V2());Check(!mines.Mines.Contains(first)&&!mines.Mines.Contains(second),"linked mine explodes without direct enemy proximity");
        }
        public static void MachineInputGuards()
        {
            var state=GameState.New();var c=new Combat(state,0,"manual");Check(!c.ActivateMachine(),"cannot activate before collecting scrap");
            bool rejected=false;try{ReplayRecorder.Verify(state,0,"manual",1,new System.Collections.Generic.List<ReplayStep>{new ReplayStep{machine=true}},1);}catch(ArgumentException){rejected=true;}
            Check(rejected,"forged early activation rejected by server simulation");
            rejected=false;try{ReplayRecorder.Verify(state,0,"manual",1,new System.Collections.Generic.List<ReplayStep>{new ReplayStep{machine=true,frames=1}},1);}catch(ArgumentException){rejected=true;}
            Check(rejected,"mixed activation and movement rejected");
            var ranked=new Combat(state,4,"rank",1,false,SkillTrial.Mode);Check(!ranked.ActivateMachine(),"manual assembly is excluded from fixed challenge");
        }
        public static void MachineRecoveryAndTelemetry()
        {
            var state=GameState.New();state.tutorialCompleted=true;var c=new Combat(state,0,"recovery");
            c.Pickups.Add(new Pickup{id=9000,position=c.Position,xp=500});c.Tick(.05f,new V2());
            while(c.Choosing)c.Choose(c.Choices[0]);Check(c.ActivateMachine(),"first assembly available");
            for(int i=0;i<240;i++){if(c.Choosing)c.Choose(c.Choices[0]);c.Enemies.Clear();c.Tick(.05f,new V2());}
            Check(c.MachineTime==0&&c.MachineRecovery>35&&!c.ActivateMachine(),"scrap cannot bypass forty-second recovery");
            Check(Math.Abs(c.MachineActiveSeconds-10)<.01f,"measure actual active duration, not activation count");
            for(int i=0;i<800;i++){if(c.Choosing)c.Choose(c.Choices[0]);c.Enemies.Clear();c.Tick(.05f,new V2());}
            Check(c.CanActivateMachine&&c.ActivateMachine(),"saved scrap becomes available after recovery");
            c.Hazards.Add(new Hazard{id=9100,position=c.Position,radius=60,delay=0,damage=100,cause=DamageCause.Boss});
            c.Tick(.05f,new V2());float low=c.Hp;
            c.Pickups.Add(new Pickup{id=9101,position=c.Position,heal=true});c.Tick(.05f,new V2());
            Check(c.HazardHits==1&&c.DamageReceived>0&&c.HealingReceived>0&&c.MinimumHp<=low,"damage, healing and minimum health remain distinct");
        }
        public static void BossArenaSafety()
        {
            foreach(int stage in new[]{0,5,17,29,41})
            {
                var c=new Combat(GameState.New(),stage,"fence",17);c.Elapsed=c.Duration-32;c.Tick(.05f,new V2());
                Check(c.BossArenaWarning>0&&!c.BossArenaActive,"three-second warning before fence");
                if(c.Choosing)c.Choose(c.Choices[0]);var before=c.Position;c.Elapsed=c.Duration-30;c.Tick(.05f,new V2());
                Check(c.BossArenaActive&&V2.Distance(before,c.Position)<1,"fence forms around player without teleport");
                Check(!c.IsBlocked(c.Blocks[0].Center),"boss arena clears regional obstacles");
                var boss=c.Enemies.Find(e=>e.finalBoss);Check(boss!=null&&V2.Distance(c.Position,boss.position)<240,"boss appears within view");
                c.Enemies.Clear();for(int i=0;i<500;i++){if(c.Choosing)c.Choose(c.Choices[0]);c.Tick(.05f,new V2(1,0));}
                Check(c.Position.x<=c.BossArenaCenter.x+210,"player cannot leave fence");
            }
            var ranked=new Combat(GameState.New(),4,"fair",17,false,SkillTrial.Mode);ranked.Tick(.05f,new V2());Check(!ranked.BossArenaActive,"fixed challenge remains unchanged");
        }
        public static void EarlyEvolutionAndGrowthTail()
        {
            var c=new Combat(GameState.New(),0,"growth");c.Skills[1]=4;c.Skills[7]=2;c.Tick(.05f,new V2());
            Check(!c.SkillEvolved(1),"early partial investment does not finish evolution");c.Skills[1]=5;c.Skills[7]=3;c.Tick(.05f,new V2());
            Check(c.SkillEvolved(1)&&c.FirstEvolutionSeconds>=0,"campaign evolution needs full attack and support investment");
            c.Level=20;Check(c.NeededXp>6+20*4,"later growth leaves room for meaningful choices");
            var ranked=new Combat(GameState.New(),4,"fair",17,false,SkillTrial.Mode);ranked.Skills[1]=4;ranked.Skills[7]=2;
            Check(!ranked.SkillEvolved(1),"fixed challenge keeps the five plus three recipe");
        }
        public static void DamageSourceAccountingAndStackGuard()
        {
            var c=new Combat(GameState.New(),0,"sources");c.Elapsed=30;
            foreach(DamageSource source in new[]{DamageSource.Contact,DamageSource.EnemyAttack,DamageSource.Terrain,DamageSource.Condition,DamageSource.Boss})
            {
                for(int i=0;i<15;i++){if(c.Choosing)c.Choose(c.Choices[0]);c.Enemies.Clear();c.Pickups.Clear();c.Tick(.05f,new V2());}
                c.Hazards.Add(new Hazard{position=c.Position,radius=60,delay=0,damage=100,cause=DamageCause.Artillery,source=source});c.Tick(.05f,new V2());
                Check(c.DamageBySource[(int)source]>0,"explicit source overrides visual/cause category");
            }
            float sum=0;foreach(float damage in c.DamageBySource)sum+=damage;
            Check(Math.Abs(sum-c.DamageReceived)<.01f,"all actual damage accounted once");
            var copy=c.DamageBySource;copy[1]=99999;Check(c.DamageBySource[1]!=99999,"telemetry snapshots cannot mutate combat");
            c=new Combat(GameState.New(),0,"stack");c.Elapsed=30;
            for(int i=0;i<6;i++)c.Hazards.Add(new Hazard{position=c.Position,radius=60,delay=0,damage=100,source=DamageSource.EnemyAttack});
            c.Tick(.05f,new V2());Check(Math.Abs(c.DamageReceived-80)<.01f,"six overlapping attacks cause one hit within recovery window");
            var early=new Combat(GameState.New(),0,"grace");early.Hazards.Add(new Hazard{position=early.Position,radius=60,damage=100,source=DamageSource.EnemyAttack});early.Tick(.05f,new V2());
            Check(early.DamageReceived==20,"opening gives readable nonzero practice damage");
            var poison=new Combat(GameState.New(),12,"poison");poison.Elapsed=30;poison.Position=poison.Zones[0].Center;poison.Tick(.05f,new V2());
            Check(poison.DamageBySource[(int)DamageSource.Terrain]>0&&poison.HazardDamageReceived==0,"direct poison is terrain even without a delayed warning");
        }
        public static void OpeningThreatBudget()
        {
            foreach(int stage in new[]{17,29,41})
            {
                var c=new Combat(GameState.New(),stage,"opening",17);
                for(int i=0;i<210;i++){if(c.Choosing)c.Choose(c.Choices[0]);c.Tick(.05f,new V2());}
                Check(!c.Ended&&c.Enemies.TrueForAll(e=>e.role==EnemyRole.Pursuer),"first ten seconds introduce approaching enemies before ranged pressure");
            }
            var pressure=new Combat(GameState.New(),17,"warnings",71);pressure.Elapsed=30;pressure.Hp=pressure.MaxHp=100000;
            for(int i=0;i<12;i++)pressure.Enemies.Add(new Enemy{id=8000+i,hp=100000,maxHp=100000,role=EnemyRole.Artillery,position=pressure.Position+V2.Angle(i)*180});
            for(int i=0;i<200;i++){if(pressure.Choosing)pressure.Choose(pressure.Choices[0]);pressure.Tick(.05f,new V2());}
            Check(pressure.PeakEnemyWarnings>0&&pressure.PeakEnemyWarnings<=3,"many artillery units share the warning budget");
        }
        public static void MachineRescueAndBurst()
        {
            var c=new Combat(GameState.New(),0,"rescue");c.Pickups.Add(new Pickup{id=9000,position=c.Position,xp=40});c.Tick(.05f,new V2());
            while(c.Choosing)c.Choose(c.Choices[0]);
            foreach(var source in new[]{DamageSource.EnemyAttack,DamageSource.Terrain,DamageSource.Boss})c.Hazards.Add(new Hazard{position=c.Position,radius=60,delay=5,damage=100,source=source});
            Check(c.ActivateMachine()&&c.MachineIntercepts==1,"activation intercepts nearby enemy artillery");
            Check(c.Hazards.Count==2&&c.Hazards.TrueForAll(h=>h.source!=DamageSource.EnemyAttack),"terrain and boss attacks cannot be erased by assembly");
            c.Enemies.Add(new Enemy{id=9999,kind=2,hp=10000,maxHp=10000,position=c.MachinePosition});c.Tick(.05f,new V2());
            Check(c.MachineDamage>=c.Damage*4&&c.DamageDuringMachine>=c.MachineDamage,"assembly delivers measured burst against a durable target");
        }
        static void RobotFixtureParts(Combat c,int quantity)
        {
            for(int i=0;i<3;i++)c.Pickups.Add(new Pickup{id=9100+i,position=c.Position,part=i,partCount=quantity});
            c.Pickups.Add(new Pickup{id=9200,position=c.Position,xp=40});c.Tick(.05f,new V2());while(c.Choosing)c.Choose(c.Choices[0]);
        }
        public static void RobotPartsAndDistinctFrames()
        {
            for(int id=1;id<=9;id++)
            {
                var c=new Combat(GameState.New(),1,"robot",17);int before=c.Salvage;
                Check(!c.PlanRobot(id,true)&&!c.ActivateMachine(id)&&c.Salvage==before,"missing parts cannot manufacture or spend salvage");
                RobotFixtureParts(c,12);var recipe=c.RobotRecipe(id);int[] stock=c.RobotParts;
                Check(c.PlanRobot(id,true)&&c.RobotRank(id)==1,"recovered parts complete a persistent specialized design");
                for(int i=0;i<3;i++)Check(c.RobotParts[i]==stock[i]-recipe[i],"both slots consume required stock exactly once");
                Check(c.PlanRobot(id,true)&&c.RobotRank(id)==2&&!c.PlanRobot(id,true),"completed design has a bounded upgrade path");
                var remaining=c.RobotParts;Check(c.ActivateMachine(id),"crafted design can be boarded");
                for(int i=0;i<3;i++)Check(c.RobotParts[i]==remaining[i],"boarding never repeatedly consumes blueprint parts");
                Check(!c.PlanRobot(id,false)&&!c.PlanRobot(id,true),"cannot alter a running machine");
                Check(c.MachineTime==18&&c.RobotReport()[0].rank==2,"completion extends run time and persists in report");
                if(RobotAssembly.Frame(id)==2)Check(c.RobotDamageReduction<=.15f&&c.Shield<=90,"longer fortress uptime has bounded defense");
            }
            Check(RobotAssembly.PartFor(new Enemy{role=EnemyRole.Charger,salvagePart=0})==0,"marked carriers make missing regional parts recoverable without replacing enemy combat roles");
            var basic=new Combat(GameState.New(),0,"basic");basic.Pickups.Add(new Pickup{id=7000,position=basic.Position,xp=40});basic.Tick(.05f,new V2());while(basic.Choosing)basic.Choose(basic.Choices[0]);
            Check(basic.PlanRobot(5,false)&&basic.ActivateMachine(),"basic walker remains available without recovered parts");
            Check(!basic.RobotAvailable(1),"artillery cannot be used merely by requesting its number");
            var c2=new Combat(GameState.New(),0,"late");RobotFixtureParts(c2,12);c2.PlanRobot(1,true);c2.PlanRobot(1,true);c2.Elapsed=c2.Duration*.65f;
            Check(c2.RobotDuration(1)==22,"late battle extends completed robot without infinite uptime");
            var target=new Enemy{id=9999,position=c2.Position+new V2(420,0),hp=100000,maxHp=100000};c2.Enemies.Clear();c2.Enemies.Add(target);c2.ActivateMachine(1);c2.Tick(.05f,new V2());Check(c2.MachineDamage>0,"cannon retains long reach");
            var trial=new Combat(GameState.New(),4,"fair",17,false,SkillTrial.Mode);Check(!trial.PlanRobot(1,true)&&!trial.ActivateMachine(1),"fixed competition remains isolated until its own rules are validated");
            var fresh=new Combat(GameState.New(),0,"new-run");Check(fresh.RobotRank(1)==0&&!fresh.RobotAvailable(1),"crafted power never carries to next run");
        }
        public static void IntroRobotRecoveryScope()
        {
            var state=GameState.New();var intro=new Combat(state,0,"intro-parts",17);
            state.tutorialCompleted=true;var repeat=new Combat(state,0,"repeat-parts",17);
            var found=new bool[3];
            for(int i=0;i<440;i++)
            {
                foreach(var c in new[]{intro,repeat}){if(c.Choosing)c.Choose(c.Choices[0]);c.Tick(.05f,new V2());}
                foreach(var e in intro.Enemies)if(e.salvagePart>=0)found[e.salvagePart]=true;
            }
            Check(Array.TrueForAll(found,x=>x),"first-clear lesson introduces all three real carrier types before 22 seconds");
            Check(repeat.Enemies.TrueForAll(e=>e.salvagePart<0),"repeat stage does not receive early tutorial carriers");
            var basic=new Combat(GameState.New(),0,"first-board");RobotFixtureParts(basic,0);basic.ActivateMachine();
            Check(basic.MachineRecovery==30,"first tutorial basic boarding returns in time for specialization");
            var regular=new Combat(state,0,"normal-board");RobotFixtureParts(regular,0);regular.ActivateMachine();
            Check(regular.MachineRecovery==50,"normal campaign recovery remains unchanged");
            var crafting=new Combat(GameState.New(),0,"no-gifts");
            Check(!crafting.CanPlanRobot(1,true)&&crafting.RobotPartsSpent==0,"a tutorial clock never substitutes for recovered parts");
        }
        public static void RobotReplayAndCollection()
        {
            var state=GameState.New();state.heroes[0].level=30;foreach(var item in state.items){item.level=20;item.rarity=2;}
            var c=new Combat(state,0,"robot-replay",17);var r=new ReplayRecorder();
            for(int frame=0;frame<6000&&!c.Ended;frame++)
            {
                if(c.Choosing){int skill=c.Choices[0];c.Choose(skill);r.Choose(skill);}
                int desired=c.MachinesBuilt%3==0?1:c.MachinesBuilt%3==1?5:9;
                if(c.CanPlanRobot(desired,true)){c.PlanRobot(desired,true);r.RobotPlan(desired,true);}
                if(c.CanActivateMachine){Check(c.ActivateMachine(),"configured machine activation");r.Machine();}
                if(c.CanUltimate){c.UseUltimate();r.Ultimate();if(c.Ended)break;}
                c.Tick(ReplayRecorder.Step,r.Move(V2.Angle(c.Elapsed*.15f)));
            }
            if(!c.Ended)c.Abandon();var copy=ReplayRecorder.Verify(state,0,"robot-replay",17,r.Steps,220);
            Check(c.MachinesBuilt>=2&&c.MachinesBuilt==copy.MachinesBuilt&&c.Hp==copy.Hp&&c.Kills==copy.Kills,"different robot designs reproduce on server");
            var report=c.RobotReport();var replay=copy.RobotReport();Check(report.Count==replay.Count,"replay preserves constructed designs");
            for(int i=0;i<report.Count;i++)Check(report[i].rank==replay[i].rank&&report[i].damage==replay[i].damage&&report[i].kills==replay[i].kills,"per-design damage and kills reconcile");
            bool denied=false;try{ReplayRecorder.Verify(state,0,"bad",17,new System.Collections.Generic.List<ReplayStep>{new ReplayStep{frames=1,robot=2}},1);}catch(ArgumentException){denied=true;}Check(denied,"design attached to movement cannot bypass input validation");
            foreach(var forged in new[]{new ReplayStep{robotPlan=1,robotUpgrade=true},new ReplayStep{robotPlan=5,frames=1},new ReplayStep{robotUpgrade=true,frames=1},new ReplayStep{robotPlan=5,machine=true}})
            {bool rejected=false;try{ReplayRecorder.Verify(GameState.New(),0,"forged",17,new System.Collections.Generic.List<ReplayStep>{forged},1);}catch(ArgumentException){rejected=true;}Check(rejected,"forged or mixed blueprint input rejected");}
            var fresh=GameState.New();Check(!RobotAssembly.Showcase(fresh,1).Success,"unbuilt robots cannot be publicly equipped");
            RobotAssembly.Record(fresh,report,0);Check(RobotAssembly.Showcase(fresh,report[0].design).Success,"real constructions unlock exhibition");
            Check(Social.Build(fresh).robotDesign==report[0].design,"public profile exposes chosen assembly");
            fresh.robotRecords=null;RobotAssembly.Normalize(fresh);Check(fresh.robotRecords.Count==0&&fresh.showcaseRobot==0,"old saves and absent records normalize without fabricated builds");
        }
        public static void IntroBossScopeAndLearning()
        {
            var state=GameState.New();var intro=new Combat(state,0,"intro",17);
            Check(intro.IntroBattle,"first clear has a dedicated introduction");
            state.tutorialCompleted=true;var repeat=new Combat(state,0,"repeat",17);
            Check(!repeat.IntroBattle&&!new Combat(GameState.New(),6,"next").IntroBattle&&!new Combat(GameState.New(),0,"trial",17,false,SkillTrial.Mode).IntroBattle,"first-clear assistance cannot leak to replay, later stages or ranked trials");
            foreach(var c in new[]{intro,repeat}){c.Elapsed=c.Duration-30;c.Tick(.05f,new V2());}
            var firstBoss=intro.Enemies.Find(e=>e.finalBoss);var normalBoss=repeat.Enemies.Find(e=>e.finalBoss);
            Check(firstBoss!=null&&normalBoss!=null&&firstBoss.maxHp<normalBoss.maxHp,"intro boss has lower fixed health rather than scaling to player gear");
            for(int i=0;i<40;i++){if(intro.Choosing)intro.Choose(intro.Choices[0]);intro.Tick(.05f,new V2(1,0));}
            Check(intro.Hazards.FindAll(h=>h.source==DamageSource.Boss).Count<=1,"intro teaches one warning shape at a time");
            var a=new Combat(GameState.New(),0,"damage");a.Elapsed=30;
            var b=new Combat(state,0,"damage");b.Elapsed=30;
            foreach(var c in new[]{a,b}){c.Hazards.Add(new Hazard{position=c.Position,radius=60,damage=100,source=DamageSource.Boss});c.Tick(.05f,new V2());}
            Check(Math.Abs(a.DamageReceived-45)<.01f&&Math.Abs(b.DamageReceived-45)<.01f,"intro and early campaign boss damage are explicit and nonzero");
            Check(new Combat(state,6,"entry").RegionEntryThreat<new Combat(state,9,"later").RegionEntryThreat&&new Combat(state,17,"final").RegionEntryThreat==1,"new regional mechanics ramp within each region");
        }
        public static void EarlyBossLessonsAndDamageAccounting()
        {
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var pattern=typeof(Combat).GetMethod("BossPattern",flags);
            var hurt=typeof(Combat).GetMethod("Hurt",flags);
            foreach(int stage in new[]{1,3,5})
            {
                var state=GameState.New();state.tutorialCompleted=true;
                var c=new Combat(state,stage,"lesson",17);
                var boss=new Enemy{id=9000,kind=3,finalBoss=true,hp=1,maxHp=100000,position=c.Position+new V2(0,-190)};
                c.Enemies.Add(boss);
                for(int cycle=0;cycle<6;cycle++)
                {
                    c.Hazards.Clear();typeof(Combat).GetField("bossActionRecovery",flags).SetValue(c,0f);pattern.Invoke(c,null);
                    int expected=stage==1?1:stage==3?(cycle%2==0?1:2):cycle%3==0?1:cycle%3==1?2:6;
                    Check(c.Hazards.Count==expected,"low-health boss does not add unlearned layers at operation "+(stage+1));
                    foreach(var h in c.Hazards)Check(h.delay>=1.9f&&h.source==DamageSource.Boss,"learning attacks retain readable boss warnings");
                    if(expected==2)Check(c.Hazards[1].delay-c.Hazards[0].delay>=.99f,"cross sweep leaves a full second between lines");
                }
                c.Hazards.Clear();int count=c.BossPatterns;boss.windup=1;
                pattern.Invoke(c,null);Check(c.Hazards.Count==0&&c.BossPatterns==count,"charge windup never stacks a new learning pattern");
                boss.windup=0;boss.dash=.5f;pattern.Invoke(c,null);
                Check(c.Hazards.Count==0,"charge movement never stacks a new learning pattern");
                c.Enemies.Clear();
                hurt.Invoke(c,new object[]{100f,DamageCause.Contact,DamageSource.Boss});
                Check(c.BossContactDamage>0&&c.BossWarningDamage==0,"boss contact is not mislabeled as telegraph damage");
                // Recovery is simulated, not bypassed, before the second source is observed.
                for(int i=0;i<13;i++)c.Tick(.05f,new V2());
                float contact=c.BossContactDamage;hurt.Invoke(c,new object[]{100f,DamageCause.Contact,DamageSource.Boss});
                Check(c.BossContactDamage==contact,"operation 6 does not double contact frequency at the shared 0.65-second contact interval");
                for(int i=0;i<7;i++)c.Tick(.05f,new V2());
                hurt.Invoke(c,new object[]{100f,DamageCause.Boss,DamageSource.Boss});
                Check(c.BossWarningDamage>0&&Math.Abs(c.BossContactDamage+c.BossWarningDamage-c.DamageBySource[(int)DamageSource.Boss])<.01f,"boss subtotals reconcile after mitigation and immunity");
            }
            var normal=GameState.New();normal.tutorialCompleted=true;
            foreach(int stage in new[]{6,17,41})Check(new Combat(normal,stage,"normal").BossLearningTier==3,"learned patterns continue across regional boundaries");
            Check(new Combat(GameState.New(),1,"trial",17,false,SkillTrial.Mode).BossLearningTier==0,"ranked mode has no campaign learning assistance");
        }
        public static void ContinuousBossCurve()
        {
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            float previousHp=0,previousDamage=0,previousInterval=5;
            for(int stage=1;stage<=18;stage++)
            {
                var c=new Combat(GameState.New(),stage,"curve",17);
                float hp=(float)typeof(Combat).GetProperty("BossBaseHealth",flags).GetValue(c);
                float damage=(float)typeof(Combat).GetProperty("BossDamageScale",flags).GetValue(c);
                float interval=(float)typeof(Combat).GetProperty("BossPatternInterval",flags).GetValue(c);
                if(stage>1)Check(hp>=previousHp&&hp<=previousHp*1.13f&&damage>=previousDamage&&damage<=previousDamage*1.08f&&interval<=previousInterval&&previousInterval-interval<=.21f,"no numeric boss cliff at operation "+(stage+1));
                previousHp=hp;previousDamage=damage;previousInterval=interval;
                Check(c.BossWarningLimit<=10,"campaign warnings have a finite shared budget");
            }
            var a=new Combat(GameState.New(),17,"end");var b=new Combat(GameState.New(),18,"next");
            Check((float)typeof(Combat).GetProperty("BossBaseHealth",flags).GetValue(a)==(float)typeof(Combat).GetProperty("BossBaseHealth",flags).GetValue(b),"curve endpoint does not restore a second health cliff");
        }
        public static void BossAttackCoordination()
        {
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var pattern=typeof(Combat).GetMethod("BossPattern",flags);
            foreach(int stage in new[]{6,12,18,24,30,36})
            {
                var state=GameState.New();state.tutorialCompleted=true;
                var c=new Combat(state,stage,"coordination",17);c.Elapsed=c.Duration-30;c.Tick(.05f,new V2());
                var boss=c.Enemies.Find(e=>e.finalBoss);Check(boss!=null,"regional boss spawned");boss.hp=boss.maxHp=1000000;
                if(boss.role!=EnemyRole.Shield)
                {
                    Check(!(bool)pattern.Invoke(c,null),"new-region role is demonstrated before common patterns");
                    boss.cooldown=0;boss.position=c.Position+new V2(100,0);c.Tick(.05f,new V2());
                    Check(c.BossRoleAttacks==1&&c.BossPatterns==0,"first attack is the boss role");
                    Check(!(bool)pattern.Invoke(c,null),"role attack reserves the common attack channel");
                }
                for(int frame=0;frame<1100;frame++)
                {
                    c.Hp=c.MaxHp;if(c.Choosing)c.Choose(c.Choices[0]);c.Tick(.05f,new V2(1,0));
                    Check(c.Hazards.FindAll(h=>h.source==DamageSource.Boss).Count<=c.BossWarningLimit,"all boss sources share a warning cap");
                }
                Check(c.BossPatterns>0,"common patterns are not starved by role attacks");
                if(boss.role!=EnemyRole.Shield)Check(c.BossRoleAttacks>=2,"boss identity survives scheduling");
            }
        }
        public static void EarlyBossReplayRoundTrip()
        {
            foreach(int stage in new[]{1,3,5})
            {
                var state=GameState.New();state.tutorialCompleted=true;
                state.heroes[0].level=10;foreach(var item in state.items)item.level=10;
                var c=new Combat(state,stage,"early-replay",17);var record=new ReplayRecorder();
                for(int frame=0;frame<12000&&!c.Ended;frame++)
                {
                    if(c.Choosing){int skill=c.Choices[0];c.Choose(skill);record.Choose(skill);}
                    if(c.CanUltimate){c.UseUltimate();record.Ultimate();if(c.Ended)break;}
                    if(c.CanActivateMachine){c.ActivateMachine();record.Machine();}
                    V2 target=new V2(270+(float)Math.Cos(c.Elapsed*.055f)*240,510+(float)Math.Sin(c.Elapsed*.055f)*280);
                    c.Tick(ReplayRecorder.Step,record.Move((target-c.Position).Normal));
                }
                Check(c.BossArenaActive&&c.BossPatterns>0,"replay exercises actual early boss patterns in operation "+(stage+1));
                if(!c.Ended)c.Abandon();
                var copy=ReplayRecorder.Verify(state,stage,"early-replay",17,record.Steps,600);
                Check(copy.Won==c.Won&&copy.Hp==c.Hp&&copy.Kills==c.Kills&&copy.BossPatterns==c.BossPatterns&&copy.BossContactDamage==c.BossContactDamage&&copy.BossWarningDamage==c.BossWarningDamage,"early lesson replay and damage accounting match");
            }
        }
        public static void HealingSourcesAndLowHealthTime()
        {
            var c=new Combat(GameState.New(),1,"healing");c.Elapsed=30;c.Hp=c.MaxHp*.25f;
            c.Tick(.05f,new V2());Check(c.LowHealthSeconds>0,"low health is measured against current maximum health");
            c.Pickups.Add(new Pickup{id=9000,position=c.Position,heal=true});c.Tick(.05f,new V2());
            Check(c.HealingBySource[(int)HealingSource.Pickup]>0,"actual pickup recovery has a source");
            c.Choosing=true;c.Choices=new[]{6};c.Choose(6);
            Check(c.HealingBySource[(int)HealingSource.RepairSkill]>0,"maximum-health skill recovery is distinct from drops");
            float sum=0;foreach(float h in c.HealingBySource)sum+=h;Check(Math.Abs(sum-c.HealingReceived)<.01f,"healing sources reconcile to actual healing");
            c.Hp=c.MaxHp;float before=c.HealingReceived;c.Pickups.Add(new Pickup{id=9001,position=c.Position,heal=true});c.Tick(.05f,new V2());
            Check(c.HealingReceived==before,"overhealing does not inflate recovery telemetry");
        }
        public static void EquipmentGrowthAndFixedChallenge()
        {
            var s=GameState.New();int attack=Catalog.Attack(s),hp=Catalog.Health(s);
            foreach(var item in s.items)item.level=10;
            Check(Catalog.Attack(s)>attack*1.2f&&Catalog.Health(s)>hp*1.1f,"equipment levels create meaningful visible attack and health growth");
            var c=new Combat(s,0,"stats");Check(c.Damage==Catalog.Attack(s)&&c.MaxHp==Catalog.Health(s),"inventory previews match actual campaign stats");
            var fixedState=SkillTrial.Loadout();var ranked=new Combat(s,4,"fixed",17,false,SkillTrial.Mode);
            Check(ranked.Damage==Catalog.Attack(fixedState,true)&&ranked.MaxHp==Catalog.Health(fixedState,true),"fixed challenge retains the previous five-percent equipment curve");
        }
    }
}
