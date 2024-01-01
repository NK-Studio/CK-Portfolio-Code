using System;
using Sirenix.OdinInspector;
using Utility;

namespace Character.Core
{
    [Serializable]
    public class CameraShakeSettings
    {
        public FloatRange RangeX;
        public FloatRange RangeY;
        public float Multiplier;
        public float Time;

        public CameraShakeSettings() : this(
            new FloatRange(-0.2f, 0.2f), 
            new FloatRange(-0.2f, 0.2f), 
            1f,
            0.2f
        )
        {
        }

        public CameraShakeSettings(FloatRange rangeX, FloatRange rangeY, float multiplier, float time = 0.2f)
        {
            RangeX = rangeX;
            RangeY = rangeY;
            Multiplier = multiplier;
            Time = time;
        }
    }
}