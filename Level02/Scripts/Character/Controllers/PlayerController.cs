using System;
using AutoManager;
using Character.Animation;
using Character.Core;
using Character.Input.Character;
using Character.Model;
using Character.USystem.Hook.Model;
using Character.USystem.Hook.View;
using Character.View;
using Enemys;
using GameplayIngredients;
using Managers;
using Settings;
using Sirenix.OdinInspector;
using UITweenAnimation;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Utility;
using Zenject;
using EHookState = Character.USystem.Hook.Model.EHookState;
using Random = UnityEngine.Random;

namespace Character.Controllers
{
    [RequireComponent(typeof(PlayerView))]
    public partial class PlayerController : MonoBehaviour
    {
        private CharacterSettings _settings;
        [Inject] private OffScreenSystemManager _offScreenSystemManager;

        //View-Model
        private PlayerView _playerView;
        private PlayerModel _playerModel;

        [Inject] private HookSystemView _hookSystemView;
        [Inject] private HookSystemModel _hookSystemModel;

        //컴포넌트
        private Mover _mover;
        private Camera _camera;

        private Transform _tr;

        //private CharacterInput _characterInput;
        private CapsuleCollider _collider;

        //현재 점프 시작 시간
        private float _currentJumpStartTime;

        //현재 momentum;
        private Vector3 _momentum = Vector3.zero;

        //마지막 프레임에서 저장된 속도;
        private Vector3 _savedVelocity = Vector3.zero;

        private ObservableStateMachineTrigger _stateMachineTrigger;

        //마지막 프레임에서 저장된 수평 이동 속도;
        private Vector3 _savedMovementVelocity = Vector3.zero;

        [Tooltip("이동 방향 계산에 사용되는 선택적 카메라 변환입니다. 할당된 경우 캐릭터 이동은 카메라 뷰를 고려합니다.")]
        public Transform cameraTransform;

        private void Awake()
        {
            _tr = transform;
            _camera = Camera.main;
            _mover = GetComponent<Mover>();
            _playerView = GetComponent<PlayerView>();
            _playerModel = GetComponent<PlayerModel>();
            _collider = GetComponent<CapsuleCollider>();
            _stateMachineTrigger = _playerView.CurrentAnimator().GetBehaviour<ObservableStateMachineTrigger>();
            _settings = Manager.Get<GameManager>().characterSettings;
        }


        private void Start()
        {
            //기본적으로는 숨깁니다.
            _hookSystemView.ChangeWeaponActiveType(WeaponType.Normal);

            #region 점프 & 더블 점프

            //점프 기능
            _playerView.GetKeyDownJumpObservable()
                .Where(_ => !_playerModel.IsStop)
                .Where(_ => _playerModel.OtherState is not (EOtherState.HookShotRotate or EOtherState.HookShotFlying
                    or EOtherState.Catch))
                .Where(_ => _playerModel.CurrentControllerState != ControllerState.Jumping)
                .Subscribe(_ => OnJumpOrDoubleJump())
                .AddTo(this);

            //땅에 닿았을 때, 점프 상태를 초기화
            _playerModel.CurrentControllerStateObservable
                .Where(state => state == ControllerState.Grounded)
                .Select(_ => Unit.Default).Subscribe(ResetOnJump)
                .AddTo(this);

            #endregion

            #region 캐치

            //Catching 상태에서 초기화를 해준다.
            _stateMachineTrigger.OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("Catching"))
                .Subscribe(_ =>
                {
                    try
                    {
                        _playerView.SetBehaviourID(0);
                        _playerView.ChangeZoomCamera(true);
                        _hookSystemView.ChangeWeaponActiveType(WeaponType.AllHide); //무기를 제거합니다.

                        //손에 있는 별사탕을 제거 합니다.
                        Transform targetPoint = _playerView.PlayerHandDummyBoneTransform.GetChild(0);

                        //스타 캔디면 던지기 UI를 띄웁니다.
                        bool isStarCandy = targetPoint.gameObject.layer == LayerMask.NameToLayer("StarCandy");

                        if (isStarCandy)
                            _playerView.throwSystem.SetActiveThrow(true);
                    }
                    catch (Exception)
                    {
                        _playerView.throwSystem.SetActiveThrow(false);
                        _playerView.ChangeZoomCamera(false, true);
                        _playerView.FreezeRotationCamera(false);
                        _hookSystemView.ChangeWeaponActiveType(WeaponType.Normal);
                        _hookSystemView.HideSystemRope();
                        _playerView.CurrentAnimator().Play("Idle");
                    }
                })
                .AddTo(this);

            //캐치를 시작합니다.
            _playerView
                .GetKeyDownInteractionKey()
                .Where(_ => _playerModel.CurrentControllerState == ControllerState.Grounded)
                .Subscribe(_ => OnTriggerCatchToCandyKey())
                .AddTo(this);

            //내려놓기 자세를 취한다.
            _playerView.GetKeyDownInteractionKey()
                .Where(_ => _playerModel.CurrentControllerState == ControllerState.Grounded)
                .Where(_ => _playerModel.OtherState == EOtherState.Catch)
                .Where(_ => _playerView.PlayerHandDummyBoneTransform.childCount > 0)
                .Subscribe(_ =>
                {
                    //손에 있는 별사탕을 제거 합니다.
                    Transform targetPoint = _playerView.PlayerHandDummyBoneTransform.GetChild(0);

                    bool isStarCandy = targetPoint.gameObject.layer == LayerMask.NameToLayer("StarCandy");

                    if (isStarCandy)
                        _playerView.SetBehaviourID(1);
                    //열쇠 캔디라면
                    else
                    {
                        //별사탕의 경우
                        bool foundStand = IsBeFoundStand(targetPoint);

                        if (foundStand)
                            _playerView.SetBehaviourID(3);
                        else
                            _playerView.SetBehaviourID(1);
                    }
                })
                .AddTo(this);


            _stateMachineTrigger.OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("PutDownStand"))
                .Subscribe(_ =>
                {
                    //카메라를 고정합니다.
                    _playerView.FreezeRotationCamera(true);

                    //캐릭터를 못움직이도록 막습니다.
                    _playerModel.IsStop = true;

                    // @ 사탕 끼우기
                    Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[17], transform.position);
                })
                .AddTo(this);

            //내려놓을 때 캐릭터를 움직이지 못하게 합니다.
            _stateMachineTrigger.OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("PutDown"))
                .Subscribe(_ =>
                {
                    //손에 있는 별사탕을 제거 합니다.
                    Transform targetPoint = _playerView.PlayerHandDummyBoneTransform.GetChild(0);

                    bool isStarCandy = targetPoint.gameObject.layer == LayerMask.NameToLayer("StarCandy");

                    if (isStarCandy)
                    {
                        StarCandy starCandy = targetPoint.GetComponent<StarCandy>();

                        //손가락 본 트랜스폼
                        Transform handTransform = _playerView.PlayerHandDummyBoneTransform;

                        //손가락 본 위치에 생성시킵니다.
                        _playerView.CreateStarCandyBomb(starCandy.starCandyThrowable, handTransform);

                        //StarCandy AI를 제거합니다.
                        Destroy(targetPoint.gameObject);
                    }

                    //-----------------------------------------------------------
                    //던지는 UI를 제거합니다.
                    _playerView.throwSystem.SetActiveThrow(false);

                    //카메라를 고정합니다.
                    _playerView.FreezeRotationCamera(true);

                    //캐릭터를 못움직이도록 막습니다.
                    _playerModel.IsStop = true;
                })
                .AddTo(this);

            //Catching 상태에서 초기화를 해준다.
            _stateMachineTrigger.OnStateExitAsObservable()
                .Where(info => info.StateInfo.IsName("PutDown") || info.StateInfo.IsName("PutDownStand"))
                .Subscribe(_ =>
                {
                    _hookSystemView.ChangeWeaponActiveType(WeaponType.Normal);
                    _playerView.SetBehaviourID(0);
                    _playerView.ChangeZoomCamera(false, true);
                    _playerView.FreezeRotationCamera(false);

                    //초기화
                    _playerModel.OtherState = EOtherState.Nothing;
                    _playerModel.IsStop = false;
                    _playerModel.TargetStand = null;
                    _playerModel.Key = null;
                    _playerModel.targetInfo.Reset();
                })
                .AddTo(this);

            //애니메이션이 캐치 상태에 들어오면 IsCatch를 true로 바꿔준다.
            _stateMachineTrigger.OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsTag("Catch"))
                .Subscribe(_ =>
                {
                    _hookSystemView.ChangeWeaponActiveType(WeaponType.AllHide); //무기를 제거합니다.

                    _playerView.throwSystem.SetActiveThrow(true);
                })
                .AddTo(this);

            //투척 상태에서 공격키를 누를시 던집니다.
            _playerView.GetKeyDownAttackObservable()
                .Where(_ => _playerModel.OtherState == EOtherState.Catch)
                .Where(_ => IsHandCatchToStarCandy())
                .Subscribe(_ => _playerView.SetBehaviourID(2)).AddTo(this);

            //던지기 오브젝트를 생성합니다.
            _stateMachineTrigger.OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("ObjectThrow"))
                .Subscribe(_ =>
                {
                    _playerModel.IsStop = true;
                    _playerView.FreezeRotationCamera(true);

                    //손에 있는 별사탕을 제거 합니다.
                    Transform targetPoint = _playerView.PlayerHandDummyBoneTransform.GetChild(0);
                    StarCandy starCandy = targetPoint.GetComponent<StarCandy>();

                    //손가락 본 트랜스폼
                    Transform handTransform = _playerView.PlayerHandDummyBoneTransform;

                    //손가락 본 위치에 생성시킵니다.
                    StarCandyBomb starCandyBomb =
                        _playerView.CreateStarCandyBomb(starCandy.starCandyThrowable, handTransform, true);

                    //진짜로 변경함
                    _playerView.throwSystem.throwObject = starCandyBomb.GetThrowable();
                    Destroy(targetPoint.gameObject);
                })
                .AddTo(this);

            //던지기 동작을 끝냈다면
            _stateMachineTrigger.OnStateExitAsObservable()
                .Where(info => info.StateInfo.IsName("ObjectThrow"))
                .Subscribe(_ =>
                {
                    //훅을 일반 상태로 되돌린다.
                    _hookSystemView.ChangeWeaponActiveType(WeaponType.Normal);

                    //애니메이션 행동을 0(일반)으로 되돌린다.
                    _playerView.SetBehaviourID(0);

                    //카메라를 일반 상태로 되돌린다.
                    _playerView.ChangeZoomCamera(false, true);
                    _playerView.FreezeRotationCamera(false);

                    //캐릭터 상태를 일반으로 하고 움직이도록 처리한다.
                    _playerModel.OtherState = EOtherState.Nothing;
                    _playerModel.IsStop = false;
                    _playerModel.targetInfo.Reset();
                })
                .AddTo(this);

            #endregion

            #region 공격

            //공격 애니메이션 재생
            _playerView.GetKeyDownAttackObservable()
                .Where(_ => !_playerModel.IsStop)
                .Where(_ => _playerModel.CurrentControllerState == ControllerState.Grounded)
                .Where(_ => _playerModel.OtherState is EOtherState.Nothing)
                .Subscribe(_ =>
                {
                    bool initFightMode = StartFightMode();

                    if (initFightMode)
                        return;

                    _playerModel.IsStop = true;
                    _playerModel.OtherState = EOtherState.Attack;
                    _playerView.SetBehaviourID(Random.Range(0, 2));
                    _playerView.OnTriggerAnimation(PlayerAnimation.OnAttack);
                    _playerView.FreezeRotationCamera(true);
                })
                .AddTo(this);

            //재공격 애니메이션 예약
            _playerView.GetKeyDownReAttackObservable()
                .Subscribe(_ => _playerView.OnTriggerAnimation(PlayerAnimation.OnReAttack))
                .AddTo(this);

            //공격 상태가 되면 무기 타입을 공격 무기로 전환한다.
            _playerModel.OtherStateObservable.Where(state => state == EOtherState.Attack)
                .Subscribe(_ => _hookSystemView.ChangeWeaponActiveType(WeaponType.Bending)).AddTo(this);

            #endregion

            #region 훅샷 & 당기기

            //파인드로 찾은 녀석들을 모두 표시하여 처리한다.
            this.UpdateAsObservable()
                .Where(_ => _playerModel.OtherState == EOtherState.Nothing)
                .Subscribe(ShowDefaultOccluded)
                .AddTo(this);

            #endregion

            #region 훅샷

            //훅샷 포인트를 찾고, 상태를 반여합니다.
            this.UpdateAsObservable()
                .Where(_ => _playerModel.OtherState != EOtherState.ThrowRope)
                .Where(_ => _playerModel.RopeState == ERopeState.MoveToTarget)
                .Where(_ => _playerModel.OtherState is not (EOtherState.HookShotFlying or EOtherState.HookShotRotate))
                .Subscribe(OnFindHookShotPoint)
                .AddTo(this);

            //훅샷 상태이면 UI를 숨깁니다.
            this.UpdateAsObservable()
                .Where(_ => _playerModel.RopeState == ERopeState.MoveToTarget)
                .Where(_ => _playerModel.OtherState is EOtherState.HookShotFlying or EOtherState.HookShotRotate)
                .Subscribe(_ => _offScreenSystemManager.AllHide())
                .AddTo(this);

            //훅샷을 트리거 합니다.
            _playerView.GetKeyDownThrowingHookObservable()
                .Where(_ => _playerModel.RopeState == ERopeState.MoveToTarget)
                .Where(_ => _playerModel.CurrentControllerState == ControllerState.Grounded)
                .Where(_ => _playerModel.OtherState == EOtherState.Nothing)
                .Subscribe(unit =>
                {
                    bool initFightMode = StartFightMode();

                    if (initFightMode)
                        return;
                    
                    OnChangeStateToHookShotRotate(unit);
                })
                .AddTo(this);

            //점프 훅샷을 트리거 합니다.
            _playerView.GetKeyDownThrowingHookObservable()
                .Where(_ => _playerModel.RopeState == ERopeState.MoveToTarget)
                .Where(_ => _playerModel.CurrentControllerState is ControllerState.Jumping or ControllerState.Falling
                    or ControllerState.Rising)
                .Where(_ => _playerModel.OtherState == EOtherState.Nothing)
                .Subscribe(unit =>
                {
                    bool initFightMode = StartFightMode();

                    if (initFightMode)
                        return;
                    
                    OnChangeStateToHookShotRotate(unit, true);
                })
                .AddTo(this);

            //훅샷을 위해 회전하는 옵저버
            HookShotRotateStateObserver()
                .Subscribe(OnHookShotRotate)
                .AddTo(this);

            //손가락 끈끈이가 등불에 붙었을 때 트리거 된다.
            StopHandJellyObservable()
                .Where(_ => _playerModel.OtherState == EOtherState.HookShotFlying) // 플레이어는 로프를 던지는 상태이며,
                .Subscribe(_ => _playerView.SetBehaviourID(1))
                .AddTo(this);

            //날아가기 상태가 되었을 때 로프 던지기 애니메이션을 실행합니다.
            _playerModel.OtherStateObservable.Where(state => state == EOtherState.HookShotFlying)
                .Subscribe(_ =>
                {
                    Vector3 targetPosition = MoveToTargetPosition;
                    _hookSystemView.LookAt(targetPosition);

                    bool isAir = _playerModel.isLockGravity;
                    _playerView.OnPlayTriggerRope(ERopeState.MoveToTarget, isAir);
                })
                .AddTo(this);

            //훅샷을 시전합니다. (실제로 캐릭터가 훅을 타고 이동하는 것은 여기서 함)
            HookShotAnimationEnterObservable()
                .Where(_ => _playerModel.OtherState == EOtherState.HookShotFlying)
                .Subscribe(_ =>
                {
                    // @로프 던지기
                    Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[7], transform.position);
                    _hookSystemView.ChangeWeaponActiveType(WeaponType.System);

                    OnHookShot();
                })
                .AddTo(this);

            //시스템 로프로 전환합니다.
            _stateMachineTrigger.OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("HookShotStart"))
                .Subscribe(_ => _hookSystemView.ChangeWeaponActiveType(WeaponType.Bending)).AddTo(this);

            //던질 때 방향을 재 계산해줍니다.
            _stateMachineTrigger.OnStateUpdateAsObservable()
                .Where(info => info.StateInfo.IsName("HookShotStarting"))
                .Subscribe(_ => _hookSystemView.RefreshRope(MoveToTargetPosition))
                .AddTo(this);

            //시스템 로프로 전환합니다.
            _stateMachineTrigger.OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("JumpRope01"))
                .Subscribe(_ => _hookSystemView.ChangeWeaponActiveType(WeaponType.Bending)).AddTo(this);

            //타겟을 향해 날아가는 것을 멈추거나,
            //훅에 타겟이 고정되어있고 플레이어가 날아가는 상태
            FlyToTargetObservable()
                .Subscribe(_ =>
                {
                    OnFlyToTarget(); //플레이어가 타겟을 향해 날아가는 시스템 동작
                    OnFlyStop(); //플레이어가 타겟을 향해 날아갈 때 타겟으로부터 특정 거리에서 멈춤
                })
                .AddTo(this);

            #endregion

            #region 로프 취소

            _playerView.GetKeyDownRopeCancelObservable()
                .Where(_ => _hookSystemModel.HookState == EHookState.Forward)
                .Where(_ => _playerModel.OtherState == EOtherState.ThrowRope)
                .Subscribe(_ => RopeCancel())
                .AddTo(this);

            this.UpdateAsObservable()
                .Where(_ => _hookSystemModel.HookState == EHookState.Forward)
                .Where(_ => _playerModel.OtherState == EOtherState.ThrowRope)
                .Where(_ => _hookSystemView.GetEndHandleTransform().localPosition.z <= 0)
                .Subscribe(_ =>
                {
                    MoveToTargetPosition = Vector3.zero;
                    PullPosition = Vector3.zero;
                    _playerModel.IsStop = false;
                    _playerView.SetBehaviourID(2);
                    _playerModel.targetInfo.hasTarget = false;
                    _playerModel.targetInfo.position = Vector3.zero;
                })
                .AddTo(this);

            #endregion

            #region 손가락 끈끈이

            //훅샷 포인트를 찾고, 상태를 반여합니다.
            this.UpdateAsObservable()
                .Where(_ => _playerModel.OtherState != EOtherState.ThrowRope)
                .Where(_ => _playerModel.RopeState == ERopeState.Pull)
                .Subscribe(OnFindCatchObjectPoint)
                .AddTo(this);

            //끈끈이 손가락 특성을 변경합니다.
            _playerView.GetKeyDownChangeHookObservable()
                .Where(_ => !_playerModel.IsStop)
                .Where(_ => _playerModel.OtherState == EOtherState.Nothing)
                .Subscribe(OnChangeRopeState)
                .AddTo(this);

            #region 키 입력 - 로프 스타일 변경

            //당기기 스타일로 변경합니다.
            _playerView.GetKeyDownPullStyleKey()
                .Where(_ => !_playerModel.IsStop)
                .Where(_ => _playerModel.OtherState == EOtherState.Nothing)
                .Subscribe(_ => OnChangeRopeState(ERopeState.Pull))
                .AddTo(this);

            //이동 스타일로 변경합니다.
            _playerView.GetKeyDownMoveToTargetKey()
                .Where(_ => !_playerModel.IsStop)
                .Where(_ => _playerModel.OtherState == EOtherState.Nothing)
                .Subscribe(_ => OnChangeRopeState(ERopeState.MoveToTarget))
                .AddTo(this);

            #endregion

            //훅을 던지도록 플레이어 상태를 변경합니다.
            _playerView.GetKeyDownThrowingHookObservable()
                .Where(_ => _playerModel.CurrentControllerState is not (ControllerState.Jumping
                    or ControllerState.Rising or ControllerState.Falling))
                .Where(_ => _playerModel.OtherState == EOtherState.Nothing)
                .Where(_ => _playerModel.RopeState == ERopeState.Pull)
                .Where(_ => _playerModel.targetInfo.hasTarget)
                .Subscribe(_=>
                {
                    bool initFightMode = StartFightMode();

                    if (initFightMode)
                        return;
                    
                    OnStateChangeThrowRope();
                })
                .AddTo(this);

            //끈끈이 손가락을 던집니다.
            ThrowingAnimationEnterObservable()
                .Merge(HookShotAnimationEnterObservable())
                .Where(_ => _playerModel.targetInfo.hasTarget)
                .Where(_ => _playerModel.OtherState == EOtherState.ThrowRope)
                .Subscribe(OnThrowHook)
                .AddTo(this);

            //시스템 로프로 전환합니다.
            _stateMachineTrigger.OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("Throwing"))
                .Subscribe(_ =>
                {
                    _hookSystemView.RefreshRope(_hookSystemModel.TargetPosition);
                    _hookSystemView.ChangeWeaponActiveType(WeaponType.System);
                })
                .AddTo(this);

            //당기기 처리를 한다.
            _stateMachineTrigger.OnStateUpdateAsObservable()
                .Where(info => info.StateInfo.IsName("PullStart"))
                .Subscribe(_ => _hookSystemView.RefreshRope(_hookSystemModel.TargetPosition))
                .AddTo(this);

            //당기기 처리를 한다.
            _stateMachineTrigger.OnStateUpdateAsObservable()
                .Where(info => info.StateInfo.IsName("RopeFailStart"))
                .Subscribe(_ => _hookSystemView.RefreshRope(_hookSystemModel.TargetPosition))
                .AddTo(this);


            //던질 때 방향을 재 계산해줍니다.
            _stateMachineTrigger.OnStateUpdateAsObservable()
                .Where(info => info.StateInfo.IsName("Throwing"))
                .Where(_ => !_hookSystemView.HasGrabTarget())
                .Subscribe(_ => _hookSystemView.LookAt(PullPosition))
                .AddTo(this);

            //손가락 끈끈이를 회수하는 상황일 경우,
            //어떤 애니메이션을 재생할 것인가?
            StopHandJellyObservable()
                .Where(_ => _playerModel.OtherState == EOtherState.ThrowRope)
                .Subscribe(_ =>
                {
                    if (_playerModel.RopeState == ERopeState.Pull)
                    {
                        _playerModel.UseWeight = false;

                        bool hasGrabTarget = _hookSystemView.HasGrabTarget();

                        if (hasGrabTarget)
                        {
                            StarCandy starCandy = _hookSystemView.GetGrabTarget().GetComponent<StarCandy>();
                            starCandy.OnTriggerState(StarCandy.State.PullCatch);
                            _playerView.SetBehaviourID(1);
                        }
                        else
                            _playerView.SetBehaviourID(3);
                    }
                    else
                        _playerView.SetBehaviourID(1);
                }).AddTo(this);

            //날아가는 동작을 시작할 때
            PullingAnimationEnterObservable()
                .Merge(MoveFlyingAnimationEnterObservable())
                .Subscribe(_ =>
                {
                    if (_playerModel.RopeState == ERopeState.MoveToTarget)
                        _playerView.OnRopeRush();

                    _hookSystemModel.HookState = EHookState.BackOrMoveTarget;

                    // @로프 날아가기
                    Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[9], transform.position);
                })
                .AddTo(this);

            //훅 상태를 끝내는 옵저버
            HookStateExitObservable()
                .Where(_ => _playerModel.RopeState == ERopeState.Pull)
                .Subscribe(_ =>
                {
                    var target = _hookSystemView.GetGrabTarget();

                    if (target)
                    {
                        //스타 캔디 컴포넌트를 가지고 있는지 체크합니다.
                        StarCandy starCandy = target.GetComponent<StarCandy>();

                        //스타 캔디를 손으로 잡습니다.
                        if (starCandy)
                        {
                            _playerModel.IsStop = false;
                            _playerModel.OtherState = EOtherState.Catch;
                            _playerView.SetBehaviourID(4);
                            _playerView.FreezeRotationCamera(false);

                            //규격
                            _playerView.throwSystem.throwObject = starCandy.starCandyThrowable;

                            //사탕을 잡은 상태로 변경
                            starCandy.transform.parent = _playerView.PlayerHandDummyBoneTransform;
                            starCandy.OnTriggerState(StarCandy.State.Catch);
                        }
                        else if (!starCandy)
                        {
                            _playerView.SetBehaviourID(2);
                            _hookSystemView.ChangeWeaponActiveType(WeaponType.Normal);
                        }
                    }
                    else
                    {
                        _playerView.SetBehaviourID(2);
                        _hookSystemView.ChangeWeaponActiveType(WeaponType.Normal);
                    }

                    MoveToTargetPosition = Vector3.zero;
                    PullPosition = Vector3.zero;
                    _playerModel.targetInfo.hasTarget = false;
                    _playerModel.targetInfo.position = Vector3.zero;
                })
                .AddTo(this);

            _stateMachineTrigger.OnStateEnterAsObservable()
                .Where(state => state.StateInfo.IsName("PullFailEnd"))
                .Subscribe(_ =>
                {
                    _hookSystemView.HideSystemRope();
                    _hookSystemView.ChangeWeaponActiveType(WeaponType.Bending);
                })
                .AddTo(this);

            _stateMachineTrigger.OnStateExitAsObservable()
                .Where(state => state.StateInfo.IsName("PullFailEnd"))
                .Subscribe(_ => _hookSystemView.ChangeWeaponActiveType(WeaponType.Normal))
                .AddTo(this);

            //PullEnd로 넘어가서 캐릭터를 움직일 수 있는 상태로 만드는 옵저버
            PullEndAnimationExitObservable()
                .Merge(HookShotEndAnimationExitObservable())
                .Subscribe(_ => OnClearState())
                .AddTo(this);

            #endregion

            #region 애니메이션 업데이트

            //땅에 닿았는지를 애니메이션에 반영합니다.
            UpdateAnimationMove()
                .Subscribe(velocity => _playerView.OnUpdateMoveAnimation(velocity, IsGrounded(), _playerModel.IsStop))
                .AddTo(this);

            //애니메이션 Rig Weight를 업데이트합니다.
            this.UpdateAsObservable()
                .Subscribe(_ => _playerView.SetRigTargetWeight(_playerModel.UseWeight ? 1f : 0f))
                .AddTo(this);

            #endregion

            #region 카메라 모드 카운트

            //파이트 모드가 있으면 공격 모드를 해제하는 카운트 다운을 실행합니다.
            Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Where(_ => _playerModel.OtherState == EOtherState.Nothing)
                .Where(_ => _playerModel.DirectionModeTime > 0)
                .Subscribe(_ => _playerModel.DirectionModeTime -= 1)
                .AddTo(this);

            //공격 모드로 전환되면서 카메라를 전환합니다.
            _playerModel.DirectionModeObservable.Where(time => time > 0).Subscribe(_ =>
            {
                if (_playerView.GetCameraStyle() == ECameraStyle.Velocity)
                    _playerView.SetCameraStyle(ECameraStyle.Direction);
            }).AddTo(this);

            //공격모드가 끝나면 원래대로 되돌립니다.
            _playerModel.DirectionModeObservable
                .Where(time => time == 0)
                .Subscribe(_ => _playerView.SetCameraStyle(ECameraStyle.Velocity))
                .AddTo(this);

            #endregion

            #region 무적모드

            //무적 상태가 바뀌었을 때,
            _playerModel.InvincibleStateObservable
                .Subscribe(state =>
                {
                    if (state == InvincibleState.Invincible)
                        InvincibleTimer().Forget();

                    _playerView.ChangeInvincible(state);
                })
                .AddTo(this);

            //히트 상태가 끝났을 때
            _stateMachineTrigger.OnStateExitAsObservable()
                .Where(stateInfo => stateInfo.StateInfo.IsName("Hit"))
                .Subscribe(_ => _playerModel.IsStop = false)
                .AddTo(this);

            #endregion

            #region 킬 오브젝트

            this.OnTriggerEnterAsObservable()
                .Where(coll => coll.CompareTag("ExplosionRange"))
                .Subscribe(_ => TakeDamageWithKnockBack(1, transform.position, 5))
                .AddTo(this);

            //플레이어를 다치게 하는 오브젝트에 닿았을 때
            this.OnTriggerEnterAsObservable()
                .Where(coll => coll.CompareTag("Bullet"))
                .Subscribe(OnHit)
                .AddTo(this);

            #endregion

            #region 메세지 등록

            Messager.RegisterMessage("PlayerStop", () =>
            {
                Time.timeScale = 0f;
                _playerView.SetActiveCursor(false);
            });

            Messager.RegisterMessage("PlayerMove", () =>
            {
                if (Manager.Get<GameManager>().IsPlayPuzzle)
                {
                    Time.timeScale = 0f;
                    _playerView.SetActiveCursor(false);
                }
                else
                {
                    Time.timeScale = 1f;
                    _playerView.SetActiveCursor(true);
                }
            });

            Messager.RegisterMessage("InitPlayer", InitPlayerState);

            #endregion

            Manager.Get<GameManager>().HPObservable
                .Where(_ => _playerModel.OtherState != EOtherState.Death)
                .Where(hp => hp <= 0)
                .Subscribe(_ =>
                {
                    _collider.enabled = false;
                    _playerModel.IsStop = true;
                    _playerModel.OtherState = EOtherState.Death;
                    _playerView.OnTriggerAnimation(PlayerAnimation.OnDeath);
                    _hookSystemView.ChangeWeaponActiveType(WeaponType.AllHide);

                    //ESC 팝업을 못켜도록 합니다.
                    UIController popViewController = FindObjectOfType<UIController>();
                    popViewController.useEscAfterPopView = false;
                })
                .AddTo(this);
        }

        #region 디버그용 기즈모

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                if (_playerView.IsDebugMode)
                {
                    Vector3 position = transform.position;
                    if (_playerModel.RopeState == ERopeState.MoveToTarget)
                    {
                        Gizmos.color = Color.white;
                        Gizmos.DrawWireSphere(position, _settings.hookShotFindRadius);
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(position, _settings.hookShotPossibleDistance);
                    }
                    else
                    {
                        Gizmos.color = Color.white;
                        Gizmos.DrawWireSphere(position, _settings.pullObjectFindRadius);
                        Gizmos.color = Color.blue;
                        Gizmos.DrawWireSphere(position, _settings.hookLengthMax);
                    }
                }
            }
        }
#endif

        #endregion

        [Button("타격 받기")]
        private void TestTakeHit()
        {
            TakeDamageWithKnockBack(1, transform.position, 5);
        }

        [Button("사망")]
        private void TestDeath()
        {
            Manager.Get<GameManager>().HP = 0;
        }

        private void FixedUpdate()
        {
            ControllerUpdate();
        }

        public void RefreshRope()
        {
            _hookSystemView.RefreshRope(_hookSystemModel.TargetPosition);
        }

        /// <summary>
        /// GFX를 숨깁니다.
        /// </summary>
        /// <param name="active">true이면 보이고, false이면 숨깁니다.</param>
        public void SetActivePlayerGFX(bool active)
        {
            _playerView.SetActiveGFX(active);
            _playerModel.IsStop = !active;
        }

        /// <summary>
        /// 컨트롤러 업데이트
        /// 이 함수는 컨트롤러가 올바르게 작동하기 위해 Fixed 업데이트에서 호출되어야 한다.
        /// </summary>
        private void ControllerUpdate()
        {
            //땅에 닿았는지 체크합니다.
            _mover.CheckForGround();

            //컨트롤러 상태 확인
            _playerModel.CurrentControllerState = DetermineControllerState();

            //'운동량'에 마찰과 중력 적용;
            HandleMomentum();

            //모멘텀 중력,이동 속도 계산;
            Vector3 velocity = Vector3.zero;

            if (_playerModel.CurrentControllerState == ControllerState.Grounded)
                velocity = CalculateMovementVelocity();

            //로컬 모멘텀을 사용하는 경우 모멘텀을 먼저 월드 공간으로 변환
            Vector3 worldMomentum = _momentum;
            if (_settings.useLocalMomentum)
                worldMomentum = _tr.localToWorldMatrix * _momentum;

            //속도에 현재 운동량 추가
            velocity += worldMomentum;

            //낙하 속도의 최대값을 적용합니다.
            if (velocity.y < -20)
                velocity.y = -20;

            //플레이어가 땅에 떨어지거나 경사면에서 미끄러지면 무버의 센서 범위 확장
            //이를 통해 플레이어는 지면 접촉을 잃지 않고 계단과 슬로프를 내려갈 수 있습니다.
            _mover.SetExtendSensorRange(IsGrounded());

            //다음 프레임의 속도 저장
            _savedVelocity = velocity;

            //무버 속도 설정		
            _mover.SetVelocity(velocity);

            //컨트롤러 이동 속도 저장
            _savedMovementVelocity = CalculateMovementVelocity();
        }

        //플레이어가 점프가 가능할 때 동작합니다.
        private void Jumping()
        {
            if (_playerModel.CurrentControllerState == ControllerState.Grounded)
            {
                //Call events;
                OnGroundContactLost();
                OnJumpStart();

                _playerView.OnTriggerAnimation(PlayerAnimation.OnJump, true);
                _playerModel.CurrentControllerState = ControllerState.Jumping;
                _playerModel.IsJumping = true;
            }
        }

        //플레이어에게 더블 점프를 시킬 때 사용합니다.
        private void DoubleJumping()
        {
            SetMomentum(Vector3.zero);
            OnGroundContactLost();
            OnDoubleJumpStart();

            _playerView.OnTriggerAnimation(PlayerAnimation.OnDJump);
            _playerModel.DoubleJumpCount += 1;
            _playerModel.CurrentControllerState = ControllerState.Jumping;
        }

        //이 함수는 플레이어가 점프를 시작할 때 호출됩니다.
        private void OnJumpStart()
        {
            //로컬 모멘텀을 사용하는 경우 모멘텀을 먼저 세계 좌표로 변환
            if (_settings.useLocalMomentum)
                _momentum = _tr.localToWorldMatrix * _momentum;

            //추진력에 점프력 추가
            _momentum += _tr.up * _settings.JumpSpeed;

            //점프 시작 시간 설정
            _currentJumpStartTime = Time.time;

            if (_settings.useLocalMomentum)
                _momentum = _tr.worldToLocalMatrix * _momentum;

            //  @플레이어 점프
            Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[3], transform.position);
        }

        //이 함수는 플레이어가 점프를 시작할 때 호출됩니다.
        private void OnDoubleJumpStart()
        {
            //로컬 모멘텀을 사용하는 경우 모멘텀을 먼저 세계 좌표로 변환
            if (_settings.useLocalMomentum)
                _momentum = _tr.localToWorldMatrix * _momentum;

            //추진력에 점프력 추가
            _momentum += _tr.up * _settings.DoubleJumpSpeed;

            //점프 시작 시간 설정
            _currentJumpStartTime = Time.time;

            if (_settings.useLocalMomentum)
                _momentum = _tr.worldToLocalMatrix * _momentum;

            //  @플레이어 더블 점프
            Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[4], transform.position);
        }

        /// <summary>
        /// 플레이어 입력을 기반으로 이동 방향 계산 및 반환
        /// </summary>
        /// <returns></returns>
        protected virtual Vector3 CalculateMovementDirection()
        {
            CharacterInput inputData = _playerView.GetInput();

            //이 개체에 문자 입력 스크립트가 첨부되지 않은 경우 반환;
            if (!inputData)
                return Vector3.zero;

            Vector3 velocity = Vector3.zero;

            //카메라 변환이 할당되지 않은 경우 캐릭터의 변환 축을 사용하여 이동 방향을 계산합니다.;
            // if (!cameraTransform)
            // {
            //      if (!_playerModel.IsStop)
            //      {
            //          velocity += _tr.right * inputData.axisHorizontal;
            //          velocity += _tr.forward * inputData.axisVertical;
            //      }
            // }
            if (cameraTransform)
            {
                //카메라 변환이 할당된 경우 이동 방향에 대해 할당된 변환의 축을 사용합니다.
                //이동 방향을 투영하여 이동이 지면과 평행을 유지하도록 합니다.


                if (!_playerModel.IsStop)
                {
                    if (_playerModel.OtherState != EOtherState.HookShotFlying &&
                        _playerModel.OtherState != EOtherState.HookShotRotate)
                    {
                        velocity += Vector3.ProjectOnPlane(cameraTransform.right, _tr.up).normalized *
                                    inputData.axisHorizontal;

                        velocity += Vector3.ProjectOnPlane(cameraTransform.forward, _tr.up).normalized *
                                    inputData.axisVertical;
                    }
                }
            }

            //필요한 경우 이동 벡터를 1f의 크기로 고정합니다.
            if (velocity.magnitude > 1f)
                velocity.Normalize();

            return velocity;
        }

        /// <summary>
        /// 플레이어 입력, 컨트롤러 상태, 지상 노멀 [...]을 기반으로 이동 속도를 계산하고 반환합니다.
        /// </summary>
        /// <returns></returns>
        private Vector3 CalculateMovementVelocity()
        {
            //(정규화된) 이동 방향 계산;
            Vector3 velocity = CalculateMovementDirection();

            //(정규화된) 속도에 이동 속도 곱하기;
            velocity *= _settings.MovementSpeed;

            return velocity;
        }

        /// <summary>
        /// 현재 운동량과 컨트롤러가 땅에 닿았는지 여부를 기반으로 현재 컨트롤러 상태를 결정합니다.
        /// </summary>
        /// <returns></returns>
        private ControllerState DetermineControllerState()
        {
            //수직 모멘텀이 위쪽을 가리키는지 확인;
            bool isRising = IsRisingOrFalling() && VectorMath.GetDotProduct(GetMomentum(), _tr.up) > 0f;

            //컨트롤러가 슬라이딩하는지 확인;
            bool isSliding = _mover.IsGrounded() && IsGroundTooSteep();

            //Grounded
            if (_playerModel.CurrentControllerState == ControllerState.Grounded)
            {
                if (isRising)
                {
                    OnGroundContactLost();
                    return ControllerState.Rising;
                }

                if (!_mover.IsGrounded())
                {
                    OnGroundContactLost();
                    return ControllerState.Falling;
                }

                if (isSliding)
                {
                    OnGroundContactLost();
                    return ControllerState.Sliding;
                }

                return ControllerState.Grounded;
            }

            //Falling;
            if (_playerModel.CurrentControllerState == ControllerState.Falling)
            {
                if (isRising)
                {
                    return ControllerState.Rising;
                }

                if (_mover.IsGrounded() && !isSliding)
                {
                    _playerView.OnGroundContactRegained(_momentum);
                    return ControllerState.Grounded;
                }

                if (isSliding)
                {
                    return ControllerState.Sliding;
                }

                return ControllerState.Falling;
            }

            //Sliding;
            if (_playerModel.CurrentControllerState == ControllerState.Sliding)
            {
                if (isRising)
                {
                    OnGroundContactLost();
                    return ControllerState.Rising;
                }

                if (!_mover.IsGrounded())
                {
                    OnGroundContactLost();
                    return ControllerState.Falling;
                }

                if (_mover.IsGrounded() && !isSliding)
                {
                    _playerView.OnGroundContactRegained(_momentum);
                    return ControllerState.Grounded;
                }

                return ControllerState.Sliding;
            }

            //Rising;
            if (_playerModel.CurrentControllerState == ControllerState.Rising)
            {
                if (!isRising)
                {
                    if (_mover.IsGrounded() && !isSliding)
                    {
                        _playerView.OnGroundContactRegained(_momentum);
                        return ControllerState.Grounded;
                    }

                    if (isSliding)
                    {
                        return ControllerState.Sliding;
                    }

                    if (!_mover.IsGrounded())
                    {
                        return ControllerState.Falling;
                    }
                }

                return ControllerState.Rising;
            }

            //Jumping;
            if (_playerModel.CurrentControllerState == ControllerState.Jumping)
            {
                //Check for jump timeout;
                if ((Time.time - _currentJumpStartTime) > _settings.JumpDuration)
                    return ControllerState.Rising;

                //점프 키를 놓았는지 확인;
                if (!_playerView.GetInput().pressJump.Value)
                    return ControllerState.Rising;

                return ControllerState.Jumping;
            }

            return ControllerState.Falling;
        }

        /// <summary>
        /// '마찰'과 '중력'을 기준으로 수직 및 수평 운동량에 마찰을 가합니다.
        /// 공중에서의 움직임 처리
        /// 가파른 경사면에서 미끄러지는 핸들
        /// </summary>
        private void HandleMomentum()
        {
            //로컬 모멘텀을 사용하는 경우 모멘텀을 먼저 세계 좌표로 변환;
            if (_settings.useLocalMomentum)
                _momentum = _tr.localToWorldMatrix * _momentum;

            Vector3 verticalMomentum = Vector3.zero;
            Vector3 horizontalMomentum = Vector3.zero;

            //운동량을 수직 및 수평 구성 요소로 분할;
            if (_momentum != Vector3.zero)
            {
                verticalMomentum = VectorMath.ExtractDotVector(_momentum, _tr.up);
                horizontalMomentum = _momentum - verticalMomentum;
            }

            //수직 운동량에 중력 추가;
            if (_playerModel.OtherState != EOtherState.HookShotFlying)
                verticalMomentum -= _tr.up * (_settings.gravity * Time.deltaTime);

            //컨트롤러가 접지된 경우 아래쪽 힘을 제거하십시오.;
            if (_playerModel.CurrentControllerState == ControllerState.Grounded &&
                VectorMath.GetDotProduct(verticalMomentum, _tr.up) < 0f)
                verticalMomentum = Vector3.zero;

            //공중에서 컨트롤러를 조종하기 위해 운동량을 조작합니다(컨트롤러가 접지되지 않았거나 슬라이딩되지 않은 경우);
            if (!IsGrounded())
            {
                Vector3 movementVelocity = CalculateMovementVelocity();

                //컨트롤러가 다른 곳에서 추가 모멘텀을 받은 경우;
                if (horizontalMomentum.magnitude > _settings.MovementSpeed)
                {
                    //현재 운동량 방향으로 원치 않는 속도 축적 방지
                    if (VectorMath.GetDotProduct(movementVelocity, horizontalMomentum.normalized) > 0f)
                        movementVelocity =
                            VectorMath.RemoveDotVector(movementVelocity, horizontalMomentum.normalized);

                    //컨트롤러에 적용되는 모멘텀에 약간의 '무게'를 추가하기 위해 승수로 공기 제어를 약간 낮춥니다.;
                    const float airControlMultiplier = 0.25f;
                    horizontalMomentum += movementVelocity *
                                          (Time.deltaTime * _settings.airControlRate * airControlMultiplier);
                }
                //컨트롤러가 추가 모멘텀을 받지 못한 경우;
                else
                {
                    //클램프 _속도의 축적을 방지하기 위한 수평 속도
                    horizontalMomentum += movementVelocity * (Time.deltaTime * _settings.airControlRate);
                    horizontalMomentum = Vector3.ClampMagnitude(horizontalMomentum, _settings.MovementSpeed);
                }
            }

            //슬로프에서 조종기
            if (_playerModel.CurrentControllerState == ControllerState.Sliding)
            {
                //기울기에서 멀어지는 벡터 계산
                Vector3 pointDownVector = Vector3.ProjectOnPlane(_mover.GetGroundNormal(), _tr.up).normalized;

                //이동 속도 계산
                Vector3 slopeMovementVelocity = CalculateMovementVelocity();

                //기울기를 가리키는 모든 속도를 제거합니다.
                slopeMovementVelocity = VectorMath.RemoveDotVector(slopeMovementVelocity, pointDownVector);

                //운동량에 이동 속도 추가
                horizontalMomentum += slopeMovementVelocity * Time.fixedDeltaTime;
            }

            //컨트롤러의 접지 여부에 따라 수평 운동량에 마찰을 가합니다.
            if (_playerModel.CurrentControllerState == ControllerState.Grounded)
                horizontalMomentum = VectorMath.IncrementVectorTowardTargetVector(horizontalMomentum,
                    _settings.GroundFriction,
                    Time.deltaTime, Vector3.zero);
            else
                horizontalMomentum = VectorMath.IncrementVectorTowardTargetVector(horizontalMomentum,
                    _settings.AirFriction,
                    Time.deltaTime, Vector3.zero);

            //수평 및 수직 운동량을 다시 추가하십시오.

            if (_playerModel.isLockGravity)
                verticalMomentum = Vector3.zero;

            _momentum = horizontalMomentum + verticalMomentum;

            //슬라이딩에 대한 추가 운동량 계산
            if (_playerModel.CurrentControllerState == ControllerState.Sliding)
            {
                //컨트롤러가 슬로프 아래로 미끄러지는 경우 현재 운동량을 현재 접지 법선에 투영합니다.
                _momentum = Vector3.ProjectOnPlane(_momentum, _mover.GetGroundNormal());

                //슬라이딩할 때 상승하는 모멘텀을 제거하십시오.
                if (VectorMath.GetDotProduct(_momentum, _tr.up) > 0f)
                    _momentum = VectorMath.RemoveDotVector(_momentum, _tr.up);

                //추가 슬라이드 중력 적용
                Vector3 slideDirection = Vector3.ProjectOnPlane(-_tr.up, _mover.GetGroundNormal()).normalized;
                _momentum += slideDirection * (_settings.slideGravity * Time.deltaTime);
            }

            if (_settings.useLocalMomentum)
                _momentum = _tr.worldToLocalMatrix * _momentum;
        }

        /// <summary>
        /// 이 함수는 컨트롤러가 땅과 닿지 않았을 때 호출됩니다.
        /// 즉, 떨어지거나 상승하거나 일반적으로 공중에서 동작합니다.
        /// </summary>
        private void OnGroundContactLost()
        {
            //로컬 모멘텀을 사용하는 경우 모멘텀을 먼저 세계 좌표로 변환
            if (_settings.useLocalMomentum)
                _momentum = _tr.localToWorldMatrix * _momentum;

            //현재 이동 속도 가져오기
            Vector3 velocity = GetMovementVelocity();

            //컨트롤러의 운동량과 현재 이동 속도가 모두 있는지 확인
            if (velocity.sqrMagnitude >= 0f && _momentum.sqrMagnitude > 0f)
            {
                //운동 방향에 추진력 투영
                Vector3 projectedMomentum = Vector3.Project(_momentum, velocity.normalized);

                //운동량과 움직임이 정렬되어 있는지 확인하기 위해 내적 계산
                float dot = VectorMath.GetDotProduct(projectedMomentum.normalized, velocity.normalized);

                //현재 운동량이 이미 이동 속도와 같은 방향을 가리키고 있는 경우,
                //원치 않는 속도 누적을 방지하기 위해 더 많은 운동량을 추가하지 마십시오(또는 이동 속도를 제한)
                if (projectedMomentum.sqrMagnitude >= velocity.sqrMagnitude && dot > 0f)
                    velocity = Vector3.zero;
                else if (dot > 0f)
                    velocity -= projectedMomentum;
            }

            //운동량에 이동 속도 추가;
            _momentum += velocity;

            if (_settings.useLocalMomentum)
                _momentum = _tr.worldToLocalMatrix * _momentum;
        }

        #region Setters

        /// <summary>
        /// 컨트롤러에 추진력 추가
        /// </summary>
        /// <param name="momentum"></param>
        public void AddMomentum(Vector3 momentum)
        {
            if (_settings.useLocalMomentum)
                _momentum = _tr.localToWorldMatrix * _momentum;

            _momentum += momentum;

            if (_settings.useLocalMomentum)
                _momentum = _tr.worldToLocalMatrix * this._momentum;
        }

        /// <summary>
        /// 컨트롤러 모멘텀 직접 설정
        /// </summary>
        /// <param name="newMomentum"></param>
        public void SetMomentum(Vector3 newMomentum)
        {
            if (_settings.useLocalMomentum)
                _momentum = _tr.worldToLocalMatrix * newMomentum;
            else
                _momentum = newMomentum;
        }

        #endregion

        #region Getters

        /// <summary>
        /// 수직 운동량이 작은 임계값을 초과하면 'true'를 반환합니다.
        /// </summary>
        /// <returns></returns>
        private bool IsRisingOrFalling()
        {
            //현재 수직 운동량 계산;
            Vector3 verticalMomentum = VectorMath.ExtractDotVector(GetMomentum(), _tr.up);

            //확인할 임계값 설정;
            //대부분의 애플리케이션에서 '0.001f' 값을 권장합니다.;
            const float limit = 0.001f;

            //수직 모멘텀이 '_limit'보다 크면 true를 반환합니다.;
            return verticalMomentum.magnitude > limit;
        }

        /// <summary>
        /// 컨트롤러와 그라운드 노멀 사이의 각도가 너무 크면(> 기울기 한계), 즉 지면이 너무 가파르면 true를 반환합니다.
        /// </summary>
        /// <returns></returns>
        private bool IsGroundTooSteep()
        {
            if (!_mover.IsGrounded())
                return true;

            return (Vector3.Angle(_mover.GetGroundNormal(), _tr.up) > _settings.slopeLimit);
        }

        /// <summary>
        /// 마지막 프레임의 속도 가져오기
        /// </summary>
        /// <returns></returns>
        public Vector3 GetVelocity() => _savedVelocity;

        /// <summary>
        /// 마지막 프레임의 이동 속도 가져오기(운동량은 무시됨)
        /// </summary>
        /// <returns></returns>
        public Vector3 GetMovementVelocity() => _savedMovementVelocity;

        /// <summary>
        /// 현재 모멘텀 반환
        /// </summary>
        /// <returns></returns>
        private Vector3 GetMomentum()
        {
            Vector3 worldMomentum = _momentum;
            if (_settings.useLocalMomentum)
                worldMomentum = _tr.localToWorldMatrix * _momentum;

            return worldMomentum;
        }

        /// <summary>
        /// 땅에 닿은 경우(또는 슬로프 아래로 미끄러지는 경우) 'true'를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public bool IsGrounded() =>
            _playerModel.CurrentControllerState is ControllerState.Grounded or ControllerState.Sliding;

        /// <summary>
        /// 컨트롤러가 슬라이딩하는 경우 'true'를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public bool IsSliding() => _playerModel.CurrentControllerState == ControllerState.Sliding;

        #endregion
    }
}