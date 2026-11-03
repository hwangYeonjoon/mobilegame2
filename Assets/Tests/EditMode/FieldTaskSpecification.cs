using System;
using System.Collections.Generic;
using ScrapGuard.Core;
namespace ScrapGuard.Tests
{
    public static partial class CoreSpecification
    {
        public static void EarlyBattleProgression()
        {
            var c=new Combat(GameState.New(),0,"intro");
            for(int i=0;i<165 && !c.Choosing;i++){c.Enemies.Clear();c.Bullets.Clear();c.Pickups.Clear();c.Tick(.05f,new V2());}
            Check(c.Choosing && c.Elapsed<8.2f && string.Join(",",c.Choices)=="1,2,3","first meaningful weapon choice by eight seconds");
            Check(c.Choose(1),"first choice usable");
            for(int i=0;i<500 && !c.Choosing;i++){c.Enemies.Clear();c.Bullets.Clear();c.Pickups.Clear();c.Tick(.05f,new V2());}
            Check(c.Choosing && c.Elapsed<24.2f,"second progression by 24 seconds");
            var weekly=new Combat(GameState.New(),4,"weekly",12,false,Endgame.PracticeMode);
            for(int i=0;i<270;i++){weekly.Tick(.05f,new V2());if(weekly.Choosing)weekly.Choose(weekly.Choices[0]);}
            Check(weekly.Task==FieldTask.None && !weekly.CanChooseTask,"weekly challenge excludes optional campaign tasks");
        }
        public static void FieldTaskChoiceGuards()
        {
            var c=new Combat(GameState.New(),1,"salvage");
            Check(!c.ChooseTask(1),"cannot choose before opening");c.Elapsed=12;c.Position=new V2(40,250);
            Check(!c.ChooseTask(2),"cannot choose remotely");c.Position=c.TaskPosition;
            Check(!c.ChooseTask(0) && !c.ChooseTask(3),"invalid option rejected");
            c.Hp=c.MaxHp*.5f;Check(c.ChooseTask(1) && c.TaskCompleted && c.Hp>c.MaxHp*.5f,"safe recovery heals and completes");
            Check(!c.ChooseTask(2),"cannot repeat or switch completed choice");
            var recorder=new ReplayRecorder();recorder.Move(new V2());recorder.Encounter(2);recorder.Move(new V2());
            Check(recorder.Steps.Count==3 && recorder.Steps[1].frames==0,"choice is not compressed into movement");
            foreach(var bad in new[]{new ReplayStep{encounter=2},new ReplayStep{encounter=2,frames=1},new ReplayStep{encounter=3},new ReplayStep{encounter=1,support=true},new ReplayStep{encounter=1,skill=0}})
            {
                bool rejected=false;try{ReplayRecorder.Verify(GameState.New(),1,"bad",1,new List<ReplayStep>{bad},30);}catch(ArgumentException){rejected=true;}
                Check(rejected,"forged or mixed encounter input rejected");
            }
        }
        public static void FieldTaskReplayAndRewards()
        {
            var state=GameState.New();state.heroes[0].level=30;foreach(var item in state.items){item.level=20;item.rarity=2;}
            var c=new Combat(state,1,"risk",71);var r=new ReplayRecorder();
            for(int i=0;i<1200 && !c.Ended;i++)
            {
                if(c.Choosing){c.Choose(c.Choices[0]);r.Choose(c.Choices[0]);}
                if(c.CanChooseTask){Check(c.ChooseTask(2),"risk choice accepted");r.Encounter(2);}
                c.Tick(ReplayRecorder.Step,r.Move((new V2(270,420)-c.Position).Normal));
            }
            c.Abandon();var copy=ReplayRecorder.Verify(state,1,"risk",71,r.Steps,45);
            Check(c.TaskChoice==2 && c.TaskCompleted,"risk survived twenty seconds");
            Check(copy.TaskCompleted && copy.Hp==c.Hp && copy.Kills==c.Kills && copy.TaskProgress==c.TaskProgress,"risk action replays identically");
            Check(c.TaskBonusGold==0,"abandoned task never awards clear bonus");
            var economy=New();economy.State.unlockedStage=1;economy.BeginRun("reward",1);
            var win=new Combat(economy.State,1,"reward");win.Elapsed=12;win.Position=win.TaskPosition;win.ChooseTask(1);win.Ended=win.Won=true;
            int gold=economy.State.gold,parts=economy.State.parts;Check(economy.FinishCampaign(win).Success,"successful task settles");
            Check(economy.State.gold-gold==Campaign.Get(1).Gold+200 && economy.State.parts-parts==Campaign.Get(1).Parts+2,"task rewards included exactly once");
            gold=economy.State.gold;Check(!economy.FinishCampaign(win).Success && economy.State.gold==gold,"repeated settlement cannot duplicate task rewards");
        }
        public static void FieldTaskDefenseAndEscort()
        {
            var defense=new Combat(GameState.New(),2,"defense");defense.Elapsed=12;
            for(int i=0;i<370 && !defense.TaskCompleted;i++)
            {defense.Enemies.Clear();defense.Bullets.Clear();defense.Position=defense.TaskPosition;defense.Tick(.05f,new V2());if(defense.Choosing)defense.Choose(defense.Choices[0]);}
            Check(defense.TaskCompleted && defense.TaskIntegrity>0,"holding defense area completes objective");
            var attacked=new Combat(GameState.New(),2,"under-attack");attacked.Elapsed=12;attacked.Hp=attacked.MaxHp=10000;
            for(int i=0;i<4;i++)attacked.Enemies.Add(new Enemy{id=990+i,position=attacked.TaskPosition,hp=100000});
            for(int i=0;i<130 && !attacked.TaskFailed;i++)attacked.Tick(.05f,new V2());
            Check(attacked.TaskFailed && !attacked.Ended,"enemies can destroy optional relay without ending main mission");
            var escort=new Combat(GameState.New(),3,"escort");escort.Elapsed=12;
            for(int i=0;i<100;i++){escort.Enemies.Clear();escort.Tick(.05f,new V2());}
            Check(escort.TaskProgress==0,"distant player cannot advance escort");
            for(int i=0;i<490 && !escort.TaskCompleted;i++)
            {escort.Enemies.Clear();escort.Bullets.Clear();escort.Position=escort.TaskPosition;escort.Tick(.05f,new V2());if(escort.Choosing)escort.Choose(escort.Choices[0]);}
            Check(escort.TaskCompleted && Math.Abs(escort.TaskPosition.y-360)<.01f,"escort reaches distinct destination");
            var expired=new Combat(GameState.New(),1,"expired");expired.Elapsed=70;expired.Tick(.05f,new V2());
            Check(expired.TaskFailed && !expired.Ended && !expired.CanChooseTask,"missed optional task does not end campaign");
        }
        public static void ExpandedModMechanics()
        {
            var all=new[]{5,5,5,5,5,5,5,5};Check(Workshop.ModMask(all)==63,"six distinct mods");
            var c=new Combat(GameState.New(),5,"blast");c.Skills[0]=3;c.Skills[4]=2;c.Enemies.Add(new Enemy{id=99,position=c.Position+new V2(150,0),hp=10000});c.Tick(.05f,new V2());
            Check(c.HasMod(3) && c.Bullets.Exists(b=>b.blast),"rupture conversion changes projectile behavior");
            c=new Combat(GameState.New(),5,"repair");c.Skills[3]=3;c.Skills[6]=2;c.Hp=c.MaxHp*.5f;c.Tick(.05f,new V2());
            float health=c.Hp;Check(c.HasMod(4) && health>c.MaxHp*.5f && c.Shield>0,"repair drone repairs and shields");
            c.Tick(.05f,new V2());Check(c.Hp==health,"repair pulse has a cooldown");
            c=new Combat(GameState.New(),5,"cold");c.Skills[2]=3;c.Skills[7]=2;
            var enemy=new Enemy{id=99,position=c.Position+new V2(160,0),hp=10000};c.Enemies.Add(enemy);c.Tick(.05f,new V2());
            Check(c.HasMod(5) && enemy.slowTime>0 && enemy.hp<10000,"cold coil expands range and slows");
        }
    }
}
