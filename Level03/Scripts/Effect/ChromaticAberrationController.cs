using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Effect
{
    public class ChromaticAberrationController : MonoBehaviour
    {
        public Volume Volume;
        private ChromaticAberration _controller;

        private void Start()
        {
            if (!Volume)
            {
                Volume = GetComponent<Volume>();

                if (!Volume)
                {
                    Debug.LogWarning($"Volume을 찾을 수 없음 !!!", gameObject);
                    return;
                }
            }

            if (!Volume.profile.TryGet(out _controller))
            {
                Debug.LogWarning($"{Volume}에 ChromaticAberration이 없음 !!!", Volume);
                return;
            }
        }

        public void SetIntensity(float value)
        {
            if(!_controller) return;
            _controller.intensity.value = value;
        }
        
        
    }
}