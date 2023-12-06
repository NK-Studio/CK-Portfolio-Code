using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using ModestTree;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Platform
{
    public class SinkPlatform : MonoBehaviour
    {
        public enum SinkEaseType
        {
            Up,
            Down
        }

        [TitleGroup("이동 속도")] public float downDuration = 1f;
        [TitleGroup("이동 속도")] public float upDuration = 1f;

        [TitleGroup("딜레이"), Tooltip("값이 작을 수록 발판의 내구도가 작아집니다.")]
        public float downTime = 1f;

        [TitleGroup("딜레이"), Tooltip("값이 작을 수록 다시 올라오는 쿨타임 간격이 짧아집니다.")]
        public float risingDelayTime = 0.5f;
        
        [TitleGroup("이동 느낌"), SerializeField] private Ease downEase = Ease.Linear;
        [TitleGroup("이동 느낌"), SerializeField] private Ease upEase = Ease.Linear;
        private Ease _targetEase;

        [SerializeField, ValidateInput("@downPosition != null", "Down Position 트랜스폼이 비어있습니다.")]
        private Transform downPosition;
        
        //초기 위치
        private Vector3 _initialPosition;
        private float _risingDelay;
        private float _downCount;
        private bool _isDown;
        private float _duration;

        //첨부된 구성 요소에 대한 참조
        private Rigidbody _rigidbody;
        private TriggerArea _triggerArea;
        private CancellationToken _cancellationToken;

        private Vector3 _destination;
        private Vector3 _movement;
        private Vector3 _targetPosition;
        private Tween _tween;
        private bool _token;

        private void Awake()
        {
            //구성 요소에 대한 참조를 가져옵니다.
            _rigidbody = GetComponent<Rigidbody>();
            _triggerArea = GetComponentInChildren<TriggerArea>();
            _cancellationToken = this.GetCancellationTokenOnDestroy();
            _initialPosition = transform.position;

            _destination = _initialPosition - transform.position;
            _movement = _destination.normalized;
            _targetPosition = _initialPosition;
        }

        private void Start()
        {
            if (!_triggerArea)
                DebugX.LogWarning($"{name}에서 TriggerArea를 찾을 수 없음!");

            //중력을 비활성화하고 리지드바디의 회전을 고정하고 운동학으로 설정합니다.
            _rigidbody.freezeRotation = true;
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;

            LateFixedUpdate().Forget();
        }

        /// <summary>
        /// LateFixedUpdate는 FixedUpdate 이후에 호출됩니다.
        /// </summary>
        private async UniTaskVoid LateFixedUpdate()
        {
            while (true)
            {
                MovePlatform();
                await UniTask.Yield(PlayerLoopTiming.LastFixedUpdate, cancellationToken: _cancellationToken);
            }
        }

        private void MovePlatform()
        {
            // TriggerArea가 필수
            if (!_triggerArea)
                return;

            bool isTrigger = !_triggerArea.rigidbodiesInTriggerArea.IsEmpty();

            //발판을 밟았을 경우
            if (isTrigger)
            {
                //내려가기를 트리거할 카운트를 올립니다. 
                _downCount += Time.deltaTime;

                //내려가는 타임이 되었을 경우
                if (_downCount >= downTime)
                {
                    _downCount = downTime;
                    _isDown = true;
                    SetMove(downPosition.position, SinkEaseType.Down);
                }

                //내려간 상태에서 다시 밟으면 올라오는 시간을 초기화합니다.
                if (_isDown)
                    _risingDelay = risingDelayTime;
            }
            //발판을 밟지 않았을 경우
            else
            {
                //내려가는 상태일 경우
                if (_isDown)
                {
                    //올라올 준비가 되면 리셋
                    if (_risingDelay <= 0f)
                        Reset();
                    else
                    {
                        //내려가도록 처리
                        SetMove(downPosition.position, SinkEaseType.Down);

                        //올라올 수 있도록 타임을 줄입니다.
                        _risingDelay -= Time.deltaTime;
                    }
                }
                //내려가는 상태가 아닐 경우
                else
                {
                    //위에 계속 머무를 수 있도록 처리
                    SetMove(_initialPosition, SinkEaseType.Up);
                    _downCount = 0f;
                }
            }

            //이 프레임의 움직임 가져오기
            _movement *= _duration * Time.deltaTime;

            float movementLength = _movement.magnitude;
            float destinationLength = _destination.magnitude;

            //destination까지의 남은 거리가 이 프레임의 이동보다 작으면 다음 웨이포인트로 바로 이동합니다.
            //그렇지 않으면 다음 웨이포인트로 이동합니다.
            if (movementLength >= destinationLength || movementLength == 0f)
                _rigidbody.position = _targetPosition;
            else
            {
                if (_token) return;
                
                _token = true;
                _rigidbody.DOMove(_targetPosition, _duration).OnComplete(() => _token = false)
                    .SetEase(_targetEase);
            }
        }

        private void SetMove(Vector3 destination, SinkEaseType easeType)
        {
            _destination = destination - transform.position;
            _movement = _destination.normalized;
            _targetPosition = destination;
            _targetEase = easeType == SinkEaseType.Down ? downEase : upEase;
            _duration = easeType == SinkEaseType.Down ? downDuration : upDuration;
        }

        private void Reset()
        {
            _downCount = 0f;
            _risingDelay = 0f;
            _isDown = false;
            _token = false;
        }

        /// <summary>
        /// 내려갈 깊이를 정하는 오브젝트를 생성합니다.
        /// </summary>
        [Button("Auto")]
        private void CreateDownPosition()
        {
            //실행중일때는 동작하지 않습니다.
            if (Application.isPlaying) return;

            if (downPosition)
                DestroyImmediate(downPosition.gameObject);

            GameObject newObj = new GameObject("Down Position")
            {
                transform =
                {
                    position = transform.position + (Vector3.down * 1.5f)
                }
            };

            downPosition = newObj.transform;
        }
    }
}