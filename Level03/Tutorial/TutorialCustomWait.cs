using Sirenix.OdinInspector;
using UnityEngine;

namespace Tutorial
{
    public class TutorialCustomWait : TutorialBase
    {
        [SerializeField, ReadOnly]
        private bool _resolved = false;
        public void Resolve()
        {
            _resolved = true;
        }
        
        public override void Enter()
        {
        }

        public override Result Execute()
        {
            return _resolved ? Result.Done : Result.Running;
        }

        public override void Exit()
        {
        }
    }
}