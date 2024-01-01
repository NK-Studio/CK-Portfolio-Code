using NaughtyAttributes;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class DitherEffect : MonoBehaviour
{
    public GameObject TargetTrigger;
    public Renderer[] TargetRenderers;
    public string TargetTag = "MainCamera";

    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float Speed = 1;
    
    private float _ditherThreshold = 1;
    private bool _isDither;

    private static readonly int DitherThreshold = Shader.PropertyToID("_DitherThreshold");

    private void Start()
    {
        TargetTrigger.OnTriggerEnterAsObservable()
            .Where(col => col.CompareTag(TargetTag))
            .Subscribe(_ => _isDither = true)
            .AddTo(this);

        TargetTrigger.OnTriggerExitAsObservable()
            .Where(col => col.CompareTag(TargetTag))
            .Subscribe(_ => _isDither = false)
            .AddTo(this);
    }

    private void Update()
    {
        if (_isDither)
            _ditherThreshold -= Time.deltaTime * Speed;
        else
            _ditherThreshold += Time.deltaTime * Speed;

        _ditherThreshold = Mathf.Clamp01(_ditherThreshold);

        var ditherValue = curve.Evaluate(_ditherThreshold);
        foreach (Renderer targetRenderer in TargetRenderers)
            targetRenderer.material.SetFloat(DitherThreshold, ditherValue);
    }
}