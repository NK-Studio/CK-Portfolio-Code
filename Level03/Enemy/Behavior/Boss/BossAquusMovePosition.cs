using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace Enemy.Behavior.Boss
{
    [TaskCategory("Boss")]
    [TaskDescription("보스 Aquus의 사라짐 / 등장을 실행합니다.")]
    public class BossAquusMovePosition : Action
    {
        public SharedBool ForgetTask = false;
        
        private BossAquus _boss;
        private UniTask.Awaiter _awaiter;
        public override void OnStart()
        {
            if (!Owner.TryGetComponent(out _boss))
            {
                Debug.LogWarning("BossAquusMovePosition의 호출자가 Boss가 아님", gameObject);
                return;
            }

            // var task = _boss.PlayDisappearSequence();
            // _awaiter = task.GetAwaiter();
        }


        public override TaskStatus OnUpdate()
        {
            if (!_boss) return TaskStatus.Failure;

            if (ForgetTask.Value)
            {
                return TaskStatus.Success;
            }
            return _awaiter.IsCompleted ? TaskStatus.Success : TaskStatus.Running;
        }
    }
}