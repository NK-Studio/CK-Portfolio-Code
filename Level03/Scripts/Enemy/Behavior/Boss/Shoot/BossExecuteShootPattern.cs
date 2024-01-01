using System.Threading;
using BehaviorDesigner.Runtime.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Enemy.Behavior.Boss
{
    [TaskCategory("Boss")]
    [TaskDescription("보스 Aquus의 탄막 패턴을 실행합니다. " +
                     "ForgetTask 시 실행 후 Success를 바로 반환합니다. " +
                     "아닐 시 패턴이 끝날 때 까지 Running으로 대기합니다.")]
    public class BossExecuteShootPattern : BossExecuteUniTask
    {
        public BossShootPattern Pattern;
        
        public override void OnAwake()
        {
            base.OnAwake();
            if (!Pattern)
            {
                Debug.LogWarning($"BossExecuteShootPattern({Pattern.name}) init but no pattern", gameObject);
            }
        }

        protected override UniTask.Awaiter Execute(CancellationToken token) 
            => Pattern.Execute(Boss, token).GetAwaiter();
    }
}