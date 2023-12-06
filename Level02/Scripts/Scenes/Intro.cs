using System;
using AutoManager;
using FMODUnity;
using Managers;
using SaveLoadSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utility;

namespace Scenes
{
    public class Intro : MonoBehaviour
    {
        public string NextScene;

        private void Start()
        {
            Manager.Get<GameManager>().ResetGame();
            Manager.Get<DataManager>().Save("Stage2Data", Vector3.zero);
            Manager.Get<AudioManager>().NoStopBGM = false;
        }

        /// <summary>
        /// 다음씬으로 이동합니다.
        /// </summary>
        public void LoadNextScene()
        {
            Manager.Get<GameManager>().NextSceneInfo.NextScene = NextScene;
            SceneManager.LoadScene("Loading");
        }

        public void ChangeScreen(int index)
        {
            Manager.Get<GameManager>().ChangeScreenSize((EScreenType)index);
        }

        public void GotoCredit()
        {
            Manager.Get<AudioManager>().StopBGM(true);
            Manager.Get<GameManager>().IsCredit = true;
            SceneManager.LoadScene("Credit");
        }
    }
}