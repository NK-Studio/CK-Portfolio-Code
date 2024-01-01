using System;
using System.Collections.Generic;
using Character.Core;
using Character.Input;
using Character.Model;
using Character.TurnRotation;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Dummy.Scripts;
using Effect;
using EnumData;
using Micosmo.SensorToolkit;
using Settings;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Utility;
using DG.Tweening;
using Doozy.Runtime.Signals;
using Enemy.Behavior.Boss;
using FMODPlus;
using MagicaCloth2;
using Managers;
using ManagerX;
using NKStudio;
using Settings.Player;
using Tutorial.Helper;
using UI;
using UnityEngine.Events;
using Logger = NKStudio.Logger;
using TimeSpan = System.TimeSpan;

namespace Character.View
{
    public class PlayerView : MonoBehaviour
    {
        [FoldoutGroup("플레이어 애니메이터", true), SerializeField]
        private Animator _animator;

        [FoldoutGroup("플레이어 애니메이터", true), SerializeField]
        private List<MagicaCloth> _cloths;

        [FoldoutGroup("플레이어 애니메이터", true)]
        public int BaseLayerIndex = 0;

        [FoldoutGroup("플레이어 애니메이터", true)]
        public int UpperLayerIndex = 1;

        [field: SerializeField, FoldoutGroup("슈팅", true)]
        public RangeSensor CompensateRange { get; private set; }

        [field: SerializeField, FoldoutGroup("슈팅", true)]
        public Transform ShootPosition { get; private set; }

        [field: SerializeField, FoldoutGroup("슈팅", true)]
        public UnityEvent OnShootEvent { get; private set; }

        [field: SerializeField, FoldoutGroup("해머", true)]
        public RangeSensor HammerRange { get; private set; }

        [field: SerializeField, FoldoutGroup("해머", true)]
        public SectorRangeSensorFilter HammerFarRange { get; private set; }

        [field: SerializeField, FoldoutGroup("해머", true)]
        public RangeSensor HammerDashStopRange { get; private set; }
        [field: SerializeField, FoldoutGroup("해머", true)]
        public DirectionIndicator HammerDashIndicator { get; private set; }

        [field: SerializeField, FoldoutGroup("해머", true)]
        public Transform HammerDummy { get; private set; }

        [field: SerializeField, FoldoutGroup("상호작용", true)]
        public Transform ItemChangeEffectPosition { get; private set; }
        
        public RadialBlurController RadialBlur { get; private set; }
        public ChromaticAberrationController ChromaticAberration { get; private set; }

        #region 카메라 설정
        [field: SerializeField, FoldoutGroup("카메라 설정", true)]
        public CinemachineVirtualCamera VirtualCamera { get; private set; }
        public Cinemachine3rdPersonFollow VirtualCameraPersonFollow { get; private set; }

        [FoldoutGroup("카메라 설정", true)]
        [SerializeField]
        private CinemachineImpulseSource _impulseListener;
        #endregion

        #region GFX

        [SerializeField, FoldoutGroup("GFX")]
        private Renderer[] _renderers;

        #endregion
        
        #region 점멸 관련 설정
        [FoldoutGroup("점멸 관련 설정", true)]
        public Transform OldCameraRoot;
        [FoldoutGroup("점멸 관련 설정", true)]
        public CinemachineVirtualCamera OldPlayerFollowCamera;
        public Cinemachine3rdPersonFollow OldVirtualCameraPersonFollow { get; private set; }

        [FoldoutGroup("점멸 관련 설정", true), Tooltip("지형을 감지할 Front Ray 센서입니다.")]
        public RaySensor ForwardRaySensor;

        [FoldoutGroup("점멸 관련 설정", true), Tooltip("지형을 감지할 Ground Ray 센서입니다.")]
        public RaySensor DownRaySensor;
        #endregion

        #region 슬라이드 설정
        [field: SerializeField]
        [field: FoldoutGroup("슬라이드 설정", true)]
        public CinemachineVirtualCamera SlideFollowCamera { get; private set; }
        #endregion

        #region 이동 설정
        [FoldoutGroup("이동 설정", true)]
        public TurnTowardControllerNavMeshAgent TurnTowardController;

        [field: SerializeField, FoldoutGroup("이동 설정", true)]
        public NavMeshAgent NavMeshAgent { get; private set; }

        [FoldoutGroup("이동 설정", true)] public DestinationVisualizer DestinationVisualizer { get; private set; }

        [FoldoutGroup("이동 설정", true), Tooltip("사용자가 마우스 버튼을 누르고 있으면서 컨트롤러를 계속 움직일 수 있는지 여부")]
        public bool HoldMouseButtonToMove;
        #endregion

        #region 피격
        [SerializeField, FoldoutGroup("피격", true)]
        private SkinnedMeshRenderer _hitTintTargetMeshRenderer;
        private Material[] _hitMaterials;
        private Sequence _hitTintTween;

        [SerializeField, FoldoutGroup("피격", true)]
        public Collider[] Colliders;
        
        [field: SerializeField, FoldoutGroup("피격", true)]
        public FallChecker FallChecker { get; private set; }
        
        private static readonly int HitColor = Shader.PropertyToID("_HitColor");
        private static readonly string HitThreshold = "_HitThreshold";

        /// <summary>
        /// 플레이어의 Collider 활성화 여부를 설정합니다. 관통 무적 등의 용도에 사용됩니다.
        /// </summary>
        public bool ColliderEnabled
        {
            get => Colliders.Length <= 0 || Colliders[0].enabled;
            set
            {
                foreach(var c in Colliders)
                {
                    c.enabled = value;
                }
            }
        }
        
        #endregion

        [FoldoutGroup("기타 설정", true)]
        public Transform CenterPoint;

        [FoldoutGroup("기타 설정", true)]
        public DialogEventCaller IdleDialogEvent;

        [FoldoutGroup("상호작용", true)]
        public RangeSensor InteractionRangeSensor;


        #region 스킬 설정
        [field: SerializeField, FoldoutGroup("스킬 설정", true)]
        public List<DecalHandler> TimeCutterDecals { get; private set; }

        [field: SerializeField, FoldoutGroup("스킬 설정", true)]
        public RangeSensor TimeCutterRange { get; private set; }


        [FoldoutGroup("스킬 설정", true), SerializeField]
        private string _circleAttackLightTargetTag;

        private List<Light> _lights = new();
        private List<float> _lightIntensities = new();
        [FoldoutGroup("스킬 설정", true)]
        public float CircleAttackTargetLightIntensity = 0f;

        [FoldoutGroup("스킬 설정", true)]
        public AnimationCurve CircleAttackStartLightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [FoldoutGroup("스킬 설정", true)]
        public AnimationCurve CircleAttackEndLightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [FoldoutGroup("스킬 설정", true)]
        public GameObject CircleAttackCameraObject;
        #endregion

        /// <summary>
        /// 현재 공격중인 공격 범위
        /// </summary>
        public EffectRange AttackRange { get; private set; }

        public PlayerState AttackType { get; set; }

        public ParticleSystemRoot CurrentEffect { get; private set; }

        public Rigidbody Rigidbody => _rigidbody;
        private Rigidbody _rigidbody;
        private CharacterInput _characterInput;
        private Camera _camera;
        private CinemachineBrain _cinemachineBrain;

        private HitEffectController _hitEffectController;

        public HitEffectController HitEffectController
        {
            get
            {
                if (_hitEffectController == null)
                    _hitEffectController = FindAnyObjectByType<HitEffectController>();

                return _hitEffectController;
            }
            set => _hitEffectController = value;
        }

        #region Shader
        private static readonly int PlayerPosition = Shader.PropertyToID("_Player");
        #endregion

        private void Awake()
        {
            _characterInput = GetComponent<CharacterInput>();
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
            VirtualCameraPersonFollow = VirtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            if (OldPlayerFollowCamera)
            {
                // Debug.Log($"{OldPlayerFollowCamera}");
                OldPlayerFollowCamera.gameObject.SetActive(true);
                OldVirtualCameraPersonFollow = OldPlayerFollowCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
                // Debug.Log($"{OldPersonFollowCameraPersonFollow}");
                OldPlayerFollowCamera.gameObject.SetActive(false);
            }

            DestinationVisualizer = FindAnyObjectByType<DestinationVisualizer>();
            _hitTintTargetMeshRenderer ??= _animator.transform.GetChild(1).GetComponent<SkinnedMeshRenderer>();

            int count = _hitTintTargetMeshRenderer.sharedMaterials.Length;
            _hitMaterials = new Material[count];

            // 매쉬 렌더러에 있는 머티리얼을 인스턴스화하고, 인스턴스된 머티리얼을 매쉬 렌더러에 적용
            for (int i = 0; i < count; i++)
                _hitMaterials[i] = Instantiate(_hitTintTargetMeshRenderer.sharedMaterials[i]);

            if (_hitMaterials.Length > 0)
                _hitTintTargetMeshRenderer.sharedMaterials = _hitMaterials;

            _camera = Camera.main;
            _cinemachineBrain = _camera.GetComponent<CinemachineBrain>();

            //회전을 제어하지 않습니다.
            NavMeshAgent.updateRotation = false;
            NavMeshAgent.acceleration = 100;
        }

        private void Start()
        {
            _lights.Clear();
            _lightIntensities.Clear();
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.CompareTag(_circleAttackLightTargetTag))
                {
                    _lights.Add(l);
                    _lightIntensities.Add(l.intensity);
                }
            }

            // 더 이상.. 라이트 가지고 장난질 안칠거 같으니 ㅎㅎ
            // if (_lights.Count <= 0)
            //     DebugX.LogWarning("PlayerView에서 Light를 찾지 못했습니다.", this);

            if (TimeCutterDecals.Count < CharacterSettings.TimeCutterSkillSettings.MaximumCount)
                DebugX.LogWarning("TimeCutterDecal이 충분하지 않습니다", this);

            foreach (var decal in TimeCutterDecals)
            {
                decal.gameObject.SetActive(false);
            }

            RadialBlur = FindAnyObjectByType<RadialBlurController>();
            ChromaticAberration = FindAnyObjectByType<ChromaticAberrationController>();
            
            if (_cinemachineBrain)
            {
                CinemachineIgnoreTimescale = true;
            }
            CinemachineImpulseManager.Instance.IgnoreTimeScale = true;
        }

        /// <summary>
        /// Unity Event 전용 MoveTo
        /// </summary>
        /// <param name="target"></param>
        public void MoveTo(Transform target)
        {
            NavMeshAgent.Warp(target.position);
        }

        #region 회전
        /// <summary>
        /// 플레이어 기준 마우스 방향 / 게임패드 스틱 방향을 구합니다.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetMouseOrGamepadStickDirection()
        {
            var controllerType = GetInput().GetControllerType();
            Vector3 direction = Vector3.zero;

            switch (controllerType)
            {
                case ControllerType.KeyboardMouse:
                case ControllerType.KeyboardWASD:
                    direction = GetMouseDirectionFromPlayer();
                    break;
                case ControllerType.Gamepad:
                    direction = CalculateInputDirectionOrCameraForward();
                    break;
            }

            return direction;
        }

        /// <summary>
        /// 캐릭터의 방향을 마우스 방향 / 게임패드 스틱 방향으로 설정합니다.
        /// </summary>
        public void ChangeDirectionToMouseOrGamepadStick()
        {
            Vector3 direction = GetMouseOrGamepadStickDirection();
            TurnTowardController.SetRotation(direction);
        }

        /// <summary>
        /// 플레이어 기준으로 마우스 방향을 반환합니다.
        /// </summary>
        /// <returns></returns>
        private Vector3 GetMouseDirectionFromPlayer()
        {
            /*
            if (!_characterInput.MoveAxis.Value.IsZero())
                return _characterInput.MoveAxis.Value.ToVectorXZ().normalized;
            */

            Vector3 mousePosition = Mouse.current.position.ReadValue();

            // 마우스로 Ray 정의
            Ray mouseRay = _camera.ScreenPointToRay(mousePosition);

            var playerPosition = transform.position;

            // 플레이어가 서 있는 가상의 plane 정의
            var plane = new Plane(Vector3.up, playerPosition);

            // ray와 plane 교차 검사
            if (!plane.Raycast(mouseRay, out var t))
            {
                return Vector3.zero;
            }

            // 교차점
            var crossPoint = mouseRay.GetPoint(t);

            // 클릭 위치와 캐릭터 위치 간의 벡터 계산
            Vector3 direction = crossPoint - playerPosition;
            direction.y = 0f;

            return direction;
        }

        /// <summary>
        /// 입력이 있으면, 카메라 기준 키보드 입력 방향을 반환하고, 입력이 없으면 카메라 정면 방향을 반환한다.
        /// </summary>
        /// <returns></returns>
        private Vector3 CalculateInputDirectionOrCameraForward()
        {
            //무브에 대한 Axis를 가져옵니다.
            Vector2 axis = GetInput().AimAxis.Value;

            //키 입력에 대한 방향 포맷팅
            Vector3 formatKeyDir = new(axis.x, 0, axis.y);

            //카메라 기준으로 텔레포트할 방향을 정합니다.
            //카메라의 rotation.x 를 0으로 변경합니다.
            Quaternion cameraRotation = _camera.transform.rotation;
            Vector3 cameraRotationEuler = cameraRotation.eulerAngles;
            cameraRotationEuler.x = 0;

            //카메라 기준으로 키 입력 방향 리턴
            Vector3 direction = Quaternion.Euler(cameraRotationEuler)*formatKeyDir;

            //키 입력이 따로 없었다면 정면으로 보겠금 처리한다.
            if (formatKeyDir == Vector3.zero)
                direction = TurnTowardController.GetForward();

            return direction;
        }
        #endregion

        #region Attack
        public void OnShootBullet(PlayerBulletSettings settings)
        {
            OnShootEvent?.Invoke();
            if (settings.MuzzleFlashType != EffectType.None)
            {
                var effect = EffectManager.Instance.Get(settings.MuzzleFlashType);
                effect.transform.SetPositionAndRotation(
                    ShootPosition.position,
                    Quaternion.LookRotation(TurnTowardController.GetForward())*effect.transform.rotation
                );
            }
        }

        public void SetRadialBlurIntensity(float intensity)
        {
            if (!RadialBlur)
            {
                Logger.LogWarning("Radial Blur Controller가 씬에 없음 !!!", gameObject);
                return;
            }
            RadialBlur.SetIntensity(intensity);
        }

        public void SetChromaticAberrationIntensity(float intensity)
        {
            if (!ChromaticAberration)
            {
                return;
            }
            ChromaticAberration.SetIntensity(intensity);
        }
        
        [Button]
        public void SetFullScreenFillerAlpha(float value)
        {
            Shader.SetGlobalFloat(FsfAlpha, value);
        }

        public bool OnExitSkillPrepareState(PlayerModel model)
        {
            var skill = model.OtherState;
            switch (skill)
            {
                case PlayerState.HammerPrepare or PlayerState.Hammer:
                {
                    model.ClearMovementSpeedModifier();
                    model.IsInputDisabled = false;
                    return true;
                }
            }

            return true;
        }

        /// <summary>
        /// 공격 이펙트 - AnimationEventHandle
        /// </summary>
        private HashSet<FakeChild> _fakeChildren = new();

        public void OnAttackSplash(int index)
        {
            GameObject effect = index switch {
                0 => EffectManager.Instance.Get(EffectType.PlayerAttackSplash01),
                1 => EffectManager.Instance.Get(EffectType.PlayerAttackSplash02),
                2 => EffectManager.Instance.Get(EffectType.PlayerAttackSplash03),
                3 => EffectManager.Instance.Get(EffectType.PlayerAttackSplash03),
                // 4 => _container.ResolveId<GameObject>(EffectType.AttackSplash5),
                _ => null
            };

            if (!effect)
            {
                DebugX.Log("OnAttackSplash함수에 알맞지 않은 Index값이 넘어왔습니다.");
                return;
            }

            if (Settings.Sounds.TryGetValue("AttackSound", out var audioPath))
                AutoManager.Get<AudioManager>().PlayOneShot(audioPath, "PlayerAttackType", index,
                    _camera.transform.position);

            Transform modelRoot = TurnTowardController.transform;
            /*
            Vector3 position = index switch
            {
                0 => Vector3.zero,
                1 => new Vector3(-0.503f, 2.634f, 0.347f),
                2 => Vector3.zero,
                3 => new Vector3(-0.22f, 2.38f, 0.477f),
                4 => new Vector3(-0.22f, 2.224f, 0.063f),
                _ => Vector3.zero
            };
            */
            // 로컬 공간에 위치한 이펙트 위치 및 회전을 ModelRoot 기준으로 변환
            effect.transform.SetPositionAndRotation(
                modelRoot.TransformPoint(effect.transform.position),
                modelRoot.rotation*effect.transform.rotation
            );

            if (effect.TryGetComponent(out FakeChild fakeChild))
            {
                fakeChild.FollowMode = FakeChild.Mode.Position;
                fakeChild.TargetParent = modelRoot;
                _fakeChildren.Add(fakeChild);
                DetachAttackSplashSequence(fakeChild).Forget();
            }

            // Vector3 position = effectObject.transform.position;
            // Quaternion rotation = effectObject.transform.rotation;
            // GameObject effect = Instantiate(effectObject, modelRoot.TransformPoint(position), modelRoot.rotation * rotation);
            // effect.transform.localPosition = position;
            // effect.transform.localRotation = rotation;

            //공격범위 적용
            AttackRange = effect.GetComponent<EffectRange>();
            AttackType = index switch {
                _ => PlayerState.None
            };
            // 평타는 하나만
        }

        // AttackMove에 해당하는 시간동안만 따라다니고, 이후로는 부착해제
        private async UniTaskVoid DetachAttackSplashSequence(FakeChild fakeChild)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
            if (_fakeChildren.Remove(fakeChild))
            {
                fakeChild.TargetParent = null;
            }
        }

        public void DisconnectAttackEffectFromModelRoot()
        {
            foreach (var fakeChild in _fakeChildren)
            {
                fakeChild.TargetParent = null;
            }

            _fakeChildren.Clear();
        }

        public void OnTriggerSkill(string skillName) => OnTriggerSkill(skillName, null);

        public readonly Subject<Unit> OnIceSpikeShoot = new();
        public void OnTriggerSkill(string skillName, Action<GameObject> effectConsumer)
        {
            GameObject effect;
            Transform modelRoot = TurnTowardController.transform; //모델 루트(자식에 이펙트 넣는 용도 사용)
            PlayerState state;

            switch (skillName)
            {
                default:
                    state = PlayerState.None;
                    effect = null;
                    break;
            }

            AttackType = state;
            if (effect)
            {
                AttackRange = effect.GetComponent<EffectRange>();
                effectConsumer?.Invoke(effect);

                CurrentEffect = effect.GetComponent<ParticleSystemRoot>();
            }
        }

        private float _pushTime = 0f;
        private Vector3 _pushForce;

        public bool IsPushing => _pushTime > 0f;

        /// <summary>
        /// NavMeshAgent에게 일정 시간동안 velocity를 강제로 설정합니다.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="mode"></param>
        /// <param name="duration"></param>
        public void Push(Vector3 force, ForceMode mode, float duration)
        {
            if (IsAnimatorDead()) return;

            NavMeshAgent.isStopped = true;
            NavMeshAgent.ResetPath();

            _pushForce = force;
            NavMeshAgent.velocity = _pushForce;
            _pushTime = duration;
        }

        /// <summary>
        /// 넉백 업데이트
        /// </summary>
        /// <param name="_"></param>
        public void OnUpdatePush(Unit _)
        {
            if (!IsPushing)
            {
                return;
            }

            _pushTime -= Time.deltaTime;
            if (_pushTime <= 0f)
            {
                NavMeshAgent.velocity = Vector3.zero;
                NavMeshAgent.updatePosition = true;
            }
            else
            {
                NavMeshAgent.velocity = _pushForce;
            }
        }

        /// <summary>
        /// AddForce와 같은 처리로 공격시 캐릭터를 이동 시킵니다.
        /// AnimationEventHandle에서 호출
        /// </summary>
        /// <param name="moveDistance"></param>
        public void MoveAttack(float moveDistance)
        {
            Push(TurnTowardController.GetForward()*moveDistance, ForceMode.Impulse, 0.1f);
        }

        // 넉백 효과 감소를 위한 코루틴
        private async UniTaskVoid DisableKnockback(float time)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(time), cancellationToken: this.GetCancellationTokenOnDestroy());

            _rigidbody.velocity = Vector3.zero; // 넉백에 의한 속도 초기화
            _rigidbody.angularVelocity = Vector3.zero; // 넉백에 의한 회전 초기화
        }
        #endregion

        #region 상호작용

        public void PlayItemChangeEffect()
        {
            var effect = EffectManager.Instance.Get(EffectType.PlayerItemChange);
            if (effect.TryGetComponent(out FakeChild f))
            {
                f.TargetParent = ItemChangeEffectPosition;
            }
        }

        #endregion
        
        #region Get
        /// <summary>
        /// 현재 애니메이터를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Animator CurrentAnimator()
        {
            return _animator;
        }

        public Animator Animator => _animator;

        /// <summary>
        /// 입력처리를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public CharacterInput GetInput()
        {
            return _characterInput;
        }

        /// <summary>
        /// 세팅파일을 반환합니다.
        /// </summary>
        public CharacterSettings Settings => GameManager.Instance.Settings;

        /// <summary>
        /// 스크린 스페이스 - 마우스 위치를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetMousePosition()
        {
            return Mouse.current.position.ReadValue();
        }

        /// <summary>
        /// 콘트롤러 타입을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public ControllerType GetControllerType()
        {
            return _characterInput.GetControllerType();
        }

        public bool IsAnimatorDead() => _animator.GetCurrentAnimatorStateInfo(0).IsName("Dead");
        #endregion

        #region Set
        [SerializeField]
        private bool _isActiveGFX = true;

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
                foreach (var r in _renderers)
                {
                    if(r) r.enabled = value;
                }
            }
        }

        /// <summary>
        /// AnimationEventHandle에서 호출
        /// CircleAttackCamera를 활성화/비활성화 합니다.
        /// </summary>
        /// <param name="active"></param>
        public void SetActiveCircleAttackCamera(bool active)
        {
            CircleAttackCameraObject.SetActive(active);
        }

        public void SetTimeLightSetting(bool active) => TimeLightSequence(active).Forget();

        private async UniTaskVoid TimeLightSequence(bool active)
        {
            AnimationCurve curve = active ? CircleAttackStartLightCurve : CircleAttackEndLightCurve;
            var length = curve.GetLength();
            var t = 0f;
            while (t < length)
            {
                for (int i = 0; i < _lights.Count; i++)
                {
                    float start = active ? _lightIntensities[i] : CircleAttackTargetLightIntensity;
                    float end = active ? CircleAttackTargetLightIntensity : _lightIntensities[i];
                    _lights[i].intensity = Mathf.Lerp(start, end, curve.Evaluate(t));
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
                t += Time.unscaledDeltaTime;
            }

            for (int i = 0; i < _lights.Count; i++)
            {
                float end = active ? CircleAttackTargetLightIntensity : _lightIntensities[i];
                _lights[i].intensity = end;
            }
        }

        public void UpdateDestination(Vector3 targetPosition)
        {
            NavMeshAgent.SetDestination(targetPosition);
        }
        #endregion

        #region Animation
        private static readonly int Forward = Animator.StringToHash("Forward");
        private static readonly int Right = Animator.StringToHash("Right");
        private static readonly int Behaviour = Animator.StringToHash("Behaviour");
        private static readonly int OnAttack = Animator.StringToHash("OnAttack");
        private static readonly int OnFlash = Animator.StringToHash("OnDash");
        private static readonly int OnShoot = Animator.StringToHash("OnShoot");
        private static readonly int OnShoot_Upper = Animator.StringToHash("OnShoot_Upper");
        private static readonly int OnShootEnd = Animator.StringToHash("OnShootEnd");
        private static readonly int OnShootEnd_Upper = Animator.StringToHash("OnShootEnd_Upper");
        private static readonly int OnReload = Animator.StringToHash("OnReload");
        private static readonly int OnItemChange = Animator.StringToHash("OnItemChange");
        private static readonly int OnItemChange_Upper = Animator.StringToHash("OnItemChange_Upper");
        private static readonly int OnReload_Upper = Animator.StringToHash("OnReload_Upper");
        private static readonly int OnReloadEnd = Animator.StringToHash("OnReloadEnd");
        private static readonly int OnReloadEnd_Upper = Animator.StringToHash("OnReloadEnd_Upper");
        private static readonly int OnEndAnimation = Animator.StringToHash("OnEndAnimation");
        private static readonly int OnHammerPrepare = Animator.StringToHash("OnHammerPrepare");
        private static readonly int OnHammerDash = Animator.StringToHash("OnHammerDash");
        private static readonly int OnHammer = Animator.StringToHash("OnHammerUse");
        private static readonly int OnHammerCancel = Animator.StringToHash("OnHammerCancel");
        private static readonly int IsDashing = Animator.StringToHash("IsDashing");
        private static readonly int OnSkill = Animator.StringToHash("OnSkill");
        private static readonly int SkillRepeat = Animator.StringToHash("SkillRepeat");
        private static readonly int OnSlide = Animator.StringToHash("OnSlide");
        private static readonly int OnDead = Animator.StringToHash("OnDead");
        private static readonly int OnFall = Animator.StringToHash("OnFall");
        private static readonly int OnStun = Animator.StringToHash("OnStun");
        private static readonly int OnKnockBack = Animator.StringToHash("OnKnockBack");
        private static readonly int FsfAlpha = Shader.PropertyToID("_FSF_Alpha");
        private static readonly int IsMoving = Animator.StringToHash("IsMoving");
  
        
        private Vector2 _currentMove = Vector2.zero;

        /// <summary>
        /// 이동 애니메이션 업데이트
        /// </summary>
        /// <param name="isStop"></param>
        /// <param name="move"></param>
        public void OnUpdateMoveAnimation(bool isStop, Vector2 move)
        {
            // 사망 상태에 들어가면 추가 Trigger 발동하지 않음
            if (IsAnimatorDead())
            {
                return;
            }

            if ((_currentMove - move).sqrMagnitude <= 0.1f)
            {
                _currentMove = move;
            }
            else
            {
                _currentMove = Vector2.Lerp(_currentMove, move, Settings.BlendTreeLerpSpeed*Time.deltaTime);
            }
            _animator.SetFloat(Forward, _currentMove.y);
            _animator.SetFloat(Right, _currentMove.x);
            _animator.SetBool(IsMoving, !isStop);
        }

        /// <summary>
        /// 애니메이션 bool 파라미터를 갱신합니다.
        /// </summary>
        /// <param name="animationID"></param>
        /// <param name="active"></param>
        public void OnTriggerAnimation(PlayerAnimation animationID, bool active)
        {
            // 사망 상태에 들어가면 추가 Trigger 발동하지 않음
            if (IsAnimatorDead())
            {
                return;
            }

            int id = 0;

            switch (animationID)
            {
                case PlayerAnimation.Flash:
                    id = IsDashing;
                    break;
                default:
                    Debug.LogError("사용되지 않는 애니메이션 ID");
                    break;
            }

            _animator.SetBool(id, active);
        }

        /// <summary>
        /// 애니메이션 Int 파라미터를 갱신합니다.
        /// </summary>
        /// <param name="animationID"></param>
        /// <param name="index"></param>
        public void OnTriggerAnimation(PlayerAnimation animationID, int index)
        {
            // 사망 상태에 들어가면 추가 Trigger 발동하지 않음
            if (IsAnimatorDead())
            {
                return;
            }

            int id = 0;

            switch (animationID)
            {
                case PlayerAnimation.Behaviour:
                    id = Behaviour;
                    break;
                default:
                    Debug.LogError("사용되지 않는 애니메이션 ID");
                    break;
            }

            _animator.SetInteger(id, index);
        }

        public void ResetTrigger(PlayerAnimation animationID)
        {
            int id;
            switch (animationID)
            {
                case PlayerAnimation.Shoot:
                    Animator.ResetTrigger(OnShoot);
                    Animator.ResetTrigger(OnShoot_Upper);
                    return;
                case PlayerAnimation.Reload:
                    Animator.ResetTrigger(OnReload);
                    Animator.ResetTrigger(OnReload_Upper);
                    return;
                case PlayerAnimation.Idle:
                    id = OnEndAnimation;
                    break;
                default:
                    Debug.LogError("사용되지 않는 애니메이션 ID");
                    return;
            }
            Animator.ResetTrigger(id);
        }
        /// <summary>
        /// 애니메이션 파라미터를 트리거합니다.
        /// </summary>
        /// <param name="animationID"></param>
        public void OnTriggerAnimation(PlayerAnimation animationID)
        {
            // 사망 상태에 들어가면 추가 Trigger 발동하지 않음
            if (IsAnimatorDead())
            {
                return;
            }

            int id = 0;

            switch (animationID)
            {
                case PlayerAnimation.Shoot:
                    id = OnShoot;
                    Animator.SetTrigger(OnShoot_Upper);
                    Animator.ResetTrigger(OnShootEnd);
                    break;
                case PlayerAnimation.ShootEnd:
                    id = OnShootEnd;
                    Animator.SetTrigger(OnShootEnd_Upper);
                    break;
                case PlayerAnimation.Reload:
                    id = OnReload;
                    Animator.SetTrigger(OnReload_Upper);
                    Animator.ResetTrigger(OnReloadEnd);
                    break;
                case PlayerAnimation.ReloadEnd:
                    id = OnReloadEnd;
                    Animator.SetTrigger(OnReloadEnd_Upper);
                    break;
                case PlayerAnimation.Idle:
                    Debug.Log("<color=yellow>OnEndAnimation Triggered</color>");
                    id = OnEndAnimation;
                    break;
                case PlayerAnimation.ItemChange:
                    id = OnItemChange;
                    break;
                case PlayerAnimation.ItemChange_Upper:
                    id = OnItemChange_Upper;
                    break;
                case PlayerAnimation.HammerPrepare:
                    id = OnHammerPrepare;
                    break;
                case PlayerAnimation.HammerDash:
                    id = OnHammerDash;
                    break;
                case PlayerAnimation.Hammer:
                    id = OnHammer;
                    break;
                case PlayerAnimation.HammerCancel:
                    id = OnHammerCancel;
                    break;
                case PlayerAnimation.Flash:
                    id = OnFlash;
                    break;
                case PlayerAnimation.Attack:
                    id = OnAttack;
                    break;
                case PlayerAnimation.FlashAttack:
                    id = OnSkill;
                    _animator.SetInteger(Behaviour, 0);
                    break;
                case PlayerAnimation.ZSlash:
                    id = OnSkill;
                    _animator.SetInteger(Behaviour, 1);
                    break;
                case PlayerAnimation.SwordAura:
                    id = OnSkill;
                    _animator.SetInteger(Behaviour, 2);
                    break;
                case PlayerAnimation.SectorAttack:
                    id = OnSkill;
                    _animator.SetInteger(Behaviour, 3);
                    break;
                case PlayerAnimation.CircleAttack:
                    id = OnSkill;
                    _animator.SetInteger(Behaviour, 4);
                    break;
                case PlayerAnimation.TimeCutter:
                    id = OnSkill;
                    _animator.SetInteger(Behaviour, 5);
                    break;
                case PlayerAnimation.IceSpike:
                    id = OnSkill;
                    _animator.SetInteger(Behaviour, 6);
                    break;
                case PlayerAnimation.IceSpray:
                    id = OnSkill;
                    _animator.SetInteger(Behaviour, 7);
                    break;
                case PlayerAnimation.SkillRepeat:
                    id = SkillRepeat;
                    break;
                case PlayerAnimation.SlideEnterLeap:
                    id = OnSlide;
                    _animator.SetInteger(Behaviour, 0);
                    break;
                case PlayerAnimation.SlideEnterLand:
                    _animator.SetInteger(Behaviour, 1);
                    return;
                case PlayerAnimation.SlideExitLeap:
                    _animator.SetInteger(Behaviour, 2);
                    return;
                case PlayerAnimation.SlideExitLand:
                    _animator.SetInteger(Behaviour, 3);
                    return;
                case PlayerAnimation.Dead:
                    id = OnDead;
                    break;
                case PlayerAnimation.Fall:
                    id = OnFall;
                    break;
                case PlayerAnimation.Stun:
                    id = OnStun;
                    break;
                case PlayerAnimation.KnockBack:
                    id = OnKnockBack;
                    break;
                default:
                    Debug.LogError("사용되지 않는 애니메이션 ID");
                    break;
            }

            _animator.SetTrigger(id);
        }
        #endregion

        #region Camera Shake
        public void CameraRandomShake() => CameraRandomShake(Settings.AttackCameraShake);

        /// <summary>
        /// 랜덤 벨로시티로 카메라를 흔듭니다.
        /// </summary>
        public void CameraRandomShake(CameraShakeSettings settings)
        {
            float randomX = settings.RangeX.Random();
            float randomY = settings.RangeY.Random();
            _impulseListener.m_DefaultVelocity =
                new Vector3(randomX, randomY, 0)*settings.Multiplier;
            _impulseListener.m_ImpulseDefinition.m_ImpulseDuration = settings.Time;
            _impulseListener.GenerateImpulse();
        }
        #endregion

        #region Time Controller
        public bool UsePlayerUnscaledTimeScale { get; private set; } = false;

        public void SetCircleAttackTimeScale() => SetTimeScale(Settings.CircleAttackTimeScale, true);

        public bool CinemachineIgnoreTimescale
        {
            get => _cinemachineBrain.m_IgnoreTimeScale;
            set => _cinemachineBrain.m_IgnoreTimeScale = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeScale"></param>
        /// <param name="usePlayerUnscaledTimeScale">플레이어가 timeScale에 독립될지 결정합니다. true일 시 메뉴 열기가 작동하지 않습니다.</param>
        public void SetTimeScale(float timeScale, bool usePlayerUnscaledTimeScale)
        {
            var changed = usePlayerUnscaledTimeScale != UsePlayerUnscaledTimeScale;
            UsePlayerUnscaledTimeScale = usePlayerUnscaledTimeScale;
            Time.timeScale = timeScale;
            if (usePlayerUnscaledTimeScale)
            {
                _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                foreach (var cloth in _cloths)
                {
                    cloth.SerializeData.updateMode = ClothUpdateMode.Unscaled;
                    cloth.SetParameterChange();
                }
                if (CurrentEffect)
                {
                    CurrentEffect.UseUnscaledTime = true;
                }

                // AutoManager.Get<GameManager>().IsActiveMenu = false;
            }
            else if (changed)
            {
                _animator.updateMode = AnimatorUpdateMode.Normal;
                foreach (var cloth in _cloths)
                {
                    cloth.SerializeData.updateMode = ClothUpdateMode.Normal;
                    cloth.SetParameterChange();
                }
                if (CurrentEffect)
                {
                    CurrentEffect.UseUnscaledTime = false;
                }

                CinemachineImpulseManager.Instance.IgnoreTimeScale = false;
                // AutoManager.Get<GameManager>().IsActiveMenu = true;
                if (_cinemachineBrain)
                {
                    CinemachineIgnoreTimescale = false;
                }
            }
        }
        #endregion

        #region Oberservable
        public System.IObservable<Unit> AttackKeyDownObservable() =>
            _characterInput.PressAttack.Where(active => active).Select(_ => Unit.Default);

        public System.IObservable<Unit> RightKeyDownObservable() =>
            _characterInput.MouseRightClick.Where(active => active).Select(_ => Unit.Default);

        public System.IObservable<Unit> RightKeyUpObservable() =>
            _characterInput.MouseRightClick.Where(active => !active).Select(_ => Unit.Default);

        public System.IObservable<Unit> DodgeFlashKeyDownObservable() =>
            _characterInput.PressDodgeFlash.Where(active => active).Select(_ => Unit.Default);

        public System.IObservable<float> ScrollYKeyObservable() => _characterInput.ScrollY.AsObservable();
        
        public System.IObservable<Unit> ReloadKeyDownObservable() =>
            _characterInput.Reload.Where(active => active).Select(_ => Unit.Default);

        public System.IObservable<Unit> InteractionKeyDownObservable() =>
            _characterInput.Interaction.Where(active => active).Select(_ => Unit.Default);
        #endregion

        #region Auto
        [Button("Auto Binding")]
        public void AutoBind()
        {
            if (GameObject.Find("PlayerFollowCamera")
                .TryGetComponent(out CinemachineVirtualCamera outResultVirtualCamera))
                VirtualCamera = outResultVirtualCamera;

            var centerPoint = transform.Find("CenterPoint");
            if (centerPoint)
            {
                CenterPoint = centerPoint;
            }

            CircleAttackCameraObject = GameObject.Find("CircleAttackCamera");
        }
        #endregion

        #region Flash
        public void CreateFlashEffect(Vector3 position)
        {
            GameObject dash = EffectManager.Instance.Get(EffectType.PlayerFlash);
            var forward = TurnTowardController.GetForward();
            dash.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
        }
        #endregion

        #region Move
        /// <summary>
        /// 이동 입력을 눌렀다 땜을 체크합니다.
        /// </summary>
        /// <returns></returns>
        public bool MoveWasJustPressed()
        {
            return _characterInput.GetPlayerAction().HammerKey.WasPerformedThisFrame();
        }

        /// <summary>
        /// 이동 입력을 계속 누르고 있음을 체크합니다.
        /// </summary>
        /// <returns></returns>
        public bool IsMovePressed()
        {
            var controllerType = _characterInput.GetControllerType();

            if (controllerType is ControllerType.Gamepad or ControllerType.KeyboardWASD)
                return _characterInput.MoveAxis.Value != Vector2.zero;

            return _characterInput.GetPlayerAction().HammerKey.IsPressed();
        }
        #endregion

        #region 피격
        /// <summary>
        /// 히트 되었을 때 반짝 연출을 처리합니다.
        /// </summary>
        public void HitTint()
        {
            _hitTintTween?.Complete();

            if (_hitMaterials == null || _hitMaterials.Length == 0)
                return;
            
            foreach (Material hitMaterial in _hitMaterials)
            {
                // 히트 컬러를 지정합니다.
                hitMaterial.SetColor(HitColor, Settings.HitTintColor);
                hitMaterial.SetFloat(HitThreshold, 1f);
                
                var hitThresholdTween = hitMaterial.DOFloat(0f,HitThreshold,Settings.HitTintTime)
                    .SetEase(Ease.InOutCirc);
                _hitTintTween.Insert(0,hitThresholdTween);
            }

            if (_hitTintTween != null)
                _hitTintTween.onComplete += () => _hitTintTween = null;
        }
        #endregion

        #region Dead
        /// <summary>
        /// 죽었을 때 렌더링을 처리합니다.
        /// </summary>
        public void OnTriggerDead()
        {
            if (NavMeshAgent && NavMeshAgent.enabled)
            {
                NavMeshAgent.isStopped = true;
            }
            OnTriggerAnimation(PlayerAnimation.Dead);
            PlaySoundOnce("Dead");
            SignalsService.SendSignal("InGame", "Dead");
            SaturationController.Instance?.OnDead();
        }
        #endregion

        #region Sound

        [field: SerializeField, FoldoutGroup("사운드", true)] 
        public LocalKeyList Sounds { get; private set; }
        
        
        private static AudioManager AudioManager => AutoManager.Get<AudioManager>();
        
        public void PlayWalkSound()
        {
            PlaySoundOnce("Walk");
        }
        public void PlayRunSound()
        {
            PlaySoundOnce("Run");
        }
        
        /// <summary>
        /// 걷기 사운드를 재생합니다.
        /// </summary>
        public void PlayFootStepSound()
        {
            //TODO : 캐릭터가 밣고 있는 곳에 따라 소리가 다르게 재생해야합니다.
            if (Sounds.TryGetValue("FootStep", out var audioPath))
                AudioManager.PlayOneShot(audioPath, "FootstepSurface", 0);
        }
        public void PlaySoundOnce(string key)
        {
            if (!Sounds.TryGetValue(key, out var clip))
            {
                return;
            }

            AudioManager.PlayOneShot(clip);
        }
        
        #endregion

        [field: SerializeField, FoldoutGroup("게임패드 진동", true)] 
        public RumbleSettingsByString Rumbles { get; private set; }

        public void PlayRumbleOnce(string key)
        {
            if (!Rumbles.TryGet(key, out var settings))
            {
                return;
            }
            GamePadManager.Instance.RumblePulse(settings);
        }

        #region Hit Material Destroy
        private void OnDestroy()
        {
            // 인스턴스화된 머티리얼을 삭제
            foreach (Material instanceMaterial in _hitMaterials)
                Destroy(instanceMaterial);
        }
        #endregion
    }
}
