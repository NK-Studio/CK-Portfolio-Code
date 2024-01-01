using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI
{
    [ExecuteAlways]
    public class SpeechBubbleTriangleUI : MonoBehaviour
    {
        public Camera Camera;
        public RectTransform Pivot;
        public GameObject WorldTarget;
        public float AngleOffsetInDegrees = 0f;
        public float AngleMultiplier = 1f;
        
        private void Update()
        {
            if (!Camera)
            {
                Camera = Camera.main;
            }
            
            if(!WorldTarget) 
            {
                return;   
            }

            if (!Pivot)
            {
                return;
            }

            var fromSS = Pivot.position;
            var toWS = WorldTarget.transform.position;
            var toSS = Camera.WorldToScreenPoint(toWS);

            var directionSS = (toSS - fromSS).normalized;
            var angleInDegrees = Mathf.Atan2(directionSS.y, directionSS.x) * Mathf.Rad2Deg;
            Pivot.rotation = Quaternion.Euler(0f, 0f, angleInDegrees * AngleMultiplier + AngleOffsetInDegrees);
        }
    }
}