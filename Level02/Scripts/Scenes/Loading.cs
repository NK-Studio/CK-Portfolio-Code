using System;
using AutoManager;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Managers;
using Sirenix.OdinInspector;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Scenes
{
    public class Loading : MonoBehaviour
    {
        [Title("내용"), SerializeField] private string message = "하리보를 굳히는 중";

        [TitleGroup("제목"), SerializeField] private TMP_Text nextText;
        [TitleGroup("제목"), SerializeField] private float speed = 1f;

        [TitleGroup("씬"), SerializeField] private float nextSceneDelay = 2f;

        [Title("팁 메세지")] [SerializeField] private string[] TipMessage;

        [SerializeField, ValidateInput("@TipText != null", "팁 메세지 텍스트가 비어있습니다.")]
        private TMP_Text TipText;


        private int _count;

        private void Start()
        {
            Manager.Get<GameManager>().IsNotAttack = false;
            
            int randomText = Random.Range(0, TipMessage.Length);
            TipText.text = TipMessage[randomText];

            nextText.SetText(message);
            Observable.Interval(TimeSpan.FromSeconds(speed)).Subscribe(_ =>
            {
                //카운트 추가
                _count += 1;

                if (_count > 3)
                    _count = 0;

                using var sb = ZString.CreateStringBuilder();
                sb.Append(message);

                //점 추가
                for (int i = 0; i < _count; i++)
                    sb.Append(".");

                //화면 렌더링
                nextText.SetText(sb);
            }).AddTo(this);

            SceneAsync().Forget();
        }

        private async UniTaskVoid SceneAsync()
        {
            string nextSceneName = Manager.Get<GameManager>().NextSceneInfo.NextScene;
            AsyncOperation loadSceneAsync = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);
            loadSceneAsync.allowSceneActivation = false; //씬 로딩이 끝나도 전환을 하지 않는다.

            await UniTask.WaitUntil(() => loadSceneAsync.progress >= 0.9f);
            await UniTask.Delay(TimeSpan.FromSeconds(nextSceneDelay));
            Manager.Get<GameManager>().NextSceneInfo.NextScene = string.Empty;
            loadSceneAsync.allowSceneActivation = true; //씬 로딩이 끝나도 전환을 하지 않는다.
        }
    }
}