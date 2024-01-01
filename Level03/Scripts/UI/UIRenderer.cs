using Doozy.Runtime.UIManager.Containers;
using UnityEngine;

public enum UIObjectType
{
    [InspectorName("우측 스노래빗 대화")]
    RightSnowRabbitDialog,
    [InspectorName("좌하단 체력")]
    LeftBottomContents,
    [InspectorName("우하단 무기")]
    RightBottomContents,
    [InspectorName("중앙 상단 키 가이드")]
    MidTopKeyGuide,
}

public class UIRenderer : MonoBehaviour
{
    [SerializeField] [Header("UI")] public UIContainer[] UIContainer;

    public static UIRenderer Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    /// <summary>
    /// UIObjectType을 가져와
    /// 그 오브젝트를 Show 합니다
    /// </summary>
    /// <param name="index"></param>
    public void ShowUI(int index)
    {
        UIContainer[index].Show();
    }
    
    /// <summary>
    /// UIObjectType을 가져와
    /// 그 오브젝트를 Hide 합니다
    /// </summary>
    /// <param name="index"></param>
    public void HideUI(int index)
    {
        // if (UIContainer[index].isVisible)
        // {
        UIContainer[index].Hide();   
        //}
    }

    [ContextMenu("Show")]
    public void ShowAll()
    {
        foreach (UIContainer uiAnimator in UIContainer)
            uiAnimator.Show();
    }

    [ContextMenu("Hide")]
    public void HideAll()
    {
        foreach (UIContainer uiAnimator in UIContainer)
            uiAnimator.Hide();
    }
}