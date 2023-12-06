using UnityEngine;

namespace Enemys.WolfBoss {
    public class WolfBossRushStartStateMachineBehavior : StateMachineBehaviour {
        private const string TargetStateName = "RushStart";
        private const string TargetParameter = "RushPrepareCount";
        private static readonly int RushPrepareCount = Animator.StringToHash(TargetParameter);
        private WolfBoss _wolfBoss;


        public override void OnStateEnter(
            Animator animator, AnimatorStateInfo stateInfo, int layerIndex
        ) {
            // RushStart 시 횟수 설정
            if (stateInfo.IsName(TargetStateName)) {
                _wolfBoss = animator.GetComponentInParent<WolfBoss>();
                if(!_wolfBoss) return;
                animator.SetInteger(RushPrepareCount, animator.GetInteger(RushPrepareCount) - 1);
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            // animator.GetNextAnimatorStateInfo()
            // base.OnStateExit(animator, stateInfo, layerIndex);
        }
    }
}