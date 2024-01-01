using Level;
using UnityEngine;
using UnityEngine.UI;

namespace Enemy.UI
{
    public class InteractionUI : MonoBehaviour, IGameObjectPooled<InteractionUI>
    {
        public GameObjectPool<InteractionUI> Pool { get; set; }
        public IItem Target { get; set; } = null;

        [field: SerializeField]
        private Image _panel;

        [field: SerializeField] 
        private Vector3 _worldOffset = Vector3.up;

        [field: SerializeField] 
        private Vector3 _screenOffset = Vector3.up;
        
        
        private Camera _camera;
        private void Awake()
        {
            _camera = Camera.main;
        }

        private void LateUpdate()
        {
            UpdatePosition();
        }

        /// <summary>
        /// UI의 위치를 갱신합니다.
        /// </summary>
        private void UpdatePosition()
        {
            if (Target == null) return;

            Vector3 worldPosition = Target.transform.position + _worldOffset;
            Vector3 screenPoint = _camera.WorldToScreenPoint(worldPosition) + _screenOffset;
            _panel.rectTransform.position = screenPoint;
        }

        public void Release() => Pool.Release(this);
    }
}