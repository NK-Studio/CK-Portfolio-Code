using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Enemy.Behavior.Boss
{
    public abstract class BossShootSubPattern : ScriptableObject
    {
        public abstract UniTask Execute(BossAquus boss, CancellationToken token);
    }
}