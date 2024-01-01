using System.Collections.Generic;
using UnityEngine;
using Character.Presenter;
using Managers;
using Settings;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine.UI;

public class PlayerHPCountRenderer : MonoBehaviour
{
    private PlayerPresenter _player;
    private CharacterSettings _characterSettings;

    public Sprite FullHeartSprite;
    public Sprite HalfHeartSprite;
    public Sprite EmptyHeartSprite;
    
    public List<GameObject> Roots = new();
    public List<Image> Hearts = new();

    private void Start()
    {
        InitUI();
    }

#if UNITY_EDITOR
    
    [field: SerializeField]
    private GameObject Root;
    [Button]
    private void Bind()
    {
        Roots.Clear();
        Hearts.Clear();
        
        var rt = Root.transform;
        for (int i = 0; i < rt.childCount; i++)
        {
            var root = rt.GetChild(i);
            var heart = root.GetChild(1);

            Roots.Add(root.gameObject);
            Hearts.Add(heart.GetComponent<Image>());
        }
    }
    
#endif

    private void InitUI()
    {
        _player = GameManager.Instance.Player;
        _characterSettings = ManagerX.AutoManager.Get<GameManager>().Settings;

        _player.Model.HealthObservable.Subscribe(_ =>
        {
            var healthFloor = Mathf.FloorToInt(_player.Model.Health);
            var healthCeil = Mathf.CeilToInt(_player.Model.Health);
            var maxHealth = Mathf.FloorToInt(_characterSettings.MaximumHealth);
            // var saturated = _player.Model.Health - healthFloor;

            // Debug.Log($"Health Changed: Raw={_player.Model.Health}, Floor={healthFloor}, Ceil={healthCeil}, Sat={saturated}");
            for (int i = 0; i < Roots.Count; i++)
            {
                if (i >= maxHealth)
                {
                    Roots[i].gameObject.SetActive(false);
                    continue;
                }

                // 체력 칸이 꽉찼으면
                if (i < healthFloor)
                {
                    Hearts[i].sprite = FullHeartSprite;
                }
                // 체력보다 크면 null
                else if (i > healthFloor)
                {
                    Hearts[i].sprite = EmptyHeartSprite;
                }
                // 마지막 칸
                else
                {
                    Hearts[i].sprite = healthFloor != healthCeil ? HalfHeartSprite : EmptyHeartSprite;
                }
                
                Roots[i].gameObject.SetActive(true);
                // Hearts[i].gameObject.SetActive(Hearts[i].sprite != EmptyHeartSprite && i <= healthCeil);
            }
        }).AddTo(this);
    }
}
