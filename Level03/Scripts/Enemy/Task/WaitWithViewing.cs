using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Utility;

namespace Enemy.Task
{
    [TaskDescription("NavMeshAgent가 특정 지점을 바라보며 Wait합니다.")]
    public class WaitWithViewing : Wait
    {
        public SharedTransform Target;
        
        private NavMeshAgent _agent;
        public override void OnStart()
        {
            base.OnStart();
            _agent = GetComponent<NavMeshAgent>();
        }

        public override TaskStatus OnUpdate()
        {
            var result = base.OnUpdate();
            if (result == TaskStatus.Running)
            {
                _agent.LookTowards(Target.Value.position);
            }
            return result;
        }
    }
}