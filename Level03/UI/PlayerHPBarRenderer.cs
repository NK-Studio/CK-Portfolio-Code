using UnityEngine;
using Character.Presenter;
using Managers;
using Settings;
using UnityEngine.UI;

public class PlayerHPBarRenderer : MonoBehaviour
{
    private PlayerPresenter _player;
    private CharacterSettings _characterSettings;
    
    public Image DelayHPBar;
    public Image PlayerHPBar;
    public float DelayFollowSpeed = 10f;

    private void Start()
    {
        InitUI();
    }

    private void Update()
    {
        ShowHPBar();
    }

    private void InitUI()
    {
        _player = GameManager.Instance.Player;
        _characterSettings = ManagerX.AutoManager.Get<GameManager>().Settings;
    }

    private void ShowHPBar()
    {
        var maxHealth = _characterSettings.MaximumHealth;
        float value = Mathf.Clamp01(_player.Model.Health / maxHealth);

        // 체력이 현재 hpbar보다 적어지는 경우 delaybar가 hpbar를 따라감
        if (value <= PlayerHPBar.fillAmount)
        {
            PlayerHPBar.fillAmount = value;
            DelayHPBar.fillAmount = Mathf.Lerp(DelayHPBar.fillAmount, value, DelayFollowSpeed * Time.deltaTime);
        }
        // 체력이 hpbar보다 증가한 경우 역으로 delaybar가 먼저, hpbar가 따라감  
        else
        {
            PlayerHPBar.fillAmount = Mathf.Lerp(PlayerHPBar.fillAmount, value, DelayFollowSpeed * Time.deltaTime);
            DelayHPBar.fillAmount = value;
        }
    }
}
