using System;
using System.Collections.Generic;
using System.Linq;
using Character.Behaviour;
using Character.Core.Weapon;
using Dummy.Scripts;
using Enemy;
using EnumData;
using Level;
using Managers;
using Settings;
using Settings.Player;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Character.Model
{
    [Serializable]
    public class PlayerModel : MonoBehaviour
    {
        private CharacterSettings _settings;
        private PlayerFSM _fsm;

        private void Awake()
        {
            _fsm = GetComponent<PlayerFSM>();
        }

        public bool StatusLoaded { get; private set; } = false;
        
        private void Start()
        {
            StatusLoaded = false;
            _settings = GameManager.Instance.Settings;
            _movementSpeed.Value = _settings.MovementSpeed;

            this.UpdateAsObservable().Where(_ => GameManager.Instance.CheatMode).Subscribe(_ =>
            {
                foreach (var pair in MagazineMapping)
                {
                    if (Keyboard.current[pair.Key].wasPressedThisFrame)
                    {
                        Magazine = pair.Settings.CreateMagazine();
                    }
                }
            });

            Debug.Log($"Loading Player Status: {CheckpointManager.Instance.CheckPoint.Storage}");
            CheckpointManager.Instance.CheckPoint.Storage.Status.Apply(this);
            StatusLoaded = true;
        }


        public SlidePlane CurrentSlidePlane = null;
        public Vector2 SlideHorizontalInput = Vector2.zero;
        
        #region Health

        [SerializeField]
        private ReactiveProperty<float> _health = new(10f);

        public float Health
        {
            get => _health.Value;
            set => _health.Value = value;
        }

        private IObservable<float> _healthTimeObservable;

        public IObservable<float> HealthObservable =>
            _healthTimeObservable ??= _health.AsObservable();

        public bool IsDead => Health <= 0f || OtherState == PlayerState.Dead;

        #endregion

        #region MovementSpeed

        public delegate float FloatModifier(in float value);
        
        [SerializeField]
        private ReactiveProperty<float> _movementSpeed = new();
        public float MovementSpeed => _movementSpeed.Value;

        private IObservable<float> _movementSpeedObservable;
        public IObservable<float> MovementSpeedObservable =>
            _movementSpeedObservable ??= _movementSpeed.AsObservable();

        private HashSet<FloatModifier> _modifier = new();
        public void RegisterMovementSpeedModifier(FloatModifier modifier)
        {
            _modifier.Add(modifier);
            UpdateMovementSpeed();
        }

        public void UnregisterMovementSpeedModifier(FloatModifier modifier)
        {
            _modifier.Remove(modifier);
            UpdateMovementSpeed();
        }

        public void ClearMovementSpeedModifier()
        {
            _modifier.Clear();
            UpdateMovementSpeed();
        }

        public bool HasMovementSpeedModifier => _modifier.Count > 0;

        public void UpdateMovementSpeed() 
            => _movementSpeed.Value = _modifier.Aggregate(_settings.MovementSpeed, (current, m) => m.Invoke(current));

        #endregion
        
        
        
        #region InvincibleTime

        [field: SerializeField]
        private ReactiveProperty<float> _invincibleTime = new();

        public float InvincibleTime
        {
            get => _invincibleTime.Value;
            set => _invincibleTime.Value = value;
        }

        private IObservable<float> _invincibleTimeObservable;

        public IObservable<float> InvincibleTimeObservable =>
            _invincibleTimeObservable ??= _invincibleTime.AsObservable();

        public bool IsInvincibleTime => InvincibleTime > 0f;

        #endregion

        #region InvincibleFlag

        private ReactiveProperty<bool> _invincibleFlag = new();
        public bool InvincibleFlag
        {
            get => _invincibleFlag.Value;
            set => _invincibleFlag.Value = value;
        }

        private IObservable<bool> _invincibleFlagObservable;
        public IObservable<bool> InvincibleFlagObservable => 
            _invincibleFlagObservable ??= _invincibleFlag.AsObservable();

        #endregion
        

        #region InvincibleType

        [field: SerializeField, ReadOnly]
        public float[] InvincibleTimeByEnemyAttackType = new float[(int)EnemyAttackType._TypeCount];

        public float GetInvincibleTimeByEnemyAttackType(EnemyAttackType type)
            => InvincibleTimeByEnemyAttackType[(int)type];
        public float SetInvincibleTimeByEnemyAttackType(EnemyAttackType type, float value)
            => InvincibleTimeByEnemyAttackType[(int)type] = value;
        

        #endregion
        
        #region CurrentControllerState

        private ReactiveProperty<ControllerState> _currentControllerState = new(ControllerState.Grounded);

        public ControllerState CurrentControllerState
        {
            get => _currentControllerState.Value;
            set => _currentControllerState.Value = value;
        }

        private IObservable<ControllerState> _currentControllerStateObservable;

        public IObservable<ControllerState> CurrentControllerStateObservable =>
            _currentControllerStateObservable ??= _currentControllerState.AsObservable();

        #endregion

        #region IsStop

        [field: SerializeField, ReadOnly]
        private ReactiveProperty<bool> _isStop = new();

        public bool IsStop
        {
            get => _isStop.Value;
            set
            {
                // Debug.Log($"IsStop = <color=yellow>{_isStop.Value}</color> => <color=green>{value}</color>");
                _isStop.Value = value;
            }
        }

        private IObservable<bool> _isStopObservable;
        public IObservable<bool> IsStopObservable => _isStopObservable ??= _isStop.AsObservable();

        #endregion
        
        #region IsInputDisabled


        public bool IsInputDisabled
        {
            get => DisabledInput == InputType.All;
            set => DisabledInput = value ? InputType.All : InputType.None;
        }


        [Flags]
        public enum InputType
        {
            None         = 0,
            Move         = 0b000001,
            Flash        = 0b000010,
            Attack       = 0b000100,
            Skill        = 0b001000,
            Reload       = 0b010000,
            Interaction  = 0b100000,
            All     = Move | Flash | Attack | Skill | Reload | Interaction,
        }

        [SerializeField]
        private ReactiveProperty<InputType> _disabledInput = new(InputType.None);

        public InputType DisabledInput
        {
            get => _disabledInput.Value;
            set => _disabledInput.Value = value;
        }

        public bool IsDisabledInput(InputType type)
        {
            return (DisabledInput & type) != 0;
        }

        public void AddDisabledInput(InputType type)
        {
            DisabledInput |= type;
        }
        public void RemoveDisabledInput(InputType type)
        {
            DisabledInput &= ~type;
        }
        
        #endregion

        #region Attack Mode

        private ReactiveProperty<bool> _attackMode = new();

        public bool AttackMode
        {
            get => _attackMode.Value;
            set => _attackMode.Value = value;
        }

        private IObservable<bool> _attackModeObservable;
        public IObservable<bool> AttackModeObservable => _attackModeObservable ??= _attackMode.AsObservable();

        #endregion

        #region OtherState

        public PlayerState OtherState => _fsm.CurrentState.Key;

        private IObservable<PlayerState> _otherStateObservable;

        public IObservable<PlayerState> OtherStateObservable 
            => _fsm.CurrentStateObservable
                .Select(
                    it => it.Key
                );

        #endregion

        
        
        #region AttackModeTime

        private ReactiveProperty<float> _attackModeTime = new();

        public float AttackModeTime
        {
            get => _attackModeTime.Value;
            set => _attackModeTime.Value = value;
        }

        private IObservable<float> _attackModeTimeObservable;

        public IObservable<float> AttackModeTimeObservable =>
            _attackModeTimeObservable ??= _attackModeTime.AsObservable();

        #endregion

        #region Target

        private ReactiveProperty<Transform> _targetTransform = new();

        public Transform TargetTransform
        {
            get => _targetTransform.Value;
            set => _targetTransform.Value = value;
        }

        private IObservable<Transform> _targetTransformObservable;

        public IObservable<Transform> TargetTransformObservable =>
            _targetTransformObservable ??= _targetTransform.AsObservable();

        #endregion

        #region IsMoveToTarget

        private ReactiveProperty<bool> _isMoveToTarget = new();

        public bool IsMoveToTarget
        {
            get => _isMoveToTarget.Value;
            set => _isMoveToTarget.Value = value;
        }

        private IObservable<bool> _isMoveToTargetObservable;

        public IObservable<bool> IsMoveToTargetObservable =>
            _isMoveToTargetObservable ??= _isMoveToTarget.AsObservable();

        #endregion
        
        #region MoveAxisAdjuster

        private ReactiveProperty<MoveAxisAdjuster> _moveAxisAdjuster = new(null);

        public MoveAxisAdjuster MoveAxisAdjuster
        {
            get => _moveAxisAdjuster.Value;
            set => _moveAxisAdjuster.Value = value;
        }

        private IObservable<MoveAxisAdjuster> _moveAxisAdjusterObservable;

        public IObservable<MoveAxisAdjuster> MoveAxisAdjusterObservable =>
            _moveAxisAdjusterObservable ??= _moveAxisAdjuster.AsObservable();

        #endregion

        #region DodgeFlashTime

        private ReactiveProperty<float> _dodgeFlash = new();

        public float DodgeFlashTime
        {
            get => _dodgeFlash.Value;
            set => _dodgeFlash.Value = value;
        }

        private IObservable<float> _dodgeFlashObservable;
        public IObservable<float> DodgeFlashObservable => _dodgeFlashObservable ??= _dodgeFlash.AsObservable();

        #endregion

        #region Soul

        private ReactiveProperty<float> _soul = new();
        public IObservable<float> SoulObservable => _soul.AsObservable();

        public float Soul
        {
            get => _soul.Value;
            set => _soul.Value = value;
        }

        #endregion

        
        
        #region Magazine
        
        public PlayerBulletMagazine DefaultMagazine { get; set; } = null;
        
        [field: SerializeField]
        private ReactiveProperty<PlayerBulletMagazine> _magazine = new(null);
        public delegate void MagazineChanged(PlayerBulletMagazine oldMagazine, PlayerBulletMagazine newMagazine);

        public event MagazineChanged OnMagazineChanged;
        public IObservable<PlayerBulletMagazine> MagazineObservable => _magazine.AsObservable();

        public PlayerBulletMagazine Magazine
        {
            get => _magazine.Value ?? DefaultMagazine;
            set
            {
                OnMagazineChanged?.Invoke(_magazine.Value, value ?? DefaultMagazine);
                _magazine.Value = value;
            }
        }

        public PlayerBulletMagazine RawMagazine => _magazine.Value;

        [Button(ButtonSizes.Gigantic)]
        public void SetNewMagazine(PlayerBulletSettings settings)
        {
            Magazine = settings.CreateMagazine();
        }

        [Serializable]
        public struct SettingsByKey
        {
            public Key Key;
            public PlayerBulletSettings Settings;
        }

        public List<SettingsByKey> MagazineMapping = new();

        #endregion

        #region HammerDashTarget
        
        private ReactiveProperty<GameObject> _hammerDashTarget = new(null);

        public GameObject HammerDashTarget
        {
            get => _hammerDashTarget.Value;
            set => _hammerDashTarget.Value = value;
        }

        public IObservable<GameObject> HammerDashTargetObservable => _hammerDashTarget.AsObservable();

        #endregion
        
        #region NearestItem

        private ReactiveProperty<IItem> _nearestItem = new(null);

        public IItem NearestItem
        {
            get => _nearestItem.Value;
            set
            {
                if (_nearestItem.Value != value)
                {
                    _nearestItem.Value?.OnEndNearestItem();
                    value?.OnStartNearestItem();
                }
                _nearestItem.Value = value;
            }
        }

        public IObservable<IItem> NearestItemObservable => _nearestItem.AsObservable();
        

        #endregion
        
        #region CanFlash

        private ReactiveProperty<bool> _canFlash = new(true);

        public bool CanFlash
        {
            get => _canFlash.Value;
            set => _canFlash.Value = value;
        }

        public IObservable<bool> CanFlashObservable => _canFlash.AsObservable();
        
        #endregion
        
        #region CanInterruptSkill

        private ReactiveProperty<bool> _canInterruptSkill = new(true);

        public bool CanInterruptSkill
        {
            get => _canInterruptSkill.Value;
            set => _canInterruptSkill.Value = value;
        }

        public IObservable<bool> CanInterruptSkillObservable => _canInterruptSkill.AsObservable();

        #endregion

        #region FlashCooldown

        private ReactiveProperty<float> _flashCooldownTime = new();

        public float FlashCooldown
        {
            get => _flashCooldownTime.Value;
            set => _flashCooldownTime.Value = value;
        }

        public IObservable<float> FlashCooldownObservable => _flashCooldownTime.AsObservable();

        #endregion

        #region FlashGauge

        private ReactiveProperty<float> _flashGauge = new(1f);

        public float FlashGauge
        {
            get => _flashGauge.Value;
            set => _flashGauge.Value = value;
        }

        public IObservable<float> FlashGaugeObservable => _flashGauge.AsObservable();

        #endregion
        
        #region PlayerFollowCameraDistance

        private ReactiveProperty<float> _playerFollowCameraDistance = new(11f);

        public float PlayerFollowCameraDistanceDefault;

        public void ResetPlayerFollowCameraDistance() => PlayerFollowCameraDistance = PlayerFollowCameraDistanceDefault;
        public float PlayerFollowCameraDistance
        {
            get => _playerFollowCameraDistance.Value;
            set => _playerFollowCameraDistance.Value = value;
        }

        public IObservable<float> PlayerFollowCameraDistanceObservable => _playerFollowCameraDistance.AsObservable();

        #endregion
        
        #region PlayerFollowCameraTargetDistance

        private ReactiveProperty<float> _playerFollowCameraTargetDistance = new(16f);
        public float PlayerFollowCameraTargetDistance
        {
            get => _playerFollowCameraTargetDistance.Value;
            set => _playerFollowCameraTargetDistance.Value = value;
        }

        public IObservable<float> PlayerFollowCameraTargetDistanceObservable => _playerFollowCameraTargetDistance.AsObservable();

        #endregion
        
        #region CurrentTargetPosition

        private ReactiveProperty<Vector3> _currentTargetPosition = new();

        public Vector3 CurrentTargetPosition
        {
            get => _currentTargetPosition.Value;
            set => _currentTargetPosition.Value = value;
        }

        public IObservable<Vector3> CurrentTargetPositionObservable => _currentTargetPosition.AsObservable();

        /// <summary>
        /// 현재 위치를 타겟 위치로 설정
        /// </summary>
        public void ApplyCurrentTargetPosition() => CurrentTargetPosition = transform.position;

        #endregion

        public List<Vector3> TimeCutterPositionQueue { get; } = new(3);

        
        #region TimeCutterElapsedTime

        private ReactiveProperty<float> _timeCutterElapsedTime = new(3);

        public float TimeCutterElapsedTime
        {
            get => _timeCutterElapsedTime.Value;
            set => _timeCutterElapsedTime.Value = value;
        }
        public IObservable<float> TimeCutterElapsedTimeObservable => _timeCutterElapsedTime.AsObservable();

        #endregion
        
        
        #region TimeCutterAvailableQueueCount

        private ReactiveProperty<int> _timeCutterAvailableQueueCount = new(3);

        public int TimeCutterAvailableQueueCount
        {
            get => _timeCutterAvailableQueueCount.Value;
            set => _timeCutterAvailableQueueCount.Value = value;
        }
        public IObservable<int> TimeCutterAvailableQueueCountObservable => _timeCutterAvailableQueueCount.AsObservable();

        #endregion
        
        
        #region SkillCoolTime
        
        // 럭키 불릿타임
        #region TimeCutter

        private ReactiveProperty<float> _TimeCutterCoolTime = new();

        public float TimeCutterCoolTime
        {
            get => _TimeCutterCoolTime.Value;
            set => _TimeCutterCoolTime.Value = value;
        }

        public IObservable<float> TimeCutterCoolTimeObservable => _TimeCutterCoolTime.AsObservable();

        #endregion

        
        #region FlashAttack

        private ReactiveProperty<float> _flashAttackCoolTime = new();

        public float FlashAttackCoolTime
        {
            get => _flashAttackCoolTime.Value;
            set => _flashAttackCoolTime.Value = value;
        }

        public IObservable<float> FlashAttackCoolTimeObservable => _flashAttackCoolTime.AsObservable();

        #endregion

        #region SectorCoolTime

        private ReactiveProperty<float> _sectorAttackCoolTime = new();

        public float SectorAttackCoolTime
        {
            get => _sectorAttackCoolTime.Value;
            set => _sectorAttackCoolTime.Value = value;
        }

        public IObservable<float> SectorAttackCoolTimeObservable => _sectorAttackCoolTime.AsObservable();

        #endregion

        #region ZSlash

        private ReactiveProperty<float> _zSlashCoolTime = new();

        public float ZSlashCoolTime
        {
            get => _zSlashCoolTime.Value;
            set => _zSlashCoolTime.Value = value;
        }

        public IObservable<float> ZSlashCoolTimeObservable => _zSlashCoolTime.AsObservable();

        #endregion

        #region SwordAura

        private ReactiveProperty<float> _swordAuraCoolTime = new();

        public float SwordAuraCoolTime
        {
            get => _swordAuraCoolTime.Value;
            set => _swordAuraCoolTime.Value = value;
        }

        public IObservable<float> SwordAuraCoolTimeObservable => _swordAuraCoolTime.AsObservable();

        #endregion

        #region CurrentBattleArea

        [SerializeField]
        private ReactiveProperty<BattleArea> _currentBattleArea = new();

        public BattleArea CurrentBattleArea
        {
            get => _currentBattleArea.Value;
            set => _currentBattleArea.Value = value;
        }

        public IObservable<BattleArea> CurrentBattleAreaObservable => _currentBattleArea.AsObservable();

        #endregion
        
        #endregion
        
        
        
        #region Helper functions

        public bool IsIdleState() => OtherState.IsIdleState();
        /// <summary>
        /// Other상태가 공격상태인지 체크합니다.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool IsAttackState() => OtherState.IsAttackState();

        /// <summary>
        /// 스킬 사용 중 & 스킬 사용 준비 중인지 체크합니다.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool IsSkillState() => OtherState.IsSkillState();

        /// <summary>
        /// 스킬 사용 준비 중인지 체크합니다.
        /// </summary>
        /// <returns></returns>
        public bool IsSkillPrepareState() => OtherState.IsSkillPrepareState();

        /// <summary>
        /// 상태가 스킬 사용 중인지 체크합니다.
        /// </summary>
        /// <returns></returns>
        public bool IsSkillUsingState() => OtherState.IsSkillUsingState();
        /// <summary>
        /// 피해를 받는 상태인지 체크합니다.
        /// </summary>
        /// <returns></returns>
        public bool IsDamagingState() => OtherState.IsDamagingState();

        /// <summary>
        /// 입력을 받을 수 있는 상태 (!IsDead && !IsDamagingState())인지 체크합니다.
        /// </summary>
        /// <returns></returns>
        public bool CanInput(InputType type = InputType.All) => !IsDisabledInput(type) && !IsDead /*&& !IsDamagingState()*/;

        #endregion
    }
}