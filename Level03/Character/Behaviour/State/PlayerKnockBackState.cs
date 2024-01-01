using Character.Core.FSM;
using EnumData;
using UniRx;
using UniRx.Triggers;

namespace Character.Behaviour.State
{
    [PlayerFSMState(PlayerState.KnockBack)]
    public class PlayerKnockBackState : PlayerFSMState
    {
        public PlayerKnockBackState(PlayerFSM player) : base(player, 0f)
        {
            
        }

        public override void OnStart()
        {
            base.OnStart();
            View.OnExitSkillPrepareState(Model);
            // Model.InvincibleTime = Settings.HitKnockBackInvincibleTime;
            View.OnTriggerAnimation(PlayerAnimation.KnockBack);
            View.CameraRandomShake(Settings.HitCameraShake);
        }

        /*
        public override void OnUpdate()
        {
            var animator = View.Animator;
            var current = animator.GetCurrentAnimatorStateInfo(0);
            var next = animator.GetNextAnimatorStateInfo(0);
            if (current.IsName("Idle") && !next.IsName("KnockBack"))
            {
                Player.ChangeState(PlayerState.Idle);
            }
        }
        */

        public override bool OnNext(FSMState<PlayerFSM, PlayerState> nextState)
            => nextState.Key == PlayerState.Idle;
    }
}