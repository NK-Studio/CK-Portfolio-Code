using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy.Task
{
    [TaskCategory("Unity/NavMeshAgent")]
    [TaskDescription("Set NavMeshAgent Component Activation.")]
    public class SetNavMeshAgentValid : Conditional
    {
        [BehaviorDesigner.Runtime.Tasks.Tooltip("The GameObject that the task operates on. If null the task GameObject is used.")]
        public SharedGameObject targetGameObject;

        public SharedBool enabled = false;

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
            if (!navMeshAgent)
            {
                return TaskStatus.Success;
            }
            navMeshAgent.enabled = enabled.Value;
            return TaskStatus.Success;
        }
    }
}