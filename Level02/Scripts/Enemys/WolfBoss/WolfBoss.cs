using System;
using Animation;
using AutoManager;
using Character.Core;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Managers;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Utility;
using Zenject;
using Random = UnityEngine.Random;

namespace Enemys.WolfBoss
{
    public class WolfBoss : Enemy
    {
        [ValidateInput("@StarCandyCreatePosition != null", "별사탕 생성 위치가 비어있습니다."), Title("별사탕 소환"), SerializeField]
        public Transform StarCandyCreatePosition;

        [ValidateInput("@StarCandyPrefab != null", "별사탕 프리팹이 비어있습니다."), SerializeField]
        public StarCandy StarCandyPrefab;

        [ValidateInput("@Settings != null", "세팅 파일이 비어 있습니다."), Title("세팅 데이터"), SerializeField]
        public WolfBossSettings Settings;

        [ValidateInput("@ExplosionPosition != null", "폭발 이펙트 위치가 비어 있습니다."), Title("세팅 데이터"), SerializeField]
        private Transform ExplosionPosition;

        [Button("Auto Binding", ButtonSizes.Large)]
        private void BindObjects()
        {
            StarCandyCreatePosition = GameObject.Find("Bip001 Spine2").transform;
        }

        [Inject] private DiContainer _container;
        private Mover _mover;
        private ObservableStateMachineTrigger _stateMachineTrigger;
        private WolfBossJumpAttackStateMachineBehavior _jumpStateMachineTrigger;

        [SerializeField] private InputAction CheatKey;

        [SerializeField] private GameObject DownAttackArea;
        private const int SpawnCount = 4;

        private CinemachineImpulseSource _impulseSource;
        
        protected override void Awake()
        {
            base.Awake();
            HP = Settings.HPMax;
            _mover = GetComponent<Mover>();
            _stateMachineTrigger = EnemyAnimator.GetBehaviour<ObservableStateMachineTrigger>();
            _jumpStateMachineTrigger = EnemyAnimator.GetBehaviour<WolfBossJumpAttackStateMachineBehavior>();
            _playerAndEnemyLayer = LayerMask.GetMask( /*"Player", */"Enemy");

            CheatKey.performed += CheatKeyOnperformed;
            _impulseSource = GetComponent<CinemachineImpulseSource>();
        }
        
        private void CheatKeyOnperformed(InputAction.CallbackContext obj)
        {
            Groggy();
        }

        private void OnEnable()
        {
            CheatKey.Enable();
        }

        private void OnDisable()
        {
            CheatKey.Disable();
        }

        // 플레이어 및 적 레이어
        private static int _playerAndEnemyLayer = 0;

        public bool IsPlayerOrEnemy(int mask)
        {
            return ((1 << mask) & _playerAndEnemyLayer) != 0;
        }

        public bool CanWalk = false;
        public bool CanRush = false;

        private void Start()
        {
            // 석상이랑 닿으면 그로기
            this.OnCollisionEnterAsObservable()
                .Where(it => it.collider.CompareTag("WitchStatue"))
                .Subscribe(it =>
                {
                    var statue = it.gameObject.GetComponentInParent<WitchStatue>();
                    if (statue.HasCracked)
                    {
                        statue.Explode();
                        it.collider.gameObject.SetActive(false);
                        Groggy();
                    }
                });

            // Roar 애니메이션이 끝나면, 다음 상태로 진행
            _stateMachineTrigger.OnStateExitAsObservable()
                .Where(info => info.StateInfo.IsName("Roar"))
                .Subscribe(_ => CustomEvent.Trigger(gameObject, "EndRoar"))
                .AddTo(this);

            _stateMachineTrigger.OnStateExitAsObservable()
                .Where(info => info.StateInfo.IsName("Hit"))
                .Subscribe(_ => CustomEvent.Trigger(gameObject, "EndHit"))
                .AddTo(this);

            _stateMachineTrigger.OnStateExitAsObservable()
                .Where(info => info.StateInfo.IsName("Defence"))
                .Subscribe(_ => CustomEvent.Trigger(gameObject, "EndDefence"))
                .AddTo(this);

            // Walk 애니메이션이 실제로 시작되고 끝나는 지점을 감지해서 CanWalk에 반영
            // => 움직여도 되는지를 판단
            _stateMachineTrigger.OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("Walk"))
                .Subscribe(_ => CanWalk = true)
                .AddTo(this);

            _stateMachineTrigger.OnStateExitAsObservable()
                .Where(info => info.StateInfo.IsName("Walk"))
                .Subscribe(_ => CanWalk = false)
                .AddTo(this);

            // CanWalk와 같은 방식으로 동작함
            _stateMachineTrigger.OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("Rush"))
                .Subscribe(_ => CanRush = true)
                .AddTo(this);

            _stateMachineTrigger.OnStateExitAsObservable()
                .Where(info => info.StateInfo.IsName("Rush"))
                .Subscribe(_ => CanRush = false)
                .AddTo(this);

            // 그로기 상태 종료 시 EndGroggy 호출
        }

        /// <summary>
        /// 다운 어택 상태를 설정합니다.
        /// </summary>
        /// <param name="active"></param>
        public void SetActiveDownAttackArea(bool active)
        {
            if (active)
            {
                _impulseSource.m_ImpulseDefinition.m_ImpulseDuration = 1;
                _impulseSource.GenerateImpulse();
            }
            else
            {
                _impulseSource.m_ImpulseDefinition.m_ImpulseDuration = 0.2f;
            }

            DownAttackArea.SetActive(active);
        }
        
        /// <summary>
        /// 페이드 인을 합니다.
        /// </summary>
        public void ScreenFadeIn()
        {
            FadeInTask().Forget();
        }

        private async UniTaskVoid FadeInTask()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(4));
            WhiteFadeManager whiteFadeManager = FindObjectOfType<WhiteFadeManager>();
            whiteFadeManager.TestFadeIn();
            Time.timeScale = 1f;

            await UniTask.Delay(TimeSpan.FromSeconds(2));
            SceneManager.LoadScene("EndCutScene");
        }

        private void FixedUpdate()
        {
            EnemyController.Core(Settings.gravity);
        }

        public Transform BossHPUIPosition;

        public override void TakeDamage(float damage, GameObject from)
        {
            if (from)
            {
                // 플레이어 직접 공격으로 피해받은 경우 => 무시
                if (from.CompareTag("Player"))
                    return;
            }

            base.TakeDamage(damage, from);
            if (HP > 0)
            {
                // 피격 모션
                CustomEvent.Trigger(gameObject, "OnHit");

                // @ 보스 피격 사운드
                // print("보스 피격");
                Manager.Get<AudioManager>().PlayOneShot(Settings.SFXClips[11], transform.position);
            }
        }

        public void EffectDeath()
        {
            GameObject bombEffect = _container.ResolveId<GameObject>(EffectType.WolfBossDeath);
            GameObject effect = Instantiate(bombEffect, transform.position, Quaternion.identity, transform);
            effect.transform.localPosition = new Vector3(0, 0.7f, 3);
            effect.transform.parent = null;
        }

        public void EffectDeath02()
        {
            GameObject bombEffect02 = _container.ResolveId<GameObject>(EffectType.WolfBossDeath02);
            GameObject effect = Instantiate(bombEffect02, transform.position, Quaternion.identity, ExplosionPosition);
            effect.transform.localPosition = Vector3.zero;
        }

        public void OnTriggerDead()
        {
            Manager.Get<GameManager>().IsNotAttack = true;
            Manager.Get<AudioManager>().StopBGM(true);
            Time.timeScale = 0.4f;
        }

        public bool CanBombExplode(StarCandyBomb bomb)
        {
            // 그로기 상태가 아니면 튕겨냄
            if (!IsGroggy)
            {
                Vector3 bossToBomb = bomb.transform.position - transform.position;
                Vector3 direction = bossToBomb.normalized;
                // 튕겨내기
                bomb.GetThrowable().Throw(direction, 5);
                // 지연 폭발
                bomb.OnTriggerExplosion(true, 0.5f).Forget();

                var stateInfo = EnemyAnimator.GetCurrentAnimatorStateInfo(0);
                // 현재 애니메이션이 Idle 또는 Walk인 경우
                if (CurrentAttackType == WolfBossAttackType.None
                    && (stateInfo.IsName("Idle") || stateInfo.IsName("Walk"))
                   )
                {
                    // Defence 상태도 실행
                    Defence();
                }
            }
            else
            {
                // 그로기 상태가 맞으면? 데미지 입힘 !!
                TakeDamage(1, bomb.gameObject);
                // 즉시 별사탕 폭발
                bomb.OnTriggerExplosion(true, 0f).Forget();
            }

            return IsGroggy;
        }

        public bool IsGroggy = false;
        private GameObject _groggyEffect;

        public void Groggy()
        {
            // 점프 공격 시에는 그로기 작동하지 않음 !!!!! 하지마 !!!
            if (CurrentAttackType == WolfBossAttackType.Jump)
            {
                return;
            }

            // 그로기 시 공격 종류 None으로 설정
            CurrentAttackType = WolfBossAttackType.None;

            // 진행 중이던 이펙트 전부 제거
            TerminateEffects();

            // Bolt에 트리거 호출 (상태 Groggy로 전환)
            CustomEvent.Trigger(gameObject, "Groggy");

            // @ 보스 그로기 사운드
            // print("보스 그로기");
            Manager.Get<AudioManager>().PlayOneShot(Settings.SFXClips[6], transform.position);
            Manager.Get<AudioManager>().PlayOneShot(Settings.SFXClips[13], transform.position);

            // 이펙트
            var effect = _container.ResolveId<GameObject>(EffectType.WolfBossGroggy);
            var t = transform;
            _groggyEffect = Instantiate(effect, t.position, Quaternion.identity);

            // 별사탕 소환
            if (StarCandyPrefab && StarCandyCreatePosition)
            {
                // int spawnCount = Random.Range(
                //     Settings.GroggyStarCandySpawnCountRange.start,
                //     Settings.GroggyStarCandySpawnCountRange.end
                // );

                // 홀수만
                // if (spawnCount % 2 == 0)
                // {
                //     spawnCount += 1;
                // }

                Vector3 directionToPlayer = (PlayerTransform.position - transform.position).normalized;
                directionToPlayer.y = 0;
                directionToPlayer.Normalize();

                Vector3 direction = (-directionToPlayer).RotatedAroundY(45f * Mathf.Deg2Rad);
                Vector3 upPower = Vector3.up * Settings.GroggyStarCandySpawnPowerUpDirection;
                float rotateAmount = (360f / SpawnCount) * Mathf.Deg2Rad;
                Vector3 spawnPosition = StarCandyCreatePosition.position;
                for (int i = 0; i < SpawnCount; i++)
                {
                    StarCandy candy =
                        _container.InstantiatePrefabForComponent<StarCandy>(
                            StarCandyPrefab.gameObject,
                            spawnPosition,
                            Quaternion.identity,
                            null
                        );

                    candy.transform.position = spawnPosition;
                    candy.CoreTriggerWithRootMotion.IsActiveCore = false;
                    float dot = Vector3.Dot(direction, directionToPlayer);
                    Vector3 shootDirection = dot < 0 ? direction : direction - directionToPlayer * (2 * dot);
                    candy.GetEnemyController().AddForce(shootDirection * Settings.GroggyStarCandySpawnPower + upPower,
                        ForceMode.VelocityChange);
                    direction.RotateAroundY(rotateAmount);
                }
            }
        }

        // Groggy 상태 끝날 때 호출
        public void OnEndGroggy()
        {
            TerminateEffects();
        }

        public void Defence()
        {
            CustomEvent.Trigger(gameObject, "Defence");
            // @ 별사탕 방어 사운드
            // print("보스 방어");
            Manager.Get<AudioManager>().PlayOneShot(Settings.SFXClips[12], transform.position);
        }

        public enum WolfBossAttackType
        {
            None,
            Rush,
            Scratch,
            Jump,
        }

        public WolfBossAttackType DeterminedAttackType = WolfBossAttackType.Rush;
        public WolfBossAttackType CurrentAttackType = WolfBossAttackType.None;

        // 근접공격 범위
        private bool IsInNearAttackRange()
        {
            Transform t = transform;
            Vector3 forward = t.forward;
            forward.y = 0;
            forward.Normalize();
            Vector3 target = PlayerTransform.position;
            Vector3 toTarget = (target - t.position);
            toTarget.y = 0;

            // 반지름 바깥
            if (toTarget.sqrMagnitude > Settings.AttackRange * Settings.AttackRange)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 공격 패턴 선택
        /// </summary>
        public void DetermineAttackType()
        {
            switch (HP)
            {
                // 무조건 돌진
                case >= 3:
                    DeterminedAttackType = WolfBossAttackType.Rush;
                    break;
                // 근접하면 할퀴기, 멀면 돌진
                case >= 2:
                    if (IsInNearAttackRange())
                    {
                        DeterminedAttackType = WolfBossAttackType.Scratch;
                    }
                    else
                    {
                        DeterminedAttackType = WolfBossAttackType.Rush;
                    }

                    break;
                // 근접하면 할퀴기, 멀면 반반 확률로 돌진 또는 점프
                case >= 1:
                    if (IsInNearAttackRange())
                    {
                        DeterminedAttackType = WolfBossAttackType.Scratch;
                    }
                    else
                    {
                        int rand = Random.Range(0, 2);
                        switch (rand)
                        {
                            case 0:
                                DeterminedAttackType = WolfBossAttackType.Rush;
                                break;
                            case 1:
                                DeterminedAttackType = WolfBossAttackType.Jump;
                                break;
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// 플레이어가 돌진 공격의 범위 내에 존재하는지 체크
        /// </summary>
        /// <returns></returns>
        public bool IsPlayerInRushRange()
        {
            Transform t = transform;
            Vector3 forward = t.forward;
            forward.y = 0;
            forward.Normalize();
            Vector3 target = PlayerTransform.position;
            Vector3 toTarget = (target - t.position);
            toTarget.y = 0;

            // Range 체크 (0 <= 앞 벡터와 위치벡터 내적 <= range)
            float forwardDot = Vector3.Dot(forward, toTarget);
            if (forwardDot < 0f || forwardDot > Settings.RushRecognizeRange) return false;

            // 높이 체크
            if (toTarget.y > Settings.RushHeight) return false;

            // Width 체크 (|오른쪽 벡터와 위치벡터 내적| <= width)
            Vector3 right = t.right;
            right.y = 0;
            right.Normalize();
            float rightDot = Vector3.Dot(right, toTarget);
            if (Mathf.Abs(rightDot) > Settings.RushWidth / 2f) return false;
            return true;
        }

        private GameObject _rushEffect;
        private Collider[] _overlapCache = new Collider[20];
        private Vector3 _lastPosition;

        /// <summary>
        /// 돌진 공격 시 매 Update마다 호출됨
        /// </summary>
        public void RushTick()
        {
            Transform t = transform;
            CurrentAttackType = WolfBossAttackType.Rush;
            if (!CanRush)
            {
                // 플레이어 바라보는 건 계속 함
                LookAtTargetSlerp(PlayerTransform);
                // 실제 Rush 애니메이션이 아니면 움직이진 않음
                EnemyController.MoveToStop();
                return;
            }

            if (!_rushEffect)
            {
                var effectPrefab = _container.ResolveId<GameObject>(EffectType.WolfBossRush);
                _rushEffect = Instantiate(effectPrefab, t.position, t.rotation, t);
            }

            float rushSpeed = Settings.RushSpeed;

            Vector3 origin = t.position;
            Vector3 forward = t.forward;
            forward.y = 0;
            forward.Normalize();
            Vector3 shift = forward * rushSpeed;
            Vector3 target = PlayerTransform.position;
            Vector3 toTarget = (target - origin);
            toTarget.y = 0;
            // 실제로 움직이는건 Rigidbody가 나중에 움직이겠지만, 일단 움직인 것으로 판정
            EnemyController.MoveTo(origin + shift * 10000, rushSpeed);

            // Range 체크 (0 <= 앞 벡터와 위치벡터 내적 <= range)
            float forwardDot = Vector3.Dot(forward, toTarget);
            if (forwardDot < 0f || forwardDot > Settings.RushAttackRange) return;

            // 높이 체크
            if (toTarget.y > Settings.RushHeight) return;

            // Width 체크 (|오른쪽 벡터와 위치벡터 내적| <= width)
            Vector3 right = t.right;
            right.y = 0;
            right.Normalize();
            float rightDot = Vector3.Dot(right, toTarget);
            if (Mathf.Abs(rightDot) > Settings.RushWidth / 2f) return;
            RushAttack();
        }

        public void RushAttack()
        {
            // 이번에 움직일 위치의 사각형 안에 플레이어가 포함됨
            PlayerController.TakeDamageWithKnockBack(1, transform.position, Settings.RushKnockBackPower);
            CustomEvent.Trigger(gameObject, "EndAttack");
        }

        public Transform RushPrepareEffectPosition;

        public void EffectRushPrepareEffect()
        {
            var effect = _container.ResolveId<GameObject>(EffectType.WolfBossRushPrepare);
            var position = RushPrepareEffectPosition.position;
            var rotation = transform.rotation;
            Instantiate(effect, position, rotation);
        }


        /// <summary>
        /// Slerp 형태로 회전하기
        /// </summary>
        /// <param name="target">바라볼 목표 Transform</param>
        /// <param name="amount">Slerp 시 사용할 계수</param>
        public void LookAtTargetSlerp(Transform target, float amount = 0.05f)
        {
            if (!target) return;
            Vector3 direction = target.position - transform.position;
            direction.y = 0;
            direction.Normalize();

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, amount);
        }

        /// <summary>
        /// 플레이어가 할퀴기 공격의 범위 내에 존재하는지 체크
        /// </summary>
        /// <returns></returns>
        public bool IsPlayerInScratchRange()
        {
            Transform t = transform;
            Vector3 forward = t.forward;
            forward.y = 0;
            forward.Normalize();
            Vector3 target = PlayerTransform.position;
            Vector3 toTarget = (target - t.position);
            toTarget.y = 0;

            // 반지름 바깥
            if (toTarget.sqrMagnitude > Settings.ScratchRange * Settings.ScratchRange)
            {
                return false;
            }

            // 높이 바깥
            if (toTarget.y > Settings.ScratchHeight)
            {
                return false;
            }

            // 각도 바깥 // TODO Dot으로 마이크로최적화?
            if (Vector3.Angle(forward, toTarget) > Settings.ScratchAngle / 2f)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 애니메이션 트리거 -> AnimationEventHandle -> UnityEvent로 호출됨
        /// </summary>
        public void ScratchAttack()
        {
            // 할퀴기 범위 안에 플레이어 있으면
            if (IsPlayerInScratchRange())
            {
                // 그냥 때림
                PlayerController.TakeDamageWithKnockBack(1, transform.position, Settings.ScratchKnockBackPower);
            }
        }

        public void EffectScratchAttack()
        {
            var effect = _container.ResolveId<GameObject>(EffectType.WolfBossScratch);
            var t = transform;
            Instantiate(effect, t.position, t.rotation);
        }

        /// <summary>
        /// 애니메이션 트리거 -> AnimationEventHandle -> UnityEvent로 호출됨
        /// Tracking 상태 시작 시 호출됨
        /// </summary>
        public void ResetCurrentAttackType()
        {
            TerminateEffects();
            CurrentAttackType = WolfBossAttackType.None;
        }

        [HideInInspector] public ParabolaInfo JumpAttackParabolaInfo;

        public bool IsPlayerInJumpAttackRecognizeRange()
        {
            Transform t = transform;
            Vector3 forward = t.forward;
            forward.y = 0;
            forward.Normalize();
            Vector3 target = PlayerTransform.position;
            Vector3 toTarget = (target - t.position);
            toTarget.y = 0;
            // 반지름 바깥
            if (toTarget.sqrMagnitude > Settings.JumpAttackRecognizeRange * Settings.JumpAttackRecognizeRange)
            {
                return false;
            }

            return true;
        }

        public bool IsPlayerInJumpAttackRange()
        {
            Transform t = transform;
            Vector3 forward = t.forward;
            forward.y = 0;
            forward.Normalize();
            Vector3 target = PlayerTransform.position;
            Vector3 toTarget = (target - (t.position + t.TransformVector(Settings.JumpAttackOffset)));
            toTarget.y = 0;
            // 반지름 바깥
            if (toTarget.sqrMagnitude > Settings.JumpAttackRange * Settings.JumpAttackRange)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// WolfBossJumpAttackStateMachineBehavior 에서 호출됨
        /// </summary>
        public void JumpAttack()
        {
            // 할퀴기 범위 안에 플레이어 있으면
            if (IsPlayerInJumpAttackRange())
            {
                // 그냥 때림
                PlayerController.TakeDamageWithKnockBack(1, transform.position, Settings.JumpAttackKnockBackPower);
            }
        }

        public void EffectJumpAttackStart()
        {
            var effect = _container.ResolveId<GameObject>(EffectType.WolfBossJumpAttackStart);
            var t = transform;
            Instantiate(effect, t.position, t.rotation);
        }

        public void EffectJumpAttackGround()
        {
            var effect = _container.ResolveId<GameObject>(EffectType.WolfBossJumpAttackGround);
            var t = transform;
            Instantiate(effect, t.position, t.rotation);
        }

        public void EffectJumpAttackSlash()
        {
            var effect = _container.ResolveId<GameObject>(EffectType.WolfBossJumpAttackSlash);
            var t = transform;
            Instantiate(effect, t.position, t.rotation, t);
        }

        public void TriggerAnimation(string parameter)
        {
            EnemyAnimator.SetTrigger(parameter);
        }

        public void TriggerAnimationBool(string parameter, bool active = true)
        {
            EnemyAnimator.SetBool(parameter, active);
        }

        public void SetAnimationInt(string parameter, int value = 0)
        {
            EnemyAnimator.SetInteger(parameter, value);
        }

        public void TerminateEffects()
        {
            // 돌진 중단
            if (_rushEffect)
            {
                Destroy(_rushEffect);
                _rushEffect = null;

                // @ 돌진하다가 벽에 부딪힘
                if (CurrentAttackType == WolfBossAttackType.Rush)
                {
                    //화면 흔들림
                    _impulseSource.GenerateImpulse();
                    
                    // print("돌진하다가 벽에 박음");
                    Manager.Get<AudioManager>().PlayOneShot(Settings.SFXClips[9], transform.position);
                }
                else
                {
                    // print("돌진 이펙트 중단, 벽에 박는 사운드 미실행");
                }
            }

            // 그로기 이펙트 중단
            if (_groggyEffect)
            {
                Destroy(_groggyEffect);
                _groggyEffect = null;
            }
        }


#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Settings)
            {
                return;
            }

            var t = transform;
            var origin = t.position;
            var forward = t.forward;
            var right = t.right;

            // 돌진
            var height = Settings.RushHeight;
            var rushStartLeft = origin - right * (Settings.RushWidth / 2f);
            var rushStartRight = origin + right * (Settings.RushWidth / 2f);
            var rushEndLeft = origin - right * (Settings.RushWidth / 2f) + forward * (Settings.RushAttackRange);
            var rushEndRight = origin + right * (Settings.RushWidth / 2f) + forward * (Settings.RushAttackRange);

            var rushUp = Vector3.up * height;
            var rushUpStartLeft = rushStartLeft + rushUp;
            var rushUpStartRight = rushStartRight + rushUp;
            var rushUpEndLeft = rushEndLeft + rushUp;
            var rushUpEndRight = rushEndRight + rushUp;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(rushStartLeft, rushStartRight);
            Gizmos.DrawLine(rushEndLeft, rushEndRight);
            Gizmos.DrawLine(rushStartLeft, rushEndLeft);
            Gizmos.DrawLine(rushStartRight, rushEndRight);

            Gizmos.DrawLine(rushUpStartLeft, rushUpStartRight);
            Gizmos.DrawLine(rushUpEndLeft, rushUpEndRight);
            Gizmos.DrawLine(rushUpStartLeft, rushUpEndLeft);
            Gizmos.DrawLine(rushUpStartRight, rushUpEndRight);

            Gizmos.DrawLine(rushStartLeft, rushUpStartLeft);
            Gizmos.DrawLine(rushEndLeft, rushUpEndLeft);
            Gizmos.DrawLine(rushStartRight, rushUpStartRight);
            Gizmos.DrawLine(rushEndRight, rushUpEndRight);

            Gizmos.DrawWireSphere(origin, Settings.RushRecognizeRange);

            // 할퀴기
            var scratchHalfAngle = (Settings.ScratchAngle / 2f) * Mathf.Deg2Rad;
            var rotateLeft = forward.RotatedAroundY(scratchHalfAngle);
            rotateLeft.Normalize();
            var rotateRight = forward.RotatedAroundY(-scratchHalfAngle);
            rotateRight.Normalize();
            var scratchUp = Vector3.up * Settings.ScratchHeight;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, origin + rotateLeft * Settings.ScratchRange);
            Gizmos.DrawLine(origin, origin + rotateRight * Settings.ScratchRange);
            Gizmos.DrawLine(origin + scratchUp, origin + rotateLeft * Settings.ScratchRange + scratchUp);
            Gizmos.DrawLine(origin + scratchUp, origin + rotateRight * Settings.ScratchRange + scratchUp);
            // 할퀴기 - 부채꼴 (호)
            int scratchLoopCount = 10;
            float scratchLoopAngle = -(Settings.ScratchAngle / scratchLoopCount) * Mathf.Deg2Rad;
            Vector3 scratchLoopDirection = new Vector3(
                rotateLeft.x,
                rotateLeft.y,
                rotateLeft.z
            );
            Vector3 scratchLoopBeforeDirection = scratchLoopDirection;
            for (int i = 0; i < scratchLoopCount; ++i)
            {
                scratchLoopDirection = scratchLoopBeforeDirection.RotatedAroundY(scratchLoopAngle);
                var loopOrigin = origin + scratchLoopBeforeDirection * Settings.ScratchRange;
                var loopDirection = origin + scratchLoopDirection * Settings.ScratchRange;
                Gizmos.DrawLine(loopOrigin, loopDirection);
                Gizmos.DrawLine(loopOrigin + scratchUp, loopDirection + scratchUp);
                scratchLoopBeforeDirection = scratchLoopDirection;
            }

            // 점프공격
            Gizmos.color = Color.cyan * 0.5f;
            Gizmos.DrawWireSphere(origin, Settings.JumpAttackRecognizeRange);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin + t.TransformVector(Settings.JumpAttackOffset), Settings.JumpAttackRange);

            // 근접 공격 판정 범위
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, Settings.AttackRange);
        }
#endif
    }
}