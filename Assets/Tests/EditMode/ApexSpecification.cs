using System;
using ScrapGuard.Core;
namespace ScrapGuard.Tests
{
    public static partial class CoreSpecification
    {
        public static void ApexUnlockSettlementAndIsolation()
        {
            var e=New();var s=e.State;Check(!e.BeginApex("locked",47).Success,"apex requires that exact normal finale");
            s.unlockedStage=59;s.clearedStages.AddRange(new[]{47,53,59});int gold=s.gold,parts=s.parts,gems=s.gems;
            Check(!e.BeginApex("invalid",46).Success,"only three optional finales");
            foreach(int stage in Apex.Stages)
            {
                Check(e.BeginApex("a"+stage,stage).Success,"apex start");
                var c=new Combat(s,stage,"a"+stage,17,false,Apex.Mode);
                Check(c.RobotEnabled&&!c.Challenge&&!c.BossPractice,"hard uses owned growth and normal robot loop");
                // Explicit settlement fixture; replay authority is separately tested on the server.
                c.Elapsed=240;c.Ended=true;c.Won=true;
                Check(!e.FinishCampaign(c).Success,"hard cannot receive campaign payouts");
                Check(e.FinishApex(c).Success&&!e.FinishApex(c).Success,"first settlement once");
                Check(s.receipts[s.receipts.Count-1].shards==6&&Apex.Find(s,stage).wins==1,"first-clear shards and record");
            }
            Check(s.coreShards==18&&Social.HasTitle(s,6)&&s.clearedStages.Count==3,"earned title without changing campaign clears");
            Check(s.gold==gold&&s.parts==parts&&s.gems==gems&&s.totalRuns==0&&s.weekly.Count==0&&s.dailySupplySeconds==300,"separate rewards plus ordinary field time only");
            e.BeginApex("repeat",47);var repeat=new Combat(s,47,"repeat",17,false,Apex.Mode);repeat.Elapsed=220;repeat.Ended=true;repeat.Won=true;
            Check(e.FinishApex(repeat).Success&&s.coreShards==18&&Apex.Find(s,47).bestSeconds==220,"repeat improves record without duplicating shards");
            e.BeginApex("lose",47);var loss=new Combat(s,47,"lose",17,false,Apex.Mode);loss.Elapsed=80;loss.Hp=0;loss.Ended=true;
            Check(!loss.CanRevive&&e.FinishApex(loss).Success&&Apex.Find(s,47).wins==2&&s.coreShards==18,"loss does not pay and no ad revival");
            s.apexRecords=null;Apex.Normalize(s);Check(!Social.HasTitle(s,6),"legacy state has no unearned title");
        }
        public static void ApexReplayAndFinaleScope()
        {
            var s=GameState.New();s.tutorialCompleted=true;s.unlockedStage=59;s.heroes[0].level=30;foreach(var item in s.items){item.level=20;item.rarity=2;}
            var normal=new Combat(s,47,"same",71);var hard=new Combat(s,47,"same",71,false,Apex.Mode);
            Check(normal.Damage==hard.Damage&&normal.MaxHp==hard.MaxHp,"hard preserves owned stats, no hidden player nerf");
            bool rejected=false;try{new Combat(s,46,"bad",71,false,Apex.Mode);}catch(ArgumentException){rejected=true;}Check(rejected,"hard non-finale rejected");
            foreach(int stage in Apex.Stages)
            {
                var c=new Combat(s,stage,"hard"+stage,71,false,Apex.Mode);var r=new ReplayRecorder();
                for(int i=0;i<2400&&!c.Ended;i++)
                {
                    if(c.Choosing){int skill=c.Choices[0];c.Choose(skill);r.Choose(skill);}
                    c.Tick(ReplayRecorder.Step,r.Move(new V2((float)Math.Cos(i*.01),(float)Math.Sin(i*.01))));
                }
                if(!c.Ended)c.Abandon();var v=ReplayRecorder.Verify(s,stage,c.RunId,71,r.Steps,600,Apex.Mode);
                Check(v.Hp==c.Hp&&v.Kills==c.Kills&&v.Won==c.Won,"hard inputs deterministic");
            }
        }
    }
}
