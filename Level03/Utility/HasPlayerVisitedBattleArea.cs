using BehaviorDesigner.Runtime.Tasks;
using Enemy;
using UnityEngine;

namespace Utility
{
    [TaskDescription("지정된 전투 구역에 플레이어가 들어왔는지 확인합니다.")]
    [TaskCategory("Battle Area")]
    public class HasPlayerVisitedBattleArea : Conditional
    {
        [BehaviorDesigner.Runtime.Tasks.Tooltip("목표 전투 구역입니다.")]
        public SharedBattleArea Target;

        [BehaviorDesigner.Runtime.Tasks.Tooltip("이 속성을 체크하면 반환값이 반대가 됩니다.")]
        public bool Inverse;
        
        public override TaskStatus OnUpdate()
        {
            if (Target.Value == null)
            {
                DebugX.LogWarning("전투 구역이 지정되지 않았습니다.", Owner.gameObject);
                return TaskStatus.Failure;
            }
            
            bool visited = Target.Value.HasPlayerVisited;
            if (Inverse) visited = !visited;
            return visited ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}