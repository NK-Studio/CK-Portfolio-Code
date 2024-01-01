using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace Utility
{
    public class FallChecker : MonoBehaviour
    {
        public static bool GlobalFallCheckerEnabled = false;
        public LayerMask Mask;
        [Tag]
        public string TargetTag = "FallingGround";
        public float MaxDistance = 5f;
        public UnityEvent OnFallEvent;
        public bool FallWhenRaycastFailed = false;

        private void Awake()
        {
            GlobalFallCheckerEnabled = false;
        }

        private void FixedUpdate()
        {
            if(!GlobalFallCheckerEnabled) return;
            
            var ray = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);
            if (!Physics.Raycast(ray, out var hitInfo, MaxDistance, Mask))
            {
                if(FallWhenRaycastFailed) {
                    Debug.Log($"<color=magenta>{name}::FallChecker Fall() by RAYCAST FAILED</color>", gameObject);
                    Fall();
                }
                return;
            }

            if (hitInfo.collider.CompareTag(TargetTag))
            {       
                Debug.Log($"<color=cyan>{name}::FallChecker Fall() by RAYCASTED TAGGED OBJECT</color>", gameObject);
                Fall();
            }
        }

        private void Fall()
        {
            OnFallEvent.Invoke();
        }
    }
}