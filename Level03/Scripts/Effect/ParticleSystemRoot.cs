using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Utility;

public class ParticleSystemRoot : MonoBehaviour
{

    [field: SerializeField, ReadOnly]
    public List<ParticleSystem> ParticleSystems { get; private set; } = new();

    private void Start()
    {
        if (ParticleSystems == null || ParticleSystems.IsEmpty())
        {
            BindParticleSystems();
        }
    }

    [Button("Particle System 사전 바인드")]
    private void BindParticleSystems()
    {
        ParticleSystems.Clear();
        var pss = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in pss)
        {
            ParticleSystems.Add(ps);
        }
    }

    public bool UseUnscaledTime
    {
        set
        {
            foreach (var ps in ParticleSystems)
            {
                var main = ps.main;
                main.useUnscaledTime = value;
            }
        }
    }
}
