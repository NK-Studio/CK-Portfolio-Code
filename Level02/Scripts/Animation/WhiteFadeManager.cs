using EasingCurve;
using Sirenix.OdinInspector;
using UnityEngine;

public class WhiteFadeManager : MonoBehaviour
{
    public enum FadeStateData
    {
        None,
        FadeIn,
        FadeOut
    }

#if ODIN_INSPECTOR
    [Title("현재 애니메이션 상태")]
#else
        [Header("현재 애니메이션 상태")]
#endif
    public FadeStateData FadeState;

    private CanvasGroup _canvasGroup;
    private float _time;

#if ODIN_INSPECTOR
    [Title("FadeIn 간격")]
#else
        [Header("FadeIn 간격")]
#endif
    public float FadeInDuration = 1;

    public EasingFunctions.Ease FadeInCurve = EasingFunctions.Ease.EaseInBack;

#if ODIN_INSPECTOR
    [Title("FadeOut 간격")]
#else
        [Header("FadeOut 간격")]
#endif
    public float FadeOutDuration = 1;

    public EasingFunctions.Ease FadeOutCurve = EasingFunctions.Ease.EaseInSine;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        OnFadeSystem();
    }

    private void OnFadeSystem()
    {
        _time += Time.deltaTime;
        float lerp;
        AnimationCurve animationCurve;

        switch (FadeState)
        {
            case FadeStateData.FadeIn:
                lerp = _time / FadeInDuration;
                animationCurve = EasingAnimationCurve.EaseToAnimationCurve(FadeInCurve);

                _canvasGroup.alpha =
                    Mathf.Lerp(_canvasGroup.alpha, 1, animationCurve.Evaluate(lerp));

                break;
            case FadeStateData.FadeOut:
                lerp = _time / FadeOutDuration;
                animationCurve = EasingAnimationCurve.EaseToAnimationCurve(FadeOutCurve);

                _canvasGroup.alpha =
                    Mathf.Lerp(_canvasGroup.alpha, 0, animationCurve.Evaluate(lerp));
                break;
        }
    }

    private void Reset()
    {
 _time = 0;
        FadeInDuration = 5;
        FadeOutDuration = 5;
    }

#if !ODIN_INSPECTOR
        private void OnValidate()
        {
            _time = 0;
        }
#endif

#if ODIN_INSPECTOR
    [Button("FadeIn 실행"), PropertySpace(20)]
#else
        [ContextMenu("FadeIn")]
#endif
    public void TestFadeIn()
    {
        FadeState = FadeStateData.FadeIn;
        _time = 0;
    }

#if ODIN_INSPECTOR
    [Button("FadeOut 실행")]
#else
        [ContextMenu("FadeOut")]
#endif
    public void TestFadeOut()
    {
        FadeState = FadeStateData.FadeOut;
        _time = 0f;
    }

    public void InitFade()
    {
        FadeState = FadeStateData.None;
        _time = 0f;
        _canvasGroup.alpha = 0;
    } 
}