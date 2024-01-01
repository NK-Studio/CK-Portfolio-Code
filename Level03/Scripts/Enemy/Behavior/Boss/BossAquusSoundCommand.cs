using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace Enemy.Behavior.Boss
{
    [TaskCategory("Boss")]
    [TaskDescription("보스 Aquus의 ExecuteSoundCommand()를 호출합니다.")]
    public class BossAquusExecuteSoundCommand : Action
    {
        public BossAquus.SoundCommandType Command;
        
        private BossAquus _boss;
        public override void OnStart()
        {
            if (!Owner.TryGetComponent(out _boss))
            {
                Debug.LogWarning("BossAquusExecuteSoundCommand의 호출자가 Boss가 아님", gameObject);
                return;
            }
        }


        public override TaskStatus OnUpdate()
        {
            if (!_boss) return TaskStatus.Failure;
            _boss.ExecuteSoundCommand(Command);
            return TaskStatus.Success;
        }
    }
}