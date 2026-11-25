using System;
using NUnit.Framework;
using ScrapGuard.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ScrapGuard.Tests
{
    public class LocalizationTests
    {
        string prior;
        [SetUp] public void Setup() { prior = Localization.Preference; Localization.SetLanguage("en", false); }
        [TearDown] public void Cleanup() { Localization.SetLanguage(prior, false); }
        [Test] public void DeviceLanguageHasPredictableFallback()
        {
            Assert.IsFalse(Localization.IsEnglish("system", SystemLanguage.Korean));
            Assert.IsTrue(Localization.IsEnglish("system", SystemLanguage.Japanese));
            Assert.IsFalse(Localization.IsEnglish("ko", SystemLanguage.English));
            Assert.IsTrue(Localization.IsEnglish("en", SystemLanguage.Korean));
        }
        [Test] public void CatalogCoversCampaignRobotsSkillsAndCommerce()
        {
            Assert.Greater(Localization.EntryCount, 2200);
            foreach (var name in Campaign.Regions) Assert.IsFalse(Localization.ContainsKorean(Localization.Text(name)), name);
            for (int i = 0; i < Campaign.Count; i++)
            {
                var stage = Campaign.Get(i);
                foreach (var s in new[] { stage.Name, stage.Boss, stage.Objective })
                    Assert.IsFalse(Localization.ContainsKorean(Localization.Text(s)), s);
            }
            for (int id = 1; id <= RobotAssembly.Count; id++)
            {
                Assert.IsFalse(Localization.ContainsKorean(Localization.Text(RobotAssembly.Name(id))));
                Assert.IsFalse(Localization.ContainsKorean(Localization.Text(RobotAssembly.Detail(id))));
            }
            foreach (var offer in CommerceCatalog.Offers) Assert.IsFalse(Localization.ContainsKorean(Localization.Text(offer.Name)));
            for(int i=1;i<=Outfits.Count;i++)
            {
                Assert.IsFalse(Localization.ContainsKorean(Localization.Text(Outfits.Details[i].Replace(" · ","\n"))));
                Assert.IsFalse(Localization.ContainsKorean(Localization.Text(Outfits.HeroName(i))));
            }
        }
        [Test] public void LongestPhrasesAndDynamicNumbersRemainAccurate()
        {
            Assert.AreEqual("100 Gems", Localization.Text("보석 100"));
            Assert.AreEqual("Gold 4500 · Parts 40", Localization.Text("골드 4500 · 부품 40"));
            Assert.AreEqual("Operation 02", Localization.Text("작전 02"));
            Assert.AreEqual("C", Localization.Text("포"));
            Assert.AreEqual("Lifetime Ad Skip", Localization.Text("영구 광고 스킵"));
            Assert.AreEqual("Pickup radius +25 → Pickup radius +28", Localization.Text("회수 반경 +25 → 회수 반경 +28"));
            Assert.AreEqual("Lv.1 Luke the Mechanic",Localization.Text("Lv.1 정비사 루크"));
            Assert.IsFalse(Localization.ContainsKorean(Localization.Text("05  정비소 전력 복구")));
            Assert.IsFalse(Localization.ContainsKorean(Localization.Text("보석 1,200개 확정 지급\n무료 보석과 사용 효력이 같아요.\n캐릭터·의상·장비·부품을 선택해 구매해요.")));
        }
        [Test] public void UserNamesAndExternalPricesAreNeverTranslated()
        {
            string name = "골드 수호대", price = "₩2,200";
            Assert.AreEqual("Selected: " + name + " · " + price,
                Localization.Text("선택: " + Localization.Verbatim(name) + " · " + Localization.Verbatim(price)));
            Localization.SetLanguage("ko", false);
            Assert.AreEqual(name, Localization.Text(Localization.Verbatim(name)));
            Assert.AreEqual("보석 100", Localization.Text("보석 100"));
        }
        [Test] public void UnknownFutureMessagesRemainIntact()
        {
            const string future="장비: 새로 추가된 안내문입니다";
            Assert.AreEqual(future,Localization.Text(future));
        }
        [Test] public void LanguageSwitchDoesNotMutateProgressOrPersistTestOverrides()
        {
            var state = GameState.New(); string before = JsonUtility.ToJson(state);
            string pref = PlayerPrefs.GetString(Localization.PreferenceKey, "missing");
            Localization.SetLanguage("ko", false); Localization.SetLanguage("en", false);
            Assert.AreEqual(before, JsonUtility.ToJson(state));
            Assert.AreEqual(pref, PlayerPrefs.GetString(Localization.PreferenceKey, "missing"));
            Assert.AreEqual("보석 300개", CommerceCatalog.Offers[0].Name);
        }
        [Test] public void LiveTextUpdatesAndLanguageSwitchWorkWithoutPolling()
        {
            var go = new GameObject("localized test", typeof(RectTransform));
            try
            {
                var text = go.AddComponent<LocalizedText>(); text.text = "상점";
                Assert.AreEqual("Shop", text.text);
                Localization.SetLanguage("ko", false); Assert.AreEqual("상점", text.text);
                text.text = "합체 준비 12초";
                Localization.SetLanguage("en", false); Assert.AreEqual("Robot ready in 12s", text.text);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void InputFieldsPreserveUserTypedKorean()
        {
            var go = new GameObject("input test", typeof(RectTransform));
            try
            {
                var field = Ui.Input(go.transform, "골드", "닉네임", 0, 0, 300);
                Assert.IsFalse(field.textComponent is LocalizedText);
                Assert.AreEqual("골드", field.text);
                Localization.SetLanguage("ko", false); Assert.AreEqual("골드", field.text);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
