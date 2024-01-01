using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Option
{
    public class LanguageView : DefaultListView
    {
        public LocalizedString English;
        public LocalizedString Korean;

        public override void Refresh(int languageIndex)
        {
            var languageText = languageIndex == 0 ? English : Korean;
            base.Refresh(languageText, languageIndex);
        }

        /// <summary>
        /// 언어를 적용합니다.
        /// </summary>
        /// <param name="languageIndex"></param>
        public void Apply(int languageIndex)
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[languageIndex];
            PlayerPrefs.SetInt("Language", languageIndex);
        }
    }
}
