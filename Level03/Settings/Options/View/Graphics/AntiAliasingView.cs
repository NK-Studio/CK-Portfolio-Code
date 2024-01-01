using Managers;
using ManagerX;
using UnityEngine;
using UnityEngine.Localization;

namespace Option
{
    public class AntiAliasingView : DefaultListView
    {
        public LocalizedString FXAA;
        public LocalizedString SMAALow;
        public LocalizedString SMAAMedium;
        public LocalizedString SMAAHigh;

        public override void Refresh(int antiAliasingIndex)
        {
            LocalizedString antiAliasingText;
            switch (antiAliasingIndex)
            {
                case 0:
                    antiAliasingText = FXAA;
                    break;
                case 1:
                    antiAliasingText = SMAALow;
                    break;
                case 2:
                    antiAliasingText = SMAAMedium;
                    break;
                default:
                    antiAliasingText = SMAAHigh;
                    break;
            }

            base.Refresh(antiAliasingText, antiAliasingIndex);
        }

        public void Apply(int antiAliasingIndex)
        {
            AutoManager.Get<DataManager>().AntiAliasingIndex.Value = antiAliasingIndex;
            PlayerPrefs.SetInt("AntiAliasingQuality", antiAliasingIndex);
        }
    }
}
