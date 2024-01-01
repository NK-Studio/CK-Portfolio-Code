using System;
using System.Collections.Generic;
using Character.Core;
using Character.Model;
using Character.Presenter;
using Character.View;
using Cysharp.Threading.Tasks;
using Damage;
using Dummy.Scripts;
using Enemy.Behavior;
using EnumData;
using FMODUnity;
using Level;
using Managers;
using ManagerX;
using Micosmo.SensorToolkit;
using Platform;
using Settings;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Utility;
using Logger = NKStudio.Logger;

namespace Character.Behaviour
{
    [RequireComponent(typeof(PlayerView))]
    public class PlayerBehaviour : MonoBehaviour
    {
        private Camera _mainCamera;
        private Transform _tr;
        private CharacterSettings _settings;
        private PlayerFSM _fsm;

        private void Awake()
        {
            Transform playerTransform = transform;
            _mainCamera = Camera.main;
            _tr = playerTransform;
            _fsm = GetComponent<PlayerFSM>();
        }

        private void Start()
        {
            _settings = GameManager.Instance.Settings;
            NavMeshHandler.LayerMask = NavMeshAreaMask;

            InitializeAttackMonsterActions();

        }

        private Vector3 CalculateMouseDirectionOrMoveDirection(PlayerView view)
        {
            Vector3 MouseFallback()
            {
                var moveAxis = view.GetInput().MoveAxis.Value;
                return !moveAxis.IsZero() ? moveAxis.ToVectorXZ().normalized : view.TurnTowardController.GetForward();
            }
            if (!MouseRaycastGroundToMove(view, out Vector3 target))
            {
                return MouseFallback();
            }

            Vector3 toTarget = target - transform.position;
            // DebugX.Log($"CalculateInputDirectionOrPlayerForward - toTarget.sqrMagnitude: {toTarget.sqrMagnitude}");
            if (toTarget.sqrMagnitude <= Vector3.kEpsilon)
            {
                return MouseFallback();
            }
            return toTarget.normalized;
        }

        /// <summary>
        /// 마우스를 통해 방향을 구하고, 실패하면 카메라 기준 캐릭터 방향을 반환한다.
        /// </summary>
        /// <returns></returns>
        public Vector3 CalculateInputDirectionOrMouse(PlayerView view, PlayerModel model)
        {
            Vector3 direction = Vector3.zero;

            var controllerType = view.GetInput().GetControllerType();

            switch (controllerType)
            {
                case ControllerType.KeyboardMouse:
                {
                    direction = CalculateMouseDirectionOrMoveDirection(view);
                    break;
                }
                case ControllerType.Gamepad:
                case ControllerType.KeyboardWASD:
                    // 이동 중 방향 기반으로 점멸
                    var rawAxis = view.GetInput().MoveAxis.Value;
                    if (!rawAxis.IsZero())
                        direction = TransformInputAxis(model, rawAxis).normalized;
                    // 입력 없으면 마우스 방향, 그것도 실패하면 플레이어 방향으로
                    else
                        direction = CalculateMouseDirectionOrMoveDirection(view);
                    break;
            }

            return direction;
        }


        #region Mouse Move

        [FoldoutGroup("마우스 이동", true), NavMeshMask]
        public int NavMeshAreaMask = ~0;
        
        [FoldoutGroup("마우스 이동", true)] 
        public LayerMask GroundLayerMask = 0;

        // NavMesh 관련 유틸 (이동 가능 여부)
        public readonly NavMeshHandler NavMeshHandler = new(); 


        /// <summary>
        /// 마우스가 가리키는 화면 상 바닥을 구합니다.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="hitPosition"></param>
        /// <returns></returns>
        public bool MouseRaycastGround(PlayerView view, out Vector3 hitPosition)
        {
            // 카메라 필요함 !!
            if (_mainCamera == null)
            {
                hitPosition = Vector3.zero;
                return false;
            }

            // 마우스 위치 기반 Ray
            Vector3 mousePosition = view.GetMousePosition();
            Ray mouseRay = _mainCamera.ScreenPointToRay(mousePosition);

            // 물리 연산으로 바닥 충돌 (단일 검사.)
            if (Physics.Raycast(mouseRay, out var rayHit, Mathf.Infinity, GroundLayerMask))
            {
                // 충돌 지점 사용
                hitPosition = rayHit.point;
            }
            else
            {
                Vector3 playerPosition = transform.position;
                // 물리 연산에 실패하면 플레이어 높이와 같은 높이의 가상 평면 raycast
                // SamplePosition을 통해 가장 가까운 점 찾기
                // SamplePosition의 Origin은?
                // => mouseRay와 플레이어랑 같은 높이의 수평 Plane과의 교차점 
                var plane = new Plane(Vector3.up, playerPosition);

                // plane 투영 실패는 거의 일어나지 않음
                if (!plane.Raycast(mouseRay, out var t))
                {
                    hitPosition = Vector3.zero;
                    return false;
                }

                // 교차점
                hitPosition = mouseRay.GetPoint(t);
            }

            return true;
        }
        /// <summary>
        /// 마우스가 가리키는 화면 상 바닥 지점(또는 방향)으로 이동 가능한 지점을 구합니다.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="targetPosition"></param>
        /// <returns></returns>
        public bool MouseRaycastGroundToMove(PlayerView view, out Vector3 targetPosition)
        {
            if (!MouseRaycastGround(view, out var target))
            {
                targetPosition = Vector3.zero;
                return false;
            }

            return NavMeshHandler.GetMovablePosition(transform.position, target, out targetPosition);
        }

        public Vector3 TransformInputAxis(PlayerModel model, Vector2 axis)
        {
            Vector3 forward, right;
            if (model.MoveAxisAdjuster)
            {
                forward = model.MoveAxisAdjuster.Forward;
                right = model.MoveAxisAdjuster.Right;
            }
            else
            {
                var cameraTransform = _mainCamera.transform;
                forward = cameraTransform.forward.Copy(y: 0f).normalized;
                right = cameraTransform.right.Copy(y: 0f).normalized;
            }
            return ((forward * axis.y) + (right * axis.x));
        }
        
        /// <summary>
        /// Axis 형태 이동 입력을 처리합니다.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="model"></param>
        public void HandleAxisMoveInput(PlayerView view, PlayerModel model)
        {
            var axis = view.GetInput().MoveAxis.Value;
            if (model.OtherState == PlayerState.Sliding)
            {
                model.SlideHorizontalInput = axis;
                return;
            }
            
            Vector3 origin = transform.position;
            
            // 미입력 시 제자리를 목표 지점으로 설정 (관성 제거)
            bool isNotInputMoveStick = axis.IsZero();
            if (isNotInputMoveStick)
            {
                model.CurrentTargetPosition = origin;
                UpdateDestinationToCurrentTargetPosition(view, model, false);
                return;
            }
 
            Vector3 direction = TransformInputAxis(model, axis).normalized;
            Vector3 sample = origin + direction;

            if (!NavMeshHandler.GetStraightMovablePosition(origin, sample, out var target, true))
            {
                return;
            }

            if ((target - origin).sqrMagnitude <= Vector3.kEpsilon)
            {
                return;
            }

            model.CurrentTargetPosition = target;
            UpdateDestinationToCurrentTargetPosition(view, model, true);
        }

        /// <summary>
        /// 네브매쉬의 Destination을 업데이트합니다.
        /// </summary>
        private void UpdateDestinationToCurrentTargetPosition(PlayerView view, PlayerModel model, bool isMoving)
        {
            // UnityEngine.Debug.Log($"UpdateDestinationToCurrentTargetPosition - isMoving={isMoving}, CanInterruptSkill={model.CanInterruptSkill}, currentState={_fsm.CurrentState.Key}");
            // 스킬 사용 중 후속 모션에 들어섰을 때 이동 판정이 작용하면?
            if (isMoving && model.CanInterruptSkill && ( 
                    /* _fsm.IsAttackState() || */
                    _fsm.IsSkillUsingState() 
                    || _fsm.CurrentState.Key == PlayerState.Dash 
                    || _fsm.CurrentState.Key == PlayerState.Stun 
            )) {
                // UnityEngine.Debug.Log("MOVE TO IDLE !!!!");
                // Idle로 상태 전이
                _fsm.ChangeState(PlayerState.Idle);
            }

            // 밀리는 중에는 이동 안 됨
            if (model.IsStop || view.IsPushing)
            {
                return;
            }

            view.UpdateDestination(model.CurrentTargetPosition);
        }

        #endregion

        #region Camera
        
        public void UpdateCameraPanning(PlayerView view, PlayerModel model, Transform cameraRoot)
        {
            Vector3 localPosition = cameraRoot.localPosition;
            Vector3 newPosition;
            if (!model.CanInput(PlayerModel.InputType.Attack) || !AutoManager.Get<DataManager>().IsEnableCameraPanning)
            {
                newPosition = localPosition;
            }
            else
            {
                Vector2 normalizedPosition;
                var controllerType = InputManager.Instance.CurrentController;
                if (controllerType == ControllerType.Gamepad)
                {
                    var aimAxis = view.GetInput().AimAxis.Value;
                    normalizedPosition = aimAxis;
                }
                else
                {
                    var mousePosition = Mouse.current.position.value;
                    float w = Screen.width, h = Screen.height;
                    // 가로, 세로 중 작은 (보통 세로) 값 기준으로 정규화
                    var minimumSize = Mathf.Min(w, h);
                    float hw = w * 0.5f, hh = h * 0.5f;

                    var centeredPosition = mousePosition - new Vector2(hw, hh);
                    normalizedPosition = centeredPosition / (minimumSize * 0.5f);
                }

                var nx = normalizedPosition.x;
                var ny = normalizedPosition.y;
                var panningOffset = new Vector2(
                    Mathf.Sign(nx) * _settings.CameraPanningHorizontalCurve.Evaluate(Mathf.Abs(nx)),
                    Mathf.Sign(ny) * _settings.CameraPanningVerticalCurve.Evaluate(Mathf.Abs(ny))
                );
                newPosition = TransformInputAxis(model, panningOffset).Copy(y: localPosition.y);
            }

            cameraRoot.localPosition = Vector3.Lerp(localPosition, newPosition, 10 * Time.deltaTime);
        }
        

        #endregion

        #region Deprecated Skills(ZSlash, FlashAttack)

        /// <summary>
        /// 마우스가 가리키는 위치 또는 게임패드 최대 거리 상대 벡터를 구합니다. 
        /// </summary>
        /// <returns></returns>
        private Vector3 CalculateInputPointerVector(PlayerView view, float maxLength = 1f)
        {
            Vector3 pointerVector = Vector3.zero;

            var controllerType = view.GetInput().GetControllerType();

            switch (controllerType)
            {
                case ControllerType.KeyboardMouse:
                    if (MouseRaycastGroundToMove(view, out Vector3 target))
                    {
                        Vector3 toTarget = target - transform.position;
                        toTarget.y = 0f;

                        pointerVector = toTarget;

                        // 최대 거리보다 클 경우
                        if (toTarget.sqrMagnitude > maxLength * maxLength)
                        {
                            // 길이 maxLength로 제한
                            pointerVector = toTarget.normalized * maxLength;
                        }
                    }
                    // 못 구했으면 플레이어 바라보는 방향 최대거리로
                    else
                        pointerVector = view.TurnTowardController.GetForward() * maxLength;

                    break;
                case ControllerType.Gamepad:

                    if (!view.GetInput().MoveAxis.Value.IsZero())
                    {
                        // [0, 1]로 오는 게임패드 조이스틱 입력을 그대로 maxLength를 곱해 반환
                        pointerVector = view.GetInput().MoveAxis.Value.ToVectorXZ() * maxLength;
                    }
                    else
                        pointerVector = view.TurnTowardController.GetForward() * maxLength;

                    break;
            }

            return pointerVector;
        }

        // 공격 정의: 각 PlayerState에 따라 공격 방식 정의
        private delegate void AttackMonsterAction(PlayerView view, IHostile enemy, DamageMode mode);

        private Dictionary<PlayerState, AttackMonsterAction> _attackActionsByState;

        private void InitializeAttackMonsterActions()
        {
            _attackActionsByState = new Dictionary<PlayerState, AttackMonsterAction>
            {
                /*
                {
                    PlayerState.ZSlash, (view, enemy, mode) =>
                    {
                        if (mode == DamageMode.Stack)
                        {
                            enemy.Damage(EnemyDamageInfo.Get(
                                _settings.ZSlashSettings.Damage,
                                gameObject,
                                mode,
                                playerAttackType: PlayerState.ZSlash
                            ));
                        }
                        else
                        {
                            enemy.Damage(EnemyDamageInfo.Get(
                                _settings.ZSlashSettings.PopAllDamage,
                                gameObject,
                                mode,
                                DamageReaction.KnockBack,
                                new KnockBackInfo(
                                    _settings.ZSlashSettings.PopAllKnockBack,
                                    transform, enemy.transform
                                ),
                                PlayerState.ZSlash
                            ));
                        }

                        ManagerX.AutoManager.Get<RumbleAutoManager>().RumblePulse(0.25f, 1f, 0.25f);
                        view.CameraRandomShake(_settings.ZSlashSettings.CameraShakeSettings);
                    }
                },
                { PlayerState.Attack01, (view, enemy, mode) => NormalAttack(view, enemy, PlayerState.Attack01, mode) },
                { PlayerState.Attack02, (view, enemy, mode) => NormalAttack(view, enemy, PlayerState.Attack02, mode) },
                { PlayerState.Attack03, (view, enemy, mode) => NormalAttack(view, enemy, PlayerState.Attack03, mode) },
                { PlayerState.Attack04, (view, enemy, mode) => NormalAttack(view, enemy, PlayerState.Attack04, mode) },
                {
                    PlayerState.SectorAttack, (view, enemy, mode) => CustomAttack(view, enemy, PlayerState.SectorAttack,
                        mode, DamageReaction.KnockBack,
                        _settings.SectorAttackSettings.Damage,
                        _settings.SectorAttackSettings.KnockBack,
                        _settings.SectorAttackSettings.CameraShakeSettings
                    )
                },
                {
                    PlayerState.IceSpray, (view, enemy, mode) =>
                    {
                        enemy.Damage(EnemyDamageInfo.Get(
                            _settings.IceSpraySettings.Damage, gameObject, DamageMode.Normal, DamageReaction.Freeze,
                            playerAttackType: PlayerState.IceSpray
                        ));
                        ManagerX.AutoManager.Get<RumbleAutoManager>().RumblePulse(0.25f, 0.5f, 0.25f);
                        view.CameraRandomShake(_settings.IceSpraySettings.CameraShakeSettings);
                    }
                },
                {
                    PlayerState.TimeCutter, (view, enemy, mode) => CustomAttack(view, enemy, PlayerState.SectorAttack,
                        mode, DamageReaction.KnockBack,
                        _settings.TimeCutterSettings.Damage,
                        _settings.TimeCutterSettings.KnockBack,
                        _settings.TimeCutterSettings.CameraShakeSettings
                    )
                },
                {
                    PlayerState.FlashAttack, (view, enemy, mode) => CustomAttack(view, enemy, PlayerState.FlashAttack,
                        mode, DamageReaction.KnockBack,
                        _settings.FlashAttackSettings.Damage,
                        _settings.FlashAttackSettings.KnockBack,
                        _settings.FlashAttackSettings.CameraShakeSettings
                    )
                },
                {
                    PlayerState.CircleAttack, (view, enemy, mode) =>
                    {
                        if (mode == DamageMode.Stack)
                        {
                            enemy.Damage(EnemyDamageInfo.Get(
                                _settings.CircleAttackSettings.Damage, gameObject, mode,
                                playerAttackType: PlayerState.CircleAttack
                            ));
                        }
                        else
                        {
                            enemy.Damage(EnemyDamageInfo.Get(
                                _settings.CircleAttackSettings.PopAllDamage,
                                gameObject,
                                mode,
                                DamageReaction.KnockBack,
                                new KnockBackInfo(
                                    _settings.CircleAttackSettings.PopAllKnockBack,
                                    transform, enemy.transform
                                ),
                                PlayerState.CircleAttack
                            ));
                        }

                        ManagerX.AutoManager.Get<RumbleAutoManager>().RumblePulse(0.25f, 1f, 0.25f);
                        view.CameraRandomShake(_settings.CircleAttackSettings.CameraShakeSettings);
                    }
                },
                */
            };
        }

        private void CustomAttack(
            PlayerView view,
            IHostile enemy,
            PlayerState state,
            DamageMode mode,
            DamageReaction reaction,
            float damage,
            CharacterSettings.KnockBackSettings knockBack,
            CameraShakeSettings shake = null
        )
        {
            enemy.Damage(EnemyDamageInfo.Get(
                damage,
                gameObject,
                mode,
                reaction,
                new KnockBackInfo(knockBack, transform, enemy.transform),
                state
            ));
            ManagerX.AutoManager.Get<GamePadManager>().RumblePulse(0.25f, 0.5f, 0.25f);
            if (shake != null)
            {
                view.CameraRandomShake(shake);
            }
        }

        private Dictionary<int, IHostile> _stackedMonsterMap = new();
        private HashSet<int> _currentAttackedIds = new();

        /// <summary>
        /// RangeSensor 내의 오브젝트들에 공격 판정
        /// AnimationEventHandle에서 호출
        /// </summary>
        public void AttackDamage(int index, DamageMode mode, PlayerView view, PlayerModel model) 
            => AttackDamage(index, mode, view, model, view.AttackRange.Sensors);

        public void AttackDamage(
            int index, DamageMode mode, PlayerView view, PlayerModel model, 
            IList<RangeSensor> ranges, HashSet<int> customDistincter = null
        ) {
            if (ranges == null)
                return;

            if (customDistincter == null)
            {
                customDistincter = _currentAttackedIds;
                customDistincter.Clear(); // custom distincter가 없으면 매 AttackDamage()마다 초기화
            }


            // 실제 타격 로직
            void Damage(IHostile enemy)
            {
                if (enemy == null || !enemy.isActiveAndEnabled || !enemy.gameObject.activeInHierarchy)
                {
                    return;
                }
                if (!_attackActionsByState.TryGetValue(view.AttackType, out var action))
                {
                    DebugX.Log($"<color=red>Player Attack Failed - tried attack while {model.OtherState}</color>");
                    return;
                }

                // DebugX.Log($"{nameof(action)} applied on {model.OtherState}");
                action(view, enemy, mode);

                // Stack 공격의 경우 별도의 map에 쌓음
                if (mode == DamageMode.Stack)
                {
                    _stackedMonsterMap.TryAdd(enemy.GetInstanceID(), enemy);
                }

                model.Soul = Mathf.Clamp01(model.Soul + _settings.SoulAmountByAttackMonster / 100f);
            }

            // Sensor 감지 로직
            void Detect(RangeSensor sensor)
            {
                IEnumerable<GameObject> detections;
                if (sensor.TryGetComponent(out SectorRangeSensorFilter filter))
                {
                    detections = filter.FilteredPulse();
                }
                else
                {
                    sensor.Pulse();
                    detections = sensor.Detections;
                }
                foreach (GameObject target in detections)
                {
                    // 중복제거
                    var id = target.GetInstanceID();
                    if (customDistincter.Contains(id))
                    {
                        continue;
                    }

                    customDistincter.Add(id);

                    if (target.CompareTag("Destructible"))
                    {
                        DestructibleObject destructible = target.GetComponent<DestructibleObject>();
                        view.CameraRandomShake();
                        destructible.Play();
                    }
                    else if (target.TryGetComponent(out IHostile enemy))
                    {
                        Logger.Log($"Detected Hostile {enemy.name}");
                        Damage(enemy);
                    }
                }
            }

            var sensors = ranges;
            // 전체 타격
            if (index < 0)
            {
                foreach (var sensor in sensors)
                {
                    Detect(sensor);
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

                Detect(sensors[index]);
            }

            // 이전 Stack 공격에서 쌓였는데, 이번 공격에서 PopAll되지 못한 Monster 타격
            if (mode == DamageMode.PopAll)
            {
                foreach (var (id, monster) in _stackedMonsterMap)
                {
                    if (customDistincter.Contains(id)) continue;

                    Damage(monster);
                }

                _stackedMonsterMap.Clear();
            }
        }

        #endregion

        #region Health & Damage

        public void OnDamage(PlayerModel model, PlayerView view, DamageInfo info)
        {
            var source = info.Source;
            var damage = info.Amount;
            var reaction = info.Reaction;

            Vector3 GetKnockBackDirection()
            {
                if (!source)
                {
                    return -transform.forward;
                }
                var knockBackDirection = transform.position - source.transform.position;
                knockBackDirection.y = 0f;
                knockBackDirection.Normalize();
                return knockBackDirection;
            }

            view.PlaySoundOnce("Hit");
            view.HitTint();
            view.HitEffectController.PlayHitEffect();
            model.Health = Mathf.Max(0, model.Health - damage);
            if (model.Health > 0) // 살아는 있어야 stun이나 knockback 적용
            {
                switch (reaction)
                {
                    case DamageReaction.Normal:
                        model.InvincibleTime = _settings.HitInvincibleTime;
                        break;
                    case DamageReaction.Stun:
                    case DamageReaction.KnockBack:
                    {
                        if (!_fsm.ChangeState(PlayerState.Stun))
                        {
                            // Stun으로 전환 실패 시 일반 피격으로 판정
                            model.InvincibleTime = _settings.HitInvincibleTime;
                        }
                        break;
                    }
                    // {
                        // _fsm.ChangeState(PlayerState.KnockBack);
                        // break;
                    // }
                    default:
                        return;
                }

                if (_fsm.CurrentState.Key != PlayerState.Hammer && _fsm.CurrentState.Key != PlayerState.Dash)
                {
                    var knockBack = info.KnockBack.IsValid()
                        ? info.KnockBack
                        : new KnockBackInfo(
                            GetKnockBackDirection(),
                            10f,
                            reaction == DamageReaction.Stun ? 0.05f : 0.1f
                        );
                    view.Push(knockBack.Direction * knockBack.Amount, ForceMode.Force, knockBack.Time);
                    view.TurnTowardController.SetRotation(-knockBack.Direction);
                }
            }
        }

        #endregion
        
        #region Animation

        public bool IsActuallyStopped(PlayerView view, PlayerModel model)
            => view.NavMeshAgent.desiredVelocity.magnitude < 0.05 
              && (model.IsDisabledInput(PlayerModel.InputType.Move) || !view.IsMovePressed());
        
        /// <summary>
        /// 기본 애니메이터 파라미터를 갱신합니다.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="model"></param>
        public void OnUpdateBaseAnimation(PlayerView view, PlayerModel model)
        {
            bool isStop = IsActuallyStopped(view, model);

            // 이동한 velocity를 현재 바라보는 방향 기준으로 표현 
            Vector3 velocity = view.NavMeshAgent.velocity;
            Vector3 forward = view.TurnTowardController.GetForward();
            Vector3 right = view.TurnTowardController.GetRight();

            Vector2 move = new Vector2(
                Mathf.Clamp(Vector3.Dot(right, velocity) / model.MovementSpeed, -1f, 1f),
                Mathf.Clamp(Vector3.Dot(forward, velocity) / model.MovementSpeed, -1f, 1f)
            );
            
            view.OnUpdateMoveAnimation(isStop, move);
        }
        
        #endregion

        #region Slide

        // TODO 언젠간 구조 바꾼다
        public void OnSlide()
        {
            _fsm.ChangeState(PlayerState.Sliding);
        }

        #endregion

        #region Auto

        [Button("Auto Binding")]
        public void AutoBind()
        {
            Debug.Assert(Camera.main != null, "Camera.main != null");
        }

        #endregion

        private void OnDisable()
        {
            GameManager.Instance.CanActiveMenu = true;
        }
    }
}