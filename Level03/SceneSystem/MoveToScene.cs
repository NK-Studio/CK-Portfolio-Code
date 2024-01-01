using System;
using Managers;
using ManagerX;
using UnityEngine;

namespace SceneSystem
{
    public class MoveToScene : MonoBehaviour
    {
        [SerializeField]
        private CheckPoint _nextSceneCheckPoint;

        [SerializeField, Tooltip("로딩 씬 UI를 보여줄지에 대한 여부입니다. (false시 안보임)")]
        private bool showLoadingSceneUI = true;
        [SerializeField]
        private FadeStyle fadeStyle = FadeStyle.Mask;
        [SerializeField]
        private Color loadingSceneBackgroundColor = Color.black;

        private SceneController SceneController => AutoManager.Get<SceneController>();

        [SerializeField, Tooltip("해당 대상 방향으로 페이드가 진행됩니다.")]
        private Transform fadeTarget;

        private bool _moveCalled = false;
        public void Move()
        {
            if (!fadeTarget)
                fadeTarget = transform;

            _moveCalled = true;
            SceneController.ChangeFadeStyle(fadeStyle);
            SceneController.SetMaskPosition(fadeTarget.position);
            SceneController.SetLoadingSceneUIVisibility(showLoadingSceneUI);
            SceneController.PushLoadingSceneBackgroundColor(loadingSceneBackgroundColor);

            var player = GameManager.Instance.Player;

            if (player)
                _nextSceneCheckPoint.Storage.Status.Save(player.Model);

            SceneController.LoadLevel(_nextSceneCheckPoint);
        }

        private void Update()
        {
            if (_moveCalled)
            {
                SceneController.SetMaskPosition(fadeTarget.position);
            }
        }
    }
}
