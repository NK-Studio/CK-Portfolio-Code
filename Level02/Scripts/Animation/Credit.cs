using System;
using AutoManager;
using FMODUnity;
using Managers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Animation
{
    public class Credit : MonoBehaviour
    {
        public EventReference CreditBGM;
        private bool isCredit;

        private void Start()
        {
            isCredit = Manager.Get<GameManager>().IsCredit;

            if (isCredit)
                Manager.Get<AudioManager>().ChangeBGMWithPlay(CreditBGM);
        }

        private void Update()
        {
            if (isCredit)
                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    
                    Manager.Get<AudioManager>().StopBGM(true);
                    Manager.Get<GameManager>().IsCredit = false;
                    SceneManager.LoadScene("Home");
                }
        }
        
        public void OnEnd()
        {
            if (!isCredit)
            {
                Manager.Get<GameManager>().NextSceneInfo.NextScene = "Home";
                Manager.Get<GameManager>().IsCredit = false;
                SceneManager.LoadScene("Loading");
            }
            else
            {
                Manager.Get<GameManager>().IsCredit = false;
                SceneManager.LoadScene("Home");
            }
        }
    }
}