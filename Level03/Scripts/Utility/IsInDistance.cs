using UnityEngine;
using Utility;

namespace BehaviorDesigner.Runtime.Tasks.Unity.SharedVariables
{
    [TaskDescription("타겟이 범위 안에 있는지 체크합니다.")]
    public class IsInDistance : Conditional
    {
        public SharedTransform Target;
        public SharedFloat Distance;

        public bool Inverse;

        public override TaskStatus OnUpdate()
        {
            if (Target.Value)
            {
                if (Vector3.Distance(transform.position, Target.Value.position) <= Distance.Value)
                {
                    //범위 안에 있을 경우
                    if (!Inverse)
                        return TaskStatus.Success;

                    return TaskStatus.Failure;
                }
            }
            else
                Debug.LogWarning("타겟이 비어있습니다.");

            //범위 안에 밖에 있을 경우
            if (!Inverse)
                return TaskStatus.Failure;

            return TaskStatus.Success;
        }

        public override void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, Distance.Value);
        }
    }
    [TaskDescription("타겟이 범위 안에 있는지 체크합니다. XZ평면에 투영합니다.")]
    public class IsInDistanceHorizontally : IsInDistance
    {
        public override TaskStatus OnUpdate()
        {
            if (Target.Value)
            {
                var a = transform.position; a.y = 0;
                var b = Target.Value.position; b.y = 0;
                if (Vector3.Distance(a, b) <= Distance.Value)
                {
                    //범위 안에 있을 경우
                    if (!Inverse)
                        return TaskStatus.Success;

                    return TaskStatus.Failure;
                }
            }
            else
                Debug.LogWarning("타겟이 비어있습니다.");

            //범위 안에 밖에 있을 경우
            if (!Inverse)
                return TaskStatus.Failure;

            return TaskStatus.Success;
        }

        public override void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            DrawUtility.DrawCircle(transform.position, Distance.Value, Vector3.up, 16, Gizmos.DrawLine); 
        }
    }
}