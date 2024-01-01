using UnityEngine;
using UnityEngine.Events;

namespace Tutorial
{
    public class TutorialCallUnityEvent : TutorialBase
    {
        public UnityEvent OnExecuted;

        public override void Enter()
        {
            
        }

        public override Result Execute()
        {
            OnExecuted?.Invoke();
            return Result.Done;
        }

        public override void Exit()
        {
            
        }
    }
}