using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Items
{
    public class Stand : MonoBehaviour
    {
        public enum StandColors
        {
            Red,
            Green,
            Yellow
        }

        [field: SerializeField, Title("색깔")] public StandColors MyStandColor { get; private set; }

        [Title("옵션")] public UnityEvent OnSuccess;
        public UnityEvent OnFail;
    }
}