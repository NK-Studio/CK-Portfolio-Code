using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Utility;

namespace Effect
{
    public class DirectionIndicator : MonoBehaviour
    {
        [LabelText("길이 배수"), Tooltip("(1 / 자식 Quad의 스케일)을 적용합니다. 예를 들어 크기가 절반(0.5)인 경우 여기에는 2를 적습니다.")]
        public float Multiplier = 1f;
        [LabelText("오프셋 변화 속도"), Tooltip("자식 Quad의 y축 Offset이 흐를 속도를 정합니다.")]
        public float OffsetSpeed = 5f;
        public Renderer Renderer;

        [SerializeField, ReadOnly]
        private Material _material;
        
        private void Start()
        {
            _material = Renderer.material;
        }

        private void OnEnable()
        {
            _lastPosition = transform.position;
        }

        private void Update()
        {
            AddOffset(OffsetSpeed * Time.deltaTime);
        }

        private Vector3 _lastPosition;
        private void LateUpdate()
        {
            var t = transform;
            var currentPosition = t.position;
            var deltaMove = (currentPosition - _lastPosition);
            var dot = Vector3.Dot(deltaMove, t.forward);
            AddOffset(-dot * Multiplier);
            
            _lastPosition = currentPosition;
        }

        private void AddOffset(float offset)
        {
            float y = _material.mainTextureOffset.y;
            y -= offset;
            if (Mathf.Abs(y) > 1f) y %= 1f;
            _material.mainTextureOffset = _material.mainTextureOffset.Copy(y: y);
        }

        [PropertyRange(0f, 20f)]
        public float Length
        {
            get => transform.localScale.z;
            set
            {
                var t = transform;
                var scale = t.localScale = t.localScale.Copy(z: value * Multiplier);
                _material.mainTextureScale = new Vector2(scale.x, scale.z);
            }
        }
    }
}