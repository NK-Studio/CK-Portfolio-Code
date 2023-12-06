using System;
using Enemys;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;
using Utility;

namespace Character.Model
{
    [Serializable]
    public class PlayerModel : MonoBehaviour
    {
        #region OtherState

        private ReactiveProperty<EOtherState> _otherState = new(EOtherState.Nothing);

        public EOtherState OtherState
        {
            get => _otherState.Value;
            set => _otherState.Value = value;
        }

        private IObservable<EOtherState> _otherStateObservable;
        public IObservable<EOtherState> OtherStateObservable => _otherStateObservable ??= _otherState.AsObservable();

        #endregion

        #region RopeState

        private ReactiveProperty<ERopeState> _ropeState = new();

        public ERopeState RopeState
        {
            get => _ropeState.Value;
            set => _ropeState.Value = value;
        }

        private IObservable<ERopeState> _ropeStateObservable;
        public IObservable<ERopeState> RopeStateObservable => _ropeStateObservable ??= _ropeState.AsObservable();

        #endregion

        #region IsStop

        private ReactiveProperty<bool> _isStop = new();

        public bool IsStop
        {
            get => _isStop.Value;
            set => _isStop.Value = value;
        }

        private IObservable<bool> _isStopObservable;
        public IObservable<bool> IsStopObservable => _isStopObservable ??= _isStop.AsObservable();

        #endregion

        #region FightMode

        private ReactiveProperty<int> _directionMode = new();

        public int DirectionModeTime
        {
            get => _directionMode.Value;
            set => _directionMode.Value = value;
        }

        private IObservable<int> _directionModeObservable;
        public IObservable<int> DirectionModeObservable => _directionModeObservable ??= _directionMode.AsObservable();

        #endregion

        #region UseWeight

        private ReactiveProperty<bool> _useWeight = new();

        public bool UseWeight
        {
            get => _useWeight.Value;
            set => _useWeight.Value = value;
        }

        private IObservable<bool> _useWeightObservable;
        public IObservable<bool> UseWeightObservable => _useWeightObservable ??= _useWeight.AsObservable();

        #endregion

        #region CurrentControllerState

        private ReactiveProperty<ControllerState> _currentControllerState = new(ControllerState.Falling);

        public ControllerState CurrentControllerState
        {
            get => _currentControllerState.Value;
            set => _currentControllerState.Value = value;
        }

        private IObservable<ControllerState> _currentControllerStateObservable;

        public IObservable<ControllerState> CurrentControllerStateObservable =>
            _currentControllerStateObservable ??= _currentControllerState.AsObservable();

        #endregion

        #region DoubleJumpCount

        private ReactiveProperty<int> _doubleJumpCount = new();

        public int DoubleJumpCount
        {
            get => _doubleJumpCount.Value;
            set => _doubleJumpCount.Value = value;
        }

        private IObservable<int> _doubleJumpCountObservable;

        public IObservable<int> DoubleJumpCountObservable =>
            _doubleJumpCountObservable ??= _doubleJumpCount.AsObservable();

        #endregion

        #region Invincible

        private ReactiveProperty<InvincibleState> _invincibleState = new();

        public InvincibleState InvincibleState
        {
            get => _invincibleState.Value;
            set => _invincibleState.Value = value;
        }

        private IObservable<InvincibleState> _invincibleStateObservable;

        public IObservable<InvincibleState> InvincibleStateObservable =>
            _invincibleStateObservable ??= _invincibleState.AsObservable();

        #endregion

        #region StandAndKey

        public Transform Key { get; set; }
        public Transform TargetStand { get; set; }

        #endregion

        public bool isLockGravity;
        
        public bool IsJumping { get; set; }
        public Vector3 hookShotPosition;

        [ReadOnly] public TargetInfo targetInfo;
    }
}