using System;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;
using Utility;

namespace Character.USystem.Hook.Model
{
    public enum EHookState
    {
        Idle,
        Forward,
        Stop,
        BackOrMoveTarget
    }

    public class HookSystemModel : MonoBehaviour
    {
        #region HookState

        private ReactiveProperty<EHookState> _hookState = new();

        public EHookState HookState
        {
            get => _hookState.Value;
            set => _hookState.Value = value;
        }

        private IObservable<EHookState> _hookStateObservable;
        public IObservable<EHookState> HookStateObservable => _hookStateObservable ??= _hookState.AsObservable();

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
        
        #region targetDirection

        public Vector3 TargetDirection { get; set; }
        
        #endregion

        #region TargetPosition

        public Vector3 TargetPosition { get; set; }
        
        #endregion

        #region Set

        /// <summary>
        /// 로프를 던집니다. 
        /// </summary>
        /// <param name="ropeState"></param>
 
        public void ShotRope(ERopeState ropeState)
        {
            RopeState = ropeState;
            HookState = EHookState.Forward;
        }

        #endregion
    }
}