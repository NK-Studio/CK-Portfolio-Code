using System.Threading;
using AutoManager;
using Character.View;
using Cysharp.Threading.Tasks;
using Managers;
using Settings;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Assertions;
using Utility;
using Zenject;

namespace Character.USystem.Throw
{
    [RequireComponent(typeof(LineRenderer))]
    public class ThrowSystem : MonoBehaviour
    {
        [Tooltip("투척 궤적 표시할 때 포물선 가로 시간단위 배수입니다.")]
        public float parabolaRenderTimeUnitMultiplier = 2f;
        [Tooltip("투척 궤적 표시할 때 사용하는 구체 오브젝트입니다.")]
        public GameObject endPointIndicator;

        [Tooltip("투척물 프리팹입니다."), ReadOnly] public Throwable throwObject;

        [SerializeField] private LineRenderer lineRenderer;

        [Tooltip("무시할 충돌체를 지정합니다."), SerializeField]
        private LayerMask ignoreColliderMasks;

        private PlayerView _playerView;
        private EOtherState _state;
        private Transform _cameraTransform;
        private Vector3 _lastDirection = Vector3.zero;
        private Vector3 _lastPosition = Vector3.zero;
        private int _collideLayerMask;
        private const int LinePositionBufferSize = 1024;
        private readonly Vector3[] _linePositionBuffer = new Vector3[LinePositionBufferSize];
        private static readonly float Gravity = Mathf.Abs(Physics.gravity.y);

        // 포물선 공식에 따른 t초 후의 포물선 상의 위치
        private static readonly float HalfGravity = Gravity * 0.5f;

        private CharacterSettings _characterSettings;

        private void Awake()
        {
            _playerView = GetComponentInParent<PlayerView>();
            _cameraTransform = UnityEngine.Camera.main.transform;
            lineRenderer.useWorldSpace = true;
        }

        private void Start()
        {
            _characterSettings = Manager.Get<GameManager>().characterSettings;

            // 충돌 검사 시 무시해야 하는 레이어 마스크들 등록
            _collideLayerMask |= ignoreColliderMasks;
            _collideLayerMask = ~_collideLayerMask;

            //조준 중일 때, Update에서 현재 각도로 물체를 쐈을 때의 궤도 연산 
            this.LateUpdateAsObservable()
                .Where(_ => _state == EOtherState.Catch)
                .Subscribe(OnUpdateAiming)
                .AddTo(this);
        }

        private void OnUpdateAiming(Unit _)
        {
            Assert.IsTrue(throwObject, "throwObject이 참조되어 있지 않습니다.");

            Vector3 currentDirection = _cameraTransform.forward;
            Vector3 initialPosition = _playerView.GetThrowPosition();

            // 같은 방향 & 같은 위치인 경우에는 또 연산을 하지 않음
            if (currentDirection == _lastDirection && initialPosition == _lastPosition)
                return;

            //마지막 방향에 현재 방향을 넣는다.
            _lastDirection = currentDirection;

            //마지막 위치에 초기화 위치를 넣는다.
            _lastPosition = initialPosition;

            float power = throwObject.Power;
            float radius = throwObject.Radius;

            //초기 힘 = 현재 방향에 힘
            Vector3 initialForce = currentDirection.normalized * power;

            //시간
            float timeUnit = Time.fixedDeltaTime * parabolaRenderTimeUnitMultiplier;

            //다음 위치는 초기화 위치
            Vector3 nextPosition = initialPosition;

            float oldMoveDistance = 0f;
            float time = 0f;
            int count = 0;
            bool hasCollision = false;
            _linePositionBuffer[count] = initialPosition;
            count += 1;

            while (count < LinePositionBufferSize || oldMoveDistance <= _characterSettings.maxThrowAttackDistance)
            {
                // t초 후 위치
                Vector3 position = nextPosition;

                // t + fixedDeltaTime 초 후 위치
                SetParabolaPosition(initialForce, initialPosition, time + timeUnit, ref nextPosition);

                Vector3 moved = nextPosition - position;
                float movedDistance = moved.magnitude;

                // 충돌이 발생했다면 반복 중단
                if (Physics.SphereCast(position, radius, moved, out RaycastHit hitInfo, movedDistance,
                        _collideLayerMask))
                {
                    float distance = hitInfo.distance;
                    Vector3 collidePoint = position + moved.normalized * distance;

                    endPointIndicator.transform.position = collidePoint;

                    _linePositionBuffer[count] = collidePoint;
                    count += 1;

                    hasCollision = true;
                    break;
                }

                oldMoveDistance += movedDistance;
                _linePositionBuffer[count] = nextPosition;
                count += 1;
                time += timeUnit;
            }

            if (!hasCollision)
                endPointIndicator.transform.position = nextPosition;

            lineRenderer.positionCount = count;
            lineRenderer.SetPositions(_linePositionBuffer);
        }

        /// <summary>
        /// 포물선 위치를 설정합니다.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="position"></param>
        /// <param name="time"></param>
        /// <param name="resultPosition"></param>
        private void SetParabolaPosition(Vector3 force, Vector3 position, float time, ref Vector3 resultPosition)
        {
            resultPosition.Set(
                position.x + force.x * time,
                position.y + force.y * time - HalfGravity * time * time,
                position.z + force.z * time
            );
        }

        /// <summary>
        /// 투척 시스템의 활성화 상태를 설정합니다.
        /// </summary>
        /// <param name="active">true가 되면 활성화, false가 되면 비활성화 됩니다.</param>
        public void SetActiveThrow(bool active)
        {
            gameObject.SetActive(active);

            endPointIndicator.SetActive(active);
            lineRenderer.gameObject.SetActive(active);

            _state = active ? EOtherState.Catch : EOtherState.Nothing;
        }

        // PlayerController로부터 넘어옴, UseItem 키
        public void Throw(Vector3 shootPosition)
        {
            //던지기 오브젝트 트랜스폼을 가져옵니다.
            Transform objectTransform = throwObject.transform;
         
            //자신을 손 더미에서 빠져나오도록 합니다.
            objectTransform.parent = null;
            
            //오브젝트의 위치를 shootPosition으로 변경합니다.
            objectTransform.position = shootPosition;

            //던집니다.
            throwObject.Throw(_lastDirection);

            //초기화
            Clear();
        }

        private void Clear()
        {
            //나머지를 초기화합니다.
            _state = EOtherState.Nothing;
            endPointIndicator.SetActive(false);
            lineRenderer.positionCount = 0;
            lineRenderer.gameObject.SetActive(false);
            throwObject = null;
            SetActiveThrow(false);
        }
    }
}