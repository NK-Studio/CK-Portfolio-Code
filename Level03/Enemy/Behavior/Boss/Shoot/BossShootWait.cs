using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Enemy.Behavior.Boss
{
    [CreateAssetMenu(fileName = "Wait", menuName = "Settings/Boss/Wait", order = 0)]
    public class BossShootWait : BossShootSubPattern
    {
        [field: SerializeField]
        public float Time { get; private set; } = 5f;
        
        public override async UniTask Execute(BossAquus boss, CancellationToken token)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(Time), cancellationToken: token);
        }
    }
}