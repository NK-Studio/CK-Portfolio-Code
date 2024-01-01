using Cysharp.Threading.Tasks;
using Doozy.Runtime.SceneManagement;
using Managers;
using ManagerX;
using SceneSystem;
using UnityEngine;
using UnityEngine.Events;

namespace UI
{
    public class Loading : MonoBehaviour
    {
        [SerializeField] private SceneLoader sceneLoader;

        [SerializeField] private bool ShowDebug;

        private CheckPoint _checkPoint;

        [SerializeField]
        private GameObject UIGroup;

        [SerializeField] 
        private UnityEvent<Color> OnBackgroundColorSet;
        
        private void Start()
        {
            Time.timeScale = 1f;
            // 체크 포인트 캐싱
            _checkPoint = AutoManager.Get<CheckpointManager>().CheckPoint;

            // Core 씬과 체크포인트에 연결된 Part 씬 하나 불러오기
            string sceneName = _checkPoint.Location.Scene.Name;
            ScheduleSceneLoad(sceneName);

            var controller = AutoManager.Get<SceneController>();
            // 색상 설정
            Color color = controller.PopLoadingSceneBackgroundColor();
            OnBackgroundColorSet.Invoke(color);
            // UI 가시성 설정
            bool hideUI = controller.LoadingSceneUIVisibility;
            if (!hideUI)
            {
                UIGroup.SetActive(false);
                controller.SetLoadingSceneUIVisibility(false);
            }
        }

        private void ScheduleSceneLoad(string sceneName)
        {
            sceneLoader.SetSceneName(sceneName);
            sceneLoader.LoadSceneAsync();
        }

        /// <summary>
        /// 완료되었을 때 호출
        /// </summary>
        public void OnComplete()
        {
            // Core씬 로드 완료 시 부가 데이터 로드
            GameManager.Instance.CurrentCheckPointStorage.Copy(_checkPoint.Storage);

            if (ShowDebug)
                DebugX.Log(
                    $"Core Scene Load Completed: New Storage {GameManager.Instance.CurrentCheckPointStorage}");

            //LoadLevelSequence(_checkPoint).Forget();
            _checkPoint = null;
        }

        // private async UniTaskVoid LoadLevelSequence(CheckPoint checkPoint)
        // {
        //     // Part씬 로드 완료 시 플레이어 위치 설정 (위치 사용 시에만)
        //     var player = GameManager.Instance.Player;
        //     if (checkPoint.Location.UsePosition)
        //         player.View.NavMeshAgent.Warp(checkPoint.Location.Position);
        // }
    }
}