using System;
using System.Collections.Generic;
using Character.Model;
using Managers;
using Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillCoolTimeUI : MonoBehaviour
{
    private PlayerModel _playerModel;
    private CharacterSettings _characterSettings;
    
    
    public List<Image> SkillImageList = new List<Image>();
    public Color BasicColor;
    public Color CoolTimeColor;
    
    public List<Image> CoolTimeImageList = new List<Image>();
    public List<TMP_Text> CoolTimeTextList = new List<TMP_Text>();
    
    private struct SkillUI
    {
        public Action<TMP_Text, Image,Image, SkillUI> Renderer;
        public Func<float> CurrentCoolTimeGetter;
        public Func<float> MaxCoolTimeGetter;

        public SkillUI(
            Action<TMP_Text, Image,Image, SkillUI> renderer,
            Func<float> currentCoolTimeGetter, 
            Func<float> maxCoolTimeGetter
        )
        {
            Renderer = renderer;
            CurrentCoolTimeGetter = currentCoolTimeGetter;
            MaxCoolTimeGetter = maxCoolTimeGetter;
        }

        public void Render(TMP_Text text, Image image,Image skillImage) => Renderer(text, image,skillImage, this);
    }

    private List<SkillUI> _uis;
    private void Awake()
    {
        InitUI();
    }
    
    private void Update()
    {
        for (int i = 0; i < _uis.Count; i++)
        {
            _uis[i].Render(CoolTimeTextList[i], CoolTimeImageList[i], SkillImageList[i]);
        }
    }

    private void InitUI()
    {
        _playerModel = FindFirstObjectByType<PlayerModel>();
        _characterSettings = ManagerX.AutoManager.Get<GameManager>().Settings;

        _uis = new List<SkillUI>
        {
            new(ShowCoolTimeSkill, () => _playerModel.SwordAuraCoolTime, () => _characterSettings.SwordAuraCoolTime),
            new(ShowCoolTimeSkill, () => _playerModel.SectorAttackCoolTime, () => _characterSettings.SectorAttackCoolTime),
            new(ShowCoolTimeSkill, () => _playerModel.ZSlashCoolTime, () => _characterSettings.ZSlashCoolTime),
            new(ShowCoolTimeSkill, () => _playerModel.FlashAttackCoolTime, () => _characterSettings.FlashAttackCoolTime), // TODO
        };
        for (int i = 0; i < _uis.Count; i++)
        {
            CoolTimeTextList[i].gameObject.SetActive(false);
        }
    }

    private void ShowCoolTimeSkill(TMP_Text text, Image image,Image skillImage, SkillUI ui)
    {
        var currentCoolTime = ui.CurrentCoolTimeGetter();
        var maxCoolTime = ui.MaxCoolTimeGetter();

        if (currentCoolTime > 0)
        {
            skillImage.color = CoolTimeColor;
            text.gameObject.SetActive(true);   
        }
        else
            text.gameObject.SetActive(false);
        
        image.fillAmount = currentCoolTime / maxCoolTime;
        
        string txt;
        if (currentCoolTime >= 1)
            txt = currentCoolTime.ToString("0");
        else if (currentCoolTime >= 0.1f)
            txt = currentCoolTime.ToString("0.0");
        else
        {
                        skillImage.color = BasicColor;
                        txt = "";
        }

        
        text.text = txt;
    }
    
}