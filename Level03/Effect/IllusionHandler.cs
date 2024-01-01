using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Effect
{
    public class IllusionHandler : MonoBehaviour
    {
        [SerializeField]
        private List<ParticleSystem> _loopDisableParticles;
        [SerializeField]
        private List<Light> _lightObjects;

        [SerializeField] 
        private float _lightIntensityDuration = 2f;
        [SerializeField] 
        private AnimationCurve _lightIntensityCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        
        
        public void Execute()
        {
            foreach (var ps in _loopDisableParticles)
            {
                var main = ps.main;
                main.loop = false;
            }
            ExecuteSequence().Forget();
        }

        private async UniTaskVoid ExecuteSequence()
        {
            float intensity = _lightObjects[0].intensity;
            await DOTween.To(() => intensity, (value) =>
            {
                intensity = value;
                foreach (var o in _lightObjects)
                {
                    o.intensity = intensity;
                }
            }, 0f, _lightIntensityDuration).SetEase(_lightIntensityCurve);
        }
    }
}