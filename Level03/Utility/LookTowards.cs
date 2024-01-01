using UnityEngine;
using Utility;

namespace BehaviorDesigner.Runtime.Tasks.Unity.SharedVariables
{
    [TaskDescription("목표 방향으로 회전합니다.")]
    public class LookTowards : Action
    {
        public SharedTransform Target;
        public SharedFloat AngularSpeed = 120f;
        public SharedFloat EpsilonAngle = 5f;

        private float _angleInCos;
        private Vector3 _target;
        public override void OnStart()
        {
            _target = Target.Value.position;
            _angleInCos = Mathf.Cos(EpsilonAngle.Value * Mathf.Deg2Rad);
        }

        public override TaskStatus OnUpdate()
        {
            transform.LookTowards(_target, AngularSpeed.Value);

            var forward = transform.forward;
            forward.y = 0f; forward.Normalize();
            var toTarget = _target - transform.position;
            var direction = toTarget;
            direction.y = 0f; direction.Normalize();

            if (Vector3.Dot(forward, direction) < _angleInCos)
            {
                return TaskStatus.Running;
            }
            
            return TaskStatus.Success;
        }
    }
}