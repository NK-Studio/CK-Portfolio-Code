using UnityEngine;
using UnityEngine.Events;

namespace Utility
{
    public class RunOnEnable : MonoBehaviour
    {
        public int ValidEnableCount = 1;

        private int _enabledCount = 0;
        protected virtual void OnEnable()
        {
            if (_enabledCount++ < ValidEnableCount)
            {
                return;
            }
            Execute();
        }

        public UnityEvent Event;
        public virtual void Execute()
        {
            Event?.Invoke();
        }
    }
}