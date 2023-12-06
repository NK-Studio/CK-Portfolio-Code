using System;
using AutoManager;
using Character.Model;
using Character.View;
using Managers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Utility;

namespace CutScene
{
    public class TriggerCutScene : MonoBehaviour
    {
        [Title("CutScene"), ValidateInput("@CutScene != null", "PlayableDirector이 비어있습니다.")]
        public PlayableDirector CutScene;

        [Title("플레이어"), ValidateInput("@PlayerModel != null", "PlayerModel이 비어있습니다.")]
        public PlayerModel PlayerModel;

        [ValidateInput("@PlayerView != null", "PlayerView가 비어있습니다.")]
        public PlayerView PlayerView;

        [Title("이동할 씬")] public string NextScene;

        private bool _isPlaying;
        private AsyncOperation _loadSceneAsync;

        private void Start()
        {
            Manager.Get<GameManager>().NextSceneInfo.NextScene = NextScene;
            _loadSceneAsync = SceneManager.LoadSceneAsync("Loading", LoadSceneMode.Single);
            _loadSceneAsync.allowSceneActivation = false; //씬 로딩이 끝나도 전환을 하지 않는다.
        }

        [Button("Auto Binding", ButtonSizes.Large), PropertySpace(20)]
        private void Bind()
        {
            CutScene = GetComponent<PlayableDirector>();
            PlayerModel = FindObjectOfType<PlayerModel>();
            PlayerView = FindObjectOfType<PlayerView>();
        }

        private void OnTriggerStay(Collider other)
        {
            //재생중이라면 리턴
            if (CutScene.state == PlayState.Playing) return;

            if (other.CompareTag("Player"))
            {
                if (PlayerModel.CurrentControllerState == ControllerState.Grounded)
                {
                    PlayerModel.IsStop = true;
                    CutScene.Play();
                }
            }
        }

        private void Update()
        {
            if (CutScene.state == PlayState.Playing)
                PlayerView.ResetGFXRotation();
        }

        /// <summary>
        /// 씬을 이동합니다.
        /// </summary>
        public void LoadScene()
        {
            //NextScene
            if (string.IsNullOrEmpty(NextScene)) return;
            _loadSceneAsync.allowSceneActivation = true; //씬 로딩이 끝나도 전환을 하지 않는다.
        }
    }
}