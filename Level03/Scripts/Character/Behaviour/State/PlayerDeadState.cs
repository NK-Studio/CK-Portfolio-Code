using Character.Core.FSM;
using EnumData;
using FMODUnity;
using ManagerX;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace Character.Behaviour.State
{
    [PlayerFSMState(PlayerState.Dead)]
    public class PlayerDeadState : PlayerFSMState
    {
        public PlayerDeadState(PlayerFSM player) : base(player, 0f)
        {
            Model.HealthObservable
                .Where(health => health <= 0)
                .Subscribe(_ => Player.ChangeState(PlayerState.Dead))
                .AddTo(Player);
        }

        public override void OnStart()
        {
            base.OnStart();
                    
            Log("사망");

            //상태를 사망으로 변경합니다.
            View.NavMeshAgent.enabled = false;
            Model.IsStop = true;

            //스킬을 사용 중이라면 스킬을 취소합니다.
            if (Model.IsSkillPrepareState())
                View.OnExitSkillPrepareState(Model);

            //사망 애니메이션을 재생합니다.
            View.OnTriggerDead();

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

        public override bool OnNext(FSMState<PlayerFSM, PlayerState> nextState)
            => false;
    }
}