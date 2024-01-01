using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RequestUICameraSync : MonoBehaviour
{
    public Canvas ScreenSpaceWorldCanvas;
    
#if UNITY_EDITOR
    private void Update()
    {
        if (!ScreenSpaceWorldCanvas)
            Assert.IsNotNull(ScreenSpaceWorldCanvas, "ScreenSpaceWorldCanvas가 비어있습니다.");
        else if (!ScreenSpaceWorldCanvas.worldCamera)
        {
            EditorApplication.isPaused = true;
            
            Assert.IsNotNull(ScreenSpaceWorldCanvas.worldCamera,
                $"<b> ETC->Canvas-InGame UI,{ScreenSpaceWorldCanvas.name}의 World Camera에 UI 카메라가 연결되어 있지 않습니다.(치명적)</b>");
            
        }
    }
#endif
    
    [Button]
    private void BindCameraToCanvas()
    {
        if (ScreenSpaceWorldCanvas)
        {
            if (Camera.main)
            {
                ScreenSpaceWorldCanvas.worldCamera = GameObject.Find("UI Camera").GetComponent<Camera>();
                ScreenSpaceWorldCanvas.planeDistance = 1;
                Debug.Log($"{ScreenSpaceWorldCanvas.name} : 바인딩 되었습니다.");
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogError("UI 카메라가 씬에 존재하지 않습니다.");
#endif
            }

        }
    }
}
