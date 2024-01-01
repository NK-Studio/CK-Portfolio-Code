using System;
using Cysharp.Threading.Tasks;
using Doozy.Runtime.SceneManagement;
using Doozy.Runtime.UIManager.Animators;
using Doozy.Runtime.UIManager.Containers;
using ManagerX;
using SceneSystem;
using UnityEngine;

namespace Managers
{
    public enum FadeStyle
    {
        Default,
        Mask,
        Disabled,
    }

    [ManagerDefaultPrefab("SceneController")]
    public class SceneController : MonoBehaviour, AutoManager
    {
        [SerializeField] private bool showDebug;

        private SceneLoader _sceneLoader;
        private UIContainer _controller;

        public bool IsPlaying { get; private set; }

        [SerializeField] private GameObject[] fadeObjects;

        [SerializeField] private UIContainerUIAnimator maskAnimation;
        [SerializeField] private Color? _loadingSceneBackgroundColorRequested = null;

        /// <summary>
        /// 로딩 씬에서 UI를 모두 숨길지에 대한 처리를 합니다.
        /// </summary>
        public bool LoadingSceneUIVisibility { get; private set; } = true;


        public void PushLoadingSceneBackgroundColor(Color color) 
            => _loadingSceneBackgroundColorRequested = color;
        public Color PopLoadingSceneBackgroundColor()
        {
            var color = _loadingSceneBackgroundColorRequested ?? Color.black;
            _loadingSceneBackgroundColorRequested = Color.black;
            return color;
        }

        /// <summary>
        /// 로딩 씬에서 UI를 숨길지 말지에 대한 설정을 처리합니다.
        /// </summary>
        /// <param name="active">false시 로딩 씬에서 숨겨진 채 진행됩니다.</param>
        public void SetLoadingSceneUIVisibility(bool active)
        {
            LoadingSceneUIVisibility = active;
        }

        private void Start()
        {
            _sceneLoader = GetComponent<SceneLoader>();
            _controller = GetComponentInChildren<UIContainer>();

            foreach (GameObject fadeObject in fadeObjects)
                fadeObject.SetActive(false);

            fadeObjects[0].SetActive(true);
        }

        /// <summary>
        /// 인자로 받은 체크 포인트로 이동합니다.
        /// </summary>
        /// <param name="checkPoint"></param>
        public void LoadLevel(CheckPoint checkPoint)
        {
#if UNITY_EDITOR
            if (showDebug)
                Debug.Log("매니저 풀 릴리즈");
#endif

            ItemManager.Instance.ReleaseUsedObjects();
            EffectManager.Instance.ReleaseUsedObjects();
            EnemyPoolManager.Instance.ReleaseUsedObjects();

            // 체크 포인트 캐싱
            AutoManager.Get<CheckpointManager>().CheckPoint = checkPoint;
            _sceneLoader.SetSceneName("Loading");
            PlayAsync().Forget();
        }

        /// <summary>
        /// 로딩 뷰로 이동합니다.
        /// </summary>
        public void LoadLevel()
        {
#if UNITY_EDITOR
            if (showDebug)
                Debug.Log("매니저 풀 릴리즈");
#endif
            ItemManager.Instance.ReleaseUsedObjects();
            EffectManager.Instance.ReleaseUsedObjects();
            EnemyPoolManager.Instance.ReleaseUsedObjects();

            var checkPointManager = CheckpointManager.Instance;

            // 체크포인트 없으면 초기화 후 이동
            if (checkPointManager.CheckPoint.IsNull())
                checkPointManager.Reset();
            // 체크포인트 있어도 세부 상태(체력, 무기)는 초기화 (재시작이라서)
            else
                checkPointManager.CheckPoint.Storage.Status.Reset();

            _sceneLoader.SetSceneName("Loading");
            PlayAsync().Forget();
        }

        /// <summary>
        /// 홈으로 이동합니다.
        /// </summary>
        public void MoveHome()
        {
#if UNITY_EDITOR
            if (showDebug)
                Debug.Log("매니저 풀 릴리즈");
#endif
            ItemManager.Instance.ReleaseUsedObjects();
            EffectManager.Instance.ReleaseUsedObjects();
            EnemyPoolManager.Instance.ReleaseUsedObjects();

            _sceneLoader.SetSceneName("Home");
            _sceneLoader.LoadScene();
        }

        /// <summary>
        /// 마스크의 위치를 설정합니다.
        /// </summary>
        /// <param name="positionWS">스크린 스페이스로 변경하고 싶은 World Position</param>
        public void SetMaskPosition(Vector3 positionWS)
        {
            Vector3 transformWorldSpaceToScreenSpace = Camera.main.WorldToScreenPoint(positionWS);
            transformWorldSpaceToScreenSpace.z = 0;

            maskAnimation.rectTransform.position = transformWorldSpaceToScreenSpace;
        }

        /// <summary>
        /// 페이드 방식을 변경합니다.
        /// </summary>
        /// <param name="fadeStyle">변경할 페이드 방식</param>
        public void ChangeFadeStyle(FadeStyle fadeStyle)
        {
            foreach (GameObject fadeObject in fadeObjects)
                fadeObject.SetActive(false);

            switch (fadeStyle)
            {
                case FadeStyle.Default:
                    fadeObjects[0].SetActive(true);
                    break;
                case FadeStyle.Mask:
                    fadeObjects[1].SetActive(true);
                    break;
                case FadeStyle.Disabled:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(fadeStyle), fadeStyle, null);
            }
        }

        private async UniTask PlayAsync()
        {
            _sceneLoader.SetAllowSceneActivation(false);
            _sceneLoader.LoadSceneAsync();
            IsPlaying = true;
            _controller.Show();
            await UniTask.WaitUntil(() => _controller.isVisible);
            _sceneLoader.SetAllowSceneActivation(true);
            await UniTask.WaitUntil(() => _sceneLoader.currentState == SceneLoader.State.ActivatingScene);
            _controller.Hide();
            await UniTask.WaitUntil(() => _controller.isHidden);
            IsPlaying = false;
        }
    }
}