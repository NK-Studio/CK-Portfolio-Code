using TMPro;
using UnityEngine;

namespace Tutorial.Helper
{
    [CreateAssetMenu(fileName = "New DialogSpeaker", menuName = "Settings/Dialog Speaker", order = 0)]
    public class DialogSpeaker : ScriptableObject
    {
        [field: SerializeField]
        public string Name { get; private set; }
        [field: SerializeField]
        public TMP_FontAsset Font { get; private set; }
    }
}