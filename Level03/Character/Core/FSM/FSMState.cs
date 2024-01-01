using System;
using EnumData;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace Character.Core.FSM
{
    [AttributeUsage(AttributeTargets.Class)]
    public class FSMStateAttribute : Attribute
    {
        public readonly int Key;

        public FSMStateAttribute(int key)
        {
            Key = key;
        }
    }


    public abstract class FSMState<T, TKey> where T : IFSMEntity where TKey : Enum
    {
        public TKey Key;
        protected readonly T OwnerEntity;

        /// <summary>
        /// 상태가 시작될 때 호출됩니다.
        /// </summary>
        public virtual void OnStart() { }
        /// <summary>
        /// 현재 상태일 때 매 프레임마다 호출됩니다.
        /// </summary>
        public virtual void OnUpdate() { }
        /// <summary>
        /// 현재 상태일 때 매 FixedUpdate 시기마다 호출됩니다.
        /// </summary>
        public virtual void OnFixedUpdate() { }
        /// <summary>
        /// 현재 상태에서 다른 상태로 넘어갈 때 호출됩니다.
        /// </summary>
        /// <param name="nextState">다음 상태입니다.</param>
        /// <returns>true 반환 시 다음 상태로 넘어갑니다. false 반환 시 넘어가지 않습니다.</returns>
        public virtual bool OnNext(FSMState<T, TKey> nextState) => true;
        /// <summary>
        /// 현재 상태가 다른 상태로 넘어가 종료될 때 호출됩니다.
        /// </summary>
        public virtual void OnEnd() { }

        public FSMState(IFSMEntity owner)
        {
            OwnerEntity = (T)owner;
        }

    }
}