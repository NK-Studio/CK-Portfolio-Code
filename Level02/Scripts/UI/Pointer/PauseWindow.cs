using AutoManager;
using FMODUnity;
using GameplayIngredients;
using Managers;
using TMPro;
using UITweenAnimation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Utility;

public class PauseWindow : MonoBehaviour
{
    [SerializeField] private Volume volume; 
    [SerializeField] private TMP_Dropdown screenSizeDropdown;

    [SerializeField]
    private EventReference EnterSFX;
    
    [SerializeField]
    private EventReference ClickSFX;
    
    public void GotoHome()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1;
        SceneManager.LoadScene("Home");
    }

    /// <summary>
    /// 게임을 다시 진행합니다.
    /// </summary>
    public void Replay()
    {
        volume.weight = 0;
        Messager.Send("PlayerMove");
    }
    
    /// <summary>
    /// 게임을 멈추고 팝업을 띄웁니다.
    /// </summary>
    public void Stop()
    {
        volume.weight = 1;
        UIController.Instance.PushView("Pause");
        Messager.Send("PlayerStop");
    }
    
    /// <summary>
    /// 게임 창 스크린을 변경합니다.
    /// </summary>
    public void ChangeScreen()
    {
        int index = screenSizeDropdown.value;
        Manager.Get<GameManager>().ChangeScreenSize((EScreenType)index);
    }

    /// <summary>
    /// 마우스 닿았을 때 효과음을 재생합니다.
    /// </summary>
    public void PlayEnterSFX()
    {
        Manager.Get<AudioManager>().PlayOneShot(EnterSFX);
    }
    
    /// <summary>
    /// 마우스 클릭 효과음을 재생합니다.
    /// </summary>
    public void PlayClickSFX()
    {
        Manager.Get<AudioManager>().PlayOneShot(ClickSFX);
    }
}