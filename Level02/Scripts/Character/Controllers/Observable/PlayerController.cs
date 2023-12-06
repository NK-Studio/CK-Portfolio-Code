using System;
using Character.View;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Assertions;
using Utility;
using EHookState = Character.USystem.Hook.Model.EHookState;

namespace Character.Controllers
{
    //옵저버 함수만 정의
    [RequireComponent(typeof(PlayerView))]
    public partial class PlayerController : MonoBehaviour
    {
        private IObservable<Vector3> UpdateAnimationMove() =>
            this.UpdateAsObservable().Select(_ => GetVelocity());

        private IObservable<Unit> ThrowingAnimationEnterObservable()
        {
            return _stateMachineTrigger.OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("Throwing")).Select(_ => Unit.Default);
        }

        private IObservable<Unit> HookShotAnimationEnterObservable()
        {
            return _stateMachineTrigger.OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("HookShotStarting")).Select(_ => Unit.Default);
        }
        
        private IObservable<Unit> PullingAnimationEnterObservable() => _stateMachineTrigger.OnStateEnterAsObservable()
            .Where(info => info.StateInfo.IsName("Pulling") || info.StateInfo.IsName("RopeFailStart"))
            .Select(_ => Unit.Default);

        private IObservable<Unit> MoveFlyingAnimationEnterObservable() => _stateMachineTrigger
            .OnStateEnterAsObservable()
            .Where(info => info.StateInfo.IsName("MoveFlying")).Select(_ => Unit.Default);


        private IObservable<Unit> PullEndAnimationExitObservable() => _stateMachineTrigger.OnStateExitAsObservable()
            .Where(info => info.StateInfo.IsName("PullEnd") || info.StateInfo.IsName("PullFailEnd"))
            .Select(_ => Unit.Default);

        private IObservable<Unit> HookShotEndAnimationExitObservable() => _stateMachineTrigger.OnStateExitAsObservable()
            .Where(info => info.StateInfo.IsName("HookShotEnd")).Select(_ => Unit.Default);

        private IObservable<EHookState> StopHandJellyObservable() => _hookSystemModel.HookStateObservable
            .Where(state => state == EHookState.Stop);

        //훅이 다시 되돌아와서 Idle이 되었을 경우
        private IObservable<EHookState> HookStateExitObservable() => _hookSystemModel.HookStateObservable
            .Where(_ => _playerModel.OtherState == EOtherState.ThrowRope) //플레이어는 훅을 던지고 있는 상태였고,
            .Where(state => state == EHookState.Idle);

        private IObservable<Unit> FlyToTargetObservable() => this.FixedUpdateAsObservable()
            .Where(_ => _playerModel.OtherState is EOtherState.ThrowRope or EOtherState.HookShotFlying)
            .Where(_ => _hookSystemModel.HookState == EHookState.BackOrMoveTarget)
            .Where(_ => _playerModel.RopeState == ERopeState.MoveToTarget);
        
        /// <summary>
        /// 훅샷 포인트를 찾습니다.
        /// </summary>
        /// <returns></returns>
        private IObservable<Unit> FindHookShotPoint() => this.UpdateAsObservable();

        /// <summary>
        /// 훅샷 상태 옵저버
        /// </summary>
        /// <returns></returns>
        private IObservable<Unit> HookShotFlyStateObserver() => this.FixedUpdateAsObservable()
            .Where(_ => _playerModel.OtherState == EOtherState.HookShotFlying);

        /// <summary>
        /// 훅샷을 위해 회전합니다.
        /// </summary>
        /// <returns></returns>
        private IObservable<Unit> HookShotRotateStateObserver() => this.FixedUpdateAsObservable()
            .Where(_ => _playerModel.OtherState == EOtherState.HookShotRotate);
    }
}