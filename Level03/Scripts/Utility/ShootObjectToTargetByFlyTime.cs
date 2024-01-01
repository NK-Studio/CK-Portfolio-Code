
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using Enemy.Behavior;
using Enemy.Behavior.TurretMonster;
using UnityEngine;

namespace Utility
{
    [TaskDescription("목표에 도달하는 포물선 - 도달시간 기반으로 오브젝트를 발사합니다.")]
    public class ShootObjectToTargetByFlyTime : Action
    {
        
        public SharedGameObject Prefab;
        public SharedGameObject TargetObject;
        public SharedTransform ShootPosition;
        public SharedFloat FlyTime;

        public override TaskStatus OnUpdate()
        {
            var gameObject = GameObject.Instantiate(Prefab.Value, ShootPosition.Value.position, Quaternion.identity);
            var rigidbody = gameObject.GetComponent<Rigidbody>();
            if (!rigidbody)
            {
                DebugX.LogError($"Prefab {Prefab.Value}이 Rigidbody를 가지지 않음!");
                return TaskStatus.Failure;
            }

            var start = gameObject.transform.position;
            var target = TargetObject.Value.transform.position;
            var flyTime = FlyTime.Value;
            
            var projectile = gameObject.GetComponent<TurretMonsterProjectile>();
            if (projectile)
            {
                projectile.Initialize(start, target, flyTime);
            }

            var velocity = GetParabolaShootVelocityByArrivalTime(start, target, flyTime);
            rigidbody.AddForce(velocity, ForceMode.VelocityChange);

            return TaskStatus.Success;
        }

        /// <summary>
        /// start에서 end로부터 가는 포물선에서 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        private static Vector3 GetParabolaShootVelocityByArrivalTime(Vector3 start, Vector3 end, float time)
        {
            return new Vector3(
                (end.x - start.x) / time,
                (end.y - start.y) / time - 0.5f * Physics.gravity.y * time,
                (end.z - start.z) / time
            );
        }
    }
}