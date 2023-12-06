using System;
using Enemys.WolfBoss;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HP {
    
    [RequireComponent(typeof(RectTransform))]
    public class BossHPUISpriteSwap : MonoBehaviour {
        [Title("원근감 적용")] 
        [SerializeField] private float _minScale = 0.3f; 
        [SerializeField] private float _maxScale = 0.75f; 
        [SerializeField] private float _minDepth = 1; 
        [SerializeField] private float _maxDepth = 50;

        private Vector3 _minScaleVector;
        private Vector3 _maxScaleVector;
        
        [Title("오브젝트 연결")]
        [SerializeField] private Image _image;
        [SerializeField] private Sprite[] _sprites;

        private RectTransform _panel;
        private WolfBoss _boss;
        private float _lastHP;
        private Camera _camera;
        private void Start() {
            _panel = GetComponent<RectTransform>();
            _camera = Camera.main;
            _boss = FindObjectOfType<WolfBoss>();
            if (!_boss) {
                DebugX.Log("BossHPUI::보스를 찾을 수 없습니다");
                _panel.gameObject.SetActive(false);
                return;
            }

            _minScaleVector = new Vector3(_minScale, _minScale, _minScale);
            _maxScaleVector = new Vector3(_maxScale, _maxScale, _maxScale);
            
            _lastHP = _boss.HP;
            UpdateHP();
        }

        private void LateUpdate() {
            if(!_boss) return;
            UpdatePosition();
            if(_lastHP != _boss.HP) {
                _lastHP = _boss.HP;
                UpdateHP();
            }
        }

        private void UpdatePosition() {
            // 뒤에 있으면 표시하지 않음
            var worldPosition = _boss.BossHPUIPosition.position;
            var cameraTransform = _camera.transform;
            var depth = Vector3.Dot(cameraTransform.forward, worldPosition - cameraTransform.position);
            if (depth < 0) {
                _image.gameObject.SetActive(false);
                return;
            }

            float normalizedDepth = Mathf.InverseLerp(_minDepth, _maxDepth, depth);
            _panel.localScale = Vector3.Lerp(_minScaleVector, _maxScaleVector, normalizedDepth);
            
            if(!_image.gameObject.activeSelf) {
                _image.gameObject.SetActive(true);
            }
            var screenPoint = _camera.WorldToScreenPoint(worldPosition);
            _panel.position = screenPoint;
        }

        private void UpdateHP() {
            float hp = _lastHP;
            int index = (int)hp;
            // DebugX.Log($"UpdateHP({hp}, (int)hp={(int)hp})");
            if (index < 0 || index >= _sprites.Length) {
                DebugX.LogWarning($"HPUI::index({index}) out of range(0, {_sprites.Length-1})");
                return;
            }
            _image.sprite = _sprites[index];
        }
    }
}