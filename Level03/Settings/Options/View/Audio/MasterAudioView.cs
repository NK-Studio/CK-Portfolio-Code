using System;
using UnityEngine;

namespace Option
{
    public class MasterAudioView : DefaultSliderView
    {
        public override void Refresh(float value)
        {
            base.Refresh(value);
            PlayerPrefs.SetFloat("MasterVolume", value);
        }
    }
}
