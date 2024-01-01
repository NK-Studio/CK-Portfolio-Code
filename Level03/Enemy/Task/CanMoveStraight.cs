using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy.Task
{
    [TaskDescription("NavMeshAgent가 특정 위치로 직선 이동할 수 있는지 체크합니다.")]
    public class CanMoveStraight : Conditional
    {
        
        [BehaviorDesigner.Runtime.Tasks.Tooltip("The GameObject that the task operates on. If null the task GameObject is used.")]
        public SharedGameObject targetGameObject;

        [BehaviorDesigner.Runtime.Tasks.Tooltip("목표 지점 Transform입니다.")]
        public SharedTransform targetPosition;

        [BehaviorDesigner.Runtime.Tasks.Tooltip("목표 거리입니다. 음수일 경우 적용하지 않습니다.")]
        public SharedFloat targetDistance = -1;
        
        // cache the navmeshagent component
        private NavMeshAgent navMeshAgent;
        private GameObject prevGameObject;
        
        public override void OnStart()
        {
            var currentGameObject = GetDefaultGameObject(targetGameObject.Value);
            if (currentGameObject != prevGameObject) {
                navMeshAgent = currentGameObject.GetComponent<NavMeshAgent>();
                prevGameObject = currentGameObject;
            }
        }

        public override TaskStatus OnUpdate()
        {
            if (navMeshAgent == null) {
                Debug.LogWarning("NavMeshAgent is null");
                return TaskStatus.Failure;
            }

            var target = targetPosition.Value.position;
            // 지정된 거리가 있으면 ...
            if (targetDistance.Value > 0)
            {
                var from = prevGameObject.transform.position;
                var to = targetPosition.Value.position;
                var toTarget = (to - from);
                toTarget.Normalize();
                toTarget *= targetDistance.Value; // 길이 설정
                toTarget += from; // 원점 더하기
                target = toTarget;
            }
            
            // 만약 목표 지점까지의 Raycast가 Edge에 충돌했을 경우
            if (navMeshAgent.Raycast(target, out _))
            {
                // 직선으로는 못 가는 거니까 실패 !!
                return TaskStatus.Failure;
            }
            
            // Raycast 실패 = false 반환 : Ray 쐈을 때 Edge에 충돌하지 않음
            // == 직선으로 갈 수 있음 !!

            return TaskStatus.Success;
        }

        public override void OnReset()
        {
            targetGameObject = null;
        }
    }
}