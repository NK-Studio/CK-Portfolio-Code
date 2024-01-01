using Managers;
using ManagerX;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Option
{

    public class AntiAliasingSystem : MonoBehaviour
    {
        private UniversalAdditionalCameraData _cameraData;

        private void Start()
        {
            TryGetComponent(out _cameraData);

            AutoManager.Get<DataManager>()
                .AntiAliasingIndex.ObserveEveryValueChanged(antiAliasing => antiAliasing.Value)
                .Subscribe(value => {
                    switch (value)
                    {
                        case 0:
                            ApplyFXAA();
                            break;
                        case 1:
                            ApplySMAA(AntialiasingQuality.Low);
                            break;
                        case 2:
                            ApplySMAA(AntialiasingQuality.Medium);
                            break;
                        default:
                            ApplySMAA(AntialiasingQuality.High);
                            break;
                    }
                }).AddTo(this);
        }

        /// <summary>
        /// FXAA를 적용합니다.
        /// </summary>
        private void ApplyFXAA()
        {
            _cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        }

        /// <summary>
        /// SMAA를 적용합니다.
        /// </summary>
        /// <param name="quality">퀄리티 설정</param>
        private void ApplySMAA(AntialiasingQuality quality)
        {
            _cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            _cameraData.antialiasingQuality = quality;
        }
    }
}
