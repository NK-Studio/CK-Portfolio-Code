using UnityEngine;
using UnityEngine.Events;

namespace Character.Behaviour.State
{
    public class PlayerItemChangeBehaviour : StateMachineBehaviour
    {
        public bool IsPlaying { get; private set; }
        public UnityAction OnExit;
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            IsPlaying = true;
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            IsPlaying = false;
            OnExit?.Invoke();
        }
    }
}