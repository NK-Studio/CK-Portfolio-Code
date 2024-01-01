using NKStudio;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RequestBlurUISourceSync : MonoBehaviour
{
    public TranslucentImage TranslucentImage;
    
#if UNITY_EDITOR
    private void Update()
    {
        if (!TranslucentImage)
            Assert.IsNotNull(TranslucentImage, "TranslucentImage가 비어있습니다.");
        else if (!TranslucentImage.Source)
        {
            EditorApplication.isPaused = true;
            
            Assert.IsNotNull(TranslucentImage.Source,
                $"<b> ETC->Canvas-UI (UI Controller),{TranslucentImage.name}의 Source에 UI 카메라의 BlurImageSource가 연결되어 있지 않습니다.(치명적)</b>");
            
        }
    }
#endif

    [Button]
    private void BindCameraToTranslucentImage()
    {
        if (TranslucentImage)
        {
            BlurImageSource blurImageSource;
            var uiCamera = GameObject.Find("UI Camera");
            
            if (uiCamera)
                blurImageSource = GameObject.Find("UI Camera").GetComponent<BlurImageSource>();
            else
                blurImageSource = Camera.main.GetComponent<BlurImageSource>();

            if (blurImageSource)
            {
                TranslucentImage.Source = blurImageSource;
                Debug.Log($"{TranslucentImage.name} : 바인딩 되었습니다.");
            }
            else
            {
                Debug.LogError("UI 카메라가 없거나, UI 카메라에 Blur Image Source가 없습니다.");
            }
        }
    }
}