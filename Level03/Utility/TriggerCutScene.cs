using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Utility
{
    /// <summary>
    /// 컷씬을 재생하고 컷씬이 끝나면 씬을 이동시킨다.
    /// </summary>
    public class TriggerCutScene : MonoBehaviour
    {
        [Title("컷 씬"), ValidateInput("@CutScene != null", "PlayableDirector이 비어있습니다.")]
        public PlayableDirector CutScene;

        public bool UseLoadingScene;
        public bool IsPlayForEnter;

        [Title("타겟 태그"),ShowIf("@IsPlayForEnter")]
        public string EnterTarget;
        
        [Title("로딩 씬"), ShowIf("@UseLoadingScene")]
        public string LoadingScene;

        [Title("이동할 씬")] public string NextScene;

        private bool _isPlaying;
        private AsyncOperation _loadSceneAsync;

        [Button("Auto Binding", ButtonSizes.Large), PropertySpace(20)]
        private void Bind()
        {
            CutScene = GetComponent<PlayableDirector>();
        }

        private void OnTriggerEnter(Collider other)
        {
            //닿으면 재생하는 방식이 아니라면 리턴 
            if (!IsPlayForEnter) return;

            //재생중이라면 리턴
            if (CutScene.state == PlayState.Playing) return;

            if (other.CompareTag(EnterTarget))
                CutScene.Play();
        }

        public void PlayCutScene()
        {
            CutScene.Play();
        }

        /// <summary>
        /// 씬을 이동합니다.
        /// </summary>
        public void LoadScene()
        {
            if (!UseLoadingScene)
            {
                //NextScene
                if (string.IsNullOrEmpty(NextScene)) return;
                SceneManager.LoadScene(NextScene);
            }
            else
            {
                //UseNextScene
                if (string.IsNullOrEmpty(LoadingScene)) return;
                if (string.IsNullOrEmpty(NextScene)) return;
                
                // //로딩 처리 필요
                // ManagerX.AutoManager.Get<GameManager>().NextScene = NextScene;
                // SceneController.LoadScene(LoadingScene);
            }
        }
    }
}