using System;
using System.Collections.Generic;
using Character.Core.FSM;
using Character.Model;
using Character.Presenter;
using Character.View;
using EnumData;
using Settings;
using Sirenix.OdinInspector;
using UnityEngine;
using Logger = NKStudio.Logger;
using Object = UnityEngine.Object;

namespace Character.Behaviour {
    
    [AttributeUsage(AttributeTargets.Class)]
    public class PlayerFSMStateAttribute : FSMStateAttribute
    {
        public PlayerFSMStateAttribute(PlayerState state) : base((int)state) { }
    }

    public abstract class PlayerFSMState : FSMState<PlayerFSM, PlayerState>
    {
        
        public PlayerFSM Player => OwnerEntity;
        public GameObject gameObject => Player.gameObject;
        public Transform transform => Player.transform;
        
        public PlayerPresenter Presenter => Player.Presenter;
        public PlayerBehaviour Behaviour => Player.Behaviour;
        public PlayerView View => Player.View;
        public PlayerModel Model => Player.Model;
        public CharacterSettings Settings => View.Settings;

        public PlayerState PreviousState => Player.PreviousState?.Key ?? PlayerState.None;
        public PlayerState CurrentState => Player.CurrentState.Key;
        public bool IsCurrentState => Player.CurrentState.Key == Key;
        
        public virtual float UpperLayerWeight { get; }
        public virtual float GetWeightCrossFadeTime(PlayerState previous) => 0.2f;
        
        public PlayerFSMState(PlayerFSM player, float upperLayerWeight) : base(player)
        {
            UpperLayerWeight = upperLayerWeight;
        }

        public void Log(string message, Object context = null)
        {
            if(!Player.ShowDebugStates.Contains(Key)) return;
            Logger.Log($"[{Key.ToString()}] {message}", context ?? Player);
        }
        public void LogWarning(string message, Object context = null)
        {
            if(!Player.ShowDebugStates.Contains(Key)) return;
            Logger.LogWarning($"[{Key.ToString()}] {message}", context ?? Player);
        }
    }
    
    public class PlayerFSM : FSM<PlayerFSM, PlayerState>, IFSMEntity
    {
        public PlayerPresenter Presenter { get; private set; }
        public PlayerBehaviour Behaviour { get; private set; }
        public PlayerView View { get; private set; }
        public PlayerModel Model { get; private set; }

        [field: SerializeField]
        public List<PlayerState> ShowDebugStates { get; private set; } = new();
        
        private void Start()
        {
            Presenter = GetComponent<PlayerPresenter>();
            Behaviour = Presenter.Behaviour;
            View = Presenter.View;
            Model = Presenter.Model;
            
            SetupStates(PlayerState.Idle);
        }

        [SerializeField]
        private float _layerFadeDuration;
        [SerializeField]
        private float _layerFadeTime;
        [SerializeField]
        private float _layerFadeStart;
        [SerializeField]
        private float _layerFadeEnd;
        protected override bool ChangeState(FSMState<PlayerFSM, PlayerState> newState, bool forced = false)
        {
            var result = base.ChangeState(newState);
            if (result && newState is PlayerFSMState p)
            {
                if (PreviousState != null)
                {
                    _layerFadeDuration = p.GetWeightCrossFadeTime(PreviousState.Key);
                    if (_layerFadeDuration <= 0f)
                    {
                        View.Animator.SetLayerWeight(View.UpperLayerIndex, p.UpperLayerWeight);
                    }
                    else
                    {
                        _layerFadeTime = 0f;
                        _layerFadeStart = View.Animator.GetLayerWeight(View.UpperLayerIndex);
                        _layerFadeEnd = p.UpperLayerWeight;
                    }
                    // Debug.Log($"Weight transition: {PreviousState.Key.ToString()}({_layerFadeStart}) => {p.Key.ToString()}({_layerFadeEnd})");
                }
                else
                {
                    View.Animator.SetLayerWeight(View.UpperLayerIndex, p.UpperLayerWeight);
                }
            }
            return result;
        }

        protected override void Update()
        {
            base.Update();

            if (_layerFadeTime < _layerFadeDuration)
            {
                View.Animator.SetLayerWeight(
                    View.UpperLayerIndex, 
                    Mathf.Lerp(_layerFadeStart, _layerFadeEnd, _layerFadeTime / _layerFadeDuration)
                );
                _layerFadeTime += Time.deltaTime;

                if (_layerFadeTime >= _layerFadeDuration)
                {
                    View.Animator.SetLayerWeight(View.UpperLayerIndex, _layerFadeEnd);
                }
            }
        }


#if UNITY_EDITOR
        [SerializeField, ReadOnly]
        private PlayerState _currentState; 
        private void LateUpdate()
        {
            _currentState = CurrentState.Key;
        }
#endif

        public bool IsAttackState() => CurrentState.Key.IsAttackState();
        public bool IsSkillUsingState() => CurrentState.Key.IsSkillUsingState();

    }
    
}