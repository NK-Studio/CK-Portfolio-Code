using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorDesigner.Runtime;
using Character.Presenter;
using Cysharp.Threading.Tasks;
using Damage;
using Effect;
using EnumData;
using FMODUnity;
using Managers;
using ManagerX;
using Micosmo.SensorToolkit;
using Micosmo.SensorToolkit.BehaviorDesigner;
using Settings.Boss;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Utility;

namespace Enemy.Behavior.Boss
{
    public class BossYorugami : Monster
    {
        private BossYorugamiSettings _settings;

        public BossYorugamiSettings BossSettings => _settings ??= (BossYorugamiSettings)base.Settings;


        public float NearAttackGiveUpTime => BossSettings.NearAttackGiveUpTime;
        public float ForcedFlashRange => BossSettings.ForcedFlashRange;
        public float FlashDistanceFromTarget => BossSettings.FlashDistanceFromTarget;
        public float RushSpeed => BossSettings.RushSpeed;
        public float RushDistance => BossSettings.RushDistance;
        public GameObject ProjectilePrefab => BossSettings.ProjectilePrefab;
        public float ProjectileSpeed => BossSettings.ProjectileSpeed;
        public float SectorAttackRange => BossSettings.SectorAttackRange;
        public float BossBombExplosionTime => BossSettings.BombExplosionTime;

        [field: SerializeField, FoldoutGroup("공격", true), UnityEngine.Tooltip("돌진 히트박스")]
        public RangeSensor RushRangeSensor { get; private set; }

        [field: SerializeField, FoldoutGroup("공격", true)]
        public Transform RushAttackChargingEffectPosition { get; private set; }
        
        [field: SerializeField, FoldoutGroup("공격", true)]
        public Transform RangedAttackAuraEffectPosition { get; private set; }

        [field: SerializeField, FoldoutGroup("공격", true)]
        public Transform ProjectileShootPosition { get; private set; }


        [field: SerializeField, FoldoutGroup("공격", true), UnityEngine.Tooltip("부채꼴 히트박스")]
        public RangeSensor SectorAttackRangeSensor { get; private set; }

        [field: SerializeField, FoldoutGroup("공격", true), UnityEngine.Tooltip("임시 검 메시")]
        public Renderer SwordMesh { get; private set; }

        private Material _swordMaterial;

        private static readonly Color[] Colors = new[]
        {
            Color.green, Color.cyan, Color.magenta, Color.yellow, Color.white
        };

        public static readonly Vector3[] StandardBasis = new[]
        {
            Vector3.left, Vector3.forward, Vector3.right, Vector3.back,
        };

        public SharedInt PhaseIndex = 0;
        public SharedFloat PhaseTargetHealth = 0;
        private bool _isInvincible;

        public bool IsGroggy { get; set; }

        public float GFXHeight
        {
            get => GFX.transform.localPosition.y;
            set
            {
                GFX.transform.localPosition = GFX.transform.localPosition.Copy(y: value);

                if (value >= BossSettings.PhaseTransitionHeight)
                {
                    foreach (var mesh in _meshes)
                    {
                        mesh.shadowCastingMode = ShadowCastingMode.Off;
                    }
                }
                else
                {
                    foreach (var mesh in _meshes)
                    {
                        mesh.shadowCastingMode = ShadowCastingMode.On;
                    }
                }
            }
        }

        [FoldoutGroup("메시", true), SerializeField]
        private Renderer[] _meshes;

        [SerializeField] private bool _isActiveGFX = true;

        /// <summary>
        /// 캐릭터 매시를 보이거나 안보이도록 합니다..
        /// </summary>
        /// <param name="active"></param>
        public bool IsActiveGFX
        {
            get => _isActiveGFX;
            set
            {
                _isActiveGFX = value;
                foreach (var mesh in _meshes)
                    mesh.enabled = value;
            }
        }

        [FoldoutGroup("보스 설정", true)] public GameObject BossStageCenter;

        [FoldoutGroup("보스 설정", true)] public DecalEffect DownAttackRangeProjector;
        
        protected override void Start()
        {
            base.Start();

            if (SwordMesh)
                _swordMaterial = SwordMesh.material;
            
            BehaviourTree.SetVariable("RushRange", new SharedSensor { Value = RushRangeSensor });
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _isInvincible = false;
            PhaseIndex = 0;
            PhaseTargetHealth = BossSettings.PhaseTransitions[PhaseIndex.Value].TargetHealth;
            BehaviourTree.SetVariable("PhaseIndex", PhaseIndex);
            BehaviourTree.SetVariableValue("DownAttackDemand", 1f);
            BehaviourTree.SetVariable(nameof(PhaseTargetHealth), PhaseTargetHealth);

            // TODO 언젠가는 바꿔야하지않을까?
            BossStageCenter = GameObject.FindWithTag("BossStageCenter");

            GFXHeight = BossSettings.PhaseTransitionHeight;

            DownAttackRangeProjector.gameObject.SetActive(false);
        }

        protected override void OnInitializeHUD()
        {
            // InGame :: BossInfo Signal 보내기 - BossInfo 활성화
            //SignalsService.SendSignal("InGame", "BossInfo", true);

            // BossHPBarRenderer 초기화
            var hpBar = FindAnyObjectByType<BossHPBarRenderer>();
            // hpBar.Initialize(this);
        }


        /// <summary>
        /// AnimationEventHandle에서 호출됨
        /// true면 NavMeshAgnet가 플레이어를 바라봅니다. (LookTowards)
        /// </summary>
        public bool LookPlayer { get; set; } = false;

        public EffectType CurrentEffectType { get; private set; } = EffectType.None;
        public EffectRange AttackRange { get; private set; }

        private static AudioManager AudioManager => AutoManager.Get<AudioManager>();
        /// <summary>
        /// AnimationEventHandle에서 호출됨
        /// 이펙트를 생성하고, EffectRange를 얻어와 반영합니다.
        /// </summary>
        /// <param name="type">이펙트 종류입니다.</param>
        public void SpawnEffect(string type)
        {
            EffectType effectType;
            EventReference sound;
            switch (type)
            {
                case "BossNearAttack01":
                    effectType = EffectType.BossNearAttack01;
                    BossSettings.Sounds.TryGetValue("NormalAttack", out sound);
                    AudioManager.PlayOneShot(sound, "BossNormalAttack", 0);
                    break;
                case "BossNearAttack02":
                    effectType = EffectType.BossNearAttack02;
                    BossSettings.Sounds.TryGetValue("NormalAttack", out sound);
                    AudioManager.PlayOneShot(sound, "BossNormalAttack", 1);
                    break;
                case "BossNearAttack03":
                    effectType = EffectType.BossNearAttack03;
                    BossSettings.Sounds.TryGetValue("NormalAttack", out sound);
                    AudioManager.PlayOneShot(sound, "BossNormalAttack", 2);
                    break;
                case "BossRushAttack":
                    effectType = EffectType.BossRushAttack;
                    break;
                case "BossRushAttackCharging":
                    effectType = EffectType.BossRushAttackCharging;
                    break;
                case "BossRangedAttack":
                    effectType = EffectType.BossRangedAttack;
                    break;
                case "BossRangedAttackEffect01":
                    effectType = EffectType.BossRangedAttackEffect01;
                    break;
                case "BossRangedAttackEffect02":
                    effectType = EffectType.BossRangedAttackEffect02;
                    break;
                case "BossRangedAttackEffect03":
                    effectType = EffectType.BossRangedAttackEffect03;
                    break;
                case "BossRangedAttackAuraEffect":
                    effectType = EffectType.BossRangedAttackAura;
                    break;
                case "BossRangedAttackExplosion":
                    effectType = EffectType.BossRangedAttackExplosion;
                    BossSettings.Sounds.TryGetValue("RangedAttackExplosion", out sound);
                    AudioManager.PlayOneShot(sound);
                    break;
                case "BossDownAttack":
                    effectType = EffectType.BossDownAttack;
                    break;
                default:
                    Debug.LogWarning($"SpawnEffect :: {type}(은)는 지원하는 EffectType이 아닙니다.");
                    return;
            }

            var effect = EffectManager.Instance.Get(effectType);
            var t = transform;
            effect.transform.SetPositionAndRotation(t.position, t.rotation);

            AttackRange = effect.GetComponent<EffectRange>();
            CurrentEffectType = effectType;

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
                return;

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

                    void PlayHitEffect(EffectType type)
                    {
                        if (!AttackRange.TryGetComponent(out VirtualPlane plane))
                        {
                            return;
                        }

                        var playerPosition = target.transform.position;
                        var effectPosition = plane.GetProjectedPosition(playerPosition);
                        var effect = EffectManager.Instance.Get(type);
                        effect.transform.position = (playerPosition + Vector3.up);
                        effect.transform.GetChild(0).forward = plane.GetCircleTangentVectorFromProjectedPosition(effectPosition);

                    }
                        
                    float damage = 0f;
                    DamageReaction reaction = DamageReaction.Stun;
                    switch (CurrentEffectType)
                    {
                        case EffectType.BossNearAttack01:
                            damage = BossSettings.NearAttack01Damage;
                            reaction = DamageReaction.KnockBack;
                            PlayHitEffect(EffectType.BossNearAttack01Hit);
                            break;
                        case EffectType.BossNearAttack02:
                            damage = BossSettings.NearAttack02Damage;
                            reaction = DamageReaction.KnockBack;
                            PlayHitEffect(EffectType.BossNearAttack02Hit);
                            break;
                        case EffectType.BossNearAttack03:
                            damage = BossSettings.NearAttack03Damage;
                            reaction = DamageReaction.KnockBack;
                            PlayHitEffect(EffectType.BossNearAttack03Hit);
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
                                    damage = BossSettings.RushDamage;
                                    reaction = DamageReaction.KnockBack;
                                    break;
                            }

                            break;
                        case EffectType.BossRangedAttackExplosion:
                            damage = BossSettings.RangedAttackDamage;
                            reaction = DamageReaction.KnockBack;
                            break;
                        case EffectType.BossDownAttack:
                            damage = BossSettings.DownAttackDamage;
                            reaction = DamageReaction.KnockBack;
                            break;
                    }

                    player.Damage(damage, gameObject, reaction);
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

        #region Phase Transition

        private static readonly int PhaseTransitionEnd = Animator.StringToHash("OnPhaseTransitionEnd");

        public void PlayPhaseTransition()
        {
            if (IsGroggy) return;
            PhaseTransitionSequence().Forget();
        }

        private async UniTaskVoid PhaseTransitionSequence()
        {
            DebugX.Log("PhaseTransitionSequence()");
            _isInvincible = true;

            var leapEffect = EffectManager.Instance.Get(EffectType.BossDownAttackLeap);
            leapEffect.transform.position = transform.position;
            DebugX.Log("PhaseTransitionSequence() - Move GFX");
            // TODO 카메라 안 보이는 높이로 GFX 높이 올리기 (실제로 올릴건 아님)
            var height = BossSettings.PhaseTransitionHeight;
            var heightCurve = BossSettings.PhaseTransitionHeightCurve;
            var curveTime = heightCurve.GetLength();
            float t = 0f;
            while (t < curveTime)
            {
                GFXHeight = heightCurve.Evaluate(t) * height;
                await UniTask.Yield(PlayerLoopTiming.Update);
                t += Time.deltaTime;
            }

            GFXHeight = height;
            // GFX.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            DebugX.Log("PhaseTransitionSequence() - Spawn Monsters");
            // TODO 연결된 BattleArea에 페이즈 설정에 따른 잡몹 소환
            var phaseTransition = BossSettings.PhaseTransitions[PhaseIndex.Value];
            var spawnArea = TargetBattleArea.Spawners[BossSettings.PhaseTransitionSpawnAreaIndex];
            var spawnedMonsters = new List<Monster>(phaseTransition.SpawnDatas.Sum(it => it.Amount));
            foreach (var data in phaseTransition.SpawnDatas)
            {
                spawnArea.Spawn(data.Type, data.Amount, ref spawnedMonsters);
            }

            // TODO 잡몹 전부 죽일 때까지 체력 회복 (초당 5)
            var killCount = 0;

            void AddKillCount(Monster m)
            {
                ++killCount;
                Debug.Log($"[{killCount}] Killed {m.name}");
                m.OnDeadEvent.RemoveListener(AddKillCount);
            }

            foreach (var monster in spawnedMonsters)
            {
                monster.OnDeadEvent.AddListener(AddKillCount);
            }

            DebugX.Log($"PhaseTransitionSequence() - Wait for Kill All Monsters ({spawnedMonsters.Count})");
            while (killCount < spawnedMonsters.Count)
            {
                if(!gameObject.activeSelf) return;
                // 다 잡을 때까지 체력 회복 
                Health += Time.deltaTime * BossSettings.PhaseTransitionHealthRegeneration;
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            spawnedMonsters.Clear();

            DebugX.Log($"PhaseTransitionSequence() - End ...");

            // 다음 페이즈로
            PhaseIndex.Value += 1;
            PhaseTargetHealth.Value = BossSettings.PhaseTransitions[PhaseIndex.Value].TargetHealth;
            BehaviourTree.SetVariable(nameof(PhaseTargetHealth), PhaseTargetHealth);
            BehaviourTree.SetVariable(nameof(PhaseIndex), PhaseIndex);
            DebugX.Log(
                $"PhaseTransitionSequence() - New Target Health = {PhaseTargetHealth.Value}(Index:{PhaseIndex.Value})");
            _isInvincible = false;

            // TODO 모두 죽이면 OnPhaseTransitionEnd
            Animator.SetTrigger(PhaseTransitionEnd);
        }

        #endregion

        #region Pattern: DownAttack

        private static readonly int DownAttackFall = Animator.StringToHash("OnDownAttackFall");

        public void PlayDownAttack()
        {
            if (IsGroggy) return;
            DownAttackSequence().Forget();
        }

        private async UniTaskVoid DownAttackSequence()
        {
            _isInvincible = true;
            DebugX.Log("DownAttackSequence() - Start");
            // TODO 카메라 안 보이는 높이로 GFX 높이 올리기 (실제로 올릴건 아님)
            var baseHeight = GFXHeight;
            var height = BossSettings.PhaseTransitionHeight - baseHeight;

            if (baseHeight <= 1f)
            {
                var leapEffect = EffectManager.Instance.Get(EffectType.BossDownAttackLeap);
                leapEffect.transform.position = transform.position;
            }

            var heightCurve = BossSettings.PhaseTransitionHeightCurve;
            var curveTime = heightCurve.GetLength();
            float t = 0f;
            while (t < curveTime)
            {
                GFXHeight = baseHeight + heightCurve.Evaluate(t) * height;
                await UniTask.Yield(PlayerLoopTiming.Update);
                t += Time.deltaTime;
            }

            GFXHeight = baseHeight + height;

            DebugX.Log($"DownAttackSequence() - GFXHeight: {GFXHeight}");
            NavMeshAgent.Warp(BossStageCenter.transform.position);

            
            BossSettings.Sounds.TryGetValue("DownAttack", out var sound);
            AudioManager.PlayOneShot(sound);
            DebugX.Log($"DownAttackSequence() - Wait for {BossSettings.DownAttackWaitTime}s");
            t = 0f;
            DownAttackRangeProjector.gameObject.SetActive(true);
            DownAttackRangeProjector.Radius = BossSettings.DownAttackRange;
            DownAttackRangeProjector.Progress = 0f;
            bool downFallEffectSpawned = false;
            bool downFallAnimationPlayed = false;
            heightCurve = BossSettings.DownAttackHeightCurve;
            var startY = GFXHeight;
            var downFallEffectStartTime = BossSettings.DownAttackWaitTime - BossSettings.DownAttackEffectFallTime;
            var downFallAnimationStartTime = BossSettings.DownAttackWaitTime - BossSettings.DownAttackFallAnimationTime;
            while (t < BossSettings.DownAttackWaitTime)
            {
                if (t >= downFallEffectStartTime)
                {
                    // 떨어지는 이펙트 먼저 생성
                    if (!downFallEffectSpawned)
                    {
                        downFallEffectSpawned = true;
                        SpawnEffect(nameof(EffectType.BossDownAttack));
                    }

                    // 이펙트 실행 시간동안 Curve에 따라 GFXHeight 조정
                    var normalizedY = t - (downFallEffectStartTime) / BossSettings.DownAttackEffectFallTime;
                    GFXHeight = heightCurve.Evaluate(1f - normalizedY) * startY;
                }

                if (!downFallAnimationPlayed && t >= downFallAnimationStartTime)
                {
                    // 애니메이션 실행
                    downFallAnimationPlayed = true;
                    Animator.SetTrigger(DownAttackFall);
                }

                DownAttackRangeProjector.Progress = t / BossSettings.DownAttackWaitTime;
                await UniTask.Yield(PlayerLoopTiming.Update);
                t += Time.deltaTime;
            }

            // 예외: 혹시라도 fall animation trigger가 안됐다면 일단 실행 ...
            if (!downFallAnimationPlayed)
            {
                Animator.SetTrigger(DownAttackFall);
            }

            DownAttackRangeProjector.gameObject.SetActive(false);

            DebugX.Log($"DownAttackSequence() - FALL !");
            // TODO 떨구기 & Trigger
            GFXHeight = 0f;
            DebugX.Log($"DownAttackSequence() - GFXHeight: {GFXHeight}");

            // 이펙트 타격
            AttackAtEffect(0);
            PlayerView.CameraRandomShake(BossSettings.DownAttackFallShake);

            _isInvincible = false;
        }

        #endregion

        #region Pattern: Rush

        public void Rush()
        {
            if (IsGroggy) return;
            RushSequence().Forget();
            // var t = transform;
            // var origin = t.position;
            // var back = -t.forward;
            // KnockBack(origin + back, 100f, ForceMode.VelocityChange);
        }

        private async UniTaskVoid RushSequence()
        {
            _isInvincible = true;
            RushRangeSensor.Pulse();
            foreach (GameObject target in RushRangeSensor.Detections)
            {
                if (target.CompareTag("Player") && target.TryGetComponent(out PlayerPresenter player))
                {
                    if (player.Damage(BossSettings.RushDamage, gameObject, DamageReaction.KnockBack))
                    {
                        // 공격 성공하면 넉백 임의로 수정
                        var pushDirection = Vector3.Cross(transform.forward, Vector3.up);
                        player.View.Push(pushDirection * 10f, ForceMode.VelocityChange, 0.2f);
                        player.View.TurnTowardController.SetRotation(-pushDirection);
                    }
                }
            }

            var start = transform.position;
            var direction = transform.forward;
            var distance = BossSettings.RushDistance;
            // NavMesh 상에 부딪힐 것 같으면 distance를 줄임 (속도가 줄어듬)
            if (NavMeshAgent.Raycast(start + direction * distance, out var rayHit))
            {
                distance = rayHit.distance;
            }

            // 5 frame
            var rushTime = 5 / 30f;
            float dt = Time.deltaTime;
            for (float t = 0f; t < rushTime; t += (dt = Time.deltaTime))
            {
                var normalizedTime = t / rushTime;
                NavMeshAgent.Warp(start + direction * (normalizedTime * distance));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            NavMeshAgent.Warp(start + direction * distance);
            _isInvincible = false;
        }

        #endregion

        #region Pattern: Flash

        private static readonly int DoAttack = Animator.StringToHash("DoAttack");

        public void PlayTeleport() => TeleportSequence(0).Forget();
        public void PlayTeleport(int index) => TeleportSequence(index).Forget();

        private async UniTaskVoid TeleportSequence(int index)
        {
            if (IsGroggy) return;
            var t = transform;

            var time = BossSettings.FlashAttackSettings[index];
            var beforeHideDelay = time.BeforeHideDelay;
            var hideTime = time.HideTime;

            // 30프레임 대기
            const float effectDelay = 8f;
            await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0, beforeHideDelay - effectDelay) / 30f));

            if (IsGroggy) // 대기하다 그로기 걸렸으면 중단
                return;

            // 이펙트 소환
            PlayFlashAttackSound(0);
            await UniTask.Delay(TimeSpan.FromSeconds(effectDelay / 30f));
            
            if (IsGroggy) // 대기하다 그로기 걸렸으면 중단
                return;

            var targetPosition = PlayerView.transform.position; // 메시 끄기 이전의 플레이어 위치 저장

            // 메시 끄기
            IsActiveGFX = false;
            await UniTask.Delay(TimeSpan.FromSeconds(hideTime / 30f));

            // 메시 키기 & 텔레포트
            Teleport(targetPosition);
            IsActiveGFX = true;

            // 이펙트 소환
            
            PlayFlashAttackSound(1);

            await UniTask.Delay(TimeSpan.FromSeconds(time.BeforeDoAttackDelay / 30f));

            if (IsGroggy) // 대기하다 그로기 걸렸으면 중단
                return;

            Animator.SetTrigger(DoAttack);
            DebugX.Log($"Trigger DoAttack at index {index}");
        }

        public void Teleport(Vector3 targetPosition)
        {
            var origin = transform.position;
            targetPosition += Vector3.up * 1f;

            var front = origin - targetPosition;
            front.y = 0f;
            front.Normalize();

            var side = Vector3.Cross(Vector3.up, front);
            front.y = 0f;
            front.Normalize();

            var distance = FlashDistanceFromTarget;
            // 플레이어 기준 4방위
            var positions = new Vector3[]
            {
                targetPosition + front * distance,
                targetPosition + -front * distance,
                targetPosition + -side * distance,
                targetPosition + side * distance,
            };

            var index = 0;
            var found = false;
            NavMeshHit hit = default;
            foreach (var position in positions)
            {
                // DebugX.Log($"{index}: {position}");
                // DrawUtility.DrawWireSphere(position, 5f, 16, (a, b) => DebugX.DrawLine(a, b, Color.cyan, 5f));
                // DebugX.DrawLine(position, position + Vector3.down * 5f, Color.cyan, 5f);
                // 4방위에 Raycast
                if (!NavMesh.SamplePosition(
                        position,
                        out hit, 5f, NavMesh.AllAreas
                    ))
                {
                    ++index;
                    continue;
                }

                // 찾으면 그 위치로 이동
                found = true;
                break;
            }

            if (!found)
            {
                DebugX.LogWarning("플레이어에게 점멸 실패 ...");
                return;
            }

            var teleportPosition = hit.position;
            // DrawUtility.DrawWireSphere(teleportPosition, 1f, 16, (a, b) => DebugX.DrawLine(a, b, Color.green, 5f));
            DebugX.Log($"{teleportPosition} 위치로 이동 :: {index}");
            NavMeshAgent.Warp(teleportPosition);

            var teleportDirection = (targetPosition - NavMeshAgent.transform.position);
            teleportDirection.y = 0;
            teleportDirection.Normalize();
            NavMeshAgent.transform.rotation = Quaternion.LookRotation(teleportDirection);
        }

        #endregion

        #region Pattern: SpawnBomb

        public void SpawnBomb(int count, float delay, float explosionTime)
        {
            SpawnBombSequence(count, delay, explosionTime).Forget();
        }

        private async UniTaskVoid SpawnBombSequence(int count, float delay, float explosionTime)
        {
            // var prefab = BossSettings.BombPrefab;

            void SpawnBombObject()
            {
                // var obj = Instantiate(prefab, PlayerView.transform.position, Quaternion.identity);
                
                
                BossSettings.Sounds.TryGetValue("SpawnBomb", out var sound);
                AudioManager.PlayOneShot(sound);
            }

            while (count > 0)
            {
                SpawnBombObject();
                --count;
                await UniTask.Delay(TimeSpan.FromSeconds(delay));
            }
        }

        #endregion

        #region Pattern: SectorAttack

        private List<GameObject> _sectorEffects = new(8);

        private static readonly int SectorAttackEnd = Animator.StringToHash("OnSectorAttackEnd");

        public void PlaySectorAttack(bool rotated) => PlaySectorAttackSequence(rotated).Forget();

        private async UniTaskVoid PlaySectorAttackSequence(bool rotated)
        {
            int modular = rotated ? 0 : 1;
            var rotation = rotated ? Quaternion.AngleAxis(45f, Vector3.up) : Quaternion.identity;

            var playerObject = PlayerPresenter.gameObject;

            // 1차 이펙트 소환
            var origin = transform.position;
            

            // 이펙트 폭발할 때 까지 대기
            await UniTask.Delay(TimeSpan.FromSeconds(BossSettings.SectorAttackExplosionDelay));

            var playerPosition = playerObject.transform.position;
            SectorAttackRangeSensor.Pulse();
            // 플레이어 충돌 시
            if (SectorAttackRangeSensor.IsDetected(playerObject))
            {
                // 보스 기준 플레이어 위치
                var bossToPlayerOS = playerPosition - origin;
                bossToPlayerOS.Normalize();
                var angle = Vector3.SignedAngle(Vector3.forward, bossToPlayerOS, Vector3.up) + 180;
                var index = Mathf.FloorToInt(angle / 45f);
                if (index % 2 == modular)
                {
                    PlayerPresenter.Damage(BossSettings.SectorAttackDamage, gameObject, DamageReaction.Stun);
                }
            }
        }

        public void PlaySectorAttackLeap(bool start) => SectorAttackLeap(start).Forget();

        private async UniTaskVoid SectorAttackLeap(bool start)
        {
            // Leap 시작할 때 무적 ON
            if (start)
            {
                _isInvincible = true;
            }

            var height = BossSettings.SectorAttackLeapHeight;
            var curve = start ? BossSettings.SectorAttackLeapStartCurve : BossSettings.SectorAttackLeapEndCurve;

            float curveLength = curve.GetLength();
            float t = 0f;
            while (t < curveLength)
            {
                var h = curve.Evaluate(t) * height;
                GFXHeight = h;
                await UniTask.Yield(PlayerLoopTiming.Update);
                t += Time.deltaTime;
            }

            GFXHeight = curve.Last().value * height;

            // Leap 끝날 때(떨어질 때) 무적 OFF
            if (!start)
            {
                _isInvincible = false;
            }
        }

        #endregion

        #region Sword Charging

        private static readonly int MainInten = Shader.PropertyToID("_Main_Inten");

        public void SwordChargeByRush()
        {
            if (IsGroggy) return;
            SwordChargeSequence(BossSettings.RushSwordChargingCurve).Forget();
        }

        private async UniTaskVoid SwordChargeSequence(AnimationCurve curve)
        {
            if (!_swordMaterial) return;

            float curveLength = curve.GetLength();
            float t = 0f;
            while (t < curveLength)
            {
                if (IsGroggy)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update);
                    _swordMaterial.SetFloat(MainInten, curve.Last().value);
                    return;
                }

                var intensity = curve.Evaluate(t);
                _swordMaterial.SetFloat(MainInten, intensity);
                await UniTask.Yield(PlayerLoopTiming.Update);
                t += Time.deltaTime;
            }

            _swordMaterial.SetFloat(MainInten, curve.Last().value);
        }

        #endregion

        #region KnockBack, Push

        // TODO KnockBack과 관계 역전 필요 ㅋㅋ;
        public void Push(Vector3 direction, float power, ForceMode forceMode, float time = 0.1f, float freezeSlippingFactor = 0.1f)
        {
            KnockBack(new KnockBackInfo(direction, power, time, ForceMode.VelocityChange, freezeSlippingFactor));
        }

        public void PushForwardByNearAttack() => PushForward(
            BossSettings.NearAttackMoveForwardPower,
            BossSettings.NearAttackMoveForwardTime
        );

        public void PushForward(float power, float time)
        {
            Push(transform.forward, power, ForceMode.Acceleration, time);
        }

        #endregion

        #region Sound Only

        public void PlayRangedAttackSound(float parameter)
        {
            BossSettings.Sounds.TryGetValue("RangedAttack", out var sound);
            AudioManager.PlayOneShot(sound, "BossRangedAttackPhase", parameter);
        }

        public void PlayRangedAttackChargingSound() => PlayRangedAttackSound(0);

        public void PlayRushAttackSound()
        {
            BossSettings.Sounds.TryGetValue("RushAttack", out var sound);
            AudioManager.PlayOneShot(sound);
        }
        
        public void PlayFlashAttackSound(float parameter)
        {
            BossSettings.Sounds.TryGetValue("Flash", out var sound);
            AudioManager.PlayOneShot(sound, "BossFlashPhase", parameter);            
        }

        public void PlaySectorAttackSound()
        {
            BossSettings.Sounds.TryGetValue("SectorAttack", out var sound);
            AudioManager.PlayOneShot(sound);
        }

        #endregion

        private void Update()
        {
            if (LookPlayer)
            {
                NavMeshAgent.LookTowards(PlayerView.transform.position);
            }
        }

        private bool IsInvincible => _isInvincible || !IsActiveGFX || GFXHeight > 0f;

        public override EntityHitResult Damage(DamageInfo info)
        {
            // 무적 상태, Mesh 끈 상태에서는 피격 불가능
            if (IsInvincible && (StackedDamage <= 0f || info.Mode != DamageMode.PopAll))
            {
                return EntityHitResult.Invincible;
            }

            return base.Damage(info);
        }

        protected override void OnDamageEnd(DamageInfo info)
        {
            EventReference sound;
            // 각성기 쳐맞고 살아있으면
            // if (info is EnemyDamageInfo { PlayerAttackType: PlayerState.CircleAttack } && !IsInvincible)
            // {
            //     // BT에 Groggy 신호 보내기
            //     BehaviourTree.SendEvent("Groggy");
            //     // 강제 Push
            //     KnockBackCurved(
            //         transform.position - info.Source.transform.position,
            //         BossSettings.GroggyKnockBackVelocityCurve
            //     );
            //     BossSettings.Sounds.TryGetValue("Groggy", out sound);
            //     AudioManager.PlayOneShot(sound);
            // }

            if (Health <= 0f)
            {
                OnDead();
            }
            
            BossSettings.Sounds.TryGetValue("Hit", out sound);
            AudioManager.PlayOneShot(sound);

            base.OnDamageEnd(info);
        }

        private void OnDead()
        {
            TargetBattleArea.EndBattleArea();
            BossSettings.Sounds.TryGetValue("Death", out var sound);
            AudioManager.PlayOneShot(sound);
        }

        private void OnDrawGizmosSelected()
        {
            DrawFourPillarSettings();
        }

        private void DrawFourPillarSettings()
        {
            var origin = transform.position;
            var index = 0;
            foreach (var pillar in BossSettings.WaveAttackSpawnSequence)
            {
                var color = Colors[index];
                Gizmos.color = color;

                foreach (var standardBasis in StandardBasis)
                {
                    var forward = pillar.Rotation * standardBasis;
                    var position = origin + forward * pillar.Distance;
                    var right = Vector3.Cross(Vector3.up, forward);
                    right.Normalize();
                    var up = Vector3.up;

                    var obb = new DrawUtility.OBB()
                    {
                        Center = position + Vector3.up * pillar.HalfExtents.y,
                        Basis = new Vector3[3]
                        {
                            right,
                            up,
                            forward
                        },
                        HalfExtents = pillar.HalfExtents,
                    };

                    DrawUtility.DrawOBB(obb, Gizmos.DrawLine);

                    var axisX = right * pillar.HalfExtents.x;
                    var axisZ = forward * pillar.HalfExtents.z;

                    var innerRight = position + axisZ + axisX;
                    var innerLeft = position + axisZ - axisX;
                    var outerRight = position - axisZ + axisX;
                    var outerLeft = position - axisZ - axisX;

                    Gizmos.DrawWireSphere(innerLeft, pillar.AdditionalNavMeshCheckerRadius);
                    Gizmos.DrawWireSphere(innerRight, pillar.AdditionalNavMeshCheckerRadius);
                    Gizmos.DrawWireSphere(outerLeft, pillar.AdditionalNavMeshCheckerRadius);
                    Gizmos.DrawWireSphere(outerRight, pillar.AdditionalNavMeshCheckerRadius);

                    var length = pillar.HalfExtents.x * 2f / (pillar.AdditionalNavMeshCheckerCount + 1);
                    for (int i = 0; i < pillar.AdditionalNavMeshCheckerCount; i++)
                    {
                        var innerAdditionalChecker = innerLeft + right * (length * (i + 1));
                        var outerAdditionalChecker = outerLeft + right * (length * (i + 1));

                        Gizmos.DrawWireSphere(innerAdditionalChecker, pillar.AdditionalNavMeshCheckerRadius);
                        Gizmos.DrawWireSphere(outerAdditionalChecker, pillar.AdditionalNavMeshCheckerRadius);
                    }
                }

                ++index;
            }
        }
    }
}