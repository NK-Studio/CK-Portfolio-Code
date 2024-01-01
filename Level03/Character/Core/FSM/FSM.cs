using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;
using Logger = NKStudio.Logger;

namespace Character.Core.FSM
{
    public interface IFSMEntity
    {
        public GameObject gameObject { get; }
        public Transform transform { get; }
    }
    
    public abstract class FSM<T, TKey> : MonoBehaviour where T : IFSMEntity where TKey : Enum
    {
        [field: SerializeField, BoxGroup("디버그")]
        public bool ShowStateChangeLog { get; set; } = false;
        
        private Dictionary<int, FSMState<T, TKey>> _states;

        public FSMState<T, TKey> CurrentState { get; private set; } = null;

        private Subject<FSMState<T, TKey>> _onCurrentStateChange;
        public IObservable<FSMState<T, TKey>> CurrentStateObservable 
            => _onCurrentStateChange ??= new Subject<FSMState<T, TKey>>();
        public FSMState<T, TKey> PreviousState { get; private set; } = null;
        public FSMState<T, TKey> GlobalState { get; private set; } = null;

        protected virtual void Update()
        {
            GlobalState?.OnUpdate();
            CurrentState?.OnUpdate();
        }

        protected virtual void FixedUpdate()
        {
            GlobalState?.OnFixedUpdate();
            CurrentState?.OnFixedUpdate();
        }

        /// <summary>
        /// 상태 클래스를 리플렉션으로 가져와 초기화합니다.
        /// </summary>
        /// <param name="firstState">FSM의 초기 상태입니다.</param>
        protected void SetupStates(ValueType firstState)
        {
            _states = new Dictionary<int, FSMState<T, TKey>>();
            // FSMState<T>의 자식 클래스 가져오기
            var stateTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(FSMState<T, TKey>) != t && typeof(FSMState<T, TKey>).IsAssignableFrom(t));

            foreach (var stateType in stateTypes)
            {
                // Attribute 기반 찾기
                var attribute = stateType.GetCustomAttribute<FSMStateAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                // 클래스 인스턴스 생성
                var state = Activator.CreateInstance(stateType, this as IFSMEntity) as FSMState<T, TKey>;
                if (!_states.TryAdd(attribute.Key, state))
                {
                    Debug.LogError($"{typeof(T)} 의 {attribute.Key} 키가 중복되었습니다.");
                }

                state.Key = (TKey)Enum.ToObject(typeof(TKey), attribute.Key);
            }

            // 초기 상태로 전이
            ChangeState(firstState);
        }

        /// <summary>
        /// 특정 상태로 전이합니다.
        /// </summary>
        /// <param name="enumValue"></param>
        public virtual bool ChangeState(ValueType enumValue, bool forced = false)
        {
            if (!_states.TryGetValue((int)enumValue, out var state))
            {
                Logger.LogError($"{GetType()} : 사용할 수 없는 상태입니다. {enumValue}");
                return false;
            }

            return ChangeState(state, forced);
        }

        private void Log(string message)
        {
            if (ShowStateChangeLog) Logger.Log(message);
        }
        // 내부 전이 로직
        protected virtual bool ChangeState(FSMState<T, TKey> newState, bool forced = false)
        {
            if (newState == null) return false;

            if (CurrentState != null)
            {
                // 넘어가는 거 거부 시 안 넘어감
                if (!CurrentState.OnNext(newState) && !forced)
                {
                    Log($"{this.GetType().Name} : <color=yellow>{CurrentState.GetType().Name}</color> =X <color=red>{newState.GetType().Name}</color> FAILED");
                    return false;
                }
                PreviousState = CurrentState;
                CurrentState.OnEnd();

                Log($"{this.GetType().Name} : <color=yellow>{CurrentState.GetType().Name}</color> => <color=green>{newState.GetType().Name}</color>");
            }
            else
            {
                Log($"{this.GetType().Name} : FSM Start with <color=green>{newState.GetType().Name}</color>");
            }
            CurrentState = newState;
            _onCurrentStateChange?.OnNext(CurrentState);
            CurrentState.OnStart();

            return true;
        }

        // 전역 상태
        public void SetGlobalState(FSMState<T, TKey> newState)
        {
            GlobalState?.OnEnd();
            GlobalState = newState;
            GlobalState?.OnStart();
        }

        // 이전 상태로 회귀
        public void RevertToPreviousState()
        {
            ChangeState(PreviousState);
        }
    }
}