using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;

namespace Utility
{
    public class SplineAnimateHandler : MonoBehaviour
    {
        [SerializeField] private SplineAnimate _spline;

        public UnityEvent OnAnimationEnd;

        private void Start()
        {
            _spline ??= GetComponent<SplineAnimate>();
        }

        private bool _eventCalled = false;
        private void Update()
        {
            if (!_eventCalled && _spline.NormalizedTime >= 1f)
            {
                OnAnimationEnd?.Invoke();
                _eventCalled = true;
            }
        }
    }
}