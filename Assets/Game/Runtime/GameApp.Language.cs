using UnityEngine;

namespace ScrapGuard
{
    public partial class GameApp
    {
        void LanguagePanel()
        {
            modal.SetAsLastSibling();
            var card = Dialog("Language / 언어", "한국어 또는 영어를 선택하세요. 이 기기에만 저장됩니다.", 450);
            string[] values = { "system", "ko", "en" };
            string[] labels = { "기기 언어 사용", Localization.Verbatim("한국어"), "English" };
            for (int i = 0; i < values.Length; i++)
            {
                string language = values[i];
                Ui.Button(card, labels[i], 24, 138 + i * 82, 444, 62, () =>
                {
                    Localization.SetLanguage(language);
                    LanguagePanel();
                }, Localization.Preference == language ? Ui.Mint : Ui.Raised, true, 21);
            }
            Ui.Label(card, "언어를 바꿔도 진행 상황과 구매 내역은 유지됩니다.", 24, 387, 444, 40, 16, Ui.Muted);
        }
    }
}
