using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utility;

namespace Effect
{
    public class ParticleTrail : MonoBehaviour
    {
        [field: SerializeField] public List<ParticleSystem> Targets { get; private set; } = new();
        [field: SerializeField] public List<TrailRenderer> TrailTargets { get; private set; } = new();

        private List<ParticleSystemRenderer> _rendererCaches = new();
        
        public AnimationCurve AlphaCurve = AnimationCurve.Linear(0f, 1f, 0.5f, 0f);
        private static readonly int Opacity = Shader.PropertyToID("_Opacity");

        private void Awake()
        {
            _rendererCaches.Clear();
            foreach (var ps in Targets)
            {
                _rendererCaches.Add(ps.GetComponent<ParticleSystemRenderer>());
            }
        }

        public void Reset()
        {
            SetColorOverLifeTime(true);
            SetAlpha(1f);
        }

        public void SetColorOverLifeTime(bool enabled)
        {
            foreach (var ps in Targets)
            {
                var module = ps.colorOverLifetime;
                module.enabled = enabled;
            }
        }

        public void SetAlpha(float alpha)
        {
            foreach (var psr in _rendererCaches)
            {
                psr.material.SetFloat(Opacity, alpha);
            }

            foreach (var trail in TrailTargets)
            {
                trail.material.SetFloat(Opacity, alpha);
            }
        }
        
        public async UniTaskVoid Execute()
        {
            SetColorOverLifeTime(false);
            float t = 0f;
            float length = AlphaCurve.GetLength();
            while (t < length)
            {
                SetAlpha(AlphaCurve.Evaluate(t));
                await UniTask.Yield(destroyCancellationToken);
                t += Time.deltaTime;
            }
            SetAlpha(AlphaCurve.Evaluate(length));
            
        }
        
    }
}