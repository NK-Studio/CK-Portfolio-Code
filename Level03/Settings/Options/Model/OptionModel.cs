using UnityEngine;

namespace Option
{
    public class OptionModel : MonoBehaviour
    {
        [Header("Game Play")]
        public int CurrentLanguage;
        public bool CameraPanning = true;
        public int WindowMode;
        public int ScreenResolution;
        public bool VSyncEnable = true;

        [Header("Graphics")]
        public int GraphicsQuality;
        public int TextureQuality;
        public int AntiAliasingQuality;
        public bool HBAOEnable;
        
        [Header("Graphics")]
        public float MasterVolume;
        public float AmbientVolume;
        public float BackgroundVolume;
        public float EffectVolume;
        
        [Header("Control")]
        public bool GamepadVibration = true;
    }

}
