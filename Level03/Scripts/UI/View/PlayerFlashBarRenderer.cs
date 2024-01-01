using Character.Presenter;
using Managers;
using Settings;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Enemy.UI.View
{
    public class PlayerFlashBarRenderer : MonoBehaviour
    {
        [Title("색상")]
        [SerializeField] private Color _normalFillColor; 
        [SerializeField] private Color _normalBorderColor;
        [SerializeField] private Color _inactiveFillColor; 
        [SerializeField] private Color _inactiveBorderColor; 
        
        [Title("원근감 적용")] [SerializeField] private float _minScale = 0.3f;
        [SerializeField] private float _maxScale = 0.75f;
        [SerializeField] private float _minDepth = 1;
        [SerializeField] private float _maxDepth = 50;

        public float Height = 1.7f;
        
        [Title("타겟 UI")] 
        [SerializeField] private CanvasGroup _borderAlpha;
        [SerializeField] private AnimationCurve _disableAlphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Title("타겟")] public Transform Follow;

        private Vector3 _minScaleVector;
        private Vector3 _maxScaleVector;
        private float _widthSize;

        // private GameObject _uiObject;
        private RectTransform _panel;
        private Camera _camera;
        
        private bool _isDead;
        
        
        private PlayerPresenter _player;
        private CharacterSettings _characterSettings;
    
        public Image PlayerFlashBar;
        public Image PlayerFlashBarBorder;
        public Image PlayerFlashFaderImage;
        public CanvasGroup PlayerFlashFaderCavnasGroup;

        private void Awake()
        {
            InitUI();
        }

        private void Update()
        {
            RenderFlashBar();
        }
 
        private void InitUI()
        {
            _player = FindFirstObjectByType<PlayerPresenter>();
            _characterSettings = ManagerX.AutoManager.Get<GameManager>().Settings;
            
            _panel = GetComponent<RectTransform>();
            // _uiObject = transform.GetChild(0).gameObject;
            _camera = Camera.main;
            
            _minScaleVector = new Vector3(_minScale, _minScale, _minScale);
            _maxScaleVector = new Vector3(_maxScale, _maxScale, _maxScale);
        }

        private float _enableTime;

        
        // public AnimationCurve PlayerFlashFaderCurve;
        // private float _lastFlashBarGauge = 1f;
        
        private void LateUpdate()
        {
            float alphaMultiplier = _player.View.IsActiveGFX ? 1f : 0f;
            // if(!PlayerFlashFaderImage || !PlayerFlashFaderCavnasGroup) return;
            // if (_playerModel.FlashGauge < _lastFlashBarGauge)
            // {
                // _enableTime = 0f;
                // PlayerFlashFaderImage.fillAmount = _lastFlashBarGauge;
                // PlayerFlashFaderCavnasGroup.alpha = 1f;
                // PlayerFlashFaderCavnasGroup
                // .DOFade(0f, _playerModel.FlashCooldown)
                // .SetEase(PlayerFlashFaderCurve);
            // }
            // _lastFlashBarGauge = _playerModel.FlashGauge;
            
            // 게이지가 1보다 작으면 항상 활성화
            if (_player.Model.FlashGauge < 1f)
            {
                _enableTime = 0f;
                _borderAlpha.alpha = 1f * alphaMultiplier;
                // PlayerFlashFaderImage.fillAmount = _lastFlashBarGauge;
                // PlayerFlashFaderCavnasGroup.alpha = 1f;
                // PlayerFlashFaderCavnasGroup
                // .DOFade(0f, _playerModel.FlashCooldown)
                // .SetEase(PlayerFlashFaderCurve);
            }
            
            // 꽉 차면 사라지는 애니메이션 출력
            else
            {
                _borderAlpha.alpha = _disableAlphaCurve.Evaluate(_enableTime) * alphaMultiplier;
                _enableTime += Time.deltaTime;
            }
            UpdatePosition();
        }

        private void RenderFlashBar()
        {
            if(!PlayerFlashBar) return;

            
            var value = _player.Model.FlashGauge;

            if (value < _characterSettings.FlashUseGaugeAmount)
            {
                PlayerFlashBar.color = _inactiveFillColor;
                PlayerFlashBarBorder.color = _inactiveBorderColor;
            }
            else
            {
                PlayerFlashBar.color = _normalFillColor;
                PlayerFlashBarBorder.color = _normalBorderColor;
            }
            
            var frontAnchorMax = PlayerFlashBar.rectTransform.anchorMax;
            frontAnchorMax.x = Mathf.Clamp01(value);
            PlayerFlashBar.rectTransform.anchorMax = frontAnchorMax;
            
            // PlayerFlashBar.fillAmount = _playerModel.FlashGauge;
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
                // _uiObject.SetActive(false);
                return;
            }
       
            float normalizedDepth = Mathf.InverseLerp(_minDepth, _maxDepth, depth);
            _panel.localScale = Vector3.Lerp(_minScaleVector, _maxScaleVector, normalizedDepth);

            // _uiObject.SetActive(true);

            Vector3 screenPoint = _camera.WorldToScreenPoint(worldPosition);
            _panel.position = screenPoint;
        }
    }
}