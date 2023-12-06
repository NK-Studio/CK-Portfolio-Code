using System;
using AutoManager;
using Cysharp.Threading.Tasks;
using Managers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utility;
using Random = UnityEngine.Random;

namespace Scenes
{
    public class Logo : MonoBehaviour
    {
        [Title("씬 이동")] [Tooltip("자동으로 씬 매니저에 등록된 다음 씬으로 이동할 것인가?"), SerializeField]
        private bool autoNextScene;

        [ShowIf("@autoNextScene == false"), SerializeField]
        private string sceneName = "Home";
        
#if UNITY_EDITOR
        [Title("옵션")] [Tooltip("씬 전환 딜레이를 무조건 1초로 제한함."), SerializeField]
        private bool debugMode;
#endif
        
        private void Start()
        {
            Manager.Get<GameManager>().ChangeScreenSize(EScreenType.FullScreen);
            GotoHome().Forget();
        }

        private async UniTaskVoid GotoHome()
        {
#if UNITY_EDITOR
            int randomTime = debugMode ? 1 : Random.Range(1, 3);
#else
            int randomTime = Random.Range(1, 3);
#endif
            await UniTask.Delay(TimeSpan.FromSeconds(randomTime));

            if (autoNextScene)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            else
                SceneManager.LoadScene(sceneName);
        }
    }
}