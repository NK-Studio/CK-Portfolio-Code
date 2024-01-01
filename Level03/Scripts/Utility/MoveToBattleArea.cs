using BehaviorDesigner.Runtime.Tasks;
using BehaviorDesigner.Runtime.Tasks.Movement;
using Enemy;
using UnityEngine;

namespace Utility
{
    [TaskDescription("전투 구역으로 Unity NavMesh를 이용해 이동합니다.")]
    [TaskCategory("Battle Area")]
    [TaskIcon("Assets/Behavior Designer Movement/Editor/Icons/{SkinColor}SeekIcon.png")]
    public class MoveToBattleArea : NavMeshMovement
    {
        [BehaviorDesigner.Runtime.Tasks.Tooltip("목표 전투 구역입니다.")]
        public SharedBattleArea target;

        private Vector3 _targetPosition;
        public override void OnStart()
        {
            base.OnStart();

            if (target.Value != null)
            {
                _targetPosition = target.Value.CenterPoint;
                SetDestination(_targetPosition);    
            }else
                DebugX.LogWarning("MoveToBattleArea에 target이 비어있습니다.", Owner.gameObject);
        }

        public override TaskStatus OnUpdate()
        {
            if (target.Value == null)
                return TaskStatus.Failure;

            if (target.Value.Contains(transform.position) || HasArrived()) {
                return TaskStatus.Success;
            }
            
            return TaskStatus.Running;
        }
        
        private Vector3 Target()
        {
            return target.Value.CenterPointOnNavMesh;
        }

        public override void OnReset()
        {
            base.OnReset();
            target = null;
        }
    }
}