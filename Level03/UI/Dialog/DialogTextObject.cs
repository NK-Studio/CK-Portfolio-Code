using FMODPlus;
using Managers;
using TMPro;
using UnityEngine;

namespace Tutorial.Helper
{
    public class DialogTextObject : MonoBehaviour
    {
        public TMP_Text Text;
        public FMODAudioSource AudioSource;

        private void Start()
        {
            Text ??= GetComponent<TMP_Text>();
            DialogManager.Instance.Target = Text;
            DialogManager.Instance.AudioSource = AudioSource;
        }
    }
}