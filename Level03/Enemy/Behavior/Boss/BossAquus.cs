using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Character.Presenter;
using Cysharp.Threading.Tasks;
using Damage;
using Dummy.Scripts;
using Effect;
using EnumData;
using FMODPlus;
using Managers;
using ManagerX;
using Micosmo.SensorToolkit;
using Settings.Boss;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Splines;
using Utility;
using Logger = NKStudio.Logger;
using Random = UnityEngine.Random;

namespace Enemy.Behavior.Boss
{
    public enum RotationState
    {
        None,
        LookDefaultDirection,
        LookTargetPosition,
        LookPlayer,
    }
    public class SharedRotationState : SharedVariable<RotationState>
    {
        public static implicit operator SharedRotationState(RotationState value)
        {
            return new SharedRotationState { mValue = value };
        }
    }
    [TaskCategory("Unity/SharedVariable")]
    [TaskDescription("Sets the SharedRotationState variable to the specified object. Returns Success.")]
    public class SetSharedRotationState : BehaviorDesigner.Runtime.Tasks.Action
    {
        [UnityEngine.Tooltip("The value to set the SharedRotationState to")]
        public SharedRotationState targetValue;
        [RequiredField]
        [UnityEngine.Tooltip("The SharedRotationState to set")]
        public SharedRotationState targetVariable;

        public override TaskStatus OnUpdate()
        {
            targetVariable.Value = targetValue.Value;

            return TaskStatus.Success;
        }

        public override void OnReset()
        {
            targetValue = RotationState.None;
            targetVariable = RotationState.None;
        }
    }
    public class BossAquus : Monster
    {
        private BossAquusSettings _settings;
        public BossAquusSettings BossSettings => _settings ??= (BossAquusSettings)base.Settings;

        private bool _isInvincible;
        public bool IsInvincible => _isInvincible || !RenderersEnabled; // || GFXHeight > 0f;

        #region GFX - Height, Active
        
        /// <summary>
        /// 보스 GFX의 로컬 높이입니다.
        /// </summary>
        public float GFXHeight
        {
            get => GFX.transform.localPosition.y;
            set => GFX.transform.localPosition = GFX.transform.localPosition.Copy(y: value);
        }
        
        /// <summary>
        /// 보스 GFX의 가시성 여부입니다.
        /// </summary>
        /// <param name="active"></param>
        public bool IsActiveGFX
        {
            get => RenderersEnabled;
            set => RenderersEnabled = value;
        }

        [field: SerializeField, BoxGroup("일반")]
        public bool IsPlayingTimeline { get; set; } = false;

        #endregion



        [field: SerializeField, BoxGroup("일반")]
        public RotationState LookState { get; set; } = RotationState.LookDefaultDirection;
        public bool LookPlayer
        {
            get => LookState == RotationState.LookPlayer;
            set => LookState = value ? RotationState.LookPlayer : RotationState.None;
        }
        [field: SerializeField, BoxGroup("일반")]
        public Vector3 LookTargetPosition { get; set; }

        public Transform LookTargetPositionFromTransform
        {
            set
            {
                LookState = RotationState.LookTargetPosition;
                LookTargetPosition = value.position;
            }
        }
        
        [field: SerializeField, BoxGroup("일반"), ReadOnly]
        public Quaternion DefaultLookDirection { get; private set; }
        
        protected override void Awake()
        {
            base.Awake();

            var shieldEffectMainShieldRenderer = ShieldEffectMainShield.GetComponent<ParticleSystemRenderer>();
            if (shieldEffectMainShieldRenderer)
            {
                _shieldEffectMainShieldMaterial = shieldEffectMainShieldRenderer.material;
            }
            _shieldEffectHitCurve = null;
        }

        protected override void Start()
        {
            base.Start();

            DefaultLookDirection = transform.rotation;
            
            // 실드 게이지 갱신
            this.UpdateAsObservable()
                .Subscribe(UpdateShield)
                .AddTo(this);

            // LookPlayer = true일 시 플레이어 방향으로 회전
            this.UpdateAsObservable()
                .Subscribe(_ => UpdateLook());
            
            // 실드 게이지 갱신
            this.UpdateAsObservable()
                .Subscribe(UpdatePhase2ShieldPattern)
                .AddTo(this);

            this.UpdateAsObservable()
                .Where(_ => GameManager.Instance.CheatMode)
                .Where(_ => PhaseIndex == 0 && !IsPhaseTransitioning || PhaseIndex == 1)
                .Where(_ => Keyboard.current[Key.PageUp].wasPressedThisFrame)
                .Subscribe(_ =>
                {
                    if (Shield > 0)
                    {
                        Shield = 0;
                        OnShieldHit();
                    }
                    // BehaviourTree.SetVariableValue("PhaseTransitionDemand", 1);
                }).AddTo(this);
            
            this.OnFreezeEvent.AddListener(_ =>
            {
                // Throw 하기 전에 Freeze된 경우
                if (_jellyfish)
                {
                    // 지속시간 길지 않은 노 데칼 폭발 실행
                    EffectManager.Instance.Get(EffectType.JellyfishExplosion).transform.position = _jellyfish.transform.position;
                    _jellyfish.gameObject.SetActive(false);
                    _jellyfish = null;
                }
            });
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (NavMeshAgent)
            {
                NavMeshAgent.enabled = false;
            }

            IsScreaming = false;
            foreach (var platform in PhaseTransitionPlatforms)
            {
                platform.gameObject.SetActive(false);
            }
            
            OnDeadEvent.AddListener(_ =>
            {
                _p2EnemySpawner.gameObject.SetActive(false);
                // 존재하는 모든 몬스터, 이펙트, 아이템 제거 
                EnemyPoolManager.Instance.ReleaseUsedObjects();
                EffectManager.Instance.ReleaseUsedObjects();
                ItemManager.Instance.ReleaseUsedObjects();
            });
        }

        protected override void OnInitializeHUD()
        {
            base.OnInitializeHUD();
            
            // BossHPBarRenderer 초기화
            var hpBar = FindAnyObjectByType<BossHPBarRenderer>();
            if(hpBar)
                hpBar.Initialize(this);
        }
        

        // 보스 목표 방향 갱신
        private void UpdateLook()
        {
            if (IsFreeze)
            {
                return;
            }
            switch (LookState)
            {
                case RotationState.None:
                    return;
                case RotationState.LookDefaultDirection:
                    transform.rotation =
                        Quaternion.RotateTowards(
                            transform.rotation, 
                            DefaultLookDirection, 
                            BossSettings.RotationSpeed * Time.deltaTime
                        );
                    return;
                case RotationState.LookPlayer:
                    // 플레이어 실시간 위치 추적
                    transform.LookTowards(PlayerView.transform.position, BossSettings.RotationSpeed);
                    return;
                case RotationState.LookTargetPosition:
                    // 지정된 위치 추적
                    transform.LookTowards(LookTargetPosition, BossSettings.RotationSpeed);
                    return;
            }
        }

        public enum SoundCommandType
        {
            PlayBeyarPhase01,
            EndBeyarPhase01,
        }
        
        private static class FMODPARAM
        {
            public const string BeyarStateID = "BeyarState";
            public enum BeyarState { Looping, Ending }
            
        }

        public void ExecuteSoundCommand(SoundCommandType command)
        {
            switch (command)
            {
                case SoundCommandType.PlayBeyarPhase01:
                {
                    // BeyarAudioSource.SetParameter(FMODPARAM.BeyarStateID, (float)FMODPARAM.BeyarState.Looping);
                    // BeyarAudioSource.Play();
                    return;
                }
                case SoundCommandType.EndBeyarPhase01:
                {
                    // BeyarAudioSource.SetParameter(FMODPARAM.BeyarStateID, (float)FMODPARAM.BeyarState.Ending);
                    return;
                }
            }
        }
        
        #region Bullet
        
        [field: FoldoutGroup("공격")]
        [field: SerializeField, BoxGroup("공격/탄막 패턴")]
        public Transform ShootPosition { get; private set; }
        
        // 현재 발사중인 Spawner
        public PauseParticleSystem BulletSpawner { get; private set; }
        
        public void ResumeBulletSpawner()
        {
            if (BulletSpawner)
            {
                BulletSpawner.Resume();
                BulletSpawner = null;
                
                PlaySoundOnce("BulletSpawnerDisappear");
            }
            else
            {
                // Debug.LogWarning($"BulletSpawner가 없는데 Resume 호출", gameObject);
            }
        }

        public void OnAppearBulletSpawner()
        {
            
        }

        #endregion
        
        #region Attack by Effect Range

        public EffectType CurrentEffectType { get; private set; } = EffectType.None;
        private EffectRange _effectRange;

        public EffectRange AttackRange
        {
            get => _effectRange;
            private set
            {
                Debug.Log($"AttackRange <color=yellow>{_effectRange}</color> => <color=lime>{value}</color>");
                _effectRange = value;
            }
        }


        /// <summary>
        /// AnimationEventHandle에서 호출됨
        /// 이펙트를 생성하고, EffectRange를 얻어와 반영합니다.
        /// </summary>
        /// <param name="type">이펙트 종류입니다.</param>
        public void SpawnEffect(string type)
        {
            EffectType effectType;
            //EventReference sound;
            switch (type)
            {
                case "AquusBulletSpawner":
                    effectType = EffectType.AquusBulletSpawner;
                    break;
                case "AquusHarpStrike":
                    effectType = EffectType.AquusHarpStrike;
                    break;
                case "AquusDeadlyStrike":
                    effectType = EffectType.AquusDeadlyStrike;
                    break;
                case "AquusBowAttack":
                    effectType = EffectType.AquusBowAttack;
                    break;
                case "AquusBowAttackExplosion":
                    effectType = EffectType.AquusBowAttackExplosion;
                    // BossSettings.Sounds.TryGetValue("RangedAttackExplosion", out sound);
                    // AudioManager.PlayOneShot(sound);
                    break;
                case "AquusScream":
                    effectType = EffectType.AquusScream;
                    break;
                default:
                    Debug.LogWarning($"SpawnEffect :: {type}(은)는 지원하는 EffectType이 아닙니다.");
                    return;
            }

            var effect = EffectManager.Instance.Get(effectType);
            var t = transform;
            switch (effectType)
            {
                case EffectType.AquusHarpStrike:
                case EffectType.AquusDeadlyStrike:
                {
                    effect.transform.position = HarpStrikePosition.position;
                    break;
                }
                case EffectType.AquusBowAttack:
                {
                    if (!effect.TryGetComponent(out BossRangedAttackProjectile projectile))
                    {
                        Debug.LogWarning("BossRangedAttack effect에 BossRangedAttackProjectile Component 없음");
                        return;
                    }

                    var bossToPlayer = (PlayerPresenter.transform.position - t.position);
                    var shootDirection = bossToPlayer.Copy(y: 0f).normalized;
                    projectile.Initialize(shootDirection, BossSettings.BowAttackSpeed, gameObject,
                        () => BossSettings.BowAttackDamage, () => DamageReaction.KnockBack
                    );
                    projectile.InitializeBossRangedAttack(bossToPlayer.magnitude, (position) =>
                    {
                        SpawnEffect(nameof(EffectType.AquusBowAttackExplosion));
                        AttackRange.transform.position = position; // 조금 억지긴 한데 암튼 이펙트 위치 설정
                        AttackAtEffect(0); // 폭발
                    });
                    effect.transform.SetPositionAndRotation(
                        BowAttackShootPosition.position,
                        Quaternion.LookRotation(shootDirection)
                    );
                    effect.transform.rotation = Quaternion.LookRotation(shootDirection);
                    return;
                }
                case EffectType.AquusBulletSpawner:
                {
                    var position = ShootPosition.position;
                    if (effect.TryGetComponent(out FakeChild f))
                    {
                        f.TargetParent = ShootPosition;
                    }
                    else
                    {
                        effect.transform.position = position;
                    }
                    BulletSpawner = effect.GetComponent<PauseParticleSystem>();
                    PlaySoundOnce("BulletSpawnerAppear");
                    
                    var appearEffect = EffectManager.Instance.Get(EffectType.AquusBulletSpawnerAppear);
                    appearEffect.transform.position = position;
                    return;
                }
                case EffectType.AquusScream:
                {
                    if (!effect.TryGetComponent(out FakeChild f))
                    {
                        Logger.LogWarning("no fakechild on AquusScream", effect);
                        return;
                    }

                    f.TargetParent = ScreamEffectPosition;
                    f.Follow(true);
                    return;
                }
                default:
                    effect.transform.SetPositionAndRotation(t.position, t.rotation);
                    break;
            }

            AttackRange = effect.GetComponent<EffectRange>();
            CurrentEffectType = effectType;

            switch (effectType)
            {
                case EffectType.AquusHarpStrike:
                {
                    break;
                }   
            }
            
            /*
            switch (effectType)
            {
                case EffectType.BossRushAttackCharging:
                    if (!effect.TryGetComponent(out FakeChild fakeChild))
                    {
                        Debug.LogWarning("BossRushAttackCharging effect에 FakeChild Component 없음");
                        return;
                    }

                    fakeChild.TargetParent = RushAttackChargingEffectPosition;
                    return;
                case EffectType.BossRangedAttackAura:
                    if (!effect.TryGetComponent(out fakeChild))
                    {
                        Debug.LogWarning("BossRangedAttackAura effect에 FakeChild Component 없음");
                        return;
                    }

                    fakeChild.TargetParent = RangedAttackAuraEffectPosition;
                    return;
                case EffectType.BossRangedAttack:
                    if (!effect.TryGetComponent(out BossRangedAttackProjectile projectile))
                    {
                        Debug.LogWarning("BossRangedAttack effect에 BossRangedAttackProjectile Component 없음");
                        return;
                    }

                    var bossToPlayer = (PlayerPresenter.transform.position - t.position);
                    projectile.Initialize(bossToPlayer.normalized, BossSettings.RangedAttackSpeed, gameObject,
                        () => BossSettings.RangedAttackDamage, () => DamageReaction.KnockBack
                    );
                    projectile.InitializeBossRangedAttack(bossToPlayer.magnitude, (position) =>
                    {
                        SpawnEffect(nameof(EffectType.BossRangedAttackExplosion));
                        AttackRange.transform.position = position; // 조금 억지긴 한데 암튼 이펙트 위치 설정
                        AttackAtEffect(0); // 폭발
                    });
                    effect.transform.rotation = Quaternion.LookRotation(bossToPlayer.normalized);
                    return;
            }
            */
        }

        public override void AttackByRangeSensor(float damage)
        {
            if (!AttackRangeSensor) return;
            AttackRangeSensor.Pulse();
            foreach (var detection in AttackRangeSensor.Detections)
            {
                if (detection.CompareTag("Player") && detection.TryGetComponent(out PlayerPresenter player))
                {
                    player.Damage(damage, gameObject, DamageReaction.KnockBack);
                }
            }
        }

        private HashSet<int> _currentAttackedIds = new();

        public void AttackAtEffect(int index)
        {
            if (!AttackRange)
            {
                DebugX.Log($"AttackAtEffect({index}) but no AttackRange");
                return;
            }

            DebugX.Log($"AttackAtEffect({index})");
            _currentAttackedIds.Clear();

            // Sensor 감지 로직
            void Detect(RangeSensor sensor, HashSet<int> distincter)
            {
                sensor.Pulse();
                foreach (GameObject target in sensor.Detections)
                {
                    // 중복제거
                    var id = target.GetInstanceID();
                    if (distincter.Contains(id))
                    {
                        continue;
                    }

                    distincter.Add(id);

                    if (!target.CompareTag("Player") || !target.TryGetComponent(out PlayerPresenter player))
                    {
                        continue;
                    }
                        
                    float damage = 0f;
                    DamageReaction reaction = DamageReaction.Stun;
                    EnemyAttackType type = EnemyAttackType.None;
                    switch (CurrentEffectType)
                    {
                        case EffectType.AquusHarpStrike:
                            damage = BossSettings.HarpStrikeDamage;
                            reaction = DamageReaction.KnockBack;
                            type = EnemyAttackType.BossHarpStrike;
                            break;
                        case EffectType.AquusBowAttackExplosion:
                            damage = BossSettings.BowAttackDamage;
                            reaction = DamageReaction.KnockBack;
                            type = EnemyAttackType.BossBow;
                            break;
                        /*
                        case EffectType.BossNearAttack01:
                            damage = BossSettings.NearAttack01Damage;
                            reaction = DamageReaction.KnockBack;
                            PlayHitEffect(EffectType.BossNearAttack01Hit);
                            break;
                        case EffectType.BossRushAttack:
                            switch (index)
                            {
                                // 돌진 자체 피해량은 RushSequence에서
                                case 0: // 1차 폭발
                                case 1:
                                case 2: // 2차 폭발
                                case 3:
                                case 4: // 3차 폭발
                                    // damage = BossSettings.RushDamage;
                                    reaction = DamageReaction.KnockBack;
                                    break;
                            }

                            break;
                        */
                    }

                    player.Damage(PlayerDamageInfo.Get(damage, gameObject, DamageMode.Normal, reaction, enemyAttackType: type));
                }
            }

            var sensors = AttackRange.Sensors;
            // 전체 타격
            if (index < 0)
            {
                foreach (var sensor in sensors)
                {
                    Detect(sensor, _currentAttackedIds);
                }
            }
            // 일부 index 타격
            else
            {
                if (index >= sensors.Count)
                {
                    DebugX.LogWarning($"tried AttackDamage({index}) but sensor count({sensors.Count}) was not enough");
                    return;
                }

                Detect(sensors[index], _currentAttackedIds);
            }
        }
        

        #endregion
        
        #region Pattern: HarpStrike

        [field: SerializeField, BoxGroup("공격/Harp Strike")]
        public Transform HarpStrikePosition { get; private set; }

        [field: SerializeField, BoxGroup("공격/Harp Strike")]
        public Transform HarpStrikeDecalPosition { get; private set; }

        [field: SerializeField, BoxGroup("공격/Harp Strike")]
        public DecalEffect HarpStrikeRange { get; private set; }

        // HarpStrike 시작 시 호출
        public void OnHarpStrikeStart()
        {
            if (!HarpStrikeRange)
            {
                Debug.LogWarning("OnHarpStrikeStart: No HarpStrikeRange");
                return;
            }

            var origin = transform.position;
            var playerPosition = PlayerPresenter.transform.position;

            // 보스 기준 플레이어 방향
            var targetForward = (playerPosition - origin).Copy(y: 0f).normalized;
            Debug.DrawLine(origin, targetForward, Color.cyan, 1.5f);
            // 보스 앞방향 -> 플레이어 방향으로의 회전
            var forward = transform.forward;
            var forwardToPlayerRotation = Quaternion.FromToRotation(forward, targetForward);

            // 예상 타격 위치 계산
            var expectPosition = forwardToPlayerRotation * (HarpStrikeDecalPosition.position - origin) + origin;
            Debug.DrawLine(origin, expectPosition, Color.yellow, 1.5f);

            // 예상 위치에서 높이는 플레이어 높이 + 1로
            // HarpStrikeRange.transform.position = expectPosition.Copy(y: PlayerPresenter.transform.position.y + 1f);
            // HarpStrikeRange.gameObject.SetActive(true);
            
            // HarpStrikeDecalSequence().Forget();
        }

        private async UniTaskVoid HarpStrikeDecalSequence()
        {
            var curve = BossSettings.HarpStrikeDecalAlphaCurve;
            float t = 0f;
            float length = curve.GetLength();
            while (t < length)
            {
                float alpha = curve.Evaluate(t);
                HarpStrikeRange.Opacity = alpha;
                HarpStrikeRange.Progress = t / length;
                await UniTask.Yield();
                t += Time.deltaTime;
            }
            HarpStrikeRange.Opacity = 0f;
            HarpStrikeRange.gameObject.SetActive(false);
        }
        
        private void OnDrawGizmosSelectedDownAttack()
        {
            var oldColor = Gizmos.color;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position.Copy(y: HarpStrikePosition.position.y), BossSettings.HarpStrikeExecuteRange);
            Gizmos.color = oldColor;
        }

        #endregion

        #region Pattern: Jellyfish
        
        [field: SerializeField, BoxGroup("공격/Jellyfish")]
        public Transform JellyfishPosition { get; private set; }

        private GameObject _jellyfish;

        public void BindJellyfish()
        {
            if (_jellyfish)
            {
                _jellyfish.gameObject.SetActive(false);
                _jellyfish = null;
            }
            _jellyfish = EffectManager.Instance.Get(EffectType.AquusJellyfish);
            if (!_jellyfish.TryGetComponent(out FakeChild f))
            {
                Debug.LogWarning("BindJellyfish but no fakechild", gameObject);
                return;
            }

            f.TargetParent = JellyfishPosition;
            f.Follow(true);
        }
        public void ThrowJellyfish()
        {
            if (!_jellyfish)
            {
                Debug.LogWarning("ThrowJellyfish but no jellyfish", gameObject);
                return;
            }

            if (!_jellyfish.TryGetComponent(out FakeChild f))
            {
                Debug.LogWarning("ThrowJellyfish but no fakechild", gameObject);
                return;
            }

            f.Follow(true);
            f.TargetParent = null;
            
            if (!_jellyfish.TryGetComponent(out Jellyfish j))
            {
                return;
            }
            j.ThrowStart();
            
            _jellyfish = null;
        }
        
        #endregion

        #region Pattern: BowAttack

        [field: SerializeField, BoxGroup("공격/Bow Attack")]
        public DecalHandler BowAttackRange { get; private set; }
        
        [field: SerializeField, BoxGroup("공격/Bow Attack")]
        public Transform BowAttackChargingEffectPosition { get; private set; }
        
        [field: SerializeField, BoxGroup("공격/Bow Attack")]
        public Transform BowAttackShootPosition { get; private set; }

        public void OnBowAttackStart()
        {
            if (!BowAttackRange)
            {
                Debug.LogWarning("OnBowAttackStart(): No BowAttackRange");
                return;
            }

            var effect = EffectManager.Instance.Get(EffectType.AquusBowAttackCharging);
            if (effect.TryGetComponent(out FakeChild f))
            {
                f.TargetParent = BowAttackChargingEffectPosition;
            }
        }
        
        #endregion

        #region Pattern: Scream

        
        [field: SerializeField, BoxGroup("공격/Scream")]
        public Transform ScreamEffectPosition { get; private set; }
        
        [field: SerializeField, BoxGroup("공격/Scream")]
        public Transform ScreamStructureFallStartPosition { get; private set; }
        
        [field: SerializeField, BoxGroup("공격/Scream")]
        public BossScreamStructureFallPositionGenerator ScreamFallPositionGenerator { get; private set; }

        [field: SerializeField, BoxGroup("공격/Scream"), ReadOnly]
        public bool IsScreaming { get; private set; } = false;
        
        
        public void PlayScream()
        {
            // 페이즈 전환 시 무시 (별도로 실행 예정)
            if(IsPhaseTransitioning) return;
            
            if (!ScreamStructureFallStartPosition)
            {
                Debug.LogError($"PlayScream() Failed - ScreamStructureFallStartPosition이 없습니다.", gameObject);
                return;
            }
            if (!ScreamFallPositionGenerator)
            {
                Debug.LogError($"PlayScream() Failed - FallPositionGenerator가 없습니다.", gameObject);
                return;
            }
            
            ScreamSequence(BossSettings.DefaultScreamSettings, ScreamFallPositionGenerator).Forget();
        }
        
        private async UniTaskVoid ScreamSequence(
            BossAquusSettings.ScreamSettings settings, 
            BossScreamStructureFallPositionGenerator generator
        ) {
            IsScreaming = true;

            // 미리 생성해둔 좌표들 중 랜덤으로 위치 얻어오기 (생성기를 통해 사전 테스트 필요)
            if (!generator.SelectRandomPositions(
                    settings.StructureCount.Random(),
                    settings.MinDistance
            )) {
                Debug.LogWarning("Scream 패턴 : SelectRandomPositions Failed", gameObject);
                return;
            }
            
            // 각 위치에 구조물 초기화
            var positions = generator.SelectedPositions;
            var tasks = new List<UniTask>(positions.Count);
            foreach (var t in positions)
            {
                var spawnPosition = t.position;
                spawnPosition.y = ScreamStructureFallStartPosition.position.y;

                var effectType = settings.StructureTypes.Random(); 
                var obj = EffectManager.Instance.Get(effectType, spawnPosition, Random.rotation);
                
                if (!obj.TryGetComponent<BossFallingStructure>(out var structure))
                {
                    Debug.LogError($"Scream 패턴 : {effectType.ToString()}에는 BossFallingStructure가 없습니다.", obj);
                    continue;
                }

                // 랜덤 소환 딜레이, 랜덤 속도로 초기화
                structure.Initialize(
                    settings,
                    settings.StructureFallDelay.Random(),
                    settings.StructureFallSpeed.Random()
                );
                // 다 떨어질때까지 대기하는 task를 추가
                tasks.Add(UniTask.WaitUntil(() => structure.Fallen || !structure.IsValid));
            }

            // 모든 task 대기(structure가 다 떨어질때까지 대기)
            await UniTask.WhenAll(tasks);
            
            // 패턴 종료
            IsScreaming = false;
        }
        
        

        #endregion

        #region Phase Transition (DeadlyStrike)

        private static readonly int PtOnScream = Animator.StringToHash("PT_OnScream");
        private static readonly int PtOnDeadlyStrike = Animator.StringToHash("PT_OnDeadlyStrike");
        private static readonly int PtOnDiving = Animator.StringToHash("PT_OnDiving");
        private static readonly int PtOnAppear = Animator.StringToHash("PT_OnAppear");
        private static readonly int PtOnSwim = Animator.StringToHash("PT_OnSwim");


        [field: SerializeField, BoxGroup("공격/페이즈 전환")]
        public BossScreamStructureFallPositionGenerator PhaseTransitionFallPositionGenerator { get; private set; }
        
        [field: SerializeField, BoxGroup("공격/페이즈 전환")]
        public BossRoomGround PhaseTransitionGround { get; private set; }

        [field: SerializeField, BoxGroup("공격/페이즈 전환")]
        public RangeSensor PhaseTransitionPlayerArriveRange { get; private set; }
        
        [field: SerializeField, BoxGroup("공격/페이즈 전환")]
        public Transform NewBossPosition { get; private set; }

        [field: SerializeField, BoxGroup("공격/페이즈 전환/플랫폼")]
        public List<GameObject> PhaseTransitionPlatforms { get; private set; } = new();
        [field: SerializeField, BoxGroup("공격/페이즈 전환/플랫폼")]
        public float PhaseTransitionPlatformDelay = 2f;
        [field: SerializeField, BoxGroup("공격/페이즈 전환/플랫폼")]
        public float PhaseTransitionPlatformInterval = 2f;

        [field: SerializeField, BoxGroup("공격/페이즈 전환/Swim")]
        public SplineAnimate SwimPathAnimator { get; private set; }
        [field: SerializeField, BoxGroup("공격/페이즈 전환/Swim")]
        public List<SplineContainer> SwimPaths { get; private set; } = new();
        
        [field: SerializeField, BoxGroup("공격/페이즈 전환")]
        public int PhaseIndex { get; private set; } = 0;
        [field: SerializeField, BoxGroup("공격/페이즈 전환")]
        public UnityEvent OnPhaseTransitionEvent { get; private set; }

        public bool IsPhaseTransitioning { get; private set; } = false;
        
        public async UniTask TransitionSequence()
        {
            IsPhaseTransitioning = true;
            IsShieldActive = false;
            FallChecker.GlobalFallCheckerEnabled = true;
            _isInvincible = true;
            
            // Scream 시전 후 대기
            Animator.SetTrigger(PtOnScream);
            await UniTask.Delay(TimeSpan.FromSeconds(1f));
            await UniTask.WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(0).IsName("Scream-Await"));
            
            // DeadlyStrike 시전 후 대기
            Animator.SetTrigger(PtOnDeadlyStrike);
            await UniTask.Delay(TimeSpan.FromSeconds(1f));
            await UniTask.WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(0).IsName("DeadlyStrike-Await"));
            
            // 다이빙
            Animator.SetTrigger(PtOnDiving);
            await UniTask.Delay(TimeSpan.FromSeconds(1f));
            // 다이빙 이후 GFX 일시 끄기
            IsActiveGFX = false;

            
            // Swim 애니 실행
            Animator.SetTrigger(PtOnSwim);
            SwimPathAnimator.enabled = true;
            
            // 바닥 무너지는 태스크 ...
            var backgroundTask = PhaseTransitionBreakBackgrounds();
            // 플랫폼 태스크 ...
            var platformTask = PhaseTransitionActivatePlatforms();
            // 수영 태스크 ...
            var swimTask = PhaseTransitionSwim();

            var waitPlayerTask = PhaseTransitionWaitForPlayer().GetAwaiter();

            await UniTask.WhenAll(backgroundTask, platformTask, swimTask);
            Debug.Log("페이즈 전환 모든 Task 종료, await until waitPlayerTask.IsCompleted");

            await UniTask.WaitUntil(() => waitPlayerTask.IsCompleted);
            var success = waitPlayerTask.GetResult();
            Debug.Log($"success = {success}");

            // 사망한 경우 아무것도 안 함
            if (!success)
            {
                Debug.Log($"success == false, 페이즈 전환 중단");
                return;
            }

            // 위치 전환
            SwimPathAnimator.enabled = false;
            transform.SetPositionAndRotation(NewBossPosition.position, NewBossPosition.rotation);
            DefaultLookDirection = NewBossPosition.rotation;
            
            // 등판
            Animator.SetTrigger(PtOnAppear);
            // IsActiveGFX = true;
            await UniTask.Delay(TimeSpan.FromSeconds(1f));
            await UniTask.WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"));
            
            FallChecker.GlobalFallCheckerEnabled = false;
            IsPhaseTransitioning = false;
            _isInvincible = false;
            ++PhaseIndex;
            OnPhaseTransitionEvent?.Invoke();
        }

        private async UniTask PhaseTransitionBreakBackgrounds()
        {
            Debug.Log("PhaseTransitionBreakBackgrounds()");
            while (PhaseTransitionGround.ExplodeCurrentAndNext(out float wait))
            {
                Debug.Log($"Ground.Index = {PhaseTransitionGround.Index}");
                // 낙하도 같이 돌림
                ScreamSequence(BossSettings.PhaseTransitionScreamSettings, PhaseTransitionFallPositionGenerator).Forget();
                Debug.Log($"called scream sequence");
                Debug.Log($"awaiting {wait} seconds ...");
                await UniTask.Delay(TimeSpan.FromSeconds(wait));
                Debug.Log($"awaited {wait} seconds");
            }
            Debug.Log($"PhaseTransitionBreakBackgrounds() End");
        }

        private async UniTask PhaseTransitionActivatePlatforms()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(PhaseTransitionPlatformDelay));
            foreach (var platform in PhaseTransitionPlatforms)
            {
                platform.gameObject.SetActive(true);
                await UniTask.Delay(TimeSpan.FromSeconds(PhaseTransitionPlatformInterval));
            }
        }

        private async UniTask<bool> PhaseTransitionWaitForPlayer()
        {
            bool reached = false;
            PhaseTransitionPlayerArriveRange.OnDetected.AddListener((obj, sensor) =>
            {
                reached = true;
            });
            // 1초 간격으로 재면서 도달 체크
            while (!PlayerPresenter.Model.IsDead)
            {
                if (reached) return true;
                await UniTask.Delay(TimeSpan.FromSeconds(1f));
            }

            return false;
        }

        private async UniTask PhaseTransitionSwim()
        {
            await UniTask.Yield();
            try
            {
                if(BossSettings.PhaseTransitionShuffleSwimPathOrder)
                    SwimPaths.Shuffle();
                var path = SwimPaths[0];
                SwimPathAnimator.Container = path;
                SwimPathAnimator.Restart(true);
                await UniTask.Delay(TimeSpan.FromSeconds(1f));
                IsActiveGFX = true;
                await UniTask.WaitWhile(() => SwimPathAnimator.IsPlaying);
                await UniTask.Delay(TimeSpan.FromSeconds(1f));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("PhaseTransitionSwim 에러 발생 !!!!!");
            }
        }
        
        #endregion
        
        private void OnDrawGizmosSelected()
        {
            OnDrawGizmosSelectedDownAttack();
        }
        
        #region 피격
        
        /// <summary>
        /// 몬스터를 주어진 정보로 타격합니다. EnemyDamageInfo.Get을 통해 타격 정보를 설정할 수 있습니다. 
        /// </summary>
        public override EntityHitResult Damage(DamageInfo info)
        {
            if (Health <= 0f || IsFreezeSlipping || IsInvincible)
            {
                Debug.Log($"BossAquus::Damage() - ignored by dead, slipping, invincible");
                return EntityHitResult.Invincible;
            }
            
            void AddFreezeLevel(float amount = 1)
            {
                UpdateFreezeLevel(FreezeLevel + (int)amount);
            }

            if (IsShieldActive)
            {
                if (IsBullet(info.Source))
                {
                    PlaySoundOnce("HitShieldBullet");
                    if (info is EnemyDamageInfo enemyDamageInfo)
                    {
                        SpawnShieldHit(enemyDamageInfo);
                    }
                    return EntityHitResult.Defend;
                }

                // 적 또는 파괴 가능 오브젝트에 대한 공격(=빙결 미끄러짐 충돌)만 받음
                if (!info.Source.CompareTag("Enemy") && !info.Source.CompareTag("Destructible") 
                    || !info.Source.TryGetComponent(out IFreezable freezable) || !freezable.IsFreeze)
                {
                    Debug.Log($"BossAquus::Damage() - ignored by not freezable sleeping(source={info.Source}, tag={info.Source.tag}, freezable.IsSlipping={info.Source.GetComponent<IFreezable>()?.IsFreeze})", info.Source);
                    return EntityHitResult.Invincible;
                }
                
                Debug.Log($"BossAquus::Damage() - shield damaged ({Shield} => {Shield - 1})");
                Shield -= 1;
                OnShieldHit();
                return EntityHitResult.Invincible;
            }
            
            // 1페이즈 피격의 경우 일반 피격 발생 상황 없음
            if (PhaseIndex == 0)
            {
                return EntityHitResult.Invincible;
            }
            
            // 완전 빙결 상태 중에 공격 받았을 때
            if (IsFreezeComplete)
            {
                var isBulletAttack = IsBullet(info.Source);
                // 완전 빙결 상태에서는 총알은 아무 의미 없음
                if (isBulletAttack)
                {
                    info.Amount = 0f;
                    return EntityHitResult.Invincible;
                }
                
                // 빙결 밀림으로 인한 피격일 경우 즉시 파괴
                if (info.Source.TryGetComponent(out IFreezable m) && m.IsFreeze)
                {
                    info.Amount = 0f;
                    return EntityHitResult.Invincible;
                }
                    
                // 피격 이벤트 전달
                Health -= 1;
                if (Health > 0)
                {
                    BehaviourTree.SendEvent("Hit");
                    UseFrostbiteTween = false;
                    // 빙결 단계 0으로 초기화
                    UpdateFreezeLevel(0);
                }
                
            }
            else if (info is EnemyDamageInfo enemyDamageInfo)
            {
                if (CanFreeze && enemyDamageInfo.Reaction == DamageReaction.Freeze)
                {
                    // Amount(=DamageToBoss)만큼 빙결 단계 증가
                    AddFreezeLevel(info.Amount);   
                }else if (enemyDamageInfo.Source 
                          && enemyDamageInfo.Source.CompareTag("Enemy") 
                          && enemyDamageInfo.Source.TryGetComponent(out IFreezable m)
                ) {
                    AddFreezeLevel(BossSettings.DamageOnIceSlipCollide);
                }
            }
            

            HitTint();
            HitEffect(info);

            OnKnockBack(info);
            OnDamageEnd(info);

            return EntityHitResult.Success;
        }

        protected override void OnFreezeTimeExpired()
        {
            var newFreezeLevel = FreezeLevel;
            // 단순 빙결 단계 감소
            if (FreezeLevel > 0)
            {
                newFreezeLevel = FreezeLevel - 1;
            }

            UpdateFreezeLevel(newFreezeLevel);
        }

        protected override void OnFreeze(DamageInfo info, int oldFreezeLevel, int newFreezeLevel)
        {
            // 빙결 단계 증가 시에는 기존 시간 사용
            if (newFreezeLevel > oldFreezeLevel)
            {
                FreezeTime = PlayerView.Settings.FreezeTime;
            }
            // 녹을 때에는 별도 시간 사용
            else
            {
                FreezeTime = BossSettings.FreezeLevelDownTime;
            }
            // 빙하 VFX 갱신
            // Logger.Log($"{name} - OnFreeze({oldFreezeLevel} -> {newFreezeLevel})", gameObject);
            UpdateFreezeLevelEffect(oldFreezeLevel, newFreezeLevel);
        }

        #endregion

        #region 실드
        
        [field: SerializeField, BoxGroup("실드")]
        public float Shield { get; private set; } = 0f;
        
        [field: SerializeField, BoxGroup("실드")]
        public bool IsShieldActive { get; private set; } = false;
        
        [field: SerializeField, BoxGroup("실드"), LabelText("ShieldEffect_Root")]
        public GameObject ShieldObjectRoot { get; private set; }
        
        [field: SerializeField, BoxGroup("실드"), LabelText("실드 MeshCollider")]
        public MeshCollider ShieldCollider { get; private set; }
        
        [field: SerializeField, BoxGroup("실드"), LabelText("실드 NavMeshObstacle")]
        public NavMeshObstacle ShieldObstacle { get; private set; }
        
        [field: SerializeField, BoxGroup("실드")]
        public Transform ShieldNewPosition { get; private set; }

        [field: SerializeField, BoxGroup("실드")] 
        public UnityEvent OnShieldBreakAtPhase1 { get; private set; }
        
        [field: SerializeField, BoxGroup("실드/이펙트"), LabelText("Shield_Root")]
        public GameObject ShieldEffectRoot { get; private set; }
        
        [field: SerializeField, BoxGroup("실드/이펙트"), LabelText("MainShield")]
        public ParticleSystem ShieldEffectMainShield { get; private set; }
        private Material _shieldEffectMainShieldMaterial;

        [field: SerializeField, BoxGroup("실드/이펙트"), LabelText("Shield_Crack_Particle")]
        public List<GameObject> ShieldEffectParticleOnDamageByHealth { get; private set; } = new();

        [field: SerializeField, BoxGroup("실드/이펙트"), LabelText("_IsCracked02")]
        public List<int> ShieldEffectCrackLevelByHealth { get; private set; } = new()
        {
// left hp: 0, 1, 2, 3
            0, 2, 1, 0
        };

        [field: SerializeField, BoxGroup("실드/이펙트"), LabelText("_Hit_ColorScaleCrackRemaped")]
        public List<AnimationCurve> ShieldEffectCrackCurveByHealth { get; private set; } = new()
        {
            null,
            AnimationCurve.EaseInOut(0f, 0.32f, 7f/60f, 0f),
            AnimationCurve.EaseInOut(0f, 0.109f, 7f/60f, 0f),
        };
        
        [field: SerializeField, BoxGroup("실드/이펙트"), LabelText("Shield_Break_Root")]
        public GameObject ShieldEffectOnBreak { get; private set; }
        
        private AnimationCurve _shieldEffectHitCurve;
        private float _shieldEffectTime;
        private static readonly int IsCracked02 = Shader.PropertyToID("_IsCracked02");
        private static readonly int HitColorScaleCrackRemaped = Shader.PropertyToID("_Hit_ColorScaleCrackRemaped");

        public void ActiveShieldDefault() => ActiveShield(BossSettings.MaxShield);
        private void ActiveShield(float level)
        {
            if (PhaseIndex != 0)
            {
                ShieldObjectRoot.transform.SetPositionAndRotation(ShieldNewPosition.position, ShieldNewPosition.rotation);
            }
            Shield = level;
            IsShieldActive = true;
            ShieldCollider.gameObject.SetActive(true);
            ShieldCollider.enabled = true;
            ShieldObstacle.enabled = true;
            ShieldEffectRoot.gameObject.SetActive(true);
            _shieldEffectMainShieldMaterial.SetFloat(IsCracked02, ShieldEffectCrackLevelByHealth[(int)level]);
        }
        
        private void OnShieldHit()
        {
            if (Shield <= 0)
            {
                IsShieldActive = false;
                ShieldCollider.enabled = false;
                ShieldObstacle.enabled = false;
                if (PhaseIndex == 0)
                {
                    BehaviourTree.SendEvent("ShieldBreak");
                    BehaviourTree.SendEvent("Hit");
                    // BeyarAudioSource.Stop();
                    OnShieldBreakAtPhase1?.Invoke();
                }
                else
                {
                    OnShieldBreakPhase2();
                }
                ShieldEffectRoot.SetActive(false);
                ShieldEffectOnBreak.SetActive(true);
            }else if (Shield >= 1)
            {
                int shieldLevel = (int)Shield;
                ShieldEffectParticleOnDamageByHealth[shieldLevel]?.SetActive(true);
                _shieldEffectHitCurve = ShieldEffectCrackCurveByHealth[shieldLevel];
                if (_shieldEffectHitCurve != null)
                {
                    _shieldEffectTime = 0f;
                }
                _shieldEffectMainShieldMaterial.SetFloat(IsCracked02, ShieldEffectCrackLevelByHealth[shieldLevel]);
                PlaySoundOnce("HitShieldMonster");
            }
        }
        
        /// <summary>
        /// 실드 상태를 갱신합니다.
        /// </summary>
        private void UpdateShield(Unit _)
        {
            if (!IsShieldActive)
            {
                // TODO 실드 활성화 디버그
                if (GameManager.Instance.CheatMode && Keyboard.current[Key.H].wasPressedThisFrame)
                {
                    ActiveShield(BossSettings.Phase2ShieldRange.Random());
                }
                return;
            }
            
            var curve = _shieldEffectHitCurve;
            if (curve != null)
            {
                var value = curve.Evaluate(_shieldEffectTime);
                _shieldEffectMainShieldMaterial.SetFloat(HitColorScaleCrackRemaped, value);
                _shieldEffectTime += Time.deltaTime;

                var length = curve.GetLength();
                if (_shieldEffectTime > length)
                {
                    _shieldEffectMainShieldMaterial.SetFloat(HitColorScaleCrackRemaped, curve.Evaluate(length));
                    _shieldEffectHitCurve = null;
                }
            }
        }

        private void SpawnShieldHit(EnemyDamageInfo info)
        {
            /* // 현재는 Sphere Collider를 사용하지 않음
            if (info.ColliderInfo is SphereCollider sc)
            {
                var effect = EffectManager.Instance.Get(EffectType.AquusShieldBulletHit);
                var bulletPosition = info.Source.transform.position;
                var scTransform = sc.transform;
                var scOrigin = scTransform.TransformPoint(sc.center);
                // DrawUtility.DrawWireSphere(scOrigin, sc.radius * scTransform.lossyScale.x, 16, DrawUtility.DebugDrawer(Color.green, 5f, false));
                var surfacePosition = sc.ClosestPoint(bulletPosition);
                // DrawUtility.DrawWireSphere(surfacePosition, 0.5f, 16, DrawUtility.DebugDrawer(Color.yellow, 5f, false));
                var normal = (surfacePosition - scOrigin).Copy(y: 0f).normalized;
                // Debug.DrawLine(surfacePosition, surfacePosition + normal * 5f, Color.cyan, 5f, false);
                effect.transform.SetPositionAndRotation(
                    surfacePosition + effect.transform.position, 
                    Quaternion.LookRotation(normal)
                );
            }
            else */
            if (info.ColliderInfo is MeshCollider mc)
            {
                var position = info.Source.transform.position;
                // DrawUtility.DrawWireSphere(position, 0.5f, 16, DrawUtility.DebugDrawer(Color.yellow, 5f, false));

                // var closest = position;
                var closest = mc.ClosestPoint(position);
                // Debug.Log($"PlayerBullet({info.Source.name}) collided MeshCollider {mc.name} at {position}(closest={closest},closestB={closestBound})");
                // DrawUtility.DrawWireSphere(closest, 0.5f, 16, DrawUtility.DebugDrawer(Color.green, 5f, false));
                
                // closest - position
                var bulletToClosest = (closest - position);
                var rayLength = bulletToClosest.magnitude;
                var ray = new Ray(position, bulletToClosest * (1f / rayLength));
                
                Vector3 normal;
                // 1. 총알(원형보다 바깥에 있음) -> 가장 가까운 점 방향으로 ray를 쏴서 normal을 구하기
                if (Physics.Raycast(ray, out var rayHit, rayLength * 2f, 1 << gameObject.layer))
                {
                    normal = rayHit.normal;
                }
                // 2. 최악의 상황이지만, 구하지 못한 경우 Mesh를 하나하나 긁어서 충돌 지점에서 가장 가까운 삼각형의 normal 구하기
                else
                {
                    var mesh = mc.sharedMesh;
                    var triangles = mesh.triangles;
                    var triangleLength = triangles.Length / 3;
                    var vertices = mesh.vertices;
                    var mct = mc.transform;
                    var localToWorld = mct.localToWorldMatrix;
                    var worldToLocal = mct.worldToLocalMatrix;

                    Vector3 closestOS = worldToLocal * closest.ToVector4(1f);
                    Vector3Int nearestTriangle = new Vector3Int(0, 1, 2);
                    float nearestDistanceSquared = closestOS.DistanceSquared((vertices[0] + vertices[1] + vertices[2]) * (1f/3f));
                    for (int i = 1; i < triangleLength; i++)
                    {
                        var t0 = triangles[i * 3 + 0];
                        var t1 = triangles[i * 3 + 1];
                        var t2 = triangles[i * 3 + 2];
                        
                        var v0 = vertices[t0];
                        var v1 = vertices[t1];
                        var v2 = vertices[t2];

                        // var v0ws = localToWorld * v0.ToVector4(1f);
                        // var v1ws = localToWorld * v1.ToVector4(1f);
                        // var v2ws = localToWorld * v2.ToVector4(1f);
                        var centerOS = (v0 + v1 + v2) * (1f/3f);
                        var distanceSquared = closestOS.DistanceSquared(centerOS);
                        if (distanceSquared < nearestDistanceSquared)
                        {
                            nearestDistanceSquared = distanceSquared;
                            nearestTriangle = new Vector3Int(t0, t1, t2);
                        }
                    }

                    var nv0 = localToWorld * vertices[nearestTriangle[0]].ToVector4(1f);
                    var nv1 = localToWorld * vertices[nearestTriangle[1]].ToVector4(1f);
                    var nv2 = localToWorld * vertices[nearestTriangle[2]].ToVector4(1f);
                    // var nearestCenterWS = (nv0 + nv1 + nv2) * (1f/3f);
                    normal = Vector3.Cross(
                        nv1 - nv0,
                        nv2 - nv1
                    ).normalized;
                    /*
                    Debug.DrawLine(nv0, nv1, Color.blue, 3f, false);
                    Debug.DrawLine(nv1, nv2, Color.blue, 3f, false);
                    Debug.DrawLine(nv0, nv2, Color.blue, 3f, false);
                    Debug.DrawLine(nearestCenter, nv0, Color.blue, 3f, false);
                    Debug.DrawLine(nearestCenter, nv1, Color.blue, 3f, false);
                    Debug.DrawLine(nearestCenter, nv2, Color.blue, 3f, false);
                    Debug.DrawLine(nearestCenter, nearestCenter + normal, Color.cyan, 3f, false);
                    */
                }
                var effect = EffectManager.Instance.Get(EffectType.AquusShieldBulletHit);
                effect.transform.SetPositionAndRotation(
                    closest + effect.transform.position, 
                    Quaternion.LookRotation(normal)
                );
            }
        }

        #endregion

        #region 실드 병렬 패턴 (2페이즈)

        [SerializeField, BoxGroup("실드/페이즈 2"), LabelText("페이즈 2 몬스터 스포너")]
        private BossEnemySpawner _p2EnemySpawner;
        
        [SerializeField, ReadOnly, BoxGroup("실드/페이즈 2"), LabelText("현재 재생성 대기 시간")]
        private float _p2ShieldPatternWait;

        [field: SerializeField, BoxGroup("실드/페이즈 2")] public UnityEvent OnShieldBreakAtPhase2 { get; private set; }

        private void OnShieldBreakPhase2()
        {
            OnShieldBreakAtPhase2?.Invoke();
            // _p2EnemySpawner.Enabled = false;
        }
        
        private void UpdatePhase2ShieldPattern(Unit _)
        {
            // 1페이즈 / 페이즈 전환 / 빙결 중에는 동작하지 않음
            if (PhaseIndex == 0 || IsPhaseTransitioning || IsFreeze)
            {
                return;
            }

            // 실드 활성화 상태에서는 몬스터 스포너 동작 
            if (IsShieldActive)
            {
                return;
            }
            
            if (_p2ShieldPatternWait > 0f)
            {
                _p2ShieldPatternWait -= Time.deltaTime;
                return;
            }

            _p2ShieldPatternWait = BossSettings.Phase2ShieldWaitTimeRange.Random();
            ActiveShield(BossSettings.Phase2ShieldRange.Random());
            _p2EnemySpawner.gameObject.SetActive(true);
            _p2EnemySpawner.Enabled = true;
        }

        #endregion
        
        #region Properties

        public float HarpStrikeExecuteRange => BossSettings.HarpStrikeExecuteRange;

        #endregion
        
    }
}