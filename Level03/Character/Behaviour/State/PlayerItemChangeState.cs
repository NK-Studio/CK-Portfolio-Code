using Character.Core.FSM;
using Character.Model;
using EnumData;
using Level;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Utility;

namespace Character.Behaviour.State
{
    [PlayerFSMState(PlayerState.BulletChange)]
    public class PlayerItemChangeState : PlayerFSMState
    {

        private PlayerItemChangeBehaviour _animationBehaviour;
        public PlayerItemChangeState(PlayerFSM player) : base(player, 0f)
        {
            _animationBehaviour = View.Animator.GetBehaviour<PlayerItemChangeBehaviour>();
            
            Player.UpdateAsObservable()
                .Subscribe(_ => UpdateInteraction())
                .AddTo(Player);
            
            View.InteractionKeyDownObservable()
                .Subscribe(_ => OnInteraction())
                .AddTo(Player);
            
            Model.OnMagazineChanged += (oldMag, newMag) =>
            {
                if(newMag.Settings == Model.DefaultMagazine.Settings) return; 
                if(!Model.StatusLoaded) return;
                Player.ChangeState(PlayerState.BulletChange);
            };
        }

        /// <summary>
        /// 아이템 변경 시작 시 정지 상태에서는 0, 이동 상태에서는 1로 전환합니다.
        /// </summary>
        // public override float UpperLayerWeight => Behaviour.IsActuallyStopped(View, Model) ? 0f : 1f;
        public override float UpperLayerWeight => 1f;
        // 즉시 전환
        public override float GetWeightCrossFadeTime(PlayerState previous) => 0f;

        private bool TryGetNearestItem(out IItem nearestItem)
        {
            var sensor = View.InteractionRangeSensor;
            sensor.Pulse();

            // 가장 가까운 아이템 상호작용
            Vector3 origin = transform.position;
            nearestItem = null;
            float nearestDistanceSquared = float.NaN;
            foreach (var obj in sensor.Detections)
            {
                if (!obj.TryGetComponent(out IItem i))
                {
                    continue;
                }

                if (!i.CanBeNearestItem(Presenter))
                {
                    continue;
                }

                float distanceSquared = origin.DistanceSquared(i.transform.position);
                if (nearestItem == null || distanceSquared < nearestDistanceSquared)
                {
                    nearestItem = i;
                    nearestDistanceSquared = distanceSquared;
                }
                
            }

            return nearestItem != null;
        }

        private void UpdateInteraction()
        {
            TryGetNearestItem(out var nearestItem);
            Model.NearestItem = nearestItem;
        }

        private void OnInteraction()
        {
            if (CurrentState is PlayerState.Idle or PlayerState.Shoot or PlayerState.PlayerBulletReload)
            {
                Model.NearestItem?.Interact(Presenter);
            }
        }

        private bool _preventTransitionIdle;
        public bool PreventTransitionIdle
        {
            get => _preventTransitionIdle;
            private set
            {
                // if(_preventTransitionIdle != value)
                    // Log($"PreventTransitionIdle {_preventTransitionIdle} => {value}");
                _preventTransitionIdle = value;
            }
        }

        public override void OnStart()
        {
            base.OnStart();
            bool isStop = Behaviour.IsActuallyStopped(View, Model);
            PreventTransitionIdle = !isStop;
            // PreventTransitionIdle = false;
            // View.OnTriggerAnimation(isStop ? PlayerAnimation.ItemChange : PlayerAnimation.ItemChange_Upper);
            View.OnTriggerAnimation(PlayerAnimation.ItemChange_Upper);
            if (isStop) // 움직이는 중에 사용 시 상체만 사용 
            {
                View.OnTriggerAnimation(PlayerAnimation.ItemChange);
            }
            // if (isStop)
            // {
                // Model.IsStop = true;
                // View.NavMeshAgent.isStopped = true;
                // View.NavMeshAgent.velocity = Vector3.zero;
            // }
            // else
            // {
                _animationBehaviour.OnExit = OnAnimationExit;
            // }

        }

        public override void OnUpdate()
        {
            if (PreventTransitionIdle)
            {
                if (!_animationBehaviour.IsPlaying)
                {
                    PreventTransitionIdle = false;
                } 
            }
            else
            {
                if (View.Animator.GetCurrentAnimatorStateInfo(0).IsName("Item") && !Behaviour.IsActuallyStopped(View, Model))
                {
                    PreventTransitionIdle = true;
                    View.Animator.CrossFade("Run", 0.25f, 0);
                }
            }
        }

        private void OnAnimationExit()
        {
            PreventTransitionIdle = false;
            Player.ChangeState(PlayerState.Idle);
            _animationBehaviour.OnExit = null;
        }

        public override bool OnNext(FSMState<PlayerFSM, PlayerState> nextState)
        {
            if (nextState.Key == PlayerState.Idle)
            {
                // 움직이는 중에 이동 중단으로 인한 Idle 전환 시도의 경우 무시함
                if (PreventTransitionIdle)
                {
                    return false;
                }
            }
            return nextState.Key 
                is PlayerState.Idle
                or PlayerState.Stun // 경직 시 뺏길 수 있음
                or PlayerState.KnockBack
                or PlayerState.Dead;
        }

        public override void OnEnd()
        {
            base.OnEnd();
            Model.IsStop = false;
            View.NavMeshAgent.isStopped = false;
            _animationBehaviour.OnExit = null;
        }
    }
}