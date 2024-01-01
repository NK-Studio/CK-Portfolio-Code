using UnityEngine;

namespace Tutorial
{
    public class TutorialDelay : TutorialBase
    {
        [SerializeField] private float _seconds = 0.3f; 
        
        private float _delay;
        private bool _infinite;
        public override void Enter()
        {
            _delay = _seconds;
            _infinite = !float.IsNormal(_delay);
        }

        public override Result Execute()
        {
            if (_infinite)
            {
                return Result.Running;
            }
            _delay -= Time.deltaTime;

            if (_delay <= 0f)
            {
                return Result.Done;
            }

            return Result.Running;
        }

        public override void Exit()
        {
            
        }
    }
}