using Character.Core.FSM;
using EnumData;
using FMODUnity;
using ManagerX;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace Character.Behaviour.State
{
    [PlayerFSMState(PlayerState.Fall)]
    public class PlayerFallState : PlayerFSMState
    {
        public PlayerFallState(PlayerFSM player) : base(player, 0f)
        {
            if (!View.FallChecker)
            {
                return;
            }
            View.FallChecker.OnFallEvent.AddListener(() =>
            {
                Player.ChangeState(PlayerState.Fall, true);
            });
        }

        private float _leftTime;
        private bool _deadTriggered;
        public override void OnStart()
        {
            base.OnStart();

            View.FallChecker.enabled = false;
            Log("사망 by FALL");

            //상태를 사망으로 변경합니다.
            View.NavMeshAgent.enabled = false;
            Model.IsStop = true;
            View.Rigidbody.isKinematic = false;
            View.Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            View.Rigidbody.useGravity = true;

            //스킬을 사용 중이라면 스킬을 취소합니다.
            if (Model.IsSkillPrepareState())
                View.OnExitSkillPrepareState(Model);

            View.OnTriggerAnimation(PlayerAnimation.Fall);

            _leftTime = Settings.FallDeadTime;
            _deadTriggered = false;

            //사망 애니메이션을 재생합니다.
            // View.OnTriggerDead();

            #region Sound

            // 비명 효과음 재생

            // Settings.Sounds.TryGetValue("Death", out EventReference clip);
            // var audioManager = AutoManager.Get<AudioManager>();
            // audioManager.PlayOneShot(clip);

            // 사망 BGM 재생
            // audioManager.ChangeBGM(AudioManager.SoundKey.Death);
            // audioManager.PlayBGM();

            // 엠비언트 정지
            // audioManager.StopAMB();

            #endregion
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (_leftTime > 0f)
            {
                _leftTime -= Time.deltaTime;
                return;
            }

            if (_deadTriggered)
            {
                return;
            }

            _deadTriggered = true;

            Model.Health = 0f;
            View.OnTriggerDead();
            Debug.Log("진짜사망 by FALL");

        }

        public override bool OnNext(FSMState<PlayerFSM, PlayerState> nextState)
            => false;
    }
}