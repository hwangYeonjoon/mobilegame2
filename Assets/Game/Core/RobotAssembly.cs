using System;
using System.Collections.Generic;
namespace ScrapGuard.Core
{
    [Serializable] public sealed class RobotRecord
    {
        public int design, builds, kills, stage;
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
        public static string Detail(int id)
        {
            string body=Frame(id)==0?"느리게 이동 · 먼 적에게 범위 포격":Frame(id)==1?"빠르게 이동 · 주변 적을 밟아 돌파":"중장 이동 · 근접 충격파와 피해 감소";
            return body+"\n"+(Support(id)==0?"보조 포신: 관통탄 추가":Support(id)==1?"보조 구동부: 속도·범위 증가":"보조 장갑: 합체 시 보호막");
        }
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
                record.builds+=run.builds;record.kills+=run.kills;record.damage+=run.damage;record.stage=Math.Max(record.stage,stage+1);
            }
        }
    }
    public sealed partial class Combat
    {
        readonly int[] robotParts=new int[3],partProgress=new int[3],robotBuilds=new int[9],robotKills=new int[9];
        readonly float[] robotDamage=new float[9];
        public int MachineDesign {get;private set;}=RobotAssembly.DefaultDesign;
        public int MachineRecoveredParts {get;private set;}
        public int[] RobotParts {get{return (int[])robotParts.Clone();}}
        public float MachineSpeed {get{return MachineTime<=0?1:(RobotAssembly.Frame(MachineDesign)==0?.65f:RobotAssembly.Frame(MachineDesign)==1?1.1f:.75f)+(RobotAssembly.Support(MachineDesign)==1?.15f:0);}}
        public int RecoveredFor(int design)
        {
            if(!RobotAssembly.Valid(design))return 0;int a=RobotAssembly.Frame(design),b=RobotAssembly.Support(design);
            return a==b?Math.Min(2,robotParts[a]):Math.Min(1,robotParts[a])+Math.Min(1,robotParts[b]);
        }
        public List<RobotRecord> RobotReport()
        {
            var result=new List<RobotRecord>();for(int i=0;i<9;i++)if(robotBuilds[i]>0)result.Add(new RobotRecord{design=i+1,builds=robotBuilds[i],kills=robotKills[i],damage=robotDamage[i]});return result;
        }
        void RecoverRobotPart(Enemy enemy)
        {
            if(Challenge)return;int type=RobotAssembly.PartFor(enemy.role);
            partProgress[type]+=enemy.kind>=2?12:enemy.role==EnemyRole.Pursuer?1:3;
            if(partProgress[type]<12)return;partProgress[type]-=12;
            // Merge pickups before adding more objects to a crowded battlefield.
            var existing=Pickups.Find(p=>p.part==type);
            if(existing!=null){existing.partCount=Math.Min(12,existing.partCount+1);return;}
            if(Pickups.Count<230)Pickups.Add(new Pickup{id=nextId++,position=enemy.position,part=type,partCount=1});
        }
        void BeginRobot(int design)
        {
            MachineDesign=design;MachineRecoveredParts=RecoveredFor(design);
            int a=RobotAssembly.Frame(design),b=RobotAssembly.Support(design);
            if(robotParts[a]>0)robotParts[a]--;if(robotParts[b]>0)robotParts[b]--;
            robotBuilds[design-1]++;
            if(b==2)Shield=Math.Max(Shield,MaxHp*.18f);
            MachinePosition=Position;
        }
        void UpdateRobotAttack()
        {
            int frame=RobotAssembly.Frame(MachineDesign),support=RobotAssembly.Support(MachineDesign);
            float quality=1+MachineRecoveredParts*.15f;
            float radius=(frame==0?100:frame==1?165:190)+(support==1?25:0)+(FieldModule==2?35:0);
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
            if(FieldModule==3)Heal(MaxHp*.005f,HealingSource.Machine);
            if(support==0||FieldModule==1||FieldModule==4)
            {
                var target=Nearest(Position);
                if(target!=null)Bullets.Add(new Bullet{id=nextId++,position=Position,velocity=(target.position-Position).Normal*600,damage=Damage*2*quality,life=1.2f,pierce=8,mod=true,machine=true,robotDesign=MachineDesign});
            }
            Effects.Add(new BattleEffect{position=center,kind=frame==0?11:4,radius=radius});
        }
    }
}
