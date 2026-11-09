using System;
using System.Collections.Generic;
namespace ScrapGuard.Core
{
    [Serializable] public sealed class RobotRecord
    {
        public int design, builds, kills, stage, rank;
        public float damage;
    }
    public static class RobotAssembly
    {
        public const int Count=9, DefaultDesign=5;
        public static readonly string[] Parts={"포신","구동부","장갑"};
        public static readonly string[] Frames={"포격 전차","돌파 보행기","철벽 수호기"};
        public static readonly string[] Supports={"연사","기동","방호"};
        public static bool Valid(int id){return id>=1&&id<=Count;}
        public static int Frame(int id){return Valid(id)?(id-1)/3:1;}
        public static int Support(int id){return Valid(id)?(id-1)%3:1;}
        public static string Name(int id){return Valid(id)?Supports[Support(id)]+" "+Frames[Frame(id)]:"미조립";}
        public static string Role(int frame){return frame==0?"떨어진 적에게 범위 포격":frame==1?"빠르게 움직이며 포위 돌파":"근접 충격파 · 받는 피해 감소";}
        public static string Tradeoff(int frame){return frame==0?"이동이 느려요":frame==1?"적 가까이 접근해야 해요":"이동과 공격 주기가 느려요";}
        public static string RegionAdvice(StageDefinition stage)
        {
            bool ranged=Array.Exists(stage.Enemies,r=>r==EnemyRole.Artillery);
            return ranged?"포격 적 주의 · 철벽은 피해 완화, 돌파는 빠른 이탈":Array.Exists(stage.Enemies,r=>r==EnemyRole.Charger)?"돌진 적 주의 · 돌파로 이동, 포격으로 접근 전 공격":"무리 접근 · 돌파는 근접 돌파, 포격은 거리 유지";
        }
        public static string Detail(int id)
        {
            string body=Frame(id)==0?"느리게 이동 · 먼 적에게 범위 포격":Frame(id)==1?"빠르게 이동 · 주변 적을 밟아 돌파":"중장 이동 · 근접 충격파와 피해 감소";
            return body+"\n"+(Support(id)==0?"보조 포신: 3갈래 관통탄":Support(id)==1?"보조 구동부: 속도·범위 증가":"보조 장갑: 탑승 시 소형 보호막");
        }
        public static int PartFor(Enemy enemy){return enemy.salvagePart>=0?enemy.salvagePart:PartFor(enemy.role);}
        public static int PartFor(EnemyRole role){return role==EnemyRole.Artillery||role==EnemyRole.Bomber?0:role==EnemyRole.Shield||role==EnemyRole.Summoner?2:1;}
        public static void Normalize(GameState s)
        {
            if(s.robotRecords==null)s.robotRecords=new List<RobotRecord>();
            if(s.showcaseRobot!=0&&!s.robotRecords.Exists(r=>r!=null&&r.design==s.showcaseRobot&&r.builds>0))s.showcaseRobot=0;
        }
        public static CommandResult Showcase(GameState s,int id)
        {
            Normalize(s);if(id!=0&&(!Valid(id)||!s.robotRecords.Exists(r=>r!=null&&r.design==id&&r.builds>0)))return CommandResult.Fail("전투에서 조립한 로봇만 전시할 수 있어요.");
            s.showcaseRobot=id;return CommandResult.Ok(id==0?"대원 전시로 돌아왔어요.":Name(id)+"를 전시했어요.");
        }
        public static void Record(GameState s,List<RobotRecord> runs,int stage)
        {
            Normalize(s);foreach(var run in runs)
            {
                if(!Valid(run.design)||run.builds<=0)continue;
                var record=s.robotRecords.Find(r=>r.design==run.design);
                if(record==null){record=new RobotRecord{design=run.design};s.robotRecords.Add(record);}
                record.builds+=run.builds;record.kills+=run.kills;record.damage+=run.damage;record.stage=Math.Max(record.stage,stage+1);record.rank=Math.Max(record.rank,run.rank);
            }
        }
    }
    public sealed partial class Combat
    {
        readonly int[] robotParts=new int[3],partProgress=new int[3],robotBuilds=new int[9],robotKills=new int[9];
        readonly float[] robotDamage=new float[9];
        readonly int[] robotRanks=new int[9],robotBestRank=new int[9];
        public bool RobotPlanned {get;private set;}
        public int RobotPartsSpent {get;private set;}
        public int RobotPlansBuilt {get;private set;}
        public float MachineActiveDuration {get;private set;}=10;
        public int RobotRank(int design){return RobotAssembly.Valid(design)?robotRanks[design-1]:0;}
        public bool RobotAvailable(int design){return RobotAssembly.Valid(design)&&(design==RobotAssembly.DefaultDesign||RobotRank(design)>0);}
        public int[] RobotRecipe(int design)
        {
            var cost=new int[3];if(!RobotAssembly.Valid(design)||RobotRank(design)>=2)return cost;
            cost[RobotAssembly.Frame(design)]+=RobotRank(design)==0?3:5;cost[RobotAssembly.Support(design)]+=RobotRank(design)==0?1:3;return cost;
        }
        public bool CanPlanRobot(int design,bool upgrade)
        {
            if(Challenge||Ended||Choosing||MachineTime>0||!RobotAssembly.Valid(design))return false;
            if(!upgrade)return RobotAvailable(design);if(RobotRank(design)>=2)return false;
            var cost=RobotRecipe(design);for(int i=0;i<3;i++)if(robotParts[i]<cost[i])return false;return true;
        }
        public bool PlanRobot(int design,bool upgrade)
        {
            if(!CanPlanRobot(design,upgrade))return false;
            if(upgrade){var cost=RobotRecipe(design);for(int i=0;i<3;i++){robotParts[i]-=cost[i];RobotPartsSpent+=cost[i];}robotRanks[design-1]++;RobotPlansBuilt++;}
            MachineDesign=design;RobotPlanned=true;return true;
        }
        public float RobotDuration(int design)
        {int rank=RobotRank(design);return rank==0?10:10+rank*4+(Elapsed>=Duration*.6f?4:0);}
        public float RobotDamageReduction {get{return MachineTime>0&&RobotAssembly.Frame(MachineDesign)==2&&RobotRank(MachineDesign)>0?.15f:0;}}
        public int MachineDesign {get;private set;}=RobotAssembly.DefaultDesign;
        public int[] RobotParts {get{return (int[])robotParts.Clone();}}
        public float MachineSpeed {get{return MachineTime<=0?1:(RobotAssembly.Frame(MachineDesign)==0?.65f:RobotAssembly.Frame(MachineDesign)==1?1.1f:.75f)+(RobotRank(MachineDesign)>0&&RobotAssembly.Support(MachineDesign)==1?.15f:0);}}
        public List<RobotRecord> RobotReport()
        {
            var result=new List<RobotRecord>();for(int i=0;i<9;i++)if(robotBuilds[i]>0)result.Add(new RobotRecord{design=i+1,builds=robotBuilds[i],kills=robotKills[i],damage=robotDamage[i],rank=robotBestRank[i]});return result;
        }
        int robotSpawnSerial,introCarrierSerial;
        int NextCarrierPart()
        {
            // First-clear lesson: finite, evenly supplied parts; still kill and collect them.
            if(IntroBattle&&Elapsed>=6&&Elapsed<=60&&introCarrierSerial<72)
            {
                introCarrierSerial++;return introCarrierSerial%4==0?(introCarrierSerial/4-1)%3:-1;
            }
            if(Challenge||Elapsed<25)return -1;
            robotSpawnSerial++;if(robotSpawnSerial%24!=0)return -1;
            bool cannon=!Array.Exists(Definition.Enemies,r=>r==EnemyRole.Artillery||r==EnemyRole.Bomber);
            bool armor=!Array.Exists(Definition.Enemies,r=>r==EnemyRole.Shield||r==EnemyRole.Summoner);
            if(cannon&&armor)return (robotSpawnSerial/24)%2==0?0:2;
            return cannon?0:armor?2:-1;
        }
        void RecoverRobotPart(Enemy enemy)
        {
            if(Challenge)return;int type=RobotAssembly.PartFor(enemy);
            partProgress[type]+=enemy.kind>=2||enemy.salvagePart>=0?12:enemy.role==EnemyRole.Pursuer?1:3;
            if(partProgress[type]<12)return;partProgress[type]-=12;
            // Merge pickups before adding more objects to a crowded battlefield.
            var existing=Pickups.Find(p=>p.part==type&&(!IntroBattle||Elapsed>65||V2.Distance(p.position,enemy.position)<100));
            if(existing!=null){existing.partCount=Math.Min(12,existing.partCount+1);return;}
            if(Pickups.Count<230)Pickups.Add(new Pickup{id=nextId++,position=enemy.position,part=type,partCount=1});
        }
        void BeginRobot(int design)
        {
            MachineDesign=design;RobotPlanned=true;
            robotBuilds[design-1]++;robotBestRank[design-1]=Math.Max(robotBestRank[design-1],RobotRank(design));
            MachineActiveDuration=RobotDuration(design);
            if(RobotRank(design)>0&&RobotAssembly.Support(design)==2)Shield=Math.Max(Shield,Math.Min(90,MaxHp*.06f));
            MachinePosition=Position;
        }
        void UpdateRobotAttack()
        {
            int frame=RobotAssembly.Frame(MachineDesign),support=RobotAssembly.Support(MachineDesign);
            int rank=RobotRank(MachineDesign);float quality=rank==0?1:1+rank*.15f;
            float radius=(frame==0?100:frame==1?165:190)+(rank>0&&support==1?35:0)+(FieldModule==2?35:0);
            V2 center=Position;
            if(frame==0)
            {
                var target=Nearest(Position);if(target!=null&&V2.Distance(target.position,Position)<640)center=target.position;
                Effects.Add(new BattleEffect{position=Position,end=center,kind=8});
            }
            float power=frame==0?7:frame==1?4.2f:3.6f;
            foreach(var enemy in Enemies)if(enemy.hp>0&&V2.Distance(enemy.position,center)<radius)
            {
                Hit(enemy,Damage*power*quality,true);
                if(!enemy.finalBoss&&!enemy.core)enemy.position=Slide(enemy.position,(enemy.position-Position).Normal*(frame==2?65:35),15);
            }
            if(FieldModule==3)Heal(MaxHp*.003f,HealingSource.Machine);
            if(rank>0&&support==0||FieldModule==1||FieldModule==4)
            {
                var target=Nearest(Position);
                if(target!=null)
                {
                    var direction=(target.position-Position).Normal;int count=rank>0&&support==0?3:1;
                    for(int i=0;i<count;i++){float angle=count==1?0:(i-1)*.14f;var shot=new V2(direction.x*(float)Math.Cos(angle)-direction.y*(float)Math.Sin(angle),direction.x*(float)Math.Sin(angle)+direction.y*(float)Math.Cos(angle));
                    Bullets.Add(new Bullet{id=nextId++,position=Position,velocity=shot*600,damage=Damage*(count==1?2:.8f)*quality,life=1.2f,pierce=8,mod=true,machine=true,robotDesign=MachineDesign});}
                }
            }
            Effects.Add(new BattleEffect{position=center,kind=frame==0?11:4,radius=radius});
        }
    }
}
