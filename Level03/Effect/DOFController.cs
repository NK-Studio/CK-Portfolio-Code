using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Effect
{
    public class DOFController : MonoBehaviour
    {
        [SerializeField] private Volume _volume;

        private DepthOfField _dof;

        private void Start()
        {
            if (!_volume)
                TryGetComponent(out _volume);

            if (_volume)
                _volume.profile.TryGet(out _dof);

            if (_dof == null) 
                DebugX.LogError("Volume에 DOF가 없습니다.");
        }

        public void SetActive(bool value)
        {
            _dof.active = value;
        }

        public float FocusDistance
        {
            get => _dof.focusDistance.value;
            set => _dof.focusDistance.value = value;
        }
    }
}