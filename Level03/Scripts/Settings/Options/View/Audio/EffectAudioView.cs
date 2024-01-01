using System;
using UnityEngine;

namespace Option
{
    public class EffectAudioView : DefaultSliderView
    {
        public override void Refresh(float value)
        {
            base.Refresh(value);
            PlayerPrefs.SetFloat("EffectVolume", value);
        }
    }
}
