using TMPro;
using Tutorial;
using UnityEngine;

namespace Tutorial.Helper
{
    public class SpeakerChanger : MonoBehaviour
    {
        [SerializeField] private TMP_Text _target;
        [SerializeField] private DialogSpeaker _speaker;

        public void Execute()
        {
            _target.text = _speaker.Name;
            _target.font = _speaker.Font ?? _target.font;
        }
    }
}