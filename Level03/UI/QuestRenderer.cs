using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;

public class QuestRenderer : MonoBehaviour
{
    [System.Serializable]
    public class QuestDataProperty
    {
        //public Image QuestImage;
        public string QuestTitle;
        public string QuestDescription;
    }

    [SerializeField] [Header("LeftQuest")] private TextMeshProUGUI _leftQuestTitle;
    [SerializeField] private TextMeshProUGUI _leftDescription;

    [SerializeField] [Header("RightQuest")]
    private TextMeshProUGUI _rightTitle;

    [SerializeField] private TextMeshProUGUI _rightDescription;

    [SerializeField] [Header("QuestDataList")]
    //TODO 데이터 파싱사용
    private List<QuestDataProperty> _leftQuestDataList;

    [SerializeField] private List<QuestDataProperty> _rightQuestDataList;

    public static QuestRenderer Instance { get; private set; }

    private const float DelayTime = 2f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        UIRenderer.Instance.HideAll();
    }

    /// <summary>
    /// LeftQuestUI를 보여주고 업데이트합니다
    /// index = 몇번째 퀘스트데이터[index]
    /// </summary>
    /// <param name="index"></param>
    public void UpdateLeftUI(int index)
    {
        UIRenderer.Instance.ShowUI(index);
        _leftQuestTitle.text = _leftQuestDataList[index].QuestTitle;
        //TextTyping(_leftDescription,_leftQuestDataList[index].QuestDescription,2.0f);
        DisableLeftQuestDelaySeconds(DelayTime).Forget();
    }

    /// <summary>
    /// RightQuestUI를 보여주고 업데이트합니다
    /// index = 몇번째 퀘스트데이터[index]
    /// </summary>
    /// <param name="index"></param>
    public void UpdateRightUI(int index)
    {
        UIRenderer.Instance.ShowUI(index);
        _rightTitle.text = _rightQuestDataList[index].QuestTitle;
        _rightDescription.text = _rightQuestDataList[index].QuestDescription;
        //TextTyping(RightDescription,RightQuestDataList[index].QuestDescription,2.0f);
    }

    /// <summary>
    /// 타자치는 연출을 해줍니다
    /// </summary>
    /// <param name="textMeshPro"></param>
    /// <param name="endValue"></param>
    /// <param name="duration"></param>
    private void TextTyping(TextMeshProUGUI textMeshPro, string endValue, float duration)
    {
        textMeshPro.DOKill();
        textMeshPro.text = string.Empty;
        textMeshPro.DOText(endValue, duration).SetEase(Ease.Linear);
    }

    private void Update()
    {
        if (Keyboard.current.yKey.wasPressedThisFrame)
        {
            UIRenderer.Instance.HideAll();
        }
    }

    /// <summary>
    /// time초뒤에 
    /// LeftQuest를 비활성화하고
    /// RightQuest를 활성화합니다
    /// </summary>
    /// <param name="time"></param>
    private async UniTaskVoid DisableLeftQuestDelaySeconds(float time)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(time));
        // UIRenderer.Instance.HideUI((int)UIObjectType.LeftQuest);
        // UIRenderer.Instance.ShowUI((int)UIObjectType.RightQuest); 
        UpdateRightUI(1);
    }
}