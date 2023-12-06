using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Platform
{
    //이 스크립트는 웨이포인트 세트를 따라 리지드바디를 이동합니다.
    //또한 맨 위에 있는 컨트롤러도 함께 이동합니다.
    public class MovingPlatform : MonoBehaviour
    {
        [Title("이동속도"), Tooltip("이동 속도")] public float movementSpeed = 10f;

        [Title("방향"), Tooltip("방향 반전")] public bool reverseDirection;

        [Title("쿨 타임"), Tooltip("웨이포인트에 도달한 후 대기 시간")]
        public float waitTime = 1f;

        [Title("이동 경로"), Tooltip("경유지로 사용되는 변환 목록")]
        public List<Transform> waypoints = new();

        //이 부울은 플랫폼이 대기하는 동안 이동을 중지하는 데 사용됩니다.
        private bool _isWaiting;
        private int _currentWaypointIndex;
        
        private Rigidbody _rigidbody;
        private TriggerArea _triggerArea;
        private Transform _currentWaypoint;
        private CancellationToken _cancellationToken;

        private void Start()
        {
            //구성 요소에 대한 참조를 가져옵니다.
            _rigidbody = GetComponent<Rigidbody>();
            _triggerArea = GetComponentInChildren<TriggerArea>();
            _cancellationToken = this.GetCancellationTokenOnDestroy();
            //중력을 비활성화하고 리지드바디의 회전을 고정하고 운동학으로 설정합니다.
            _rigidbody.freezeRotation = true;
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;

            //웨이포인트가 할당되었는지 확인하고 할당되지 않은 경우 경고를 표시합니다.
            if (waypoints.Count <= 0)
            {
                DebugX.LogWarning("'MovingPlatform'에 지정된 웨이포인트가 없습니다!");
            }
            else
            {
                //첫 번째 웨이포인트를 설정합니다.
                _currentWaypoint = waypoints[_currentWaypointIndex];
            }

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
            //웨이포인트가 지정되지 않은 경우 돌아가십시오.
            if (waypoints.Count <= 0)
                return;

            if (_isWaiting)
                return;

            //현재 웨이포인트에 대한 벡터를 계산합니다.
            Vector3 toCurrentWaypoint = _currentWaypoint.position - transform.position;

            //정규화된 이동 방향을 가져옵니다.
            Vector3 movement = toCurrentWaypoint.normalized;

            //이 프레임의 움직임 가져오기
            movement *= movementSpeed * Time.deltaTime;

            //다음 웨이포인트까지의 남은 거리가 이 프레임의 이동보다 작으면 다음 웨이포인트로 바로 이동합니다.
            //그렇지 않으면 다음 웨이포인트로 이동합니다.
            if (movement.magnitude >= toCurrentWaypoint.magnitude || movement.magnitude == 0f)
            {
                movement = _currentWaypoint.position - transform.position;
                UpdateWaypoint();
            }
            else
                _rigidbody.position += movement;

            if (_triggerArea == null)
                return;

            for (int i = 0; i < _triggerArea.rigidbodiesInTriggerArea.Count; i++)
            {
                _triggerArea.rigidbodiesInTriggerArea[i]
                    .MovePosition(_triggerArea.rigidbodiesInTriggerArea[i].position + movement);
            }
        }

        //이 함수는 현재 웨이포인트에 도달한 후에 호출됩니다.
        //다음 웨이포인트는 웨이포인트 목록에서 선택됩니다.
        private void UpdateWaypoint()
        {
            if (reverseDirection)
                _currentWaypointIndex--;
            else
                _currentWaypointIndex++;

            //목록의 끝에 도달하면 인덱스를 재설정합니다.
            if (_currentWaypointIndex >= waypoints.Count)
                _currentWaypointIndex = 0;

            if (_currentWaypointIndex < 0)
                _currentWaypointIndex = waypoints.Count - 1;

            _currentWaypoint = waypoints[_currentWaypointIndex];

            //플랫폼 이동을 중지하십시오.
            _isWaiting = true;

            //플랫폼 이동을 재개하십시오.
            OnCoolTime().Forget();
        }

        //대기 시간을 추적하고 'waitTime'이 지난 후 'isWaiting'을 다시 'false'로 설정하는 코루틴.
        private async UniTaskVoid OnCoolTime()
        {
            if (_isWaiting)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: _cancellationToken);
                _isWaiting = false;
            }
        }
    }
}