using System.Threading;
using BehaviorDesigner.Runtime.Tasks;
using Cysharp.Threading.Tasks;

namespace Enemy.Behavior.Boss
{
    [TaskCategory("Boss")]
    [TaskDescription("보스 Aquus의 탄막 패턴을 실행합니다. " +
                     "ForgetTask 시 실행 후 Success를 바로 반환합니다. " +
                     "아닐 시 패턴이 끝날 때 까지 Running으로 대기합니다.")]
    public class BossExecutePhaseTransition : BossExecuteUniTask
    {
        protected override UniTask.Awaiter Execute(CancellationToken _) 
            => Boss.TransitionSequence().GetAwaiter();
    }
}