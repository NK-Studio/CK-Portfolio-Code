using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Enemy.Behavior.Boss
{
    [CreateAssetMenu(fileName = "ShootPattern", menuName = "Settings/Boss/Shoot Pattern", order = 0)]
    public class BossShootPattern : ScriptableObject
    {
        [field: SerializeField]
        public List<BossShootSubPattern> Patterns { get; private set; } = new();
        
        public async UniTask Execute(BossAquus boss, CancellationToken token)
        {
            foreach (var pattern in Patterns)
            {
                await pattern.Execute(boss, token);
            }
        }
        
    }
}