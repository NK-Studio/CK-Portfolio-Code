using Cysharp.Threading.Tasks;
using Enemy.Behavior;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using Utility;

namespace Enemy.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class EnemyHUD : MonoBehaviour
    {
        [Title("원근감 적용")] [SerializeField] private float _minScale = 0.3f;
        [SerializeField] private float _maxScale = 0.75f;
        [SerializeField] private float _minDepth = 1;
        [SerializeField] private float _maxDepth = 50;

        public float Height => _enemy.Settings.Height;
        
        [Title("타겟 UI")] 
        [SerializeField] private Image _front;
        [SerializeField] private Image _back;
        [SerializeField] private RectTransform _border;
        [SerializeField] private CanvasGroup _borderAlpha;
        [SerializeField] private AnimationCurve _enableAlphaOnDamaged;
        [SerializeField] private AnimationCurve _widthAnimationOnDead;
        [SerializeField] private AnimationCurve _alphaAnimationOnDead;
        [SerializeField] private float _backFollowSpeed = 10f;

        [Title("타겟")] public Transform Follow;

        private Vector3 _minScaleVector;
        private Vector3 _maxScaleVector;
        private float _widthSize;

        private GameObject _uiObject;
        private RectTransform _panel;
        private Monster _enemy;
        private Camera _camera;

        private IObjectPool<EnemyHUD> _managedPool;
        private bool _isDead;

        private void Awake()
        {
            _panel = GetComponent<RectTransform>();
            _uiObject = transform.GetChild(0).gameObject;
            _camera = Camera.main;
            // _widthSize = _front.sizeDelta.x;
            
            _minScaleVector = new Vector3(_minScale, _minScale, _minScale);
            _maxScaleVector = new Vector3(_maxScale, _maxScale, _maxScale);
        }

        private float _lastHealth;
        private float _healthEnableTime;

        private void OnEnable()
        {
            _border.sizeDelta = new Vector2(_widthAnimationOnDead.Evaluate(0f), _border.sizeDelta.y);
            _borderAlpha.alpha = 1f;
            _isDead = false;
            _healthEnableTime = _enableAlphaOnDamaged.GetLength();
        }

        private void LateUpdate()
        {
            if (!_enemy || !_enemy.gameObject.activeSelf)
            {
                DestroyHUD();
                return;
            }
            UpdatePosition();
            SetHealth();
            CheckDead();
            UpdateAlpha();
        }

        private void UpdateAlpha()
        {
            var health = _enemy.Health - _enemy.StackedDamage;
            if (_lastHealth != health)
            {
                _lastHealth = health;
                _healthEnableTime = 0f;
            }
            // if (_healthEnableTime > _enableAlphaOnDamaged[_enableAlphaOnDamaged.length-1].time)
            // {
                // return;
            // }


            if (!_isDead)
            {
                _borderAlpha.alpha = _enableAlphaOnDamaged.Evaluate(_healthEnableTime);
                _healthEnableTime += Time.deltaTime;
            }
        }

        /// <summary>
        /// UI의 위치를 갱신합니다.
        /// </summary>
        private void UpdatePosition()
        {
            if (!Follow) return;

            // 뒤에 있으면 표시하지 않음
            Vector3 worldPosition = Follow.position + (Vector3.up * Height);

            Transform cameraTransform = _camera.transform;
            float depth = Vector3.Dot(cameraTransform.forward, worldPosition - cameraTransform.position);

            if (depth < 0)
            {
                _uiObject.SetActive(false);
                return;
            }
       
            float normalizedDepth = Mathf.InverseLerp(_minDepth, _maxDepth, depth);
            _panel.localScale = Vector3.Lerp(_minScaleVector, _maxScaleVector, normalizedDepth);

            _uiObject.SetActive(true);

            Vector3 screenPoint = _camera.WorldToScreenPoint(worldPosition);
            _panel.position = screenPoint;
        }

        /// <summary>
        /// HP값을 조절합니다.
        /// </summary>
        public void SetHealth()
        {
            var maxHealth = _enemy.Settings.Health;
            float front = (_enemy.Health - _enemy.StackedDamage) / maxHealth;
            float back = _enemy.Health / maxHealth;

            // var frontAnchorMax = _front.anchorMax;
            // frontAnchorMax.x = Mathf.Clamp01(front);
            // _front.anchorMax = frontAnchorMax;
            
            // Vector2 frontSize = _front.sizeDelta;
            // frontSize.x = _widthSize * Mathf.Clamp01(front);
            // _front.sizeDelta = frontSize;

            _front.fillAmount = Mathf.Clamp01(front);

            // var backAnchorMax = _back.anchorMax;
            // backAnchorMax.x = Mathf.Lerp(backAnchorMax.x, Mathf.Clamp01(back), _backFollowSpeed * Time.deltaTime);
            // _back.anchorMax = backAnchorMax;

            // Vector2 backSize = _back.sizeDelta;
            // backSize.x = Mathf.Lerp(backSize.x, _widthSize * Mathf.Clamp01(back), _backFollowSpeed * Time.deltaTime);
            // _back.sizeDelta = backSize;
            
            _back.fillAmount = Mathf.Lerp(_back.fillAmount, Mathf.Clamp01(back), _backFollowSpeed * Time.deltaTime);

        }

        public void CheckDead()
        {
            if(_isDead) return;
            if (_enemy.Health <= 0f)
            {
                _isDead = true;
                DeadSequence().Forget();
            }
        }
        
        /// <summary>
        /// 매니지드 풀을 설정합니다
        /// </summary>
        /// <param name="pool"></param>
        public void SetPool(IObjectPool<EnemyHUD> pool)
        {
            _managedPool = pool;
        }

        public void SetEnemy(Monster monster)
        {
            _enemy = monster;
            _lastHealth = _enemy.Health;
        }

        private async UniTaskVoid DeadSequence()
        {
            var length = Mathf.Max(
                _widthAnimationOnDead.GetLength(),    
                _alphaAnimationOnDead.GetLength()    
            );
            var time = 0f;

            var borderSize = _border.sizeDelta;
            while (time < length)
            {
                _border.sizeDelta = new Vector2(_widthAnimationOnDead.Evaluate(time), borderSize.y);
                _borderAlpha.alpha = _alphaAnimationOnDead.Evaluate(time);
                
                time += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            
            _border.sizeDelta = new Vector2(_widthAnimationOnDead.Evaluate(length), borderSize.y);
            _borderAlpha.alpha = _alphaAnimationOnDead.Evaluate(length);
            
            DestroyHUD();
        }

        /// <summary>
        /// Pool에 반환합니다.
        /// </summary>
        public void DestroyHUD()
        {
            _managedPool.Release(this);
        }
    }
}