using System;
using System.Collections.Generic;
using Enemy.UI;
using Micosmo.SensorToolkit;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class CombinableOffScreenUI : MonoBehaviour, IGameObjectPooled<CombinableOffScreenUI>
    {
        [FoldoutGroup("설정", true)] 
        public RectTransform TargetUI;
        [FoldoutGroup("설정", true)] 
        public Image TargetImage;
        [FoldoutGroup("설정", true)]
        public Vector2 Padding = new(32f, 32f);
        
        [FoldoutGroup("설정/결합", true)]
        public bool UseCombine = true;
        [FoldoutGroup("설정/결합", true)]
        public RangeSensor2D CombineRange;
        
        

        [FoldoutGroup("설정/회전", true)]
        public bool UseRotation = true;
        
        [FoldoutGroup("설정/회전", true), EnableIf("UseRotation")]
        public float RotationOffset = 0f;
        
        [FoldoutGroup("설정/회전", true), EnableIf("UseRotation")]
        public RectTransform RotationTargetUI;

        
        
        [field: SerializeField, FoldoutGroup("상태", true)]
        public Transform TargetObject { get; set; }

        [field: SerializeField, FoldoutGroup("상태", true)]
        public bool IsVisible { get; set; }
        
        /// <summary>
        /// 계산된 OffScreen 위치
        /// </summary>
        [field: SerializeField, FoldoutGroup("상태", true)]
        public Vector2 ComputedScreenPosition { get; set; }

        [field: SerializeReference, FoldoutGroup("상태", true)]
        public CombinedGroup Group { get; set; }
        
        [HideInInspector]
        public bool CombineMarked; // Controller에서 조작

        public bool IsValid => TargetUI.gameObject.activeInHierarchy;

        private Camera _camera;
        private void Awake()
        {
            _camera = Camera.main;
        }

        public enum OffScreenState
        {
            Invalid,
            OnScreen,
            OffScreen,
        }

        public OffScreenState State = OffScreenState.Invalid;
        
        public virtual void UpdatePosition()
        {
            State = OffScreenState.Invalid;
            if (!TargetUI)
            {
                return;
            }
            TargetImage.gameObject.SetActive(IsVisible);
            
            // 목표가 없으면 암것도 안 함
            if (!TargetObject)
            {
                TargetUI.gameObject.SetActive(false);
                return;
            }
            TargetUI.gameObject.SetActive(true);

            // 월드 위치
            Vector3 targetPositionWS = TargetObject.position;
            // 화면상 위치
            Vector3 targetPositionSS = _camera.WorldToScreenPoint(targetPositionWS);
            if (targetPositionSS.z > 1f) targetPositionSS.z = 1f; // z값 폭주 방지
            if (targetPositionSS.z < 0f)
            {
                // 역투명된 ScreenPoint 반전
                targetPositionSS.x = -targetPositionSS.x;
                targetPositionSS.y = -targetPositionSS.y;
                targetPositionSS.z = 0f; // z값 폭주 방지
            }

            
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            bool isOffScreen = targetPositionSS.z <= 0f
                               || targetPositionSS.x < 0f
                               || targetPositionSS.x > screenWidth
                               || targetPositionSS.y < 0f
                               || targetPositionSS.y > screenHeight;

            // 화면 안에 있는 경우: 단순 비활성화
            if (!isOffScreen)
            {
                State = OffScreenState.OnScreen;
                TargetUI.gameObject.SetActive(false);
                return;
            }

            State = OffScreenState.OffScreen;
            
            // 화면 밖에 있는 경우
            Vector3 computedPositionSS = ClampPosition(targetPositionSS, Padding);

            TargetUI.position = computedPositionSS;

            if (UseRotation)
            {
                RotationTargetUI.rotation = GetRotation(computedPositionSS);       
            }

        }

        protected Quaternion GetRotation(Vector3 positionSS)
        {
            Vector3 centeredPosition = positionSS - new Vector3(Screen.width, Screen.height) * 0.5f;
            var angle = (Mathf.Atan2(centeredPosition.y, centeredPosition.x) * Mathf.Rad2Deg + 360f) % 360f;
            return Quaternion.Euler(0f, 0f, angle + RotationOffset);
        }

        protected virtual Vector3 ClampPosition(Vector3 positionSS, Vector2 padding)
        {
            float widthHalf = padding.x * 0.5f;
            float heightHalf = padding.y * 0.5f;
            float paddedWidth = widthHalf;
            float paddedHeight = heightHalf;
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            
            Vector3 computedPositionSS = positionSS;
            if (computedPositionSS.x < paddedWidth)
                computedPositionSS.x = paddedWidth;
            else if (computedPositionSS.x > screenWidth - paddedWidth)
                computedPositionSS.x = screenWidth - paddedWidth;
            
            if (computedPositionSS.y < paddedHeight)
                computedPositionSS.y = paddedHeight;
            else if (computedPositionSS.y > screenHeight - paddedHeight)
                computedPositionSS.y = screenHeight - paddedHeight;

            return computedPositionSS;
        }

        public GameObjectPool<CombinableOffScreenUI> Pool { get; set; }
    }
}