using System;
using Character.Presenter;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI
{
    [ExecuteAlways]
    public class ObjectFollowUI : MonoBehaviour
    {
        public enum UpdateMethod
        {
            Update,
            LateUpdate,
        }

        [BoxGroup("설정"), LabelText("추적 기준")]
        public UpdateMethod Method = UpdateMethod.LateUpdate;
        [FormerlySerializedAs("Target")] [BoxGroup("설정"), LabelText("UI")]
        public RectTransform TargetUI;
        [BoxGroup("설정"), LabelText("월드 오프셋")]
        public Vector3 OffsetInWorldSpace = Vector3.up;
        [BoxGroup("설정"), LabelText("로컬 오프셋")]
        public Vector3 OffsetInLocalSpace = Vector3.zero;

        [SerializeField, BoxGroup("원근"), LabelText("사용 여부")]
        public bool UsePerspective;
        [SerializeField, BoxGroup("원근"), MinMaxSlider(1f, 100f), EnableIf("UsePerspective"), LabelText("거리 범위")] 
        public Vector2 PerspectiveDepthRange = new(1f, 50f);
        [SerializeField, BoxGroup("원근"), MinMaxSlider(0f, 2f), EnableIf("UsePerspective"), LabelText("Scale값")] 
        public Vector2 PerspectiveScaleRange = new(0.3f, 1f);
        
        [SerializeField]
        public Transform TargetObject;
        
        private Vector3 _minScaleVector;
        private Vector3 _maxScaleVector;
        
        private Camera _camera;

        private void Awake()
        {
            TargetUI ??= GetComponent<RectTransform>();
            _camera = Camera.main;
            
            _minScaleVector = Vector3.one * PerspectiveScaleRange.x;
            _maxScaleVector = Vector3.one * PerspectiveScaleRange.y;
        }

        private void Update()
        {
            if(Method != UpdateMethod.Update) return;
            UpdatePosition();
        }

        private void LateUpdate()
        {
            if(Method != UpdateMethod.LateUpdate) return;
            UpdatePosition();
        }

        /// <summary>
        /// UI의 위치를 갱신합니다.
        /// </summary>
        private void UpdatePosition()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorUtility.IsPersistent(gameObject))
            {
                return;
            }
            if (!TargetObject)
            {
                return;
            }
            if (!_camera)
            {
                _camera = Camera.main;
            }

            if (!TargetUI)
            {
                TargetUI = GetComponent<RectTransform>();
            }
#endif
            
            Vector3 worldPosition = TargetObject.transform.TransformPoint(OffsetInLocalSpace) + OffsetInWorldSpace;

            Transform cameraTransform = _camera.transform;
            float depth = Vector3.Dot(cameraTransform.forward, worldPosition - cameraTransform.position);

            // 뒤에 있으면 표시하지 않음
            if (depth < 0)
            {
                TargetUI.gameObject.SetActive(false);
                return;
            }
            TargetUI.gameObject.SetActive(true);

            if (UsePerspective)
            {
                float normalizedDepth = Mathf.InverseLerp(
                    PerspectiveDepthRange.x, 
                    PerspectiveDepthRange.y, 
                    depth
                );
                TargetUI.localScale = Vector3.Lerp(_minScaleVector, _maxScaleVector, normalizedDepth);
            }

            Vector3 screenPoint = _camera.WorldToScreenPoint(worldPosition);
            TargetUI.position = screenPoint;
        }
        
    }
}