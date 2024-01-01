using HorizonBasedAmbientOcclusion.Universal;
using Managers;
using ManagerX;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;


namespace Option
{
    public class HBAOSystem : MonoBehaviour
    {
        private Volume postProcessingVolume;
        private HBAO _hbao;

        private void Start()
        {
            if (TryGetComponent(out postProcessingVolume))
                postProcessingVolume.profile.TryGet(out _hbao);
            
            AutoManager.Get<DataManager>()
                .HBAOEnable.ObserveEveryValueChanged(hbao => hbao.Value)
                .Subscribe(value => {
                    switch (value)
                    {
                        case true:
                            SetActiveHBAO(true);
                            break;
                        case false:
                            SetActiveHBAO(false);
                            break;
                    }
                }).AddTo(this);
        }
        
        /// <summary>
        /// HBAO를 활성화 또는 비활성화 합니다.
        /// </summary>
        /// <param name="active">true시 활성화,false시 비활성화 합니다.</param>
        private void SetActiveHBAO(bool active)
        {
            if (_hbao) _hbao.EnableHBAO(active);
        }
    }
}
