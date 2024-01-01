using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace Enemy.Behavior.Boss
{
    [TaskCategory("Boss")]
    [TaskDescription("보스 Aquus Bullet Spawner의 이펙트 일시정지를 해제합니다.")]
    public class BossAquusResumeSpawner : Action
    {
        private BossAquus _boss;
        public override void OnStart()
        {
            if (!Owner.TryGetComponent(out _boss))
            {
                Debug.LogWarning("BossAquusResumeSpawner의 호출자가 Boss가 아님", gameObject);
                return;
            }
        }


        public override TaskStatus OnUpdate()
        {
            if (!_boss) return TaskStatus.Failure;
            _boss.ResumeBulletSpawner();
            return TaskStatus.Success;
        }
    }
}