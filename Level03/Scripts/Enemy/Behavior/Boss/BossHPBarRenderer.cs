using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Utility;

namespace Enemy.Behavior.Boss
{
    public class BossHPBarRenderer : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;
        [SerializeField] private Image _front;
        [SerializeField] private Image _back;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private float _backFollowSpeed = 10f;

        [SerializeField] private Image _shieldBack;
        [SerializeField] private Image _shieldFront;

        private BossAquus _enemy;
        private IDisposable _healthUpdater;
        public void Initialize(BossAquus boss)
        {
            _enemy = boss;
            //_root.gameObject.SetActive(true);

            _healthUpdater = _enemy.HealthObservable.Subscribe(OnUpdateHealth);
            _enemy.OnDeadEvent.AddListener(OnTargetDead);
        }

        private void OnUpdateHealth(float newHealth)
        {
            var current = Mathf.CeilToInt(_enemy.Health - _enemy.StackedDamage);
            var max = Mathf.Floor(_enemy.MaximumHealth);

            _text.text = $"{current} / {max}";
        }

        private void OnTargetDead(Monster m)
        {
            if (_enemy.IsNotNull())
            {
                _enemy?.OnDeadEvent?.RemoveListener(OnTargetDead);
            }
            _enemy = null;

            _healthUpdater?.Dispose();
            _healthUpdater = null;
            
            //_root.gameObject.SetActive(false);
            // UIRenderer.Instance.HideUI((int)UIObjectType.BossHP);
        }
        
        private void Update()
        {
            if(!_enemy) return;

            _root.gameObject.SetActive(_enemy.gameObject.activeSelf);

            var maxHealth = _enemy.MaximumHealth;
            float front = (_enemy.Health - _enemy.StackedDamage) / maxHealth;
            float back = _enemy.Health / maxHealth;

            _front.fillAmount = Mathf.Clamp01(front);
            _back.fillAmount = Mathf.Lerp(_back.fillAmount, back, _backFollowSpeed * Time.deltaTime);

            float shieldFront = (_enemy.Shield / _enemy.BossSettings.MaxShield);
            _shieldFront.fillAmount = shieldFront;

            // var frontAnchorMax = _front.anchorMax;
            // frontAnchorMax.x = Mathf.Clamp01(front);
            // _front.anchorMax = frontAnchorMax;

            // var backAnchorMax = _back.anchorMax;
            // backAnchorMax.x = Mathf.Lerp(backAnchorMax.x, Mathf.Clamp01(back), _backFollowSpeed * Time.deltaTime);
            // _back.anchorMax = backAnchorMax;
        }
    }
}