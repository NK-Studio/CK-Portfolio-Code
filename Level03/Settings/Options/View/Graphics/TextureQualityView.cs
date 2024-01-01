using Managers;
using ManagerX;
using UnityEngine;
using UnityEngine.Localization;

namespace Option
{
    public class TextureQualityView : DefaultListView
    {
        public LocalizedString Full;
        public LocalizedString Half;
        public LocalizedString Quarter;
        public LocalizedString Eighth;

        public override void Refresh(int textureQualityIndex)
        {
            switch (textureQualityIndex)
            {
                case 0:
                    base.Refresh(Eighth, textureQualityIndex);
                    break;
                case 1:
                    base.Refresh(Quarter, textureQualityIndex);
                    break;
                case 2:
                    base.Refresh(Half, textureQualityIndex);
                    break;
                default:
                    base.Refresh(Full, textureQualityIndex);
                    break;
            }
        }

        public void Apply(int textureQualityIndex)
        {
            AutoManager.Get<GameManager>().ChangeTextureMipmapQuality(textureQualityIndex);
            PlayerPrefs.SetInt("TextureQuality", textureQualityIndex);
        }
    }
}
