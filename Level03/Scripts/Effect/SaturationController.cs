using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Utility;
using Logger = NKStudio.Logger;

namespace Effect
{
    /// <summary>
    /// Color Adjustments의 Saturation 값을 조절합니다.
    /// </summary>
    public class SaturationController : MonoBehaviour
    {
        [Header("Volume")]
        [SerializeField, Tooltip("자신을 타겟으로 할지 여부")]
        private bool selfTarget = true;

        [SerializeField, ShowIf("@selfTarget == false")]
        private Volume targetVolume;

        [Header("Saturation")]
        [SerializeField]
        private float targetSaturation = -100f;
        
        [SerializeField]
        private AnimationCurve onDeadCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public static SaturationController Instance { get; private set; }

        private ColorAdjustments _colorAdjustments;
        private float _initialSaturation;

        private void Start()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Logger.Log("이미 SaturationController가 존재합니다.");
                Destroy(gameObject);
            }

            if (selfTarget)
            {
                if (TryGetComponent(out targetVolume))
                    targetVolume.profile.TryGet(out _colorAdjustments);
            }

            if (targetVolume)
                _initialSaturation = _colorAdjustments.saturation.value;
            else
                Logger.LogError("Volume이 없습니다.");
        }

        public void OnDead() => DeadSequence().Forget();

        private async UniTaskVoid DeadSequence()
        {
            var length = onDeadCurve.GetLength();
            var t = 0f;
            while (t < length)
            {
                var weight = onDeadCurve.Evaluate(t);
                var saturation = Mathf.Lerp(_initialSaturation, targetSaturation, weight);
                _colorAdjustments.saturation.value = saturation;

                await UniTask.Yield(PlayerLoopTiming.Update);
                t += Time.deltaTime;
            }

            _colorAdjustments.saturation.value = targetSaturation;
        }
    }
}
