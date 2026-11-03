using System.Collections;
using System.IO;
using ScrapGuard.Core;
using UnityEngine;
namespace ScrapGuard
{
    public partial class GameApp
    {
        IEnumerator CommerceSmokeRoutine()
        {
            DismissTitleScreen();CloseModal();research.Clear();research.Consent(true);
            Store.Change(e=>{e.State.testProgress=true;e.State.clearedStages.AddRange(new[]{0,1,2,3,4});e.State.selectedHero=3;e.State.heroes[3].unlocked=true;e.State.heroes[3].level=30;e.State.gems=600;return CommandResult.Ok("Isolated commerce fixtures");});
            Show("shop");yield return Capture("01-shop");GemWalletPanel();yield return Capture("00-wallet");
            foreach(var offer in CommerceCatalog.Offers){StoreOfferPanel(offer);yield return Capture("offer-"+offer.Id);}
            ConceptRoom();yield return Capture("02-concepts");ClickVisibleButton("이 콘셉트가 좋아요");
            ResearchPanel();yield return Capture("03-research");
            Show("weekly");yield return Capture("04-growth-hub");SkillTrialHub();yield return Capture("05-skill-hub");
            ClickVisibleButton("동일 조건 도전 시작");battlePaused=true;yield return Capture("06-skill-battle");
            if(combat.Hero!=0||State.selectedHero!=3)smokeErrors.Add("Fixed hero leaked into account or wrong battle hero");
            combat.Hp=combat.MaxHp=100000;
            while(combat.Elapsed<32&&!combat.Ended){if(combat.Choosing)combat.Choose(combat.Choices[0]);combat.Tick(ReplayRecorder.Step,new V2());}
            combat.Abandon();FinishBattle();yield return Capture("07-skill-result");
            if(State.weekly.Count!=0||State.skillWeekly.Count!=1)smokeErrors.Add("Skill record mixed with growth history");
            combat=null;Show("home");SkillTrialHub();yield return Capture("08-skill-record");
            var output=research.Export();if(!File.Exists(output)||research.Data.events.Count==0)smokeErrors.Add("Opt-in export missing");
            foreach(var entry in research.Data.events)if(!entry.test)smokeErrors.Add("Smoke event not marked test");
            Settings();yield return Capture("09-account-options");
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Commerce runtime errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Debug.Log("COMMERCE_SMOKE_COMPLETE errors="+smokeErrors.Count);Application.Quit(smokeErrors.Count==0?0:1);
        }
    }
}
