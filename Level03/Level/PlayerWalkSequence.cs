using System;
using Character.Model;
using Character.Presenter;
using Character.View;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using Utility;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace Level 
{
    public class PlayerWalkSequence : MonoBehaviour
    {
        [BoxGroup("설정"), LabelText("목적지")]
        public Transform Destination;
        [BoxGroup("설정"), LabelText("카메라 고정 사용 여부")]
        public bool UseCameraRootPositioning = true;
        [BoxGroup("설정"), LabelText("카메라를 목적지에 고정"), DisableIf("@UseCameraRootPositioning == false")]
        public bool UseCameraRootPositionAsDestination;
        [BoxGroup("설정"), LabelText("씬 시작 시 실행")]
        public bool StartOnAwake;
        [BoxGroup("설정"), LabelText("최소 대기 시간")]
        public float DelayAfterSetDestination = 0.5f;

        [BoxGroup("이벤트")]
        public UnityEvent OnStart;
        [BoxGroup("이벤트")]
        public UnityEvent OnEnd;
        
        private PlayerPresenter _player;
        private PlayerView _view;
        private PlayerModel _model;
        private Vector3 _origin;
        private void Start()
        {
            _player = FindAnyObjectByType<PlayerPresenter>();
            _view = _player.View;
            _model = _player.Model;

            if (StartOnAwake)
            {
                ExecuteSequence();
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Keyboard.current.digit0Key.wasPressedThisFrame)
                OnStart?.Invoke();
        }
#endif

        public void ExecuteSequence()
        {
            OnStart.Invoke();
            Execute().Forget();
        }
        
        private async UniTask Execute()
        {
            _origin = _player.transform.position; // Origin: 플레이어 위치
            if (UseCameraRootPositioning)
            {
                _view.OldCameraRoot.rotation = _player.CameraRoot.rotation; // ㅋㅋ;
                if (UseCameraRootPositionAsDestination)
                {
                    _view.OldCameraRoot.transform.position = Destination.position.Copy(y: _player.CameraRoot.position.y);
                }
                else
                {
                    _view.OldCameraRoot.transform.position = _origin.Copy(y: _player.CameraRoot.position.y); // ㅋㅋ;
                }
                _view.OldPlayerFollowCamera.gameObject.SetActive(true);
            }

            // 모든 종류의 입력 비활성화
            _model.IsInputDisabled = true;
            
            // 목적지 설정
            _view.NavMeshAgent.SetDestination(Destination.position);

            // 목적지 도착 시까지 대기
            await UniTask.Delay(TimeSpan.FromSeconds(DelayAfterSetDestination));
            await UniTask.WaitUntil(() => _view.NavMeshAgent.remainingDistance <= 0.01f);

            if (UseCameraRootPositioning && UseCameraRootPositionAsDestination)
            {
                // 카메라 고정 해제
                _view.OldPlayerFollowCamera.gameObject.SetActive(false);

                // 입력 재활성화
                _model.IsInputDisabled = false;
            }
            
            // 이벤트 호출
            OnEnd.Invoke();
        }
        
        
    }
}