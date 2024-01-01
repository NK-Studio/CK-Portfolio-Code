using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Utility
{
    [System.Serializable]
    public class StringByEvent
    {
        public string Key;
        public UnityEvent UnityEvent;
    }

    public class AnimationEventHandle : MonoBehaviour
    {
        [Searchable] public StringByEvent[] StringByEvents;
        private readonly Dictionary<string, UnityEvent> _events = new();

        private void Awake()
        {
            foreach (var stringByEvent in StringByEvents)
            {
                if (_events.ContainsKey(stringByEvent.Key))
                {
                    DebugX.LogWarning($"Duplicate key {stringByEvent.Key} at {gameObject.name} !!!");
                }

                _events.Add(stringByEvent.Key, stringByEvent.UnityEvent);
            }
        }

        public void OnAnimationEvent(string id)
        {
            if (_events.TryGetValue(id, out var unityEvent))
            {
                unityEvent?.Invoke();
            }
        }

#if UNITY_EDITOR && ODIN_INSPECTOR
        [Button("Copy HandleName", ButtonSizes.Large), PropertySpace(20)]
        private void CopyHandlerName()
        {
            TextEditor te = new()
            {
                text = $"OnAnimationEvent : {StringByEvents.Length - 1}"
            };
            te.SelectAll();
            te.Copy();

            Debug.Log("복사되었습니다.");
        }
#endif
    }
}