using System;
using AutoManager;
using Character.Animation;
using Character.Input.Character;
using Character.USystem.Camera;
using Character.USystem.Throw;
using Cinemachine;
using Enemys;
using FMODUnity;
using GameplayIngredients;
using Items;
using Managers;
using Settings;
using Sirenix.OdinInspector;
using UITweenAnimation;
using UniRx;
using UniRx.Triggers;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Utility;
using Zenject;

namespace Character.View
{
    public class PlayerView : MonoBehaviour
    {
        [Inject] private DiContainer _container;

        [FoldoutGroup("카메라"), SerializeField] private GameObject defaultCamera;

        [FoldoutGroup("카메라"), SerializeField] private GameObject zoomCamera;

        [FoldoutGroup("카메라"), SerializeField] private CameraController cameraController;

        [FoldoutGroup("카메라"), SerializeField] private TurnTowardPlayerController turnTowardTransformDirection;

        [FoldoutGroup("카메라"), SerializeField] private Animator animator;

        [field: SerializeField, FoldoutGroup("투척"), Tooltip("투척 시스템")]
        public ThrowSystem throwSystem { get; private set; }

        [field: SerializeField, FoldoutGroup("손가락 끈끈이")]
        private Rig rig;

        [field: SerializeField, FoldoutGroup("손가락 끈끈이")]
        private TwoBoneIKConstraint constraint;

        [field: SerializeField, FoldoutGroup("공격 및 잡기 영역")]
        [field: ValidateInput("@AttackGuide != null", "공격 가이드가 없습니다.")]
        public BoxCollider AttackGuide { get; private set; }

        [field: SerializeField, FoldoutGroup("공격 및 잡기 영역")]
        [field: ValidateInput("@CatchGuide != null", "잡기 가이드가 없습니다.")]
        public BoxCollider CatchGuide { get; private set; }

        [field: SerializeField, FoldoutGroup("무적"), Title("오리지널")]
        private Material[] originalMaterial;

        [field: SerializeField, FoldoutGroup("무적"), Title("무적")]
        private Material[] invincibleMaterial;

        [field: SerializeField, FoldoutGroup("무적"), Title("매쉬 렌더러")]
        private SkinnedMeshRenderer playerMeshRenderer;

        [field: FoldoutGroup("디버그 모드"), SerializeField, Tooltip("훅을 걸 수 있는가? 를 확인하는 용도")]
        public bool IsDebugMode { get; private set; }

        [field: FoldoutGroup("잡기"), SerializeField, Tooltip("플레이어 손에 붙어있는 더미 본")]
        public Transform PlayerHandDummyBoneTransform { get; private set; }

        [Inject(Id = "CROSS-HAIR")] private GameObject _crossHair;

        private Transform _tr;

        public bool useStrafeAnimations;

        [Tooltip("컨트롤러의 변환을 기준으로 운동량을 계산하고 적용할지 여부입니다.")]
        public bool useLocalMomentum;

        private const float SmoothingFactor = 40f;
        private Vector3 _oldMovementVelocity = Vector3.zero;

        //착륙 애니메이션의 속도 임계값
        //애니메이션은 하향 속도가 이 임계값을 초과하는 경우에만 트리거됩니다.
        public float landVelocityThreshold = 5f;
        private CharacterInput _characterInput;

        private CharacterSettings _settings;

        private static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");
        private static readonly int HorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
        private static readonly int ForwardSpeed = Animator.StringToHash("ForwardSpeed");
        private static readonly int StrafeSpeed = Animator.StringToHash("StrafeSpeed");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int IsStrafing = Animator.StringToHash("IsStrafing");
        private static readonly int BehaviourID = Animator.StringToHash("Behaviour");
        private static readonly int OnLand = Animator.StringToHash("OnLand");
        private static readonly int OnRope = Animator.StringToHash("OnRope");
        private static readonly int OnHookShot = Animator.StringToHash("OnHookShot");
        private static readonly int OnAttack = Animator.StringToHash("OnAttack");
        private static readonly int OnReAttack = Animator.StringToHash("OnReAttack");
        private static readonly int OnIsReAttackInputCheck = Animator.StringToHash("IsReAttackInputCheck");
        private static readonly int IsJump = Animator.StringToHash("IsJump");
        private static readonly int OnJumpRope = Animator.StringToHash("OnJumpRope");
        private static readonly int OnCatch = Animator.StringToHash("OnCatch");
        private static readonly int OnDoubleJump = Animator.StringToHash("OnDoubleJump");
        private static readonly int OnHit = Animator.StringToHash("OnHit");
        private static readonly int OnDeath = Animator.StringToHash("OnDeath");


        private void Awake()
        {
            _tr = transform;
            _characterInput = GetComponent<CharacterInput>();
            _settings = Manager.Get<GameManager>().characterSettings;
        }

        private void Start()
        {
            Messager.RegisterMessage("GetItem", OnGetItem);
            Messager.RegisterMessage("HpRecovery", OnHpRecovery);
            Messager.RegisterMessage("LandDust", OnLandDust);
        }

        /// <summary>
        /// 플레이어가 죽으면 릴리즈합니다.
        /// </summary>
        private void OnDestroy()
        {
            Messager.RemoveAllMessages();
        }

        public void OpenDeathUI()
        {
            _container.ResolveId<UIView>("DeadUI").Show(ShowAnimation.Show);
        }

        /// <summary>
        /// GFX의 회전을 Zero로 만듭니다.
        /// </summary>
        public void ResetGFXRotation()
        {
            turnTowardTransformDirection.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        /// <summary>
        /// 플레이어 GFX를 보이거나, 숨기도록 처리합니다.
        /// </summary>
        /// <param name="active"></param>
        public void SetActiveGFX(bool active)
        {
            animator.gameObject.SetActive(active);
        }

        public StarCandyBomb CreateStarCandyBomb(Throwable starCandyThrowable, Transform parentBone,
            bool useCenter = false)
        {
            // //스타 밤 프리팹을 생성합니다.
            StarCandyBomb starCandyBomb =
                _container.InstantiatePrefabForComponent<StarCandyBomb>(starCandyThrowable.gameObject);
            Transform starCandyBombTransform = starCandyBomb.transform;

            //부모 자식 변경
            starCandyBombTransform.parent = parentBone;

            //트랜스폼 조정
            starCandyBombTransform.localPosition = Vector3.zero;
            starCandyBombTransform.localRotation = Quaternion.Euler(97, 0, 0);
            starCandyBombTransform.localScale = Vector3.one;

            //물리 제거
            starCandyBomb.OnTriggerNoPhysics();

            //피벗을 중앙으로 이동합니다.
            if (useCenter)
                starCandyBomb.ChangePivot(PivotStyle.Center);

            return starCandyBomb;
        }

        public void PutDownThrowableObject()
        {
            Transform targetPoint = PlayerHandDummyBoneTransform.GetChild(0);

            bool isStarCandy = targetPoint.gameObject.layer == LayerMask.NameToLayer("BombObject");
            bool isKey = targetPoint.gameObject.layer == LayerMask.NameToLayer("CatchObject");

            //스타 캔디 트랜스폼
            Transform targetTransform;

            if (isStarCandy)
            {
                StarCandyBomb starCandy = targetPoint.GetComponent<StarCandyBomb>();

                //스타 캔디 트랜스폼
                targetTransform = starCandy.transform;

                //손에서 빠져 나옵니다.
                targetTransform.parent = null;

                //땅에 붙습니다.
                starCandy.FloorSnap();
                starCandy.ChangePivot(PivotStyle.CenterBottom);

                Vector3 currentRotation = targetTransform.eulerAngles;
                currentRotation.x = 0;
                currentRotation.z = 0;

                //회전을 되돌립니다.
                targetTransform.eulerAngles = currentRotation;

                //물리를 적용합니다.
                starCandy.GetThrowable().OnTriggerPhysics();

                //폭발을 준비합니다.
                starCandy.OnTriggerExplosion().Forget();
            }
            else if (isKey)
            {
                KeyObject keyObject = targetPoint.GetComponent<KeyObject>();

                //스타 캔디 트랜스폼
                targetTransform = keyObject.transform;

                //손에서 빠져 나옵니다.
                targetTransform.parent = null;

                //물리를 다시 적용합니다.
                keyObject.OnApplyPhysics();

                Vector3 currentRotation = targetTransform.eulerAngles;
                currentRotation.x = 0;
                currentRotation.z = 0;

                //회전을 되돌립니다.
                targetTransform.eulerAngles = currentRotation;
            }
        }

        /// <summary>
        /// 던지기 오브젝트를 던집니다.
        /// </summary>
        public void ShotThrowableObject()
        {
            //발사
            Transform targetPoint = PlayerHandDummyBoneTransform.GetChild(0);
            StarCandyBomb starCandy = targetPoint.GetComponent<StarCandyBomb>();

            //  @ 오브젝트 던지기 사운드
            Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[12], transform.position);

            //피벗을 중심 하단으로 이동시킵니다.
            starCandy.ChangePivot(PivotStyle.CenterBottom);

            //무언가에 닿으면 터지도록 합니다.
            starCandy.OnTriggerEnterExplosion();

            throwSystem.Throw(GetThrowPosition());
        }

        public Vector3 GetThrowPosition() => PlayerHandDummyBoneTransform.position;

        public Transform GetDummyTransform => PlayerHandDummyBoneTransform;

        /// <summary>
        /// 이 함수는 컨트롤러가 공중에 있다가 표면에 착륙했을 때 호출됩니다.
        /// </summary>
        /// <param name="momentum"></param>
        public void OnGroundContactRegained(Vector3 momentum)
        {
            Vector3 collisionVelocity = momentum;

            //로컬 모멘텀을 사용하는 경우 모멘텀을 먼저 월드 좌표로 변환;
            if (useLocalMomentum)
                collisionVelocity = _tr.localToWorldMatrix * collisionVelocity;

            //하향 속도가 임계값을 초과하는 경우에만 사운드를 트리거합니다.;
            if (VectorMath.GetDotProduct(collisionVelocity, _tr.up) > -landVelocityThreshold)
                return;

            //땅 착지 애니메이션 재생
            animator.SetTrigger(OnLand);

            // @착지 사운드
            Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[5], transform.position);
            Messager.Send("LandDust");
        }

        /// <summary>
        /// 이동 애니메이션 업데이트
        /// </summary>
        /// <param name="velocity"></param>
        /// <param name="isGrounded"></param>
        public void OnUpdateMoveAnimation(Vector3 velocity, bool isGrounded, bool isStop)
        {
            Vector3 up = _tr.up;
            Vector3 horizontalVelocity = VectorMath.RemoveDotVector(velocity, up);
            Vector3 verticalVelocity = velocity - horizontalVelocity;

            horizontalVelocity =
                Vector3.Lerp(_oldMovementVelocity, horizontalVelocity, SmoothingFactor * Time.deltaTime);
            _oldMovementVelocity = horizontalVelocity;

            if (!isStop)
            {
                animator.SetFloat(VerticalSpeed,
                    verticalVelocity.magnitude * VectorMath.GetDotProduct(verticalVelocity.normalized, up));

                animator.SetFloat(HorizontalSpeed, horizontalVelocity.magnitude);
            }
            else
            {
                animator.SetFloat(VerticalSpeed, 0f);
                
                animator.SetFloat(HorizontalSpeed, 0f);
            }


            if (useStrafeAnimations)
            {
                Vector3 localVelocity = animator.transform.InverseTransformVector(horizontalVelocity);
                animator.SetFloat(ForwardSpeed, localVelocity.z);
                animator.SetFloat(StrafeSpeed, localVelocity.x);
            }

            animator.SetBool(IsGrounded, isGrounded);
            animator.SetBool(IsStrafing, useStrafeAnimations);
        }

        #region Get

        /// <summary>
        /// 현재 애니메이터를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Animator CurrentAnimator() => animator;

        /// <summary>
        /// 캐릭터가 바라보고 있는 방향을 가져옵니다.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetForward() => turnTowardTransformDirection.GetForward();

        /// <summary>
        /// 타겟을 향해 몸만 회전합니다. 
        /// </summary>
        /// <param name="target"></param>
        public void RotateTarget(Vector3 target)
        {
            turnTowardTransformDirection.SetRotationBody(target);
        }

        /// <summary>
        /// 카메라 정면 방향으로 회전합니다.
        /// </summary>
        public void RotateToCameraForward()
        {
            Vector3 direction = cameraController.GetFacingDirection();
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

            turnTowardTransformDirection.SetRotation(angle);
        }

        /// <summary>
        /// 카메라 스타일을 가져옵니다.
        /// </summary>
        /// <returns></returns>
        public ECameraStyle GetCameraStyle()
        {
            return turnTowardTransformDirection.cameraStyle;
        }

        public CharacterInput GetInput()
        {
            return _characterInput;
        }

        #endregion

        #region Set

        /// <summary>
        /// 플레이어의 무적 상태를 변화합니다.
        /// </summary>
        /// <param name="state"></param>
        public void ChangeInvincible(InvincibleState state)
        {
            //0 :Body
            //1 :Head
            playerMeshRenderer.materials = state == InvincibleState.Original ? originalMaterial : invincibleMaterial;
        }

        /// <summary>
        /// 카메라를 줌하거나 디폴트 상태로 변경합니다.
        /// </summary>
        /// <param name="active"></param>
        /// <param name="showCrossHair"></param>
        public void ChangeZoomCamera(bool active, bool showCrossHair = false)
        {
            if (!zoomCamera || !_crossHair || !defaultCamera)
                DebugX.LogError("defaultCamera 또한 zoomCamera 또는 crossHair가 참조되어 있지 않습니다.");

            if (active)
            {
                zoomCamera.SetActive(true);
                defaultCamera.SetActive(false);
            }
            else
            {
                defaultCamera.SetActive(true);
                zoomCamera.SetActive(false);
            }

            _crossHair.SetActive(showCrossHair);
        }

        /// <summary>
        /// 카메라 스타일을 변경합니다.
        /// </summary>
        /// <param name="style"></param>
        public void SetCameraStyle(ECameraStyle style)
        {
            turnTowardTransformDirection.cameraStyle = style;
        }

        /// <summary>
        /// 크로스 헤어의 활성화 상태를 설정합니다.
        /// </summary>
        /// <param name="active"></param>
        public void SetActiveCrossHire(bool active)
        {
            _crossHair.SetActive(active);
        }

        /// <summary>
        /// 카메라 상태를 얼리거나 해제합니다.
        /// active가 true이면 얼리고, false이면 해제합니다.
        /// </summary>
        /// <param name="active"></param>
        public void FreezeRotationCamera(bool active)
        {
            cameraController.enabled = !active;
        }

        /// <summary>
        /// 카메라 회전을 설정합니다.
        /// </summary>
        /// <param name="direction"></param>
        public void SetRotationCamera(Vector3 direction)
        {
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.forward);
            cameraController.SetRotationAngles(rotation.eulerAngles.x, rotation.eulerAngles.y);
        }

        /// <summary>
        /// 카메라 마우스 락을 처리합니다.
        /// </summary>
        /// <param name="active">true시 마우스가 잠기고, false가 되면 마우스가 표시됩니다.</param>
        public void SetActiveCursor(bool active)
        {
            cameraController.SetActiveCursor(active);
        }

        /// <summary>
        /// Rig 타겟의 위치를 설정합니다.
        /// </summary>
        public void SetTwoBoneIKTargetPosition(Vector3 targetPosition)
        {
            constraint.data.target.position = targetPosition;
        }

        /// <summary>
        /// Rig 타겟의 위치를 가져옵니다.
        /// </summary>
        public Transform GetRigTargetPosition() => constraint.data.target;

        /// <summary>
        /// Rig 타겟의 위치를 설정합니다.
        /// </summary>
        public void SetRigTargetWeight(float weight)
        {
            rig.weight = weight;
        }

        /// <summary>
        /// RHand를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Transform GetHand()
        {
            return constraint.data.tip;
        }

        /// <summary>
        /// 로프를 던지는 애니메이션을 재생합니다.
        /// </summary>
        public void OnPlayTriggerRope(ERopeState state, bool isAir = false)
        {
            if (state == ERopeState.Pull)
                animator.SetTrigger(OnRope);
            else if (state == ERopeState.MoveToTarget)
            {
                if (isAir)
                    animator.SetTrigger(OnJumpRope);
                else
                    animator.SetTrigger(OnHookShot);
            }
        }

        /// <summary>
        /// 점프 애니메이션을 재생합니다.
        /// </summary>
        public void OnTriggerAnimation(PlayerAnimation animationType, bool active = false)
        {
            switch (animationType)
            {
                case PlayerAnimation.OnJump:
                    animator.SetBool(IsJump, active);
                    break;
                case PlayerAnimation.OnDJump:
                    animator.SetTrigger(OnDoubleJump);
                    break;
                case PlayerAnimation.OnDeath:
                    animator.SetTrigger(OnDeath);
                    
                    //지금 나오는 BGM을 멈춥니다.
                    Manager.Get<AudioManager>().StopBGM(true);
                    
                    // @플레이어 사망 사운드
                    Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[15]);
                    break;
                case PlayerAnimation.OnCatch:
                    animator.SetTrigger(OnCatch);
                    break;
                case PlayerAnimation.OnHit:
                    animator.SetTrigger(OnHit);
                    break;
                case PlayerAnimation.OnAttack:
                    animator.SetTrigger(OnAttack);
                    break;
                case PlayerAnimation.OnReAttack:
                    animator.SetTrigger(OnReAttack);
                    break;
                case PlayerAnimation.OnReAttackInputCheck:
                    animator.SetBool(OnIsReAttackInputCheck, active);
                    break;
            }
        }

        /// <summary>
        /// 애니메이션 디테일 행동을 제어합니다.
        /// </summary>
        /// <param name="id"></param>
        public void SetBehaviourID(int id)
        {
            animator.SetInteger(BehaviourID, id);
        }

        /// <summary>
        /// 행동 애니메이션 디테일을 초기화합니다.
        /// </summary>
        public void ResetBehaviourAnimation()
        {
            animator.SetInteger(BehaviourID, 0);
            animator.ResetTrigger(OnRope);
            animator.ResetTrigger(OnDoubleJump);
            animator.SetBool(IsJump, false);
        }

        public void PutDownStarCandy(StarCandy target)
        {
            var targetTransform = target.transform;
            targetTransform.parent = null;
            ;
            targetTransform.rotation = Quaternion.Euler(0, 0, 0);
            target.OnTriggerState(StarCandy.State.PreExplosion);
        }

        #endregion

        #region Effect

        /// <summary>
        /// 착지 먼지 이펙트
        /// </summary>
        private void OnLandDust()
        {
            GameObject effect = _container.ResolveId<GameObject>(EffectType.LandDust);
            Vector3 position = _tr.position;
            position.y += 0.569f;

            //지형의 굴곡에 따라 랜드 더스트 위치를 수정하기 위함이다.
            bool isHit = Physics.Raycast(position, Vector3.down, out RaycastHit hit, 2f, LayerMask.GetMask("Ground"));
            if (isHit)
            {
                position = hit.point;
                position.y += 0.01f;
            }
            else
            {
                position = _tr.position;
                position.y += 0.01f;
            }

            Instantiate(effect, position, Quaternion.identity);
        }

        /// <summary>
        /// 초코탄에 맞았을 때 폭발 이펙트
        /// </summary>
        /// <param name="position"></param>
        public void OnChocolateBomb(Vector3 position)
        {
            GameObject effect = _container.ResolveId<GameObject>(EffectType.ChocolateBomb);
            Instantiate(effect, position, Quaternion.identity);
        }

        public void SetOldMovementVelocity(Vector3 movementVelocity)
        {
            _oldMovementVelocity = movementVelocity;
        }

        /// <summary>
        /// 로프를 던져서 날아가는 이펙트
        /// </summary>
        public void OnRopeRush()
        {
            float angle = turnTowardTransformDirection.GetEulerAngle().y;

            GameObject effect = _container.ResolveId<GameObject>(EffectType.RopeRush);
            Instantiate(effect, transform.position, Quaternion.Euler(new Vector3(0, angle, 0)));
        }

        /// <summary>
        /// 로프를 던져서 날아가는 이펙트
        /// </summary>
        public void OnWatterSlash(float positionY, bool isAdd)
        {
            Vector3 position = _tr.position;

            if (isAdd)
                position.y += positionY;
            else
                position.y = positionY;

            GameObject effect = _container.ResolveId<GameObject>(EffectType.WaterSplash);
            Instantiate(effect, position, quaternion.identity);

            // @물에 빠지는 사운드
            Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[16], transform.position);
        }


        /// <summary>
        /// 로프를 던져서 날아가는 이펙트
        /// </summary>
        public void OnAttack01(ERopeState state)
        {
            float angle = turnTowardTransformDirection.GetEulerAngle().y;

            GameObject effect = _container.ResolveId<GameObject>(state == ERopeState.Pull
                ? EffectType.Attack01Red
                : EffectType.Attack01Blue);

            Instantiate(effect, transform.position, Quaternion.Euler(new Vector3(0, angle, 0)));
        }

        /// <summary>
        /// 로프를 던져서 날아가는 이펙트
        /// </summary>
        public void OnAttack02(ERopeState state)
        {
            float angle = turnTowardTransformDirection.GetEulerAngle().y;

            GameObject effect = _container.ResolveId<GameObject>(state == ERopeState.Pull
                ? EffectType.Attack02Red
                : EffectType.Attack02Blue);

            Instantiate(effect, transform.position, Quaternion.Euler(new Vector3(0, angle, 0)));
        }

        /// <summary>
        /// 피해를 받는 이펙트
        /// </summary>
        public void OnHitEffect()
        {
            float angle = turnTowardTransformDirection.GetEulerAngle().y;

            Vector3 pos = transform.position;
            pos.y += 0.731f;

            GameObject effect = _container.ResolveId<GameObject>(EffectType.Hit);
            Instantiate(effect, pos, Quaternion.Euler(new Vector3(0, angle, 0)));
        }

        /// <summary>
        /// 달고나를 N개 모아서 회복하는 이펙트
        /// </summary>
        private void OnHpRecovery()
        {
            GameObject effect = _container.ResolveId<GameObject>(EffectType.HpRecovery);
            var position = _tr.position + new Vector3(0, 0.008f, 0);
            Instantiate(effect, position, Quaternion.identity);
            // @체력 회복 사운드
            Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[13], transform.position);
        }

        /// <summary>
        /// 달고나 획득 이펙트
        /// </summary>
        private void OnGetItem()
        {
            GameObject effect = _container.ResolveId<GameObject>(EffectType.GetItem);
            var position = _tr.position + new Vector3(0, 0.62f, 0);
            Instantiate(effect, position, Quaternion.identity);
            //  @달고나 획득 사운드
            Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[10], transform.position);
        }

        #endregion

        #region Oberservable

        public IObservable<Unit> GetKeyDownHookShotAttackKey() => _characterInput.pressHookShot
            .Where(active => active).Select(_ => Unit.Default);

        public IObservable<Unit> GetKeyDownInteractionKey() => _characterInput.pressInteraction
            .Where(active => active).Select(_ => Unit.Default);

        public IObservable<Unit> GetKeyDownPullStyleKey() => _characterInput.pressPullStyle
            .Where(active => active).Select(_ => Unit.Default);

        public IObservable<Unit> GetKeyDownMoveToTargetKey() => _characterInput.pressMoveToTargetStyle
            .Where(active => active).Select(_ => Unit.Default);


        // public IObservable<Unit> GetKeyJumpObservable() =>
        //     this.UpdateAsObservable().Where(_ => _characterInput.pressJump.Value).Where(_ => !_playerModel.IsStop);

        public IObservable<Unit> GetKeyDownJumpObservable() =>
            _characterInput.pressJump.Where(active => active).Select(_ => Unit.Default);

        public IObservable<Unit> GetKeyDownAttackObservable() =>
            _characterInput.pressAttack.Where(active => active).Select(_ => Unit.Default);

        public IObservable<Unit> GetKeyDownReAttackObservable() =>
            _characterInput.pressAttack.Where(active => active)
                .Where(_ => animator.GetBool(OnIsReAttackInputCheck))
                .Select(_ => Unit.Default);

        public IObservable<Unit> GetAKeyDownAttackObservable() =>
            _characterInput.pressAttack.Where(active => active).Select(_ => Unit.Default);

        // public IObservable<EOtherState> ChangeHookStateObservable() => _playerModel.OtherStateObservable
        //     .Where(state => state == EOtherState.ReadyRope)
        //     .Where(_ => _characterInput.pressReadyHook.Value);

        public IObservable<float> GetKeyDownChangeHookObservable() =>
            _characterInput.pressChangeHook.AsObservable();

        public IObservable<Unit> GetKeyDownRopeCancelObservable() =>
            _characterInput.RopeCancel.Where(active => active).Select(_ => Unit.Default);
        
        public IObservable<Unit> GetKeyDownReadyHookStateObserver() =>
            _characterInput.pressReadyHook.Where(active => active).Select(_ => Unit.Default);

        public IObservable<Unit> GetKeyDownThrowingHookObservable() =>
            _characterInput.pressThrowHook.Where(value => value).Select(_ => Unit.Default);

        public IObservable<Unit> GetKeyDownReadyThrowAttackKey() => _characterInput.pressUseItemAttack
            .Where(active => active).Select(_ => Unit.Default);

        // public IObservable<bool> GetKeyDownThrowingAttackObservable() =>
        //     _characterInput.pressThrowHook.Where(value => value).AsObservable()
        //         .Where(_ => _playerModel.OtherState == EOtherState.Aiming);

        #endregion
    }
}