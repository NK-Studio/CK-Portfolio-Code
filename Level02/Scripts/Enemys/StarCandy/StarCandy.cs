using System;
using Character.USystem.Throw;
using JetBrains.Annotations;
using ModestTree;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Utility;
using Zenject;

namespace Enemys
{
    public class StarCandy : Enemy
    {
        public enum StarCandyAnimation
        {
            Move, //점프
            Attack, //더블 점프
            Death //사망
        }

        public enum State
        {
            Spawning,
            Normal, //일반 상태
            Catch, //손에 잡힌 상태
            PullCatch, //당기기 로프에 잡힌 상태
            PreExplosion //폭발 전 상태
        }

        public enum Behaviour
        {
            Normal, //일반 상태
            Catch,
            OnHand //핸드 위에 있는 상태
        }

        [ValidateInput("@Settings != null", "스타 캔디에 Settings가 비어있습니다.")]
        public StarCandySettings Settings;

        [ValidateInput("@starCandyThrowable != null", "스타 캔디에 StarCandyThrowable이 비어있습니다.")]
        public Throwable starCandyThrowable;


        [field: SerializeField, ValidateInput("@GFXTransform != null", "스타 캔디의 GFX 콜라이더가 비어있습니다.")]
        public Transform GFXTransform { get; private set; }


        private CapsuleCollider _myCollider;
        private Rigidbody _rigid;

        public float TurnTrackingToIdle { get; set; }

        private OffScreenSystem _offScreenSystem;
        [Inject] private OffScreenSystemManager _offScreenSystemManager;

        private State _currentState;

        [Title("디버그 모드")] [SerializeField] private bool isDebugMode;

        private static readonly int IsMove = Animator.StringToHash("IsMove");
        private static readonly int OnAttack = Animator.StringToHash("OnAttack");
        private static readonly int OnDeath = Animator.StringToHash("OnDeath");
        private static readonly int OnCatch = Animator.StringToHash("OnCatch");

        [SerializeField] private StateMachine stateMachine;

        public CoreTriggerWithRootMotion CoreTriggerWithRootMotion;

        [Inject] private DiContainer _container;

        private IDisposable _stateEnabler;

        protected override void Awake()
        {
            HP = Settings.HPMax;
            _offScreenSystem = GetComponent<OffScreenSystem>();
            base.Awake();

            _myCollider = GetComponent<CapsuleCollider>();
            _rigid = GetComponent<Rigidbody>();

            CoreTriggerWithRootMotion.velocityMultiplier = Settings.MoveSpeed;
            stateMachine.enabled = true;

            _rigid.isKinematic = false;
        }

        private void Start()
        {
            CoreTriggerWithRootMotion.IsActiveCore = false;
            _stateEnabler = EnemyController.LateUpdateAsObservable()
                .Where(_ => EnemyController.IsGrounded)
                .Subscribe(_ =>
                {
                    CoreTriggerWithRootMotion.IsActiveCore = true;
                    OnTriggerState(State.Normal);
                    _stateEnabler.Dispose();
                    _stateEnabler = null;
                });
            OnTriggerState(State.Spawning);

            this.OnTriggerEnterAsObservable()
                .Where(other => other.CompareTag("Boss"))
                .Subscribe(_ => Explosion())
                .AddTo(this);
        }

        private void FixedUpdate()
        {
            bool isCurrentNormalState = _currentState == State.Normal;
            CoreTriggerWithRootMotion.IsActiveCore = isCurrentNormalState;
        }

        /// <summary>
        /// 플레이어인지 체크합니다.
        /// </summary>
        /// <param name="collision"></param>
        /// <returns></returns>
        [UsedImplicitly]
        public bool IsPlayer(Collider collision)
        {
            return collision.CompareTag("Player");
        }

        /// <summary>
        /// 폭발 범위인지 체크합니다.
        /// </summary>
        /// <param name="collision"></param>
        /// <returns></returns>
        [UsedImplicitly]
        public bool IsExplosion(Collider collision)
        {
            return collision.CompareTag("ExplosionRange");
        }

        /// <summary>
        /// 사망 처리를 합니다.
        /// </summary>
        [UsedImplicitly]
        public void Dead()
        {
            //OffScreen을 모두 제거합니다.
            RemoveOffScreenSystem();

            //생성될 위치 정의
            Vector3 nextPosition = transform.position;
            nextPosition.y += 0.5f;

            //생성
            StarCandyBomb starCandyBomb =
                _container.InstantiatePrefabForComponent<StarCandyBomb>(starCandyThrowable.gameObject,
                    nextPosition, quaternion.identity, null);

            //기타 설정
            starCandyBomb.ChangePivot(PivotStyle.CenterBottom);
            starCandyBomb.OnTriggerEnterExplosion();
            starCandyBomb.OnTriggerPhysics(true, 1, true);
            starCandyBomb.KnockBack(PlayerTransform, Settings.KnockbackPowerToPlayer, ForceMode.Impulse);

            //생성할 이펙트
            GameObject effect = _container.ResolveId<GameObject>(EffectType.MonsterHit);

            //생성 위치
            Vector3 position = transform.position;
            position.y += 0.37f;

            //히트 이펙트 생성
            Instantiate(effect, position, Quaternion.identity);

            //제거
            Destroy(gameObject);
        }

        /// <summary>
        /// 즉시 폭발
        /// </summary>
        [UsedImplicitly]
        public void Explosion()
        {
            RemoveOffScreenSystem();

            StarCandyBomb starCandyBomb =
                _container.InstantiatePrefabForComponent<StarCandyBomb>(starCandyThrowable.gameObject,
                    transform.position, quaternion.identity, null);

            starCandyBomb.OnTriggerExplosion(time: 0f).Forget();
            Destroy(gameObject);
        }

        [UsedImplicitly]
        public void AddTurnTrackingToIdle(float deltaTime)
        {
            TurnTrackingToIdle += deltaTime;
        }

        /// <summary>
        /// 상태를 트리거합니다.
        /// </summary>
        public void OnTriggerState(State state)
        {
            _currentState = state;

            switch (state)
            {
                case State.Normal:
                    CustomEvent.Trigger(gameObject, "OnNormal");
                    break;
                case State.PullCatch:
                    CustomEvent.Trigger(gameObject, "OnPullCatch");
                    break;
                case State.Catch:
                    CustomEvent.Trigger(gameObject, "OnCatch");
                    break;
            }
        }

        /// <summary>
        /// OffScreenSystem을 제거합니다.
        /// </summary>
        private void RemoveOffScreenSystem()
        {
            Image pointer = _offScreenSystem.GetPointer();
            _offScreenSystemManager.Remove(_offScreenSystem);
            Destroy(pointer.gameObject);
        }

        /// <summary>
        /// 별사탕의 상태를 변경합니다.
        /// </summary>
        /// <param name="behaviour"></param>
        [UsedImplicitly]
        public void ChangeBehaviour(Behaviour behaviour)
        {
            switch (behaviour)
            {
                case Behaviour.Catch:
                    //움직이지 못하도록 처리
                    OnTriggerAnimation(StarCandyAnimation.Move, false);
                    EnemyAnimator.SetTrigger(OnCatch);

                    //물리 제거
                    _myCollider.enabled = false;
                    _rigid.isKinematic = true;

                    //가운데로 이동
                    GFXTransform.localPosition = new Vector3(0, 0.214f, 0);

                    //OffScreen을 제거합니다.
                    RemoveOffScreenSystem();

                    //OffScreen을 제거합니다.
                    Destroy(_offScreenSystem);
                    break;

                case Behaviour.OnHand:
                    GFXTransform.localPosition = new Vector3(0, -0.148f, 0);
                    transform.localPosition = Vector3.zero;
                    break;
                case Behaviour.Normal:
                    GFXTransform.localPosition = Vector3.zero;
                    FloorSnap();

                    _myCollider.enabled = true;
                    _rigid.isKinematic = false;
                    break;
            }
        }

        /// <summary>
        /// 땅에 스냅을 겁니다.
        /// </summary>
        private void FloorSnap()
        {
            bool isHit = Physics.Raycast(transform.position, Vector3.down, out var hit, 10f,
                LayerMask.GetMask("Ground"));

            if (isHit)
                transform.position = hit.point;
        }

        #region Animation

        /// <summary>
        /// 애니메이션을 트리거 합니다.
        /// </summary>
        /// <param name="starCandyAnimation"></param>
        /// <param name="active"></param>
        public void OnTriggerAnimation(StarCandyAnimation starCandyAnimation, bool active = false)
        {
            switch (starCandyAnimation)
            {
                case StarCandyAnimation.Move:
                    EnemyAnimator.SetBool(IsMove, active);
                    break;
                case StarCandyAnimation.Attack:
                    EnemyAnimator.SetTrigger(OnAttack);
                    break;
                case StarCandyAnimation.Death:
                    EnemyAnimator.SetTrigger(OnDeath);
                    break;
            }
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!isDebugMode) return;
            Vector3 position = transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(position, Settings.TrackingRange);
        }
#endif
    }
}