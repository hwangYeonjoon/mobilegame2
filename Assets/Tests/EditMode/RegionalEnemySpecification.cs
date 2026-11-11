using System;
using ScrapGuard.Core;
namespace ScrapGuard.Tests
{
    public static partial class CoreSpecification
    {
        public static void RegionalEnemyActionsAndBudgets()
        {
            for(int region=0;region<10;region++)
            {
                var s=GameState.New();s.tutorialCompleted=true;
                var c=new Combat(s,region*6,"regional",17);c.Elapsed=30;c.Position=new V2(270,510);
                var role=RegionalEnemies.Signature[region];
                var enemy=new Enemy{id=9000,role=role,position=new V2(335,540),hp=100000,maxHp=100000,speed=41,cooldown=0};c.Enemies.Add(enemy);
                Check(Array.IndexOf(c.Definition.Enemies,role)>=0,"signature actually spawns "+region);
                c.Tick(.05f,new V2());
                Check(c.RegionalActions>0,"signature behavior executes "+region);
                if(region==1)Check(enemy.windup>=1&&enemy.direction.y==0,"rail charge locks to axis with warning");
                if(region==3)Check(enemy.windup>2&&enemy.position.x==335,"cooler braces without advancing");
                if(region==4||region==9)Check(c.Enemies.Exists(e=>e.id!=9000&&e.role==(region==4?EnemyRole.Shield:EnemyRole.Charger)),"assembler deploys its actual regional unit");
                if(region==5||region==8)Check(c.Hazards.Exists(h=>h.source==DamageSource.EnemyAttack&&h.delay>1.8f&&(region==5?h.width>h.height:h.height>h.width)),"regional line has time to dodge and enemy attribution");
                if(region==2||region==6)Check(c.Hazards.Exists(h=>h.source==DamageSource.EnemyAttack&&h.cause==(region==2?DamageCause.Poison:DamageCause.Heat)),"canister is enemy attack, not floor damage");
                for(int i=0;i<120;i++){if(c.Choosing)c.Choose(c.Choices[0]);c.Tick(.05f,new V2());}
                Check(c.PeakEnemyWarnings<=3,"regional skills share warning budget");
            }
            var trial=new Combat(GameState.New(),0,"trial-regional",17,false,SkillTrial.Mode);trial.Elapsed=30;
            trial.Enemies.Add(new Enemy{id=9999,role=EnemyRole.Pursuer,position=trial.Position+new V2(60,0),hp=10000,maxHp=10000,cooldown=0});
            trial.Tick(.05f,new V2());Check(trial.RegionalActions==0,"fixed trial excludes campaign regional attacks");
        }
        public static void RegionalShieldFlankAndReplay()
        {
            float HitFrom(V2 direction)
            {
                var c=new Combat(GameState.New(),18,"shield");c.Elapsed=30;
                var enemy=new Enemy{id=9000,role=EnemyRole.Shield,position=c.Position+new V2(200,0),hp=10000,maxHp=10000,windup=2,direction=direction};c.Enemies.Add(enemy);
                c.Bullets.Add(new Bullet{id=10000,position=enemy.position,velocity=new V2(),damage=100,life=1});
                c.Tick(.05f,new V2());return 10000-enemy.hp;
            }
            Check(HitFrom(new V2(-1,0))==45&&HitFrom(new V2(1,0))==100,"braced shield rewards flank instead of distance alone");
            for(int region=0;region<10;region++)
            {
                var s=GameState.New();s.tutorialCompleted=true;s.heroes[0].level=30;
                var c=new Combat(s,region*6,"region-replay",71);var input=new ReplayRecorder();
                for(int i=0;i<1800&&!c.Ended;i++)
                {
                    if(c.Choosing){input.Choose(c.Choices[0]);c.Choose(c.Choices[0]);}
                    c.Tick(ReplayRecorder.Step,input.Move(V2.Angle(i*.01)));
                }
                var replay=ReplayRecorder.Verify(s,region*6,c.RunId,71,input.Steps,80);
                Check(replay.Kills==c.Kills&&replay.Hp==c.Hp&&replay.Position.x==c.Position.x&&replay.RegionalActions==c.RegionalActions,"regional actions replay exactly "+region);
            }
        }
    }
}
