using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class RunAfterOnEnable : MonoBehaviour
{
    public UnityEvent Event;
    public float Time = 1f;
    
    private void OnEnable()
    {
        Execute().Forget();
    }

    private async UniTaskVoid Execute()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(Time));
        Event.Invoke();
    }
}
