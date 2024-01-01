using System;
using UnityEngine;

namespace Option
{
    public class AmbientAudioView : DefaultSliderView
    {
        public override void Refresh(float value)
        {
            base.Refresh(value);
            PlayerPrefs.SetFloat("AmbientVolume", value);
        }
    }
}
