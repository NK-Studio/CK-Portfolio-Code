using System;
using System.Collections;
using System.Threading;
using Character.Model;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Spine.Unity;
using UniRx;
using UnityEngine;
using Utility;

namespace UI
{
    public class UIRopeStateChanger : MonoBehaviour
    {
        private PlayerModel _playerModel;

        [Title("스파인"), SerializeField] private SkeletonGraphic ropeStateUI;

        [Title("딜레이"), SerializeField, Tooltip("UI가 튀어나오는 딜레이")]
        private float delay = 0.25f;

        private bool _isShow;

        private CancellationTokenSource _tokenSource;

        private void Awake()
        {
            _playerModel = FindObjectOfType<PlayerModel>();
            _tokenSource = new();
        }

        private void Start()
        {
            _playerModel.DirectionModeObservable
                .Where(_ => !_isShow)
                .Where(time => time > 0)
                .Subscribe(_ => Show())
                .AddTo(this);

            _playerModel.DirectionModeObservable
                .Where(_ => _isShow)
                .Where(time => time == 0)
                .Subscribe(_ => Hide())
                .AddTo(this);

            _playerModel.RopeStateObservable
                .Where(_ => _isShow)
                .Subscribe(state => ChangeRopeUI(state).Forget())
                .AddTo(this);
        }

        private async UniTaskVoid ChangeRopeUI(ERopeState state)
        {
            //out애니메이션 실행
            ropeStateUI.AnimationState.SetAnimation(0, "out", false);

            //1초를 기다림
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: _tokenSource.Token);

            //Skin 변경
            ropeStateUI.Skeleton.SetSkin(state == ERopeState.Pull ? "rope" : "hook");
            ropeStateUI.Skeleton.SetSlotsToSetupPose();

            //다시 나옴
            ropeStateUI.AnimationState.SetAnimation(0, "in", false);
        }


        private void Show()
        {
            _isShow = true;
            ERopeState state = _playerModel.RopeState;
            ropeStateUI.Skeleton.SetSkin(state == ERopeState.Pull ? "rope" : "hook");
            ropeStateUI.Skeleton.SetSlotsToSetupPose();
            ropeStateUI.AnimationState.SetAnimation(0, "in", false);
        }

        private void Hide()
        {
            _tokenSource.Cancel();
            _tokenSource = new();
            _isShow = false;
            ropeStateUI.AnimationState.SetAnimation(0, "out", false);
        }

        /// <summary>
        /// UI를 오른쪽으로 숨깁니다.
        /// </summary>
        public void OnTriggerHide()
        {
            ropeStateUI.AnimationState.SetAnimation(0, "out", false);
        }

        /// <summary>
        /// UI를 오른쪽으로 숨깁니다.
        /// </summary>
        public void OnTriggerShow()
        {
            ropeStateUI.AnimationState.SetAnimation(0, "in", false);
        }
    }
}