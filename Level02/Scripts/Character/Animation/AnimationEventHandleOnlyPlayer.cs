using AutoManager;
using Character.Controllers;
using Character.Model;
using Character.USystem.Hook.View;
using Character.View;
using Managers;
using Settings;
using UnityEngine;
using Utility;

namespace Character.Animation
{
    public class AnimationEventHandleOnlyPlayer : MonoBehaviour
    {
        private CharacterSettings _settings;

        private HookSystemView _hookSystemView;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerView playerView;
        [SerializeField] private PlayerModel playerModel;

        private void Awake()
        {
            _hookSystemView = FindObjectOfType<HookSystemView>();
            _settings = Manager.Get<GameManager>().characterSettings;
        }

        public void OnAnimationEvent(int id)
        {
            switch (id)
            {
                case 1:
                    //Empty(시스템 로프를 던지는 코드가 있었습니다.)
                    break;
                case 2:
                    playerController.OnAttack();
                    break;
                case 3:
                    playerController.OnFinishAttack();
                    break;
                case 4:
                    playerView.OnTriggerAnimation(PlayerAnimation.OnReAttackInputCheck, true);
                    break;
                case 5:
                    playerView.OnTriggerAnimation(PlayerAnimation.OnReAttackInputCheck, false);
                    break;
                case 6:
                    //바닥에 내려놓았을 때
                    playerView.PutDownThrowableObject();
                    
                    // @플레이어 키 사탕 내려놓기 사운드
                    Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[18], transform.position);
                    break;
                case 7:
                    //던집니다.
                    playerView.ShotThrowableObject();
                    break;
                case 8:
                    playerView.OnAttack01(playerModel.RopeState);
                    break;
                case 9:
                    playerView.OnAttack02(playerModel.RopeState);
                    break;
                case 10:
                    //공중 로프 샷
                    playerController.OnAirShotRope();
                    break;
                case 11:
                    //키 사탕을 잡는 처리를 함
                    playerController.OnCatchToCandyKey();
                    
                    // @플레이어 키 사탕 들기 사운드
                    Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[11], transform.position);
                    break;
                case 12:
                    playerModel.IsStop = false;
                    break;
                case 13:
                    playerController.OnPutDownStand();
                    break;
                case 14:
                    // @플레이어 공격2
                    Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[6], transform.position);
                    break;
                case 201:
                    //몬스터를 향해 훅샷을 해야할 때 보정을 해줍니다.
                    playerController.RefreshRope(); 
                    break;
                case 202:
                    //사망 연출
                    playerController.ShowDeathUI();
                    break;
            }
        }
    }
}