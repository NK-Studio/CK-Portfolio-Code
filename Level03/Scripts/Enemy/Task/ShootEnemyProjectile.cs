using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using EnumData;
using UnityEngine;

namespace Enemy.Task
{
    [TaskDescription("투사체를 발사합니다.")]
    public class ShootEnemyProjectile : Action
    {
        public SharedGameObject Prefab;
        public SharedTransform ShootPosition;
        public SharedTransform Target;
        public SharedFloat Speed;
        public SharedFloat Damage = 10f;
        public DamageReaction Reaction = DamageReaction.Stun;

        public override TaskStatus OnUpdate()
        {
            var projectileObject = GameObject.Instantiate(Prefab.Value, ShootPosition.Value.position, Quaternion.identity);
            if (projectileObject.TryGetComponent(out EnemyProjectile projectile))
            {
                var direction = Target.Value.position - ShootPosition.Value.position;
                direction.y = 0f; direction.Normalize();
                projectile.Initialize(direction, Speed.Value, Owner.gameObject, () => Damage.Value, () => Reaction);
            }
            return TaskStatus.Success;
        }
    }
}