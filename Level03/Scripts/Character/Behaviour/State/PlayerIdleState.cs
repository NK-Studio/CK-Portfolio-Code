using Character.Core.FSM;
using Character.Model;
using EnumData;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace Character.Behaviour.State
{
    [PlayerFSMState(PlayerState.Idle)]
    public class PlayerIdleState : PlayerFSMState
    {
        private float _idleTime;
        public PlayerIdleState(PlayerFSM player) : base(player, 0f)
        {
            var observableStateMachineTrigger 
                = View.CurrentAnimator().GetBehaviour<ObservableStateMachineTrigger>();

            // 기본 상태가 되면 초기화합니다. // TODO 없앨까 생각중
            observableStateMachineTrigger.OnStateEnterAsObservable().Where(info =>
                    info.StateInfo.IsName("Idle") || info.StateInfo.IsName("Run")
                )
                .Where(_ => !IsCurrentState)
                .Subscribe(_ => Player.ChangeState(PlayerState.Idle))
                .AddTo(Player);
            
            
        }

        public override void OnStart()
        {
            base.OnStart();
            View.OnTriggerAnimation(PlayerAnimation.Behaviour, 0);
            View.NavMeshAgent.isStopped = false;
            View.SetTimeScale(1f, false);
            Model.IsStop = false;

            
            if (PreviousState is PlayerState.Hammer /*or PlayerState.Dash*/ or PlayerState.Stun)
            {
                // Log($"PrevState: {PreviousState.ToString()} -> goto idle");
                var stateName = View.NavMeshAgent.desiredVelocity.sqrMagnitude >= Vector3.kEpsilon ? "Run" : "Idle";
                View.Animator.CrossFade(stateName, 0.25f, 0);
            }

            _idleTime = 0f;
            View.IdleDialogEvent.ResetEvent();

        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (View.IsMovePressed() || Model.DisabledInput != PlayerModel.InputType.None)
            {
                if (_idleTime > 0f)
                {
                    View.IdleDialogEvent.ResetEvent();
                }
                _idleTime = 0f;
            }
            else
            {
                _idleTime += Time.deltaTime;

                if (_idleTime >= 1f)
                {
                    View.IdleDialogEvent.CallEvent();
                    _idleTime = 0f;
                }
            }
            
            // Idle 상태로 전이 시 탄창 없으면 자동 장전
            if (Model.Magazine.Ammo <= 0)
            {
                if (Model.Magazine.CanReload)
                {
                    Player.ChangeState(PlayerState.PlayerBulletReload);
                    return;
                }else
                {
                    Model.DefaultMagazine.Reset(fullAmmo: false);
                    Model.Magazine = null;
                    Player.ChangeState(PlayerState.PlayerBulletReload);
                    return;
                }
            }
            else if (View.GetInput().IsTriggerPressing 
                     && Model.CanInput(PlayerModel.InputType.Attack)
                     && Model.Health > 0f 
                     && Model.Magazine.Ammo > 0 
                     && Model.Magazine.CoolTime <= 0f)
            {
                Player.ChangeState(PlayerState.Shoot);
                return;
            }
        }

        public override void OnEnd()
        {
            base.OnEnd();
            _idleTime = 0f;
            View.IdleDialogEvent.ResetEvent();
        }
    }
}