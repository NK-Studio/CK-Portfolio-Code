using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class ParticleSystemLocalToWorldByPlayBackTime : MonoBehaviour
{
    private ParticleSystem _particleSystem;

    public float WorldTime = 0.7f;

    private void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        this.UpdateAsObservable()
            .Select(_ => _particleSystem.time)
            .Where(time => time > WorldTime)
            .Subscribe(_ => transform.SetParent(null))
            .AddTo(this);
    }
}