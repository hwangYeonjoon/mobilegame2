using UnityEngine.UI;

namespace ScrapGuard
{
    // Covers labels created once as well as .text updates in combat, toasts and callbacks.
    public sealed class LocalizedText : Text
    {
        string source = "";
        int designSize;
        public string Source => source;
        public override string text
        {
            get => base.text;
            set { source = value ?? ""; RefreshLanguage(); }
        }
        public void ConfigureLocalization(int size) { designSize = size; RefreshLanguage(); }
        protected override void OnEnable() { base.OnEnable(); Localization.Changed += RefreshLanguage; RefreshLanguage(); }
        protected override void OnDisable() { Localization.Changed -= RefreshLanguage; base.OnDisable(); }
        void RefreshLanguage()
        {
            if (designSize > 0)
            {
                resizeTextForBestFit = Localization.English;
                verticalOverflow = Localization.English ? UnityEngine.VerticalWrapMode.Truncate : UnityEngine.VerticalWrapMode.Overflow;
                resizeTextMaxSize = designSize;
                resizeTextMinSize = System.Math.Min(designSize, System.Math.Max(11, designSize - 6));
            }
            base.text = Localization.Text(source);
        }
    }
}
