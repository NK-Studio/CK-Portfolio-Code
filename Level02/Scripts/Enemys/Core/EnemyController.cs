using Character.Controllers;
using Character.Core;
using UnityEngine;
using UnityEngine.Events;
using Utility;

namespace Enemys
{
    public class EnemyController : MonoBehaviour
    {
        [Tooltip(
            "컨트롤러가 벽에 기대어 걸어가는 데 갇힌 경우 특정 시간 동안 특정 거리 이상 이동하지 않으면 이동이 취소됩니다." +
            "\n'timeOutTime'은 컨트롤러가 이동해야 하는(또는 이동을 중지하는) 시간을 제어합니다.")]
        public float timeOutTime = 1f;

        [Tooltip("이동에 필요한 최소 거리를 제어합니다.")] public float timeOutDistanceThreshold = 0.05f;

        //컨트롤러와 목표 위치 사이의 거리가 이보다 작으면 목표에 도달합니다.
        private const float ReachTargetThreshold = 0.001f;
        protected float MovementSpeed = 10f;
        public float CurrentVerticalSpeed;
        protected float CurrentTimeOutTime = 1f;
        protected bool HasTarget; //컨트롤러가 현재 이동할 수 있는 유효한 대상 위치가 있는지 여부입니다.
        public bool IsGrounded;
        protected Vector3 CurrentTargetPosition;
        private Vector3 _lastPosition;
        private Vector3 _lastVelocity = Vector3.zero;
        private Vector3 _lastMovementVelocity = Vector3.zero;

        //첨부된 'Mover' 및 변환 구성 요소에 대한 참조.
        private Mover _mover;
        private Transform _tr;


        private void Awake()
        {
            _mover = GetComponent<Mover>();
            _tr = transform;
        }

        private void Start()
        {
            Vector3 position = _tr.position;
            _lastPosition = position;
            CurrentTargetPosition = transform.position;
        }

        public void Core(float gravity)
        {
            //땅에 닿았는지 사전 체크합니다.
            _mover.CheckForGround();

            //캐릭터가 땅에 닿았는지 되어 있는지 확인합니다.
            IsGrounded = _mover.IsGrounded();

            //시간 초과 처리(고정된 경우 컨트롤러 중지);
            HandleTimeOut();

            //이 프레임의 최종 속도를 계산합니다.
            Vector3 velocity = CalculateMovementVelocity();

            _lastMovementVelocity = velocity;

            //중력을 계산하고 적용
            HandleGravity(gravity);
            velocity += _tr.up * CurrentVerticalSpeed;

            //나중을 위해 속도를 저장
            _lastVelocity = velocity;

            //캐릭터가 땅에 닿은 경우 땅 감지 센서 범위를 확장합니다.
            _mover.SetExtendSensorRange(IsGrounded);

            //무버 속도를 설정합니다.
            _mover.SetVelocity(velocity);
        }
        public void CoreAsRigidbody(float gravity)
        {
            //땅에 닿았는지 사전 체크합니다.
            _mover.CheckForGround();

            //캐릭터가 땅에 닿았는지 되어 있는지 확인합니다.
            IsGrounded = _mover.IsGrounded();

            //시간 초과 처리(고정된 경우 컨트롤러 중지);
            HandleTimeOut();
            
            //이 프레임의 최종 속도를 계산합니다.
            Vector3 velocity = _mover.GetRigidbody().velocity;
            _lastMovementVelocity = velocity;

            //중력을 계산하고 적용
            HandleGravity(gravity);
            velocity += _tr.up * CurrentVerticalSpeed;
            velocity.y = Mathf.Max(-15.0f, velocity.y);

            //나중을 위해 속도를 저장
            _lastVelocity = velocity;

            //캐릭터가 땅에 닿은 경우 땅 감지 센서 범위를 확장합니다.
            _mover.SetExtendSensorRange(IsGrounded);

            //무버 속도를 설정합니다.
            _mover.SetVelocity(velocity);
        }

        public void Core(float gravity, Vector3 velocity)
        {
            //땅에 닿았는지 사전 체크합니다.
            _mover.CheckForGround();

            //캐릭터가 땅에 닿았는지 되어 있는지 확인합니다.
            IsGrounded = _mover.IsGrounded();

            //시간 초과 처리(고정된 경우 컨트롤러 중지);
            HandleTimeOut();
            
            _lastMovementVelocity = velocity;

            //중력을 계산하고 적용
            HandleGravity(gravity);
            velocity += _tr.up * CurrentVerticalSpeed;

            //나중을 위해 속도를 저장
            _lastVelocity = velocity;

            //캐릭터가 땅에 닿은 경우 땅 감지 센서 범위를 확장합니다.
            _mover.SetExtendSensorRange(IsGrounded);

            //무버 속도를 설정합니다.
            _mover.SetVelocity(velocity);
        }


        /// <summary>
        /// 현재 목표 위치를 기반으로 이동 속도를 계산합니다.
        /// </summary>
        /// <returns></returns>
        private Vector3 CalculateMovementVelocity()
        {
            // TODO 왜인지 땅을 뚫음
            //컨트롤러에 현재 대상이 없으면 속도를 반환하지 않습니다.	
            if (!HasTarget)
                return Vector3.zero;

            //벡터를 목표 위치로 계산합니다.
            Vector3 toTarget = CurrentTargetPosition - _tr.position;

            //벡터의 모든 수직 부분을 제거합니다.
            toTarget = VectorMath.RemoveDotVector(toTarget, _tr.up);

            //목표까지의 거리를 계산합니다.
            float distanceToTarget = toTarget.magnitude;

            //컨트롤러가 이미 목표 위치에 도달한 경우 속도를 반환하지 않습니다.
            if (distanceToTarget <= ReachTargetThreshold)
            {
                HasTarget = false;
                return Vector3.zero;
            }

            Vector3 velocity = toTarget.normalized * MovementSpeed;

            //오버슈팅을 처리
            if (MovementSpeed * Time.fixedDeltaTime > distanceToTarget)
            {
                velocity = toTarget.normalized * distanceToTarget;
                HasTarget = false;
            }

            return velocity;
        }

        public UnityAction OnJump;

        /// <summary>
        /// 현재 중력을 계산합니다.
        /// </summary>
        /// <param name="gravity"></param>
        private void HandleGravity(float gravity)
        {
            if (IsGrounded)
            {
                if (CurrentVerticalSpeed <= 0f)
                    CurrentVerticalSpeed = 0f;
            }
            else
                CurrentVerticalSpeed -= gravity * Time.deltaTime;

            if (OnJump != null)
            {
                OnJump.Invoke();
                OnJump = null;
            }
        }

        /// <summary>
        /// 시간 초과 처리(레벨 지오메트리에 대해 멈춘 경우 컨트롤러 이동 중지)
        /// </summary>
        private void HandleTimeOut()
        {
            //컨트롤러에 현재 대상이 없으면 시간을 재설정하고 반환합니다.
            if (!HasTarget)
            {
                CurrentTimeOutTime = 0f;
                return;
            }

            //컨트롤러가 충분한 거리를 이동한 경우 시간을 재설정합니다.
            if (Vector3.Distance(_tr.position, _lastPosition) > timeOutDistanceThreshold)
            {
                CurrentTimeOutTime = 0f;
                _lastPosition = _tr.position;
            }
            //컨트롤러가 충분한 거리를 이동하지 않은 경우 현재 시간 초과 시간을 증가시킵니다.
            else
            {
                CurrentTimeOutTime += Time.deltaTime;

                //현재 시간 초과 시간이 한계에 도달하면 컨트롤러가 움직이지 않도록 하십시오.
                if (CurrentTimeOutTime >= timeOutTime)
                    HasTarget = false;
            }
        }

        /// <summary>
        /// 목표 지점까지 이동
        /// </summary>
        public void MoveTo(Transform targetPosition, float moveSpeed)
        {
            CurrentTargetPosition = targetPosition.position;
            MovementSpeed = moveSpeed;
            HasTarget = true;
        }

        /// <summary>
        /// 목표 지점까지 이동
        /// </summary>
        public void MoveTo(Vector3 targetPosition, float moveSpeed)
        {
            CurrentTargetPosition = targetPosition;
            MovementSpeed = moveSpeed;
            HasTarget = true;
        }

        /// <summary>
        /// 이동을 강제로 멈춥니다.
        /// </summary>
        public void MoveToStop()
        {
            MovementSpeed = 0f;
            CurrentTargetPosition = Vector3.zero;
            HasTarget = false;
        }

        /// <summary>
        /// 타겟으부터 거리 구하기
        /// </summary>
        /// <returns></returns>
        public float GetDistanceFromTarget(Transform target)
        {
            if (!target)
                return float.NaN;

            float distance = Vector3.Distance(target.position, transform.position);
            return distance;
        }

        /// <summary>
        /// 타겟이 추적 범위 이내일 때 
        /// </summary>
        /// <returns></returns>
        public bool IsTargetInTrackingRange(Transform target, float trackingRange)
        {
            if (!target) return false;

            float playerDistance = GetDistanceFromTarget(target);

            //플레이어를 트래킹하는 거리일 경우
            bool isPlayerInTrackingRange = playerDistance <= trackingRange;
            return isPlayerInTrackingRange;
        }

        /// <summary>
        /// 타겟이 공격 범위 이내일 때
        /// </summary>
        /// <returns></returns>
        public bool IsTargetInAttackRange(Transform target, float attackRange)
        {
            if (!target) return false;
            float playerDistance = GetDistanceFromTarget(target);

            bool isPlayerInTrackingRange = playerDistance <= attackRange;
            return isPlayerInTrackingRange;
        }

        /// <summary>
        /// 땅에 닫고 있는지 반환합니다.
        /// </summary>
        /// <returns></returns>
        public bool IsGround()
        {
            return IsGrounded;
        }

        /// <summary>
        /// 현재 이동 속도를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetMovementVelocity()
        {
            return _lastMovementVelocity;
        }

        /// <summary>
        /// 속도를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetVelocity()
        {
            return _lastVelocity;
        }

        /// <summary>
        /// 플레이어 방향으로 바라봅니다.
        /// </summary>
        public void LookAtTarget(Transform target)
        {
            if (!target) return;
            Vector3 targetPosition = target.position - transform.position;
            targetPosition.y = 0;
            targetPosition.Normalize();

            Quaternion rotation = Quaternion.LookRotation(targetPosition, Vector3.up);
            transform.rotation = rotation;
        }

        public void AddJump(float jumpPower)
        {
            OnJump = () =>
            {
                CurrentVerticalSpeed = jumpPower;
                IsGrounded = false;
            };
        }

        /// <summary>
        /// 객체에게 힘을 줍니다.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="mode"></param>
        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)
        {
            _mover.AddForce(force, mode);
        }
        public Mover GetMover() => _mover;
    }
}