using System;
using System.Collections.Generic;
namespace ScrapGuard.Core
{
    public static class RobotStyles
    {
        public const int PackPrice=900,PackGold=18000,PackParts=180;
        public const int CraftGold=30000,CraftParts=300,CraftShards=12;
        public const string PackKey="robot_style_astral";
        public static readonly string[] Names={"기본 기체","성운 기사","회수 선봉대"};
        public static bool Owned(GameState s,int frame,int style)
        {return frame>=0&&frame<3&&(style==0||style==1&&s.ownedCosmetics!=null&&s.ownedCosmetics.Contains(PackKey)||style==2&&s.ownedCosmetics!=null&&s.ownedCosmetics.Contains("robot_style_reclaimer_"+frame));}
        public static void Normalize(GameState s)
        {
            if(s.robotStyles==null)s.robotStyles=new List<int>();
            while(s.robotStyles.Count<3)s.robotStyles.Add(0);
            if(s.robotStyles.Count>3)s.robotStyles.RemoveRange(3,s.robotStyles.Count-3);
            for(int i=0;i<3;i++)if(!Owned(s,i,s.robotStyles[i]))s.robotStyles[i]=0;
        }
        public static int Active(GameState s,int frame)
        {return frame>=0&&frame<3&&s.robotStyles!=null&&s.robotStyles.Count>frame&&Owned(s,frame,s.robotStyles[frame])?s.robotStyles[frame]:0;}
        public static CommandResult Equip(GameState s,int frame,int style)
        {
            Normalize(s);if(!Owned(s,frame,style))return CommandResult.Fail("보유한 기체 외형만 적용할 수 있어요.");
            s.robotStyles[frame]=style;return CommandResult.Ok(Names[style]+" 적용 · 전투와 전시실에 표시돼요.");
        }
    }
    public sealed partial class Economy
    {
        public CommandResult BuyRobotStylePack()
        {
            if(RobotStyles.Owned(State,0,1))return CommandResult.Fail("이미 성운 기사 팩을 보유하고 있어요.");
            if(!PremiumWallet.Spend(State,RobotStyles.PackPrice))return CommandResult.Fail("보석 잔액 또는 환불 조정 내역을 확인하세요.");
            if(State.ownedCosmetics==null)State.ownedCosmetics=new List<string>();
            State.ownedCosmetics.Add(RobotStyles.PackKey);RobotStyles.Normalize(State);
            for(int i=0;i<3;i++)State.robotStyles[i]=1;
            State.gold+=RobotStyles.PackGold;State.parts+=RobotStyles.PackParts;
            return CommandResult.Ok("성운 기사 기체 3종 적용 · 골드 18,000 · 부품 180 획득");
        }
        public CommandResult CraftRobotStyle(int frame)
        {
            if(frame<0||frame>2||RobotStyles.Owned(State,frame,2))return CommandResult.Fail("제작할 기체 외형을 확인하세요.");
            if(!State.clearedStages.Contains(41))return CommandResult.Fail("작전 42 완료 후 제작할 수 있어요.");
            if(State.gold<RobotStyles.CraftGold||State.parts<RobotStyles.CraftParts||State.coreShards<RobotStyles.CraftShards)return CommandResult.Fail("골드 30,000 · 부품 300 · 코어 파편 12개가 필요해요.");
            if(State.ownedCosmetics==null)State.ownedCosmetics=new List<string>();
            State.gold-=RobotStyles.CraftGold;State.parts-=RobotStyles.CraftParts;State.coreShards-=RobotStyles.CraftShards;
            State.ownedCosmetics.Add("robot_style_reclaimer_"+frame);return RobotStyles.Equip(State,frame,2);
        }
    }
    public sealed class GrowthPath
    {
        public int slot,from,to,count,gold,parts,attack,health;public string id,name;
    }
    public static class GrowthPaths
    {
        // Each quote is an alternative spending plan, never four simultaneous upgrades.
        public static GrowthPath Quote(GameState s,int slot,int limit=30,int extraGold=0,int extraParts=0)
        {
            if(slot<0||slot>3||limit<1||limit>30)return null;
            var step=Growth.Steps(s,extraGold,extraParts).Find(x=>x.slot==slot);if(step==null)return null;
            var q=new GrowthPath{slot=slot,id=step.id,name=step.name,from=step.level,to=step.level};
            int cap=slot==3?30:20;
            while(q.to<cap&&q.count<limit)
            {
                int gold=q.to*(slot==3?500:300),parts=q.to*(slot==3?3:2);
                if((long)q.gold+gold>(long)s.gold+extraGold||(long)q.parts+parts>(long)s.parts+extraParts)break;
                q.gold+=gold;q.parts+=parts;q.count++;q.to++;
            }
            var weapon=s.items.Find(i=>i.id==s.equipped[0]);var armor=s.items.Find(i=>i.id==s.equipped[1]);
            if(slot==0)weapon=new Item{definition=weapon.definition,rarity=weapon.rarity,level=q.to};
            if(slot==1)armor=new Item{definition=armor.definition,rarity=armor.rarity,level=q.to};
            int hero=slot==3?q.to:s.heroes[s.selectedHero].level;
            q.attack=Catalog.AttackFor(s.selectedHero,hero,weapon);q.health=Catalog.HealthFor(s.selectedHero,hero,armor);return q;
        }
    }
    public sealed partial class Economy
    {
        public CommandResult UpgradePath(int slot,int count,string expectedId,int expectedLevel)
        {
            var q=GrowthPaths.Quote(State,slot,count);
            if(q==null||q.count!=count||q.from!=expectedLevel||q.id!=expectedId)return CommandResult.Fail("강화 대상이나 재료가 바뀌었어요. 다시 확인하세요.");
            // Preflight the complete quote so failures cannot partially spend materials.
            State.gold-=q.gold;State.parts-=q.parts;
            if(slot==3)State.heroes[State.selectedHero].level=q.to;else State.items.Find(i=>i.id==q.id).level=q.to;
            State.guideUpgrade=true;return CommandResult.Ok(q.name+" Lv."+q.from+" → "+q.to+" 강화 완료");
        }
    }
}
