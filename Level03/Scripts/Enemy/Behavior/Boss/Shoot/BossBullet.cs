using System;
using Cysharp.Threading.Tasks;
using Damage;
using Dummy.Scripts;
using Effect;
using EnumData;
using Managers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using Utility;

namespace Enemy.Behavior.Boss
{
    [RequireComponent(typeof(Rigidbody))]
    public class BossBullet : MonoBehaviour, IHostile
    {
        [field: SerializeField]
        public BossBulletSettings Settings { get; private set; }

        [field: SerializeField, ReadOnly, Tooltip("빙결 여부입니다.")] 
        public bool IsFreeze { get; private set; } = false;
        public float Height => 1f;
        
        [field: SerializeField, ReadOnly, Tooltip("빙결 이후 반사 여부입니다.")]
        public bool IsFreezeReflecting { get; private set; } = false;

        [field: SerializeField] 
        public EffectType TrailEffectType { get; private set; } = EffectType.None;
        [field: SerializeField] 
        public EffectType HitEffectType { get; private set; } = EffectType.AquusBulletHit01;

        public bool CanBlockByNonFrozenEnemy = false;

        public DamageReaction Reaction = DamageReaction.Normal;
        
        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
        }

        [SerializeField, ReadOnly] private float _moved;
        [SerializeField, ReadOnly] private float _time;
        [SerializeField, ReadOnly] private BossAquus _shooter;
        [SerializeField, ReadOnly] private Transform _target;
        [SerializeField, ReadOnly] private float _lastUpdatedTime;
        [SerializeField, ReadOnly] private Vector3 _targetPosition;
        [SerializeField, ReadOnly] private bool _initialized;
        [SerializeField, ReadOnly] private Vector3 _shootDirection;

        public UnityEvent OnDisabled;
        private void OnEnable()
        {
            _initialized = false;
        }

        public void Initialize(BossAquus shooter, Transform target)
        {
            // Debug.Log($"BossBullet::Initialize() {name}", gameObject);
            Reset();
            OnDisabled.RemoveAllListeners();
            _initialized = true;
            _shooter = shooter;
            _target = target;
            _targetPosition = _target.position;
            _shootDirection = transform.forward;
            
            
            if (TrailEffectType != EffectType.None)
            {
                var projectileTransform = transform;
                var trail = EffectManager.Instance.Get(TrailEffectType, 
                    projectileTransform.position, 
                    projectileTransform.rotation,
                    false
                );
                if (!trail.TryGetComponent(out FakeChild f))
                {
                    return;
                }
                f.TargetParent = projectileTransform;
                
                if (!trail.TryGetComponent(out TrailRendererRoot trr) || !trail.TryGetComponent(out ParticleTrail pt))
                {
                    return;
                }
                
                pt.Reset();
                trail.gameObject.SetActive(true);
                trr.Clear();
                trr.RendererEnabled = true;
                
                
                OnDisabled.AddListener(() =>
                {
                    f.Follow(true);
                    f.TargetParent = null;
                    pt.Execute().Forget();
                    DisableAfter(3f, trr).Forget();
                });

                return;

                // t초 후 비활성화
                static async UniTaskVoid DisableAfter(float t, TrailRendererRoot target)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(t));
                    target.Clear();
                    target.RendererEnabled = false;
                    target.gameObject.SetActive(false);
                }

                /*
                static async UniTaskVoid ExecuteNextTick(TrailRendererRoot target)
                {
                    await UniTask.Yield();
                    target.RendererEnabled = true;
                    target.Clear();
                }
                */
            }
            
        }

        private void Reset()
        {
            _initialized = false;
            IsFreeze = false;
            IsFreezeReflecting = false;
            _shooter = null;
            _target = null;
            _moved = 0f;
            _time = 0f;
            _lastUpdatedTime = 0f;
        }

        private void FixedUpdate()
        {
            if(!_initialized) return;
            
            // 최대 거리 넘어가면 자동 비활성화
            if (_moved >= Settings.BulletRange)
            {
                gameObject.SetActive(false);
                return;
            }

            if (IsFreeze && !IsFreezeReflecting)
            {
                return;
            }

            var dt = Time.deltaTime;
            _time += dt;
            var delta = Settings.BulletSpeed * dt;
            _moved += delta;
            // 직선탄
            if (Settings.BulletType == BossBulletSettings.Type.Straight)
            {
                // _rigidbody.velocity = transform.forward * Settings.BulletSpeed;
                _rigidbody.MovePosition(_rigidbody.position + transform.forward * delta);
            }
            // 유도탄
            else if(Settings.BulletType == BossBulletSettings.Type.Guided)
            {
                var rigidbodyPosition = _rigidbody.position;
                var transformPosition = transform.position;
                if (_target)
                {
                    // 유도 목표 갱신 방식
                    bool updateTargetPosition = Settings.GuideUpdateType switch
                    {
                        BossBulletSettings.GuideTargetUpdateType.None => false, // 초기 위치 계속 사용
                        BossBulletSettings.GuideTargetUpdateType.Periodically  // 주기적 위치 갱신
                            => _time - _lastUpdatedTime > Settings.GuideUpdatePeriod,
                        BossBulletSettings.GuideTargetUpdateType.EveryFrame => true, // 매 프레임 위치 갱신
                        _ => false
                    };
                    if (updateTargetPosition)
                    {
                        _targetPosition = _target.position;
                        _lastUpdatedTime = _time;
                    }
                    
                    // 목표와의 거리가 비활성화 범위 안에 들어간 경우
                    float distanceFromTarget = transformPosition.Distance(_targetPosition.Copy(y: rigidbodyPosition.y));
                    bool isInGuideDisableRange = distanceFromTarget <= Settings.GuideDisableRangeFromTarget;
                    // 최초 발사 방향보다 일정 각도 이상 벌어진 경우
                    bool isOveredAngleFromShootDirection = !isInGuideDisableRange && Vector3.Dot(transform.forward, _shootDirection) <
                                                           Settings.GuideDisableAngleFromShootDirectionInCos;
                    if (isInGuideDisableRange || isOveredAngleFromShootDirection)
                    {
                        _target = null; // 유도 중단
                    }
                }
                
                if (!_target || _time < Settings.GuideStartDelay)
                {
                    // _rigidbody.velocity = transform.forward * Settings.BulletSpeed;
                    _rigidbody.MovePosition(rigidbodyPosition + transform.forward * delta);
                    return;
                }


                var direction = (_targetPosition - rigidbodyPosition).Copy(y: 0f).normalized;
                var oldRotation = _rigidbody.rotation;
                var newRotation = Quaternion.LookRotation(direction, Vector3.up);
                // _rigidbody.angularVelocity = Quaternion.RotateTowards(
                    // oldRotation,
                    // newRotation,
                    // Settings.GuideRotationSpeed
                // ).eulerAngles; 
                _rigidbody.MoveRotation(Quaternion.RotateTowards(
                    oldRotation, 
                    newRotation,
                    Settings.GuideRotationSpeed * dt
                ));
                // DebugX.Log($"from: {oldRotation.eulerAngles}, to: {newRotation.eulerAngles}, maxDegreesDelta: {Settings.GuideRotationSpeed * dt}, result: {_rigidbody.rotation.eulerAngles}");
                _rigidbody.MovePosition(rigidbodyPosition + (_rigidbody.rotation * Vector3.forward) * delta);
                // _rigidbody.velocity = (_rigidbody.rotation * Vector3.forward) * Settings.BulletSpeed;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 얼어 있는 상태에서는 보스와 지형에 충돌
            // 얼어있지 않은 상태에서는 플레이어와 지형에 충돌
            // 1. 얼어있는 상태에서 보스와 충돌했을 때
            if (IsFreeze)
            {
                if (IsFreezeReflecting && other.CompareTag("Enemy") && other.TryGetComponent(out BossAquus boss))
                {
                    // TODO 보스 실드 까기 (임시로 피격 처리)
                    boss.Damage(EnemyDamageInfo.Get(Settings.BulletDamage, gameObject, DamageMode.Normal, DamageReaction.Normal));
                }else if (other.CompareTag("Player"))
                {
                    return;
                }
            }
            // 2. 얼지 않은 상태에서 
            else
            {
                // 플레이어와 충돌했을 때: 플레이어 공격
                if (other.CompareTag("Player"))
                {
                    GameManager.Instance.Player.Damage(PlayerDamageInfo.Get(Settings.BulletDamage, gameObject, reaction: Reaction));
                }
                // 몬스터와 충돌했을 때: 빙결된 적한테만 막혀야 한다면
                else if (other.CompareTag("Enemy") 
                         && !CanBlockByNonFrozenEnemy 
                         && (other.TryGetComponent(out Monster m) && !m.IsFreeze 
                             || other.attachedRigidbody.TryGetComponent(out BossAquus b))
                         )
                {
                    return;
                }
            }

            if (HitEffectType != EffectType.None)
            {
                var effect = EffectManager.Instance.Get(HitEffectType);
                effect.transform.position = transform.position;
            }
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            Reset();

            OnDisabled.Invoke();
        }

        public float Health { get; set; } = 1f;
        public EntityHitResult Damage(DamageInfo info)
        {
            // 얼릴 수 없는 종류의 투사체는 피격받을 일 없음
            // 또는 얼어서 이미 날아가는 상태의 투사체는 피격받아도 반응 없음
            if (Settings.PreventFreeze || IsFreezeReflecting)
            {
                return EntityHitResult.Invincible;
            }

            // 이미 빙결인 상태에서 공격을 받았을 경우: 쏜 사람에게 반사
            // TODO '쏜 사람에게 반사'는 그냥 캐주얼이라 임의로 지정. 공격 방향으로 반사로 변경될 수도
            if (IsFreeze)
            {
                IsFreezeReflecting = true;
                var toShooter = (_shooter.transform.position + Vector3.up - transform.position).normalized;
                _rigidbody.MoveRotation(Quaternion.LookRotation(toShooter));
                _target = null; // _target 뺏어서 유도 해제 => 종류에 상관없이 보스 방향으로 이동
                
                // TODO 탄환 빙결으로 전환
            }
            // 빙결이 아닌 상태에서 빙결 공격을 건 경우: 빙결 상태로 전환
            else if(info.Reaction == DamageReaction.Freeze)
            {
                IsFreeze = true;
            }
            
            return EntityHitResult.Success;
        }
    }
}