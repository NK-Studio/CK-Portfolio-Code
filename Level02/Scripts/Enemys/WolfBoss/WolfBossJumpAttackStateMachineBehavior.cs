using UnityEngine;

namespace Enemys.WolfBoss {
    public class WolfBossJumpAttackStateMachineBehavior : StateMachineBehaviour {
        private const string TargetStateName = "JumpAttack02";
        private WolfBoss _wolfBoss;
        
        public override void OnStateEnter(
            Animator animator, AnimatorStateInfo stateInfo, int layerIndex
        ) {
            // JumpAttack02 들어갈 때 포물선 결정
            if (stateInfo.IsName(TargetStateName)) {
                _wolfBoss = animator.GetComponentInParent<WolfBoss>();
                if(!_wolfBoss) return;
                Transform t = _wolfBoss.transform;
                Vector3 origin = t.position;
                // 점프 공격 오프셋만큼 뺀 위치를 목표 플레이어 위치로 삼음
                Vector3 jumpTargetPosition = _wolfBoss.GetPlayerTransform().position - t.TransformVector(_wolfBoss.Settings.JumpAttackOffset);
                jumpTargetPosition.y = origin.y;
                _wolfBoss.JumpAttackParabolaInfo = new ParabolaInfo(
                    origin,
                    jumpTargetPosition, 
                    _wolfBoss.Settings.JumpAttackMaxHeight
                );

                if (!_wolfBoss.JumpAttackParabolaInfo.Valid) {
                    return;
                }
            }
        }

        public override void OnStateExit(
            Animator animator, AnimatorStateInfo stateInfo, int layerIndex
        ) {
            // JumpAttack02 벗어날 때 공격 판정
            if (stateInfo.IsName(TargetStateName)) {
                if(!_wolfBoss) return;
                _wolfBoss.JumpAttack();
            }
        }

        public override void OnStateUpdate(
            Animator animator, AnimatorStateInfo stateInfo, int layerIndex
        ) {
            // JumpAttack02 상태 매 프레임마다 포물선상 위치로 업데이트
            if (stateInfo.IsName(TargetStateName)) {
                if(!_wolfBoss) return;
                if(!_wolfBoss.JumpAttackParabolaInfo.Valid) return;
                var t = stateInfo.normalizedTime;
                _wolfBoss.transform.position = _wolfBoss.JumpAttackParabolaInfo.GetPosition(t);
            }
        }

    }
}