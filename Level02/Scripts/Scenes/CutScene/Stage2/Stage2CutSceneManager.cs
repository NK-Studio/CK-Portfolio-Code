using AutoManager;
using Managers;
using UnityEngine;
using UnityEngine.SceneManagement;


public class Stage2CutSceneManager : MonoBehaviour
{
    public string NextScene;
    private AsyncOperation _loadSceneAsync;

    /// <summary>
    /// 이동할 씬을 로딩합니다.
    /// </summary>
    public void GotoLoading()
    {
        Manager.Get<AudioManager>().NoStopBGM = true;
        Manager.Get<GameManager>().NextSceneInfo.NextScene = NextScene;
        SceneManager.LoadScene("Loading");
    }
}