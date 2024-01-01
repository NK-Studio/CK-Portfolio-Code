using UnityEngine;
using UnityEngine.SceneManagement;

namespace NKStudio
{
    public class Splash : MonoBehaviour
    {
        [Tooltip("이동할 씬")] public SceneReference NextScene;

        [Space] [Tooltip("스플래쉬 이미지를 빠르게 이동합니다.")]
        public bool DebugMode;

        private void Start()
        {
            if (DebugMode)
                OnNext();
        }
        
        /// <summary>
        /// 씬 이동
        /// </summary>
        public void OnNext()
        {
            SceneManager.LoadScene(NextScene.Name);
        }
    }
}