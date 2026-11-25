using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ScrapGuard
{
    public partial class GameApp
    {
        readonly StringBuilder localizationEvidence = new StringBuilder();
        IEnumerator LocalizationSmoke()
        {
            // Isolated local profile; never purchase or mutate an online account.
            cloud.Session.enabled=false;cloud.Session.guestRegistrationPending=false;cloud.Session.localPlaySelected=true;Store=localStore;
            string preference=Localization.Preference;
            Localization.SetLanguage("en",false);
            yield return CaptureLanguage("00-title");
            string savedPreference=PlayerPrefs.GetString(Localization.PreferenceKey,"system");bool hadPreference=PlayerPrefs.HasKey(Localization.PreferenceKey);
            LanguagePanel();yield return CaptureLanguage("00-title-language");
            ClickVisibleButton(Localization.Verbatim("한국어"));
            if(Localization.English||PlayerPrefs.GetString(Localization.PreferenceKey)!="ko")smokeErrors.Add("Korean language selection did not persist");
            ClickVisibleButton("English");
            if(!Localization.English||PlayerPrefs.GetString(Localization.PreferenceKey)!="en")smokeErrors.Add("English language selection did not persist");
            if(hadPreference)PlayerPrefs.SetString(Localization.PreferenceKey,savedPreference);else PlayerPrefs.DeleteKey(Localization.PreferenceKey);PlayerPrefs.Save();
            CloseModal();yield return null;
            DismissTitleScreen();Show("home");yield return CaptureLanguage("01-home");
            foreach(string target in new[]{"growth","gear","heroes","robots","more","weekly","showcase"})
            {Show(target);yield return CaptureLanguage("02-"+target);}
            for(int i=0;i<3;i++){shopTab=i;Show("shop");yield return CaptureLanguage("03-shop-"+i);}
            foreach(var offer in CommerceCatalog.Offers){StoreOfferPanel(offer);yield return CaptureLanguage("04-"+offer.Id);}
            GrowthKitPanel();yield return CaptureLanguage("05-growth-kit");
            GrowthPassPanel();yield return CaptureLanguage("05-pass");
            Attendance();yield return CaptureLanguage("05-attendance");
            Missions();yield return CaptureLanguage("05-missions");
            AccountPanel();yield return CaptureLanguage("05-account");
            ResearchPanel();yield return CaptureLanguage("05-research");
            Settings();yield return CaptureLanguage("05-settings");
            LanguagePanel();yield return CaptureLanguage("05-language");
            string before=JsonUtility.ToJson(State);
            Localization.SetLanguage("ko",false);yield return CaptureLanguage("06-korean");
            Localization.SetLanguage("en",false);
            if(before!=JsonUtility.ToJson(State))smokeErrors.Add("Language switch mutated game progress");
            for(int i=0;i<Campaign.Regions.Length;i++){CampaignMap(i);yield return CaptureLanguage("07-map-"+i);}
            foreach(int id in new[]{1,5,9,10})foreach(int rank in new[]{1,2})
            {RobotCodexDetail(id,rank);yield return CaptureLanguage("08-robot-"+id+"-"+rank);}
            RobotPartSources(1,0,1);yield return CaptureLanguage("08-parts");
            OpenBattle(0,"localization-ui-fixture",17,false);battlePaused=true;
            RobotAssembler(1);yield return CaptureLanguage("09-assembler");
            CloseModal();battlePaused=false;
            float until=Time.realtimeSinceStartup+20;bool capturedChoice=false;
            while(!combat.Ended&&Time.realtimeSinceStartup<until)
            {
                if(combat.Choosing)
                {
                    if(!capturedChoice){yield return CaptureLanguage("09-skills");capturedChoice=true;}
                    ChooseBattleSkill(combat.Choices[0]);
                }
                yield return null;
            }
            yield return CaptureLanguage("09-battle");
            combat.Abandon();ShowResult();yield return CaptureLanguage("10-result");
            File.WriteAllText(Path.Combine(smokeOutput,"texts.txt"),localizationEvidence.ToString());
            File.WriteAllText(Path.Combine(smokeOutput,"report.txt"),"Localization errors: "+smokeErrors.Count+"\n"+string.Join("\n",smokeErrors));
            Localization.SetLanguage(preference,false);
            Debug.Log("LOCALIZATION_SMOKE_COMPLETE errors="+smokeErrors.Count);
            Application.Quit(smokeErrors.Count==0?0:1);
        }
        IEnumerator CaptureLanguage(string name)
        {
            yield return Capture(name);
            Transform scope=modal.childCount>0?(Transform)modal:titleScreen!=null&&titleScreen.gameObject.activeSelf?titleScreen:screen;
            localizationEvidence.AppendLine("\n### "+name);
            foreach(var label in scope.GetComponentsInChildren<LocalizedText>())
            {
                if(!label.isActiveAndEnabled||string.IsNullOrEmpty(label.text))continue;
                localizationEvidence.AppendLine(label.text.Replace("\n"," / "));
                if(Localization.English&&label.Source.IndexOf('\uE000')<0&&label.Source!="Language / 언어"&&Localization.ContainsKorean(label.text))
                    smokeErrors.Add(name+" untranslated: "+label.text);
                if(Localization.English&&label.font!=null)
                {
                    var settings=label.GetGenerationSettings(label.rectTransform.rect.size);
                    settings.resizeTextForBestFit=false;
                    settings.fontSize=label.cachedTextGenerator.fontSizeUsedForBestFit>0?label.cachedTextGenerator.fontSizeUsedForBestFit:label.fontSize;
                    using(var generator=new TextGenerator())
                    {
                        float required=generator.GetPreferredHeight(label.text,settings)/label.pixelsPerUnit;
                        if(required>label.rectTransform.rect.height+4)
                            smokeErrors.Add(name+" text does not fit ("+Mathf.CeilToInt(required)+"/"+label.rectTransform.rect.height+"): "+label.text);
                    }
                }
            }
        }
    }
}
