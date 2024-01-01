using System;
using Sirenix.OdinInspector;

namespace Utility
{
    [Serializable, InlineProperty]
    public struct FloatRange
    {
        [HorizontalGroup(Width = 0.5f), LabelWidth(30f)]
        public float Min;
        [HorizontalGroup(Width = 0.5f), LabelWidth(30f)]
        public float Max;

        public FloatRange(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Random() => UnityEngine.Random.Range(Min, Max);
    }
}