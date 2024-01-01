using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Character.Core.Weapon;
using Character.Presenter;
using Character.View;
using Cysharp.Threading.Tasks;
using Damage;
using DG.Tweening;
using DG.Tweening.Core;
using Dummy.Scripts;
using Effect;
using Enemy.Behavior.Boss;
using Enemy.UI;
using EnumData;
using FMODPlus;
using Level;
using MagicaCloth2;
using Managers;
using ManagerX;
using Micosmo.SensorToolkit;
using Micosmo.SensorToolkit.BehaviorDesigner;
using RayFire;
using Settings;
using Sirenix.OdinInspector;
using UI;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.VFX;
using Utility;
using Action = System.Action;
using Logger = NKStudio.Logger;
using TimeSpan = System.TimeSpan;

namespace Enemy.Behavior
{
    public class Monster : MonoBehaviour, IHostile, IFreezable
    {
        [Sirenix.OdinInspector.BoxGroup, UnityEngine.Tooltip("활성화시 초기에 스스로 등장합니다.")]
        public bool AutoSingleSpawn;

        [field: SerializeField, FoldoutGroup("스탯", true), UnityEngine.Tooltip("보스 개체의 스탯입니다.")]
        public EnemySettings Settings { get; set; }

        [SerializeField, FoldoutGroup("사망 이펙트", true), UnityEngine.Tooltip("사망 시 출력될 VFX")]
        private VisualEffect _dissolveParticle;

        [SerializeField, FoldoutGroup("사망 이펙트", true)]
        private Renderer _dissolveTargetMeshRenderer;

        private FakeChild.TransformData _gfxTransformData;

        private static readonly int AlphaClipThreshold = Shader.PropertyToID("_AlphaClipThreshold");

        [SerializeField, FoldoutGroup("사망 이펙트", true)]
        private AnimationCurve _deadDissolveCurve;

        [SerializeField, FoldoutGroup("사망 이펙트", true)]
        private int _deadDissolveMaterialIndex = -1;

        private Material _deadDissolveMaterial;
        private float _deadDissolveTime;

        [FoldoutGroup("전투 구역", true)]
        [UnityEngine.Tooltip("몬스터가 속해있는 전투 구역입니다. 영역 소환 시에는 자동으로 배정되고, 사전 배치된 몬스터는 직접 배정해 주어야 합니다.")]
        public BattleArea TargetBattleArea;

        [SerializeField, FoldoutGroup("피격 이펙트", true)]
        private Renderer _hitTintTargetMeshRenderer;

        [SerializeField, FoldoutGroup("피격 이펙트", true)]
        private float _hitTintTime = 0.3f;

        [SerializeField, FoldoutGroup("피격 이펙트", true)]
        private Color _hitTintColor = new(0.7f, 0.7f, 0.7f);
        
        [field: SerializeField, FoldoutGroup("피격 이펙트", true)]
        public FallChecker FallChecker { get; protected set; }

        private Material _hitMaterial;

        private List<Material> _frostbiteMaterialList = new();
        protected float TweenTargetFrostbiteProperty = 0f;
        protected float FrostbiteProperty
        {
            get => _frostbiteMaterialList.Count <= 0 ? 0f : _frostbiteMaterialList[0].GetFloat(Control);
            set
            {
                foreach (var m in _frostbiteMaterialList)
                {
                    if(m) m.SetFloat(Control, value);
                }
            }
        }

        [Serializable]
        public class FrostbiteRendererSettings
        {
            public Renderer Renderer;
            public int MaterialIndex = 2;
        }
        
        [field: SerializeField, FoldoutGroup("빙결", true)]
        public List<FrostbiteRendererSettings> IceFrostbiteRenderers { get; private set; }

        [field: SerializeField, FoldoutGroup("공격", true), UnityEngine.Tooltip("근거리 공격 히트박스")]
        public RangeSensor AttackRangeSensor { get; private set; }

        [field: SerializeField, FoldoutGroup("공격", true), UnityEngine.Tooltip("근거리 공격 이펙트")]
        public EffectType AttackRangeSensorEffectType { get; private set; } = EffectType.None;

        [field: SerializeField, FoldoutGroup("공격", true),
                UnityEngine.Tooltip("공격 시야 Ray - 플레이어가 이 Ray에 충돌하지 않으면 공격을 시작하지 않습니다.")]
        public RaySensor AttackStartRangeRay { get; private set; }

        [field: SerializeField, FoldoutGroup("피격", true)]
        public SectorRangeSensorFilter SlipCompensateSensor { get; private set; }

        public virtual bool CanBeDashTarget => true;

        public PlayerPresenter PlayerPresenter { get; private set; }
        public PlayerView PlayerView { get; private set; }
        public Rigidbody Rigidbody { get; private set; }
        public Animator Animator { get; private set; }
        public NavMeshAgent NavMeshAgent { get; private set; }
        public BehaviorTree BehaviourTree { get; private set; }

        // 체력 관련
        private ReactiveProperty<float> _health = new();

        public float Health
        {
            get => _health.Value;
            set => _health.Value = Mathf.Clamp(value, 0f, MaximumHealth);
        }

        private System.IObservable<float> _healthObservable;
        public System.IObservable<float> HealthObservable => _healthObservable ??= _health.AsObservable();

        public bool DebugMode;

        private float _initialAngularSpeed;
        private int _initialNavMeshPriority;

        protected virtual void Awake()
        {
            if (!GFX)
            {
                GFX = transform.GetChild(0).gameObject;
            }

            BehaviourTree = GetComponent<BehaviorTree>();
            if (TryGetComponent(out NavMeshAgent agent))
            {
                NavMeshAgent = agent;
                _initialAngularSpeed = NavMeshAgent.angularSpeed;
                _initialNavMeshPriority = NavMeshAgent.avoidancePriority;
            }

            if (BehaviourTree)
            {
                BehaviourTree.SetVariable("AttackRange", new SharedSensor { Value = AttackRangeSensor });
                BehaviourTree.SetVariable("AttackRangeRay", new SharedSensor { Value = AttackStartRangeRay });
            }

            if (AttackStartRangeRay)
                AttackStartRangeRay.Length = Settings.AttackStartRange;

            Rigidbody = GetComponent<Rigidbody>();
            Rigidbody.isKinematic = true;

            Animator = GFX.GetComponent<Animator>();

            _gfxTransformData = new FakeChild.TransformData(GFX.transform, false);

            FallChecker ??= GetComponent<FallChecker>();
            if (FallChecker)
            {
                // FallChecker.enabled = false;
                FallChecker.OnFallEvent.AddListener(() =>
                {
                    if(IsRunningSpawnSequence) return;
                    Debug.Log($"<color=magenta>{name}::Monster FallEvent Received() !!!</color>", gameObject);
                    // FallChecker.enabled = false;
                    // 빙결 시에는 즉시 파괴
                    if (IsFreeze)
                    {
                        // 터지면 FallChecker는 안 해도 되지 .. 음음
                        FallChecker.enabled = false;
                        OnFreezeBreak();
                        return;
                    }
                    // 그 외에는 Fall 이벤트
                    BehaviourTree.SendEvent("Fall");
                    NavMeshAgent.enabled = false;
                    Rigidbody.isKinematic = false;
                    Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                    Rigidbody.useGravity = true;
                    ReleaseCombinableOffScreenUI();
                });
            }
        }

        protected virtual void OnEnable()
        {
            if (AutoSingleSpawn)
                AutoSpawn().Forget();

            PlayerPresenter = FindAnyObjectByType<PlayerPresenter>();
            PlayerView = FindAnyObjectByType<PlayerView>();

            if (PlayerView && BehaviourTree)
            {
                SharedGameObject sharedPlayerObject = new() { Value = PlayerView.gameObject };
                SharedTransform sharedPlayerTransform = new() { Value = PlayerView.transform };

                BehaviourTree.SetVariable("PlayerObject", sharedPlayerObject);
                BehaviourTree.SetVariable("PlayerTransform", sharedPlayerTransform);
            }

            Health = Settings.Health;
            IsFreezeSlipping = false;
            IsFreezeFalling = false;
            FreezeLevel = 0;
            FreezeTime = 0f;
            FrostbiteTime = 0f;
            SlipGuided = false;
            if (Animator)
            {
                Animator.speed = 1f;
            }

            if (Rigidbody)
            {
                Rigidbody.interpolation = RigidbodyInterpolation.None;
                Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            }

            if (NavMeshAgent)
            {
                NavMeshAgent.enabled = true;
                NavMeshAgent.speed = MoveSpeed;
                NavMeshAgent.avoidancePriority = _initialNavMeshPriority;
            }

            RenderersEnabled = true;

            if (_hitMaterial)
            {
                _hitMaterial.color = Color.white.Copy(a: 0f);
            }

            FrostbiteProperty = 0f;

            _gfxTransformData.Apply(GFX.transform);
            if (_deadDissolveMaterial)
                _deadDissolveMaterial.SetFloat(AlphaClipThreshold, 0f);
            PreventDropItem = false;

            if (!PlayerPresenter)
            {
                gameObject.SetActive(false);
                return;
            }
            InitializeHUDAfterSeconds().Forget();
        }

        private async UniTaskVoid InitializeHUDAfterSeconds()
        {
            await UniTask.DelayFrame(10);
            OnInitializeHUD();
        }

        private async UniTaskVoid AutoSpawn()
        {
            await UniTask.Delay(1000);
            gameObject.SetActive(true);
        }

        private CombinableOffScreenUIController _offScreenUIController;
        private CombinableOffScreenUI _offScreenUI;
        protected virtual void OnInitializeHUD()
        {
            if (InitializeCombinableOffScreenUI())
            {
                return;
            }
            
            var hudPoolManager = GameManager.Instance.CurrentHUDPoolManager;
            if (hudPoolManager)
            {
                EnemyHUD enemyHUD = hudPoolManager.HUDPool.Get();
                enemyHUD.Follow = transform;
                enemyHUD.SetEnemy(this);
            }
        }

        private bool InitializeCombinableOffScreenUI()
        {
            if (!gameObject.activeInHierarchy || !Settings.UseCombinedOffScreenUI || IsRunningSpawnSequence || _offScreenUI)
            {
                return false;
            }

            _offScreenUIController = FindAnyObjectByType<CombinableOffScreenUIController>();
            _offScreenUI = _offScreenUIController.RegisterNew();
            _offScreenUI.TargetObject = transform;
            return true;

        }

        private void ReleaseCombinableOffScreenUI()
        {
            if (_offScreenUI)
            {
                _offScreenUI.TargetObject = null;
                _offScreenUIController.ReleaseAndUnregister(_offScreenUI);
                _offScreenUIController = null;
                _offScreenUI = null;
            }
        }

        private static readonly int Jump = Animator.StringToHash("OnJump");
        private static readonly int JumpImmediately = Animator.StringToHash("OnJumpImmediately");
        private static readonly int JumpLand = Animator.StringToHash("OnJumpLand");

        [field: SerializeField, BoxGroup("포물선 소환"), ReadOnly]
        public bool IsRunningSpawnSequence { get; private set; } = false;

        [field: SerializeField, BoxGroup("포물선 소환")]
        public bool UseAnimationWhenSpawnSequence { get; private set; } = false;
        [field: SerializeField, BoxGroup("포물선 소환")]
        public bool UseSpawnSequenceJumpImmediately { get; private set; } = false;

        /// <summary>
        /// 포물선을 날아오며 소환합니다.
        /// </summary>
        /// <param name="parabola"></param>
        /// <param name="xPositionCurveOrNull"></param>
        public virtual async UniTaskVoid InitializeParabola(ParabolaByMaximumHeight parabola,
            AnimationCurve xPositionCurveOrNull, float delay)
        {
            IsRunningSpawnSequence = true;
            bool oldNavMeshAgentEnabled = false;
            if (NavMeshAgent)
            {
                oldNavMeshAgentEnabled = NavMeshAgent.enabled;
                NavMeshAgent.enabled = false;
            }

            // 최초 위치 및 방향 설정
            transform.SetPositionAndRotation(parabola.Start, Quaternion.LookRotation(parabola.HorizontalDirection));
            // TODO 뛰는 애니메이션 트리거

            GameObject indicator = null;
            if (Settings.UseParabolaSpawnIndicator)
            {
                void SpawnIndicator()
                {
                    if (!gameObject || !gameObject.activeInHierarchy)
                    {
                        return;
                    }
                    indicator = EffectManager.Instance.Get(EffectType.EnemyParabolaSpawnIndicator);
                    if (indicator)
                    {
                        indicator.transform.position = parabola.GetPosition(1f) + Vector3.up * indicator.transform.position.y;
                    }
                }
                if (delay > Settings.ParabolaSpawnIndicatorDuration)
                {
                    var delayForSpawnIndicator = delay - Settings.ParabolaSpawnIndicatorDuration;
                    ExecuteAfter(delayForSpawnIndicator, SpawnIndicator).Forget();
                }
                else
                {
                    SpawnIndicator();
                }
            }

            if (UseAnimationWhenSpawnSequence)
            {
                if (UseSpawnSequenceJumpImmediately)
                {
                    Animator.SetTrigger(JumpImmediately);
                }
                else
                {
                    Animator.SetTrigger(Jump);
                }
            }

            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: destroyCancellationToken);
            // 커브 기반 이동 
            if (xPositionCurveOrNull != null)
            {
                float x = 0f;
                float length = xPositionCurveOrNull.GetLength();
                while (x < length)
                {
                    var position = parabola.GetPosition(xPositionCurveOrNull.Evaluate(x));
                    transform.position = position;

                    await UniTask.Yield(cancellationToken: destroyCancellationToken);
                    x += Time.deltaTime;
                }

                // 도착 위치 설정
                transform.position = parabola.GetPosition(1f);
            }
            // 이동 속도 기반 이동
            else
            {
                float x = 0f;
                while (x < parabola.HorizontalLength)
                {
                    var position = parabola.GetPositionByRelativeX(x);
                    transform.position = position;

                    await UniTask.Yield(cancellationToken: destroyCancellationToken);
                    x += MoveSpeed * Time.deltaTime;
                }

                // 도착 위치 설정
                transform.position = parabola.GetPositionByRelativeX(parabola.HorizontalLength);
            }

            if (!gameObject || !gameObject.activeInHierarchy)
            {
                return;
            }
            if (indicator)
            {
                EffectManager.Instance.Get(EffectType.EnemyParabolaSpawnSplash).transform.position = indicator.transform.position;
                indicator.gameObject.SetActive(false);
            }

            IsRunningSpawnSequence = false;
            if (NavMeshAgent)
            {
                NavMeshAgent.enabled = oldNavMeshAgentEnabled;
                if(oldNavMeshAgentEnabled)
                    NavMeshAgent.Warp(transform.position);
            }

            InitializeCombinableOffScreenUI();
            if (UseAnimationWhenSpawnSequence)
            {
                Animator.SetTrigger(JumpLand);
                // Idle 대기
                await UniTask.WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), cancellationToken: destroyCancellationToken);
            }

            return;

            static async UniTaskVoid ExecuteAfter(float delay, Action func)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delay));
                func.Invoke();
            }
        }

        protected virtual void Start()
        {
            if (BehaviourTree)
            {
                BehaviourTree.SetVariable("TargetBattleArea", new SharedBattleArea { Value = TargetBattleArea });
            }

            if (_dissolveTargetMeshRenderer && _deadDissolveMaterialIndex >= 0)
            {
                _deadDissolveMaterial = _dissolveTargetMeshRenderer.materials[_deadDissolveMaterialIndex];
            }

            if (!_hitMaterial && _hitTintTargetMeshRenderer)
                _hitMaterial = _hitTintTargetMeshRenderer.materials[0];

            foreach (var settings in IceFrostbiteRenderers)
            {
                var r = settings.Renderer;
                if (!r)
                {
                    continue;
                }
                var materials = r.materials;
                if (settings.MaterialIndex >= materials.Length)
                {
                    Debug.LogWarning($"{name}의 Frostbite Renderer Index가 실제 Material 갯수보다 많음: {settings.MaterialIndex} > {materials.Length}", gameObject);
                    continue;
                }
                _frostbiteMaterialList.Add(r.materials[settings.MaterialIndex]);
            }
            TweenTargetFrostbiteProperty = FrostbiteProperty;

            HealthObservable
                .Where(value => value <= 0f)
                .Subscribe(OnDead)
                .AddTo(this);

            this.UpdateAsObservable()
                .Subscribe(UpdateKnockBackReduce)
                .AddTo(this);

            this.UpdateAsObservable()
                .Subscribe(UpdateFreezeState)
                .AddTo(this);

            this.OnCollisionEnterAsObservable()
                .Where(_ => IsFreezeSlipping)
                .Where(it => ((1 << it.gameObject.layer) & FreezeSlippingCollideMask.value) != 0)
                .Subscribe(OnFreezeSlippingCollide)
                .AddTo(this);
        }

        public void Initialize(BattleArea area)
        {
            TargetBattleArea = area;

            if (Settings.SpawnEffectType != EffectType.None)
            {
                var spawnEffect = EffectManager.Instance.Get(Settings.SpawnEffectType);
                spawnEffect.transform.position = transform.position;
            }
        }

        [field: SerializeField, FoldoutGroup("피격", true)]
        public UnityEvent<Monster> OnDeadEvent { get; private set; } = new();

        [field: SerializeField, FoldoutGroup("빙결", true)]
        public UnityEvent<Monster> OnFreezeEvent { get; private set; } = new();

        [field: SerializeField, FoldoutGroup("빙결", true)]
        public UnityEvent<Monster> OnFreezeSlipEvent { get; private set; } = new();

        private IDisposable _deadDissolveObserver = null;

        private void OnDead(float _)
        {
            Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            if (BehaviourTree)
            {
                BehaviourTree.SendEvent("Dead");
            }

            if (_hitTintTween != null)
            {
                _hitTintTween.Complete();
                _hitTintTween = null;
            }

            /*
            if (_dissolveParticle && _deadDissolveMaterial)
            {
                _dissolveParticle.Play();
                _deadDissolveTime = 0f;
                _deadDissolveObserver = this.UpdateAsObservable().Subscribe(_ =>
                {
                    _deadDissolveTime += Time.deltaTime;
                    _deadDissolveMaterial.SetFloat(AlphaClipThreshold,
                        _deadDissolveCurve.Evaluate(_deadDissolveTime));

//                       // TODO 나중에 사용할 때가 있으면 ... 플레이어 따라가는 코드
//                      if (!PlayerView)
//                          PlayerView = PlayerPresenter.View;
//
//                      var playerCenterPoint = PlayerView.CenterPoint;
//
//                       _dissolveParticle.SetVFXTransformProperty("PlayerTransform", playerCenterPoint,
//                           VFXExtensions.ApplyTarget.Position);
//
               });
           }
                */

            if (NavMeshAgent)
            {
                NavMeshAgent.enabled = false;
            }

            gameObject.layer = LayerMask.NameToLayer("Character Ignore");
            OnDeadEvent?.Invoke(this);
            DropItemOnDead();
            ReleaseCombinableOffScreenUI();
        }

        protected bool PreventDropItem = false;

        private void DropItemOnDead()
        {
            if (!Settings.DropTableOnDead) return;
            if (PreventDropItem) return;
            PreventDropItem = true;

            var type = Settings.DropTableOnDead.Instantiated.Get();
            if (type != ItemType.None)
            {
                var spawnPosition = transform.position;
                var itemObj = ItemManager.Instance.Get(type, spawnPosition, Quaternion.identity);
                if (!itemObj || !itemObj.TryGetComponent(out IItem item))
                {
                    return;
                }

                item.Initialize(spawnPosition);
            }
        }

        /// <summary>
        /// 일부 스킬에 의해 축적된 피해량입니다.
        /// </summary>
        public float StackedDamage { get; protected set; } = 0f;

        // 빙결 적용으로 인해 현재 생성된 빙하 VFX 오브젝트
        private Renderer _freezeEffect;

        /// <summary>
        /// 현재 남은 빙결 시간입니다. 0 이상이면 빙결 상태로 간주하며, 이 상태에서 피해를 받으면 빙결 해제가 됩니다.
        /// </summary>
        public float FreezeTime { get; protected set; } = 0f;

        /// <summary>
        /// 지금 빙결 상태인지 확인합니다.
        /// </summary>
        public bool IsFreezeComplete => FreezeLevel >= FreezeCompleteLevel;

        public bool IsFreeze => IsFreezeComplete;
        public float Height => Settings.Height;

        /// <summary>
        /// 빙결로 인한 미끄러짐 상태 여부입니다. true일 경우 FreezeTime이 줄지 않습니다.
        /// </summary>
        /// <returns></returns>
        public bool IsFreezeSlipping { get; private set; } = false;

        /// <summary>
        /// 빙결 밀림 상태가 지속된 시간입니다. 5초 이상 밀리면 자동으로 파괴됩니다.
        /// </summary>
        [field: SerializeField, FoldoutGroup("빙결/상태", true), ReadOnly]
        public float FreezeSlippingTime { get; private set; } = 0f;

        /// <summary>
        /// 빙결로 인한 미끄러짐 상태 여부입니다. true일 경우 FreezeTime이 줄지 않습니다.
        /// </summary>
        /// <returns></returns>
        public bool IsFreezeFalling { get; private set; } = false;

        public LayerMask FreezeSlippingCollideMask => Settings.FreezeSlippingCollideMask;

        /// <summary>
        /// 현재 남은 동상 지속 시간입니다. 남아있으면 빙결 피해를 받지 않습니다.
        /// </summary>
        public float FrostbiteTime { get; private set; } = 0f;

        /// <summary>
        /// 지금 동상 상태인지 확인합니다.
        /// </summary>
        public bool IsFrostbite => FrostbiteTime > 0f;

        /// <summary>
        /// 빙결시킬 수 있는지 확인합니다.
        /// </summary>
        public bool CanFreeze => Health > 0f;

        /// <summary>
        /// 빙결 단계입니다. Settings.MaxFreezeLevel 도달 시 빙결됩ㄴ디ㅏ.
        /// </summary>
        public int FreezeLevel { get; private set; } = 0;

        public int FreezeCompleteLevel => Settings.FreezeCompleteLevel;

        public int MaxFreezeLevel => Settings.MaxFreezeLevel;

        // public List<EffectType> FreezeEffectsByLevel => Settings.FreezeEffectsByLevel;
        public List<Material> FreezeEffectMaterialByLevel => Settings.FreezeEffectMaterialByLevel;

        public bool SlipGuided { get; set; } = false;

        [SerializeField, FoldoutGroup("빙결", true), UnityEngine.Tooltip("빙결 이펙트가 묶이는 위치입니다.")]
        private Transform _freezeEffectBindPosition;

        [SerializeField, FoldoutGroup("빙결", true)]
        private int _freezeEffectMaterialIndex = 1;


        private Vector3 _slipDebug_LastPosition;

        // 빙결 관련 상태 갱신
        private void UpdateFreezeState(Unit _)
        {
            var dt = Time.deltaTime;
            // 미끄러지는 중인 경우 시간 카운트 없음
            if (IsFreezeSlipping)
            {
                if (FreezeSlippingTime >= Settings.SlipMaxTime)
                {
                    IsFreezeSlipping = false;
                    OnFreezeBreak(timeExpire: true);
                    return;
                }

                var position = transform.position;
                Debug.DrawLine(position, _slipDebug_LastPosition, Color.white, 5f, false);
                Debug.DrawLine(position, position + Rigidbody.velocity * 0.1f, Color.cyan, 5f, false);
                _slipDebug_LastPosition = position;
                FreezeSlippingTime += dt;
                if (Rigidbody.velocity.sqrMagnitude <= Vector3.kEpsilon)
                {
                    FreezeSlippingTime += dt * Settings.SlipTimeMultiplierWhenNotMoving;
                }

                var origin = position + Vector3.up * Settings.Height * 0.5f;
                var axis = Vector3.Cross(Vector3.up, KnockBackDirection.normalized).normalized;
                var angle = Settings.SlipRotationSpeed * dt;
                // var oldRotation = GFX.transform.rotation;
                GFX.transform.RotateAround(origin, axis, angle);
                // var newRotation = GFX.transform.rotation;
                // Logger.Log($"RotateAround(origin={origin},angle={angle},before={oldRotation.eulerAngles},after={newRotation.eulerAngles})");

                if (IsFreezeFalling)
                {
                    // if (Rigidbody.velocity.y > 0f)
                    // {
                    // Rigidbody.velocity = Rigidbody.velocity.Copy(y: 0f);
                    // }
                    return;
                }

                // 바닥과 떨어진 경우 자유낙하
                if (!Physics.Raycast(transform.position + Vector3.up * Settings.Height * 0.5f, Vector3.down,
                        Settings.Height, LayerMask.GetMask("Ground", "Wall")))
                {
                    // Debug.Log($"{name} lost ground at {transform.position}", gameObject);
                    // Debug.DrawLine(transform.position, transform.position + Vector3.up * 5f, Color.yellow, 5f);
                    Rigidbody.isKinematic = false;
                    Rigidbody.constraints = RigidbodyConstraints.None;
                    IsFreezeFalling = true;
                }
                // 아니면 속도 유지
                else
                {
                    // Debug.Log($"{name} keep velocity {KnockBackDirection}", gameObject);
                    // Debug.DrawLine(transform.position, transform.position + KnockBackDirection, Color.yellow, 5f);
                    Rigidbody.velocity = KnockBackDirection;
                }

                return;
            }

            if (Health <= 0f) return;
            if (FreezeTime > 0f)
            {
                FreezeTime -= dt;
                // 시간 다 달아서 ...
                if (FreezeTime <= 0f)
                {
                    OnFreezeTimeExpired();
                }
            }

            /*
            if (FrostbiteTime > 0f)
            {
                FrostbiteTime -= dt;
                if (FrostbiteTime <= 0f)
                {
                    OnFrostbiteEnd();
                }
            }
            */
        }

        protected virtual void OnFreezeTimeExpired()
        {
            var newFreezeLevel = FreezeLevel;
            // 단순 빙결 단계 감소
            if (FreezeLevel > 0)
            {
                newFreezeLevel = FreezeLevel - 1;
            }

            UpdateFreezeLevel(newFreezeLevel);
        }

        public void StartFreezeSlipping(Vector3 knockBackPower)
        {
            // NavMeshAgent 해제.
            // 어차피 이 시점까지 오면 다시 NavMesh 받아서 걸어다닐 일은 없다는 뜻  
            NavMeshAgent.enabled = false;

            // 밀림 상태로 전환. Rigidbody 특정 방향 고정 미끄러짐 시작
            Rigidbody.isKinematic = false;
            Rigidbody.constraints = RigidbodyConstraints.None;
            Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            IsFreezeSlipping = true;
            IsFreezeFalling = true;
            FreezeSlippingTime = 0f;
            KnockBackDirection = knockBackPower;
            Debug.DrawLine(transform.position, KnockBackDirection * 10f, Color.magenta, 5f);
            _slipDebug_LastPosition = transform.position;


            // 방향 보정: 현재 설정된 방향 진로 내의 몬스터 방향으로 보정
            if (SlipCompensateSensor && KnockBackDirection.sqrMagnitude > 0f)
            {
                const float Time = 10f;
                // static void DrawMonsterMesh(Monster m, Color color, float time = Time)
                // {
                // var renderer = m._hitTintTargetMeshRenderer as SkinnedMeshRenderer;
                // renderer?.sharedMesh?.DrawMesh(DrawUtility.DebugXDrawer(color, time), renderer.transform);
                // }

                // DrawMonsterMesh(this, Color.white);

                var origin = transform.position;
                var originalStrength = knockBackPower.magnitude;
                var originalDirection = knockBackPower / originalStrength;
                SlipCompensateSensor.transform.forward = originalDirection;
                var radius = SlipCompensateSensor.Radius;
                DebugX.DrawLine(origin, origin + originalDirection * radius, Color.white, Time);
                var leftRotator = Quaternion.AngleAxis(SlipCompensateSensor.Angle * 0.5f, Vector3.up);
                DebugX.DrawLine(origin, origin + (leftRotator * originalDirection) * radius, Color.white.Copy(a: 0.5f),
                    Time);
                DebugX.DrawLine(origin, origin + (Quaternion.Inverse(leftRotator) * originalDirection) * radius,
                    Color.white.Copy(a: 0.5f), Time);
                IFreezable target = null;
                float highestWeight = -1f;
                foreach (var obj in SlipCompensateSensor.FilteredPulse())
                {
                    if (obj.GetInstanceID() == GetInstanceID() || !obj.TryGetComponent(out IFreezable m) ||
                        m.SlipGuided)
                    {
                        continue;
                    }

                    // DrawMonsterMesh(m, Color.yellow.Copy(a: 0.3f));
                    var toMonsterDirection = (m.transform.position - origin).Copy(y: 0f).normalized;
                    DebugX.DrawLine(origin, m.transform.position, Color.yellow.Copy(a: 0.3f), Time);
                    var dot = Vector3.Dot(toMonsterDirection, originalDirection);

                    var weight = dot + (m.IsFreeze ? Settings.SlipCompensateWeightToFreeze : 0f);
                    if (weight > highestWeight)
                    {
                        highestWeight = weight;
                        target = m;
                    }
                }

                if (target != null)
                {
                    // DrawMonsterMesh(target, Color.green);
                    target.SlipGuided = true;
                    KnockBackDirection = (target.transform.position - origin).Copy(y: 0f).normalized * originalStrength;
                }
            }

            Rigidbody.velocity = KnockBackDirection;
            DropItemOnDead();
            ReleaseCombinableOffScreenUI();
            OnFreezeSlipEvent?.Invoke(this);
        }

        private readonly ContactPoint[] _slipContacts = new ContactPoint[8];

        // 빙결 미끄러짐 중 충돌 시
        private void OnFreezeSlippingCollide(Collision c)
        {
            // 지형 충돌은 충돌각 제한 필요
            if (!c.collider.CompareTag("Enemy") && !c.collider.CompareTag("Destructible"))
            {
                int contactCount = c.GetContacts(_slipContacts);
                // 접점 normal 평균
                Vector3 normal = Vector3.zero;
                Vector3 impulse = Vector3.zero;
                Vector3 relativeVelocity = c.relativeVelocity;
                for (int i = 0; i < contactCount; ++i)
                {
                    var contact = _slipContacts[i];
                    normal += contact.normal;
                    impulse += contact.impulse;

                    DrawUtility.DrawWireSphere(contact.point, 0.1f, 16, DrawUtility.DebugDrawer(Color.red, 3f, false));
                    Debug.DrawLine(contact.point, contact.point + contact.normal * 2f, Color.red, 3f, false);
                    // Debug.DrawLine(contact.point, contact.point + contact.impulse * 2f, Color.red, 3f, false);   
                }

                normal *= (1f / contactCount);
                normal.Normalize();
                impulse *= (1f / contactCount);
                impulse.Normalize();
                var impulseForce = c.impulse.magnitude;

                Vector3 dotTarget = -relativeVelocity.normalized;
                // 충돌 normal이 velocity와 일정 각도 이하면 충돌하지 않음
                // velocity와 normal이 0이상일 경우는 적으므로, 이 경우는 [-1, 0]의 범위에서 생각
                // dot이 임계값보다 큰 경우 => 각도가 임계값보다 작은 경우 충돌로 판정하지 않음  
                float angleInCos = Vector3.Dot(normal, dotTarget);
#if UNITY_EDITOR
                // Debug.DrawLine(transform.position, transform.position + Rigidbody.velocity.normalized, Color.yellow, 3f, false);
#endif
                if (angleInCos >= Settings.SlipCollideThresholdAngleInCos &&
                    impulseForce < Settings.SlipCollisionImpulseForce)
                {
                    for (int i = 0; i < contactCount; ++i)
                    {
                        var contact = _slipContacts[i];
                        Debug.DrawLine(contact.point, contact.point + dotTarget * 2f, Color.magenta, 3f, false);
                        // Debug.DrawLine(contact.point, contact.point + contact.impulse * 2f, Color.red, 3f, false);   
                    }

                    Debug.DrawRay(transform.position, c.impulse, Color.green, 3f, false);
                    Debug.Log($"[Slip] Collision {name} with <color=yellow>{c.collider.name}</color>:" +
                              $" angleInCos={angleInCos:F3} <color=red>>=</color> threshold={Settings.SlipCollideThresholdAngleInCos:F3} " +
                              $"<color=red>ignored</color> <color=lime>(impulse={impulseForce})</color>", c.collider);
                    return;
                }

                for (int i = 0; i < contactCount; ++i)
                {
                    var contact = _slipContacts[i];
                    DrawUtility.DrawWireSphere(contact.point, 0.1f, 16,
                        DrawUtility.DebugDrawer(Color.yellow, 3f, false));
                    Debug.DrawLine(contact.point, contact.point + contact.normal * 2f, Color.yellow, 3f, false);
                    Debug.DrawLine(contact.point, contact.point + dotTarget * 2f, Color.magenta, 3f, false);
                    // Debug.DrawLine(contact.point, contact.point + contact.impulse * 2f, Color.red, 3f, false);   
                }

                Debug.Log(
                    $"[Slip] Collision {name} with <color=yellow>{c.collider.name}</color>: angleInCos={angleInCos:F3} <color=yellow><</color> threshold={Settings.SlipCollideThresholdAngleInCos:F3} <color=yellow>COLLISION</color> <color=lime>(impulse={impulseForce})</color>",
                    c.collider);
            }
            else
            {
                Debug.Log(
                    $"[Slip] Collision {name} with <color=yellow>{c.collider.name}</color>({c.collider.tag}) just COLLISION",
                    c.collider);
            }

            IsFreezeSlipping = false;
            KnockBackDirection = c.impulse;
            OnFreezeBreak();

            // 충돌 대상이 적일 경우
            if (c.gameObject.TryGetComponent(out IEntity enemy))
            {
                enemy.Damage(EnemyDamageInfo.Get(0, gameObject));
            }
        }

        protected bool IsBullet(GameObject obj) => obj.CompareTag("PlayerBullet");
        protected bool IsTrap(GameObject obj) => obj.CompareTag("Trap");

        protected bool GetBullet(GameObject obj, out PlayerBullet bullet)
        {
            bullet = null;
            return IsBullet(obj) && obj.TryGetComponent(out bullet);
        }

        /// <summary>
        /// 몬스터를 주어진 정보로 타격합니다. EnemyDamageInfo.Get을 통해 타격 정보를 설정할 수 있습니다. 
        /// </summary>
        public virtual EntityHitResult Damage(DamageInfo info)
        {
            // 이미 죽었거나, 물리 효과를 받고 있으면 완전히 무시함
            if (Health <= 0f || IsFreezeSlipping || IsFreezeFalling)
            {
                return EntityHitResult.Ignored;
            }

            // 소환 중이면 단순 무적
            if (IsRunningSpawnSequence)
            {
                return EntityHitResult.Invincible;
            }

            float freezePower;
            if (info is EnemyDamageInfo enemyDamageInfo)
            {
                freezePower = enemyDamageInfo.FreezeFactor;
            }
            else
            {
                freezePower = 1f;
            }

            void AddFreezeLevel(float amount = 1)
            {
                UpdateFreezeLevel(FreezeLevel + (int)amount);
            }


            // 완전 빙결 상태 중에 공격 받았을 때
            if (IsFreezeComplete)
            {
                var isBulletAttack = IsBullet(info.Source);
                // 맞았을 때 파괴 레벨 안 넘어가는 경우 증가만
                if (isBulletAttack && FreezeLevel + freezePower <= MaxFreezeLevel)
                {
                    info.Amount = 0f;
                    AddFreezeLevel(freezePower);
                }
                // 이번 거 맞으면 터지는 경우의 탄환 피격일 경우 즉시 파괴
                else if (isBulletAttack
                         // 빙결 밀림으로 인한 피격일 경우 즉시 파괴
                         || info.Source.TryGetComponent(out IFreezable m) && m.IsFreeze
                         // 함정에 의한 공격일 경우 즉시 파괴
                         || IsTrap(info.Source)
                        )
                {
                    info.Amount = 0f;
                    OnFreezeBreak(info);
                }
                // Push인데 일반 타격 받은 경우 공격 방향으로 밀림 시작
                else if (info.KnockBack.IsValid())
                {
                    StartFreezeSlipping(info.KnockBack.Direction * info.KnockBack.FreezeSlippingAmount);

                    return EntityHitResult.Success;
                }
                else return EntityHitResult.Invincible;
            }
            // 빙결 공격일 때 & 빙결 가능 상태일 경우(동상 상태가 아닐 경우)
            else if (info is EnemyDamageInfo { Reaction: DamageReaction.Freeze } && CanFreeze)
            {
                info.Amount = 0f;
                AddFreezeLevel(freezePower);
            }
            else if (info.Reaction == DamageReaction.KnockBack)
            {
                BehaviourTree?.SendEvent("KnockBack");
            }
            else if (info.Reaction == DamageReaction.Stun)
            {
                BehaviourTree?.SendEvent("Damage");
            }

            var damage = info.Amount;
            switch (info.Mode)
            {
                case DamageMode.Normal:
                    Health -= damage;
                    break;
                case DamageMode.Stack:
                    StackedDamage += damage;
                    break;
                case DamageMode.PopAll:
                    Health -= damage + StackedDamage;
                    StackedDamage = 0f;
                    break;
            }

            HitTint();
            HitEffect(info);

            OnKnockBack(info);
            OnDamageEnd(info);

            return EntityHitResult.Success;
        }

        /// <summary>
        /// Damage에 의해 넉백 시 다음을 호출합니다.
        /// <code>KnockBack(info.KnockBack)</code> 
        /// </summary>
        /// <param name="info">Damage 정보입니다.</param>
        protected virtual void OnKnockBack(DamageInfo info)
        {
            // 빙결 중에는 넉백 X
            if (IsFreezeComplete)
            {
                return;
            }

            // 체력 0 이상일 때 || RagDollChanger가 없을 때 || KnockBack이 유효하지 않을 때는 일반 넉백 적용
            if (Health > 0f)
            {
                KnockBack(info.KnockBack);
                return;
            }

            // 죽을 때 radDollChanger가 있는 경우
            Vector3 playerPosition = info.Source.transform.position;
            Vector3 monsterPosition = transform.position;
            Vector3 direction = monsterPosition - playerPosition;
            direction.Normalize();
        }

        /// <summary>
        /// 빙하 파괴로 인한 빙결 해제
        /// </summary>
        /// <param name="info"></param>
        public virtual void OnFreezeBreak(DamageInfo info = null, bool timeExpire = false)
        {
            Debug.Log($"OnFreezeBreak on {name} by {info?.Source}", gameObject);
            // 피해 과정 중 발생한 파괴일 경우, info에 전달
            if (info != null)
            {
                // 빙결 파괴 & 즉사
                info.Amount += PlayerView.Settings.FreezeBreakDamage;

                // 빙결 파괴 시에는 넉백 무효
                // _ragDollChanger.ChangeRagDoll(info.KnockBack.IsValid() 
                // ? info.KnockBack.Direction * info.KnockBack.FreezeSlippingAmount
                // : (transform.position - info.Source.transform.position).normalized * 0.1f
                // );
                info.KnockBack = KnockBackInfo.None;
            }
            else
            {
                Health = 0f;
                // _ragDollChanger.ChangeRagDoll(KnockBackDirection);
            }

            // 빙결 해제 처리
            // OnUnFreeze();

            // 빙하 VFX 제거
            if (_freezeEffect)
            {
                var originalTransform = _freezeEffect.transform;
                if (!timeExpire)
                {
                    var effect = EffectManager.Instance.Get(EffectType.EnemyFreezeMeshBreak);
                    effect.transform.SetPositionAndRotation(originalTransform.position, originalTransform.rotation);
                    if (effect.TryGetComponent(out RayfireRigid rigid))
                    {
                        rigid.Fade();
                    }

                    if (effect.TryGetComponent(out RayFireUtility util))
                    {
                        // 빙결 단계 중 5, 6, 7, 8이 전부 Material이 다르므로 이를 조각 Mesh Material에도 반영
                        var originalMaterial = _freezeEffect.GetComponent<Renderer>().material;
                        foreach (var r in util.SegmentRenderers)
                        {
                            r.material = originalMaterial;
                        }
                    }

                    // 3초 뒤 조각 비활성화
                    async UniTaskVoid DisableAfter(GameObject obj, float time)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(time));
                        obj.gameObject.SetActive(false);
                    }

                    DisableAfter(effect, 3f).Forget();
                }

                _freezeEffect.gameObject.SetActive(false);
                _freezeEffect = null;
            }

            if (!timeExpire)
            {
                // 빙하 파괴 VFX
                var breakEffect = EffectManager.Instance.Get(EffectType.EnemyIceCollide);
                breakEffect.transform.position = transform.position;
            }

            RenderersEnabled = false;
        }

        /// <summary>
        /// 빙결 처리
        /// </summary>
        /// <param name="info"></param>
        protected virtual void OnFreeze(DamageInfo info, int oldFreezeLevel, int newFreezeLevel)
        {
            // 빙결 시간 적용
            FreezeTime = PlayerView.Settings.FreezeTime; // 이게 맞나?
            // 빙하 VFX 갱신
            // Logger.Log($"{name} - OnFreeze({oldFreezeLevel} -> {newFreezeLevel})", gameObject);
            UpdateFreezeLevelEffect(oldFreezeLevel, newFreezeLevel);
        }

        private void UpdateFrostbiteProperty(float oldFrostbite, float newFrostbite)
        {
            _frostbitePropertyTween?.Complete();
            if (!UseFrostbiteTween || newFrostbite > oldFrostbite)
            {
                // 단계 증가는 즉시 빙결 (왜? 총알에 맞는 경우니까)
                FrostbiteProperty = newFrostbite;
                UseFrostbiteTween = true;
            }
            else
            {
                // 현재 설정된 FreezeTime동안 Tween (왜? 시간에 따라 가니까)
                _frostbitePropertyTween = DOTween.To(
                    () => FrostbiteProperty, 
                    (value) => FrostbiteProperty = value,
                    newFrostbite, 
                    FreezeTime >= 0f ? FreezeTime : PlayerView.Settings.FreezeTime
                );
                _frostbitePropertyTween.SetEase(Ease.Linear);
                _frostbitePropertyTween.onComplete += () => _frostbitePropertyTween = null;
            }
        }

        protected bool UseFrostbiteTween { get; set; } = true;
        private Tween _frostbitePropertyTween;
        protected virtual void UpdateFreezeLevelEffect(int oldFreezeLevel, int newFreezeLevel)
        {
            EnemySettings.FrostbiteFactor frostbite;
            if (Settings.FrostbiteByLevel.Count > 0)
            {
                frostbite = Settings.FrostbiteByLevel[Mathf.Min(FreezeLevel, Settings.FrostbiteByLevel.Count-1)];
            }else
            {
                frostbite = new EnemySettings.FrostbiteFactor { ColorThreshold = 1f, AnimationSpeed = .5f };
            }

            var oldFrostbite = FrostbiteProperty;
            var newFrostbite = frostbite.ColorThreshold;
            UpdateFrostbiteProperty(oldFrostbite, newFrostbite);
            if (FreezeLevel < FreezeCompleteLevel)
            {
                // 이번에 완전 빙결 해제된 경우 UnFreeze 신호 전송
                if (oldFreezeLevel == FreezeCompleteLevel)
                {
                    BehaviourTree?.SendEvent("UnFreeze");
                }

                RenderersEnabled = true;

                // 애니메이션 둔화
                Animator.speed = frostbite.AnimationSpeed;
                // NavMesh 속도 갱신
                if (NavMeshAgent && NavMeshAgent.enabled)
                {
                    NavMeshAgent.avoidancePriority = _initialNavMeshPriority;
                    NavMeshAgent.speed = MoveSpeed;
                }

                // Logger.Log($"UpdateFreezeLevelEffect({oldFreezeLevel} -> {newFreezeLevel}) - AnimationSpeed={frostbite.AnimationSpeed}");
                // 동상 셰이더 값 설정
            }
            else
            {
                bool isFrozenNow = oldFreezeLevel < FreezeCompleteLevel;
                // 이번에 완전 빙결된 경우 Freeze 신호 전송
                if (isFrozenNow)
                {
                    StopStunEffect();
                    BehaviourTree?.SendEvent("Freeze");
                    OnFreezeEvent?.Invoke(this);
                    if (!Settings.FreezeCompleteSound.IsNull)
                        AudioManager.Instance.PlayOneShot(Settings.FreezeCompleteSound, transform.position);
                }

                // 애니메이션 Idle로 전환
                if (!string.IsNullOrWhiteSpace(Settings.AnimationStateNameWhenFreezeComplete))
                {
                    Animator.Play(Settings.AnimationStateNameWhenFreezeComplete);
                }

                // 애니메이션 정지
                Animator.speed = 0f;
                // NavMesh 정지
                if (NavMeshAgent && NavMeshAgent.enabled)
                {
                    NavMeshAgent.isStopped = true;
                    NavMeshAgent.velocity = Vector3.zero;
                    NavMeshAgent.avoidancePriority = Settings.NavMeshPriorityOnFreeze;
                }

                // foreach (var r in Renderers)
                // {
                    // if (r) r.enabled = false;
                // }

                if (Settings.UseFallWhenFreezeOnAir && isFrozenNow)
                {
                    if (NavMeshAgent && NavMeshAgent.isOnOffMeshLink)
                    {
                        Debug.Log(
                            $"{name} - UseFallWhenFreezeOnAir={Settings.UseFallWhenFreezeOnAir} while NavMeshOffLink",
                            gameObject);
                        StartFreezeSlipping(NavMeshAgent.velocity);
                    }
                    // 바닥과 거리가 꽤 떨어져있는 경우
                    else if (!Physics.Raycast(
                                 new Ray(transform.position, Vector3.down),
                                 out var hitInfo,
                                 Settings.FreezeDistanceFromGroundThreshold,
                                 LayerMask.GetMask("Ground", "Wall")
                             ))
                    {
                        Debug.Log(
                            $"{name} - UseFallWhenFreezeOnAir={Settings.UseFallWhenFreezeOnAir} no hit so start slipping",
                            gameObject);
                        // 그냥 공중에서 낙하 시작
                        StartFreezeSlipping(Vector3.zero);
                    }
                    else
                    {
                        UnityEngine.Debug.Log(
                            $"{name} - UseFallWhenFreezeOnAir={Settings.UseFallWhenFreezeOnAir}, hit with {hitInfo.collider.name}",
                            hitInfo.collider);
                    }
                }
            }

            var arr = FreezeEffectMaterialByLevel;
            if (FreezeLevel < arr.Count)
            {
                var target = arr[FreezeLevel];
                if (FreezeLevel >= FreezeCompleteLevel && target)
                {
                    if (!_freezeEffect)
                    {
                        var freezeObj = EffectManager.Instance.Get(Settings.FreezeEffectType);
                        freezeObj.TryGetComponent(out _freezeEffect);
                        if (_freezeEffect.TryGetComponent(out FakeChild f))
                        {
                            f.TargetParent = _freezeEffectBindPosition;
                            f.Follow(true);
                        }
                    }

                    var mats = _freezeEffect.sharedMaterials;
                    mats[_freezeEffectMaterialIndex] = target;
                    _freezeEffect.sharedMaterials = mats;
                }
                else
                {
                    if (_freezeEffect)
                    {
                        _freezeEffect.gameObject.SetActive(false);
                        _freezeEffect = null;
                    }
                }
            }
        }

        protected virtual void UpdateFreezeLevel(int newFreezeLevel)
        {
            var oldFreezeLevel = FreezeLevel;
            // Debug.Log($"{name} : UpdateFreezeLevel({oldFreezeLevel} => {newFreezeLevel})");
            // 비-빙결 ~ 완전 빙결 이상으로 넘어가려 한 경우 완전 빙결 1단계로 제한
            if (oldFreezeLevel < FreezeCompleteLevel && newFreezeLevel >= FreezeCompleteLevel)
            {
                // Debug.Log($"{name} : ({oldFreezeLevel} => {newFreezeLevel}) limited into {FreezeCompleteLevel}");
                newFreezeLevel = FreezeCompleteLevel;
            }

            FreezeLevel = newFreezeLevel;

            if (newFreezeLevel > MaxFreezeLevel)
            {
                // Debug.Log($"{name} : ({oldFreezeLevel} => {newFreezeLevel}) overed MaxFreezeLevel, Break");
                OnFreezeBreak(null);
                return;
            }

            // 냉동 해제
            if (newFreezeLevel <= 0)
            {
                OnUnFreeze();
                return;
            }

            OnFreeze(null, oldFreezeLevel, newFreezeLevel);
        }

        /// <summary>
        /// 빙결 해제
        /// </summary>
        protected virtual void OnUnFreeze()
        {
            // 동상 시간 풀리는 설정
            UpdateFrostbiteProperty(FrostbiteProperty, -0.02f);
            // 빙결 시간 초기화
            FreezeTime = 0f;
            // 애니메이션 둔화 해제
            Animator.speed = 1f;
            // 빙하 VFX 제거
            if (_freezeEffect)
            {
                _freezeEffect.gameObject.SetActive(false);
                _freezeEffect = null;
            }

            BehaviourTree?.SendEvent("UnFreeze");
        }

        /// <summary>
        /// 동상 해제
        /// </summary>
        protected virtual void OnFrostbiteEnd()
        {
            FreezeTime = 0f;
            FrostbiteTime = 0f;
            Animator.speed = 1f;
            NavMeshAgent.speed = MoveSpeed; // 이동속도 초기화: BT에서 기존 speed때문에 느려져 있던 거 수정 ...
            // 동상 셰이더 해제
            FrostbiteProperty = 0f;
        }

        /// <summary>
        /// Damage에 의해 넉백 시 다음을 호출합니다.
        /// <code>info.Release();</code> 
        /// </summary>
        /// <param name="info">Damage 정보입니다.</param>
        protected virtual void OnDamageEnd(DamageInfo info)
        {
            info.Release();
        }

        private Tween _hitTintTween;

        protected void HitTint()
        {
            _hitTintTween?.Complete();

            if (!_hitTintTargetMeshRenderer)
                return;

            if (!_hitMaterial && _hitTintTargetMeshRenderer)
                _hitMaterial = _hitTintTargetMeshRenderer.materials[0];

            var oldColor = _hitMaterial.color;
            _hitMaterial.color = _hitTintColor;
            _hitTintTween = _hitMaterial
                .DOColor(oldColor, _hitTintTime)
                .SetEase(Ease.InOutCirc);
            _hitTintTween.onComplete += () => _hitTintTween = null;
        }

        private StunEffect _stunEffect;

        protected void HitEffect(DamageInfo info)
        {
            var height = Settings.Height;
            if (Settings.HitEffectType != EffectType.None)
            {
                var effect = EffectManager.Instance.Get(Settings.HitEffectType);
                effect.transform.position = transform.position + Vector3.up * (height * 0.5f);

                // TODO 히트 이펙트는 일단 시간정지에 영향 안 받게 ... 나중에 수정해야할지도? 
                if (effect.TryGetComponent(out ParticleSystemRoot ps))
                {
                    ps.UseUnscaledTime = true;
                }
            }

            // 스턴 & 이펙트 있음 & 빙결 중 아니면 스턴 이펙트 적용
            if (info.Reaction == DamageReaction.Stun && Settings.StunEffectType != EffectType.None && !IsFreeze)
            {
                if (_stunEffect)
                {
                    _stunEffect.Stop();
                }

                var effect = EffectManager.Instance.Get(Settings.StunEffectType);
                if (effect.TryGetComponent(out FakeChild f))
                {
                    f.LocalTransform.Position = Vector3.up * (height * Settings.StunEffectHeightMultiplier);
                    f.TargetParent = transform;
                }

                _stunEffect = effect.GetComponent<StunEffect>();
            }
        }

        public void StopStunEffect()
        {
            if (_stunEffect) _stunEffect.Stop();
            _stunEffect = null;
        }

        private enum KnockBackType
        {
            Linear,
            Curved,
        }

        private KnockBackType _knockBackType = KnockBackType.Linear;
        private AnimationCurve _knockBackVelocityCurve;
        private float _knockBackTime = 0f;
        private static readonly int Control = Shader.PropertyToID("_Control");
        public Vector3 KnockBackDirection { get; set; }

        /// <summary>
        /// 뒤로 날려갑니다.
        /// </summary>
        public virtual void KnockBack(in KnockBackInfo info)
        {
            if (!info.IsValid()) return;
            _knockBackType = KnockBackType.Linear;
            // Rigidbody.isKinematic = false;
            // Rigidbody.velocity = Vector3.zero;
            // Vector3 direction = (transform.position - from).normalized;
            Vector3 direction = info.Direction;
            // Rigidbody.AddForce(direction * (knockBackPower * Settings.KnockBackPower), mode);

            var mode = info.ForceMode;
            var knockBackPower = info.Amount;
            var knockBackIntensity = (mode == ForceMode.Force || mode == ForceMode.Impulse)
                ? Settings.KnockBackPower
                : 1f;

            KnockBackDirection = direction * (knockBackPower * knockBackIntensity);
            // DrawUtility.DrawWireSphere(from, 0.1f, 32, (a, b) => DebugX.DrawLine(a, b, Color.yellow, 5f));
            // DebugX.DrawLine(from, from + _knockBackPower, Color.yellow, 5f);
            _knockBackTime = info.Time;
            if (NavMeshAgent)
            {
                // NavMeshAgent.updatePosition = false;
                NavMeshAgent.velocity = KnockBackDirection;
                NavMeshAgent.isStopped = true;
                NavMeshAgent.ResetPath();
                NavMeshAgent.angularSpeed = 0f;
            }
        }

        public void KnockBackCurved(Vector3 direction, AnimationCurve velocityCurve)
        {
            _knockBackType = KnockBackType.Curved;
            direction.Normalize();
            _knockBackVelocityCurve = velocityCurve;
            KnockBackDirection = direction;
            _knockBackTime = _knockBackVelocityCurve.GetLength();
            if (NavMeshAgent)
            {
                // NavMeshAgent.updatePosition = false;
                NavMeshAgent.velocity = KnockBackDirection * _knockBackVelocityCurve[0].value;
                NavMeshAgent.isStopped = true;
                NavMeshAgent.ResetPath();
                NavMeshAgent.angularSpeed = 0f;
            }
        }

        // 넉백 업데이트
        private void UpdateKnockBackReduce(Unit _)
        {
            if (!NavMeshAgent || !NavMeshAgent.enabled) return;
            if (_knockBackTime <= 0f)
            {
                return;
            }

            _knockBackTime -= Time.deltaTime;
            if (_knockBackTime <= 0f)
            {
                // Rigidbody.velocity = Vector3.zero;
                // Rigidbody.angularVelocity = Vector3.zero;
                // Rigidbody.isKinematic = true;
                NavMeshAgent.angularSpeed = _initialAngularSpeed;
                NavMeshAgent.velocity = Vector3.zero;
                NavMeshAgent.updatePosition = true;
            }
            else
            {
                switch (_knockBackType)
                {
                    case KnockBackType.Linear:
                        NavMeshAgent.velocity = KnockBackDirection;
                        break;
                    case KnockBackType.Curved:
                        // 1-t로 그래프 설정
                        var t = (_knockBackVelocityCurve.GetLength() - _knockBackTime);
                        var value = _knockBackVelocityCurve.Evaluate(t);
                        Debug.Log($"<color=yellow>t: {t}, value: {value}</color>");
                        NavMeshAgent.velocity = KnockBackDirection * value;
                        break;
                }
            }
        }

        public virtual void AttackByRangeSensor(float damage)
        {
            if (!AttackRangeSensor) return;
            AttackRangeSensor.Pulse();
            foreach (var detection in AttackRangeSensor.Detections)
            {
                if (detection.CompareTag("Player") && detection.TryGetComponent(out PlayerPresenter player))
                {
                    player.Damage(damage, gameObject);
                    if (AttackRangeSensorEffectType != EffectType.None)
                    {
                        var effect = EffectManager.Instance.Get(AttackRangeSensorEffectType);
                        effect.transform.position = player.transform.position;
                    }

                    DebugX.Log("플레이어 피격 by SpearMonster");
                }
            }
        }

        #region SharedProperty

        public float MaximumHealth => Settings.Health;
        public float MoveSpeed => Animator.speed * Settings.MovementSpeed;
        public float AttackPower => Settings.AttackPower;
        public float AttackStartRange => Settings.AttackStartRange;
        public float AttackCooldown => Settings.AttackCooldown;
        public float StaggerTime => Settings.StaggerTime;
        public float StunTime => Settings.StunTime;
        public float KnockBackPower => Settings.KnockBackPower;

        [field: SerializeField, BoxGroup("GFX")]
        public GameObject GFX { get; set; }

        [field: SerializeField, BoxGroup("GFX")]
        public List<Renderer> Renderers { get; private set; } = new();
        public bool RenderersEnabled
        {
            get => Renderers[0].enabled;
            set
            {
                foreach (var r in Renderers)
                {
                    if (r) r.enabled = value;
                }
            }
        }
        
        [field: SerializeField, BoxGroup("GFX")]
        public List<MagicaCloth> Cloths { get; private set; } = new();
        public float ClothTimeScale
        {
            get => Cloths[0].GetTimeScale();
            set
            {
                foreach (var cloth in Cloths)
                {
                    cloth.SetTimeScale(value);
                }
            }
        }

        public bool IsPlayerDead => PlayerPresenter.Model.IsDead;

        #endregion

        private void OnDisable()
        {
            _frostbitePropertyTween?.Kill();
            _frostbitePropertyTween = null;
            _deadDissolveObserver?.Dispose();
            _deadDissolveObserver = null;
            gameObject.layer = LayerMask.NameToLayer("Enemy");
        }

        [Sirenix.OdinInspector.Button]
        private void TestTaskDamage()
        {
            Health -= 34;
        }

        public void SetTargetBattleArea(BattleArea value)
        {
            TargetBattleArea = value;
            BehaviourTree?.SetVariable("TargetBattleArea", new SharedBattleArea { Value = TargetBattleArea });
        }

        [Sirenix.OdinInspector.Button("가장 가까운 전투 구역 연결")]
        private void AutoBindEnemiesInHierarchy()
        {
            if (!DebugMode)
                return;

            var areas = FindObjectsOfType<BattleArea>();
            BattleArea nearestArea = null;
            var nearestDistance = float.PositiveInfinity;
            var origin = transform.position;
            foreach (var area in areas)
            {
                var closest = area.ClosestPoint(transform.position);
                var distanceSquared = (closest - origin).sqrMagnitude;
                if (nearestDistance <= Vector3.kEpsilon)
                {
                    TargetBattleArea = area;
                    return;
                }

                if (distanceSquared < nearestDistance)
                {
                    nearestDistance = distanceSquared;
                    nearestArea = area;
                }
            }

            if (nearestArea)
            {
                TargetBattleArea = nearestArea;
            }
            else
            {
                DebugX.LogError("배정 실패: 가장 가까운 Battle Area 찾는데 실패");
            }
        }

        protected virtual void OnDrawGizmos()
        {
            if (!Settings) return;
            Gizmos.color = Color.magenta;

            var attackStartRange = Settings.AttackStartRange;

            DrawUtility.DrawCircle(transform.position, attackStartRange, Vector3.up, 16,
                Gizmos.DrawLine);
        }

        #region 사운드

        [field: SerializeField, BoxGroup("사운드")]
        public LocalKeyList Sounds { get; private set; }
        
        // [field: SerializeField, BoxGroup("사운드/Beyar")]
        // public FMODAudioSource BeyarAudioSource { get; private set; }
        

        private static AudioManager AudioManager => AutoManager.Get<AudioManager>();
        public void PlaySoundOnce(string key) => PlaySoundOnce(key, transform.position);
        public void PlaySoundOnce(string key, Vector3 position)
        {
            if (!Sounds.TryFindClip(key, out var clip))
            {
                Debug.Log($"{key}에 해당하는 클립 없음 !!!", Sounds);
                return;
            }
            
            AudioManager.PlayOneShot(clip, position);
        }

        #endregion
    }
}