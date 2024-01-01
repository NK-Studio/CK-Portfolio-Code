using UnityEngine;

/// <summary>
/// Particle System과 별개로 셰이더 내에서 Time 사용이 필요할 경우 사용하는 Component
/// <code>_Unscaled_Time</code> 프로퍼티에 <code>Time.unscaledTime</code> 전달해줌
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class UnscaledTimePasser : MonoBehaviour
{
    public ParticleSystem Particle;

    private Material _material;

    private static readonly int UnscaledTime = Shader.PropertyToID("_Unscaled_Time");

    private void Start()
    {
        if (!Particle) return;

        var renderer = Particle.GetComponent<Renderer>();
        _material = renderer.material;
    }

    private void Update()
    {
        if (!_material) return;

        _material.SetFloat(UnscaledTime, Time.unscaledTime);
    }
}
