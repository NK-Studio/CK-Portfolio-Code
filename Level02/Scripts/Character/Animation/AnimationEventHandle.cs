using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Character.Animation
{
    public class AnimationEventHandle : MonoBehaviour
    {
        public UnityEvent[] events;

        public void OnAnimationEvent(int id)
        {
            events[id]?.Invoke();
        }

#if UNITY_EDITOR && ODIN_INSPECTOR
        [Button("Copy HandleName", ButtonSizes.Large), PropertySpace(20)]
        private void CopyHandlerName()
        {
            TextEditor te = new()
            {
                text = "OnAnimationEvent"
            };
            te.SelectAll();
            te.Copy();

            Debug.Log("복사되었습니다.");
        }
#endif
    }
}