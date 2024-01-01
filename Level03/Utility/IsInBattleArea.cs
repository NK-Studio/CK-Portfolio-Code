using Enemy;
using UnityEngine;

namespace BehaviorDesigner.Runtime.Tasks.Unity.SharedVariables
{
    [TaskDescription("타겟이 지정된 전투 구역 안에 있는지 체크합니다.")]
    [TaskCategory("Battle Area")]
    public class IsInBattleArea : Conditional
    {
        [Tooltip("목표 전투 구역입니다.")]
        public SharedBattleArea Target;

        [Tooltip("이 속성을 체크하면 전투 구역 바깥에 있는지를 체크합니다.")]
        public bool Inverse;

        public override TaskStatus OnUpdate()
        {
            TaskStatus result;
            if (!Target.Value)
            {
                result = TaskStatus.Failure;
                Debug.LogWarning("타겟 전투 구역이 비어있습니다.", Owner.gameObject);
            }
            else
            {
                result = Target.Value.Contains(Owner.transform.position) ? TaskStatus.Success : TaskStatus.Failure;
            }

            if (Inverse)
            {
                result = result == TaskStatus.Success ? TaskStatus.Failure : TaskStatus.Success;
            }
            return result;
        }
    }
}