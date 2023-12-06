using Animation;
using Character.Model;
using Sirenix.OdinInspector;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Utility;

namespace UI.Text
{
    public class Text3DAlphaTrigger : MonoBehaviour
    {
        private enum Text3DAlphaTriggerType
        {
            FadeIn,
            FadeOut
        }

        [SerializeField, ReadOnly] private BoxCollider TriggerArea;
        [SerializeField, ReadOnly] private TMP_Text Text3D;

        public bool StartShow;

        [Title("Show")] public float ShowDuration = 1f;
        public AnimationCurve ShowEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Title("Hide")] public float HideDuration = 1f;
        public AnimationCurve HideEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Title("옵션")] public bool IgnoreTimeScale = false;

        private float _alpha;
        private float _timer;
        private bool _isTrigger;
        private Text3DAlphaTriggerType _triggerType;

        private PlayerModel _playerModel;
        private PutDownInfoMessage _putDownInfoMessage;
        
        [Button("Init")]
        public void Init()
        {
            if (TryGetComponent(out PutDownInfoMessage message))
            {
                _playerModel = FindObjectOfType<PlayerModel>();
                _putDownInfoMessage = message;
            }

            if (!TriggerArea)
                TriggerArea = GetComponentInChildren<BoxCollider>();

            if (!Text3D)
                Text3D = GetComponentInChildren<TMP_Text>();
        }

        private void Awake() => Init();

        private void Start()
        {
            _alpha = StartShow ? 1f : 0f;
            Text3D.alpha = _alpha;

            TriggerArea.OnTriggerEnterAsObservable()
                .Where(other => other.CompareTag("Player"))
                .Subscribe(_ =>
                {

                    //만약 내려놓기 메세지의 경우, 캐치 상태가 아니라면 금지한다.
                    if (_putDownInfoMessage)
                        if (_playerModel.OtherState != EOtherState.Catch)
                            return;

                    _isTrigger = true;
                    _timer = 0f;
                    _triggerType = Text3DAlphaTriggerType.FadeIn;
                }).AddTo(this);

            TriggerArea.OnTriggerExitAsObservable()
                .Where(other => other.CompareTag("Player"))
                .Subscribe(_ =>
                {
                    _timer = 0f;
                    _triggerType = Text3DAlphaTriggerType.FadeOut;
                }).AddTo(this);
        }

        private void Update()
        {
            Core();
        }

        private void Core()
        {
            if (!_isTrigger) return;

            if (IgnoreTimeScale)
                _timer += Time.unscaledDeltaTime;
            else
                _timer += Time.deltaTime;

            switch (_triggerType)
            {
                case Text3DAlphaTriggerType.FadeIn:
                {
                    float percentage = _timer / ShowDuration;
                    Text3D.alpha = Mathf.Lerp(Text3D.alpha, 1, ShowEase.Evaluate(percentage));
                    break;
                }
                case Text3DAlphaTriggerType.FadeOut:
                {
                    float percentage = _timer / HideDuration;
                    Text3D.alpha = Mathf.Lerp(Text3D.alpha, 0, HideEase.Evaluate(percentage));
                    break;
                }
            }
        }
    }
}