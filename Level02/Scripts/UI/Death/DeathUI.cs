using System;
using Animation;
using AutoManager;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Managers;
using SaveLoadSystem;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class DeathUI : MonoBehaviour
{
    public Volume BlurVolume;

    private AsyncOperation _homeScene;
    private AsyncOperation _loadScene;

    [SerializeField] private EventReference MouseEnterSFX;
    [SerializeField] private EventReference MouseClickSFX;

    public void Open()
    {
        HomeLoadScene().Forget();
        BlurVolume.enabled = true;
        Time.timeScale = 0;
    }

    private void OnDestroy()
    {
        //게임 리셋
        Manager.Get<GameManager>().GameReset();
    }

    public void RePlay()
    {
        if (SceneManager.GetActiveScene().name.Equals("Stage_2"))
        {
            Gate gate = FindObjectOfType<Gate>();
            Vector3 hasCandyInfo = gate.GetHasCandyInTarget();
            Manager.Get<DataManager>().Save("Stage2Data", hasCandyInfo);
        }

        Time.timeScale = 1;
        Manager.Get<GameManager>().NextSceneInfo.NextScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene("Loading");
    }

    public void PlayMouseEnter()
    {
        Manager.Get<AudioManager>().PlayOneShot(MouseEnterSFX);
    }
    
    public void PlayMouseClick()
    {
        Manager.Get<AudioManager>().PlayOneShot(MouseClickSFX);
    }
    
    public void Quit()
    {
        Time.timeScale = 1;

        if (_homeScene is { progress: >= 0.9f })
        {
            WhiteFadeManager whiteFadeManager = FindObjectOfType<WhiteFadeManager>();

            if (whiteFadeManager)
                Destroy(whiteFadeManager.gameObject);

            Manager.Get<DataManager>().Save("Stage2Data", Vector3.zero);
            _homeScene.allowSceneActivation = true; //씬 로딩이 끝나도 전환을 하지 않는다.       
        }
    }

    private async UniTaskVoid HomeLoadScene()
    {
        _homeScene = SceneManager.LoadSceneAsync("Home", LoadSceneMode.Single);
        _homeScene.allowSceneActivation = false; //씬 로딩이 끝나도 전환을 하지 않는다.

        while (!_homeScene.isDone)
        {
            if (_homeScene.progress >= 0.9f)
                break;

            await UniTask.Yield();
        }
    }
}