using System;
using UnityEngine;

namespace Option
{
    public class BackgroundAudioView : DefaultSliderView
    {
        public override void Refresh(float value)
        {
            base.Refresh(value);
            PlayerPrefs.SetFloat("BackgroundVolume", value);
        }
    }
}
