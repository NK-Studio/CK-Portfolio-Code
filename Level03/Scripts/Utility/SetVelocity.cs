using UnityEngine;
using UnityEngine.AI;

namespace BehaviorDesigner.Runtime.Tasks.Unity.UnityNavMeshAgent
{
    [TaskCategory("Unity/NavMeshAgent")]
    [TaskDescription("Sets the maximum acceleration of an agent as it follows a path, given in units / sec^2. Returns Success.")]
    public class SetVelocity : Action
    {
        [Tooltip("The GameObject that the task operates on. If null the task GameObject is used.")]
        public SharedGameObject targetGameObject;
        [Tooltip("The NavMeshAgent acceleration")]
        public SharedVector3 velocity;

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

            navMeshAgent.velocity = velocity.Value;

            return TaskStatus.Success;
        }

        public override void OnReset()
        {
            targetGameObject = null;
            velocity = Vector3.zero;
        }
    }
}
