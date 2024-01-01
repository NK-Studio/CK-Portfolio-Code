using UnityEngine;
using UnityEngine.Events;

namespace Utility
{
    public class RunOnDisable : MonoBehaviour
    {
        public UnityEvent Event;

        private void OnDisable()
        {
            Event?.Invoke();
        }
    }
}