using Character.Core.FSM;
using EnumData;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace Character.Behaviour.State
{
    [PlayerFSMState(PlayerState.Stun)]
    public class PlayerStunState : PlayerFSMState
    {
        public PlayerStunState(PlayerFSM player) : base(player, 0f)
        {
            
        }

        public override void OnStart()
        {
            base.OnStart();
            View.OnExitSkillPrepareState(Model);
            Model.InvincibleTime = Settings.HitStunInvincibleTime;
            Model.CanInterruptSkill = false;
            View.OnTriggerAnimation(PlayerAnimation.Stun);
            View.CameraRandomShake(Settings.HitCameraShake);
        }

        
        public override void OnUpdate()
        {
            /*
            var animator = View.Animator;
            var current = animator.GetCurrentAnimatorStateInfo(0);
            var next = animator.GetNextAnimatorStateInfo(0);
            if (current.IsName("Idle") && !next.IsName("Stun"))
            {
                Player.ChangeState(PlayerState.Idle);
            }
            */
        }
        
 
        public override bool OnNext(FSMState<PlayerFSM, PlayerState> nextState)
            => nextState.Key == PlayerState.Idle || Model.CanInterruptSkill;

        public override void OnEnd()
        {
            base.OnEnd();
            Model.CanInterruptSkill = false;
        }
    }
}