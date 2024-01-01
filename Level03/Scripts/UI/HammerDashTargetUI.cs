using System;
using Character.Presenter;
using UniRx;
using UnityEngine;

namespace UI
{
    public class HammerDashTargetUI : MonoBehaviour
    {
        public ObjectFollowUI TargetUI;
        private PlayerPresenter _player;

        private void Start()
        {
            _player = FindAnyObjectByType<PlayerPresenter>();
            _player.Model.HammerDashTargetObservable.Subscribe(target =>
            {
                if (!TargetUI)
                {
                    return;
                }

                TargetUI.gameObject.SetActive(target);
                if (target)
                {
                    TargetUI.TargetObject = target.transform;
                }
            }).AddTo(_player);
        }
    }
}