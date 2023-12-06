using System;
using Enemys.WolfBoss;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HP {
    
    [RequireComponent(typeof(RectTransform))]
    public class BossHPUIGauge : MonoBehaviour {
        [SerializeField] private Image _image;

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
            if (Vector3.Dot(_camera.transform.forward, worldPosition - _camera.transform.position) < 0) {
                _image.gameObject.SetActive(false);
                return;
            }
            if(!_image.gameObject.activeSelf) {
                _image.gameObject.SetActive(true);
            }
            var screenPoint = _camera.WorldToScreenPoint(worldPosition);
            _panel.position = screenPoint;
        }

        private void UpdateHP() {
            float hp = _lastHP;
            float maxHp = _boss.Settings.HPMax;
            float ratio = Mathf.Clamp01(hp / maxHp);
            _image.fillAmount = ratio;
        }
    }
}