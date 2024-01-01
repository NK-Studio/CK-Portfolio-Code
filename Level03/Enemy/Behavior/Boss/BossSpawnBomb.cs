using System;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace Enemy.Behavior.Boss
{
    public class BossSpawnBomb : Action
    {

        public SharedInt Count = 3;
        public SharedFloat Delay = 3f;
        public SharedFloat ExplosionTime = 2.25f;

        public SharedBool RunningEndFlag;
        
        private BossYorugami _boss;
        public override void OnStart()
        {
            if (!Owner.TryGetComponent(out _boss))
            {
                Debug.LogWarning("BossCallMethod의 호출자가 Boss가 아님");
                return;
            }

            RunningEndFlag.Value = true;
        }


        public override TaskStatus OnUpdate()
        {
            if (!_boss) return TaskStatus.Failure;

            _boss.SpawnBomb(Count.Value, Delay.Value, ExplosionTime.Value);
            RunningFlagSequence().Forget();
            
            return TaskStatus.Success;
        }

        private async UniTaskVoid RunningFlagSequence()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(Count.Value * Delay.Value + ExplosionTime.Value));
            RunningEndFlag.Value = false;
        }
    }
}