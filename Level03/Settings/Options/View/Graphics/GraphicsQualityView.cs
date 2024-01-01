using UnityEngine;
using UnityEngine.Localization;

namespace Option
{
    public class GraphicsQualityView : DefaultListView
    {
        public LocalizedString Ultra;
        public LocalizedString VeryHigh;
        public LocalizedString High;
        public LocalizedString Medium;
        public LocalizedString Low;
        public LocalizedString VeryLow;

        public override void Refresh(int graphicsQualityIndex)
        {
            LocalizedString graphicsQualityText;

            switch (graphicsQualityIndex)
            {
                case 0:
                    graphicsQualityText = VeryLow;
                    break;
                case 1:
                    graphicsQualityText = Low;
                    break;
                default:
                    graphicsQualityText = Medium;
                    break;
                case 3:
                    graphicsQualityText = High;
                    break;
                case 4:
                    graphicsQualityText = VeryHigh;
                    break;
                case 5:
                    graphicsQualityText = Ultra;
                    break;

            }

            base.Refresh(graphicsQualityText, graphicsQualityIndex);
        }
        
        /// <summary>
        /// 그래픽 품질 프리셋을 적용합니다.
        /// </summary>
        /// <param name="graphicsQualityIndex"></param>
        public void Apply(int graphicsQualityIndex)
        {
            QualitySettings.SetQualityLevel(graphicsQualityIndex, true);
            PlayerPrefs.SetInt("GraphicsQuality", graphicsQualityIndex);
        }
    }
}
