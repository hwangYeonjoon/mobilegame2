using System.Collections;
using System.Collections.Generic;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator BattleClaritySmokeRoutine()
        {
            DismissTitleScreen();CloseModal();
            Store.Change(e=>{e.State.testProgress=true;e.State.gems=900;return CommandResult.Ok("Isolated battle presentation QA");});
            Show("home");yield return Capture("01-home-goals");
            string before=JsonUtility.ToJson(State);ProgressGoals();yield return Capture("02-goals");
            modal.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=0;yield return Capture("03-goals-collection");
            if(before!=JsonUtility.ToJson(State))smokeErrors.Add("Goal view mutated save");
            for(int hero=0;hero<HeroCatalog.Count;hero++)
            {
                HeroMotionRoom(hero);yield return Capture("04-motion-"+hero);
                modal.GetComponentInChildren<GuardianMotionPreview>().Mode=2;yield return Capture("05-fire-"+hero);
            }
            if(before!=JsonUtility.ToJson(State))smokeErrors.Add("Hero motion preview mutated save");
            Show("heroes");yield return Capture("06-hero-actions");Settings();yield return Capture("07-settings");
            BattleDisplayOptions();yield return Capture("08-display-options");
            CloseModal();yield return null;
            PrepareOutfitHero(1,true);if(combat!=null)smokeErrors.Add("Unowned outfit launched a battle");
            OutfitPurchase(1);ClickVisibleButton("보석 600개로 구매");yield return Capture("09-purchase");
            if(!Outfits.Owned(State,1)||State.gems!=300)smokeErrors.Add("Purchase did not debit exactly once");
            ClickVisibleButton(Outfits.Active(State,0)==1?"이 모습으로 바로 출격":"장착하고 이 모습으로 출격");
            if(combat==null){smokeErrors.Add("One-action equip and launch failed");WriteClarityReport();yield break;}
            battlePaused=true;
            if(State.selectedHero!=0||battleOutfit!=1||State.gems!=300)smokeErrors.Add("Wrong purchased appearance or duplicate debit");
            for(int i=0;i<8;i++)combat.Enemies.Add(new Enemy{id=8000+i,kind=0,role=(EnemyRole)(i%6),hp=100,maxHp=100,position=new V2(75+i%4*130,300+i/4*320)});
            RenderBattle();yield return Capture("09b-purchased-outfit-in-battle");
            combat.Enemies.Clear();combat.Bullets.Clear();combat.Hazards.Clear();
            for(int i=0;i<120;i++)combat.Enemies.Add(new Enemy{id=10000+i,kind=0,role=(EnemyRole)(i%6),hp=100,maxHp=100,position=new V2(32+i%12*42,265+i/12*48),windup=i%6==1?.8f:0});
            for(int i=0;i<180;i++)combat.Bullets.Add(new Bullet{id=20000+i,position=new V2(25+i%15*35,250+i/15*43),velocity=new V2(30,-20),life=2});
            for(int i=0;i<8;i++)combat.Hazards.Add(new Hazard{id=30000+i,position=i==0?combat.Position:new V2(70+i%4*120,350+i/4*200),radius=45,delay=1.2f});
            visualBudget.Mode=1;RenderBattle();
            for(int i=0;i<8;i++)particles.Burst(new V2(75+i%4*130,350+i/4*250),20,Ui.Gold,100,5);
            yield return Capture("10-dense-rich");
            float hp=combat.Hp,elapsed=combat.Elapsed;int kills=combat.Kills;
            visualBudget.Mode=0;RenderBattle();
            if(particles.Count!=48)smokeErrors.Add("Live particle population was not trimmed in automatic mode");
            if(!visualBudget.Compact||enemyViews.Count!=120||bulletViews.Count!=180||hazardViews.Count!=8)smokeErrors.Add("Automatic budget removed combat actors or hazards");
            if(identityLayer.GetSiblingIndex()<=effectLayer.GetSiblingIndex()||hazardLayer.GetSiblingIndex()<=identityLayer.GetSiblingIndex())smokeErrors.Add("Appearance / warning layer order incorrect");
            yield return Capture("11-dense-auto");
            var costs=new List<double>();
            for(int i=0;i<90;i++)
            {
                var watch=System.Diagnostics.Stopwatch.StartNew();RenderBattle();watch.Stop();costs.Add(watch.Elapsed.TotalMilliseconds);yield return null;
            }
            costs.Sort();File.WriteAllText(Path.Combine(smokeOutput,"render-cpu.txt"),"macOS presentation only; paused deterministic fixture, 120 enemies / 180 bullets / 8 hazards. Not Android GPU/frame-time evidence.\nmedian_ms="+costs[45]+" p95_ms="+costs[85]);
            if(combat.Hp!=hp||combat.Elapsed!=elapsed||combat.Kills!=kills||combat.Enemies.Count!=120||combat.Bullets.Count!=180)smokeErrors.Add("Presentation changed combat simulation");
            for(int type=0;type<3;type++)
            {
                var first=TakeEffect(type);ReturnEffect(new FloatingEffect{root=first,pooledType=type});
                var reused=TakeEffect(type);if(first!=reused)smokeErrors.Add("Effect object was not reused: "+type);
                ReturnEffect(new FloatingEffect{root=reused,pooledType=type});
            }
            PauseBattle();yield return Capture("12-pause");BattleDisplayOptions();yield return Capture("13-battle-options");
            WriteClarityReport();
        }
        void WriteClarityReport()
        {
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Battle clarity runtime errors: "+smokeErrors.Count+"\nResolution: "+Screen.width+"x"+Screen.height+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("BATTLE_CLARITY_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
