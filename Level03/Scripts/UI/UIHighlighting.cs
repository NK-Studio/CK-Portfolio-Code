using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    public class UIHighlighting : MonoBehaviour
    {
        private RectTransform _rectTransform;
        public EventSystem TargetEventSystem;

        private void Start()
        {
            _rectTransform = GetComponent<RectTransform>();

            OnSelectObjectChangeAsObservable().Subscribe(target =>
            {
                _rectTransform.position = target.transform.position;
            }).AddTo(this);
        }

        private IObservable<GameObject> OnSelectObjectChangeAsObservable()
        {
            return this.UpdateAsObservable()
                .ObserveEveryValueChanged(x => TargetEventSystem.currentSelectedGameObject)
                .Where(x => x != null)
                .Select(x => x);
        }
    }
}