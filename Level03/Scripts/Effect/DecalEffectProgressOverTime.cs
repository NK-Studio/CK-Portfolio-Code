using System;
using UnityEngine;

namespace Effect
{
    public class DecalEffectProgressOverTime : MonoBehaviour
    {
        public DecalEffect Decal;
        public AnimationCurve Curve;

        private float _time;
        private void OnEnable()
        {
            _time = 0f;
        }

        private void Update()
        {
            var t = _time;
            var value = Curve.Evaluate(t);
            Decal.Progress = value;
            _time += Time.deltaTime;
        }
    }
}