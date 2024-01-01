using System;
using Character.Core.FSM;
using Character.Model;
using EnumData;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace Character.Behaviour.State
{
    [PlayerFSMState(PlayerState.PlayerBulletReload)]
    public class PlayerReloadState : PlayerFSMState
    {
        public PlayerReloadState(PlayerFSM player) : base(player, 1f)
        {
            Func<Unit, bool> reloadTransitionCondition = _ => 
                Model.Health > 0f 
                && Model.Magazine.CanReload 
                && Model.Magazine.Ammo < Model.Magazine.MaxAmmo
                && Model.CanInput(PlayerModel.InputType.Reload);
            
            // 재장전 키 입력 시 상태 전이
            View.ReloadKeyDownObservable()
                .Where(reloadTransitionCondition)
                .Subscribe(_ => Player.ChangeState(PlayerState.PlayerBulletReload))
                .AddTo(Player);

            // 상호작용 키 입력 시 주변 아이템 없으면 재장전 시도
            // 사유: 게임패드 키 조작 이슈
            View.InteractionKeyDownObservable()
                .Where(_ => Model.NearestItem == null)
                .Where(reloadTransitionCondition)
                .Subscribe(_ => Player.ChangeState(PlayerState.PlayerBulletReload))
                .AddTo(Player);

        }

        public override void OnStart()
        {
            base.OnStart();
            Model.Magazine.ReloadTime = Model.Magazine.ReloadTimeDuration;
            View.OnTriggerAnimation(PlayerAnimation.Reload);
        }

        public override void OnUpdate()
        {
            if (Model.Magazine.ReloadTime <= 0f)
            {
                Model.Magazine.Ammo = Model.Magazine.MaxAmmo;
                Player.ChangeState(PlayerState.Idle);
                return;
            }
            Model.Magazine.ReloadTime -= Time.deltaTime;
        }

        public override bool OnNext(FSMState<PlayerFSM, PlayerState> nextState)
        {
            if (nextState.Key == PlayerState.Idle)
            {
                // 재장전 중에는 Idle 전환 불가능
                if (Model.Magazine.ReloadTime > 0f)
                {
                    return false;
                }
                View.OnTriggerAnimation(PlayerAnimation.ReloadEnd);
            }
            return nextState.Key 
                is PlayerState.Idle 
                or PlayerState.Hammer 
                or PlayerState.Dash
                or PlayerState.Stun
                or PlayerState.KnockBack
                or PlayerState.Dead;
        }

        public override void OnEnd()
        {
            base.OnEnd();
            
            View.ResetTrigger(PlayerAnimation.Reload);
        }
    }
}