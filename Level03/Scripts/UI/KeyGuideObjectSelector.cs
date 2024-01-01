
using System.Collections.Generic;
using Managers;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;

namespace UI
{
    public class KeyGuideObjectSelector : SerializedMonoBehaviour
    {
        public Dictionary<ControllerType, GameObject> Mapping = new();

        private void Start()
        {
            InputManager.Instance.CurrentControllerObservable
                .Subscribe(_ => Refresh()).AddTo(this);
        }

        public void Refresh()
        {
            var current = InputManager.Instance.CurrentController;
            foreach (var (type, obj) in Mapping)
            {
                obj.SetActive(type == current);
            }
        }
    }
}