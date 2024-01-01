using System;
using TMPro;
using UnityEngine;

namespace Option
{
    public class GraphicsHardwareView : MonoBehaviour
    {
        private TMP_Text _title;

        private void Awake()
        {
            TryGetComponent(out _title);
        }

        public void SetText(string text)
        {
            if (_title)
            {
                _title.text = text;
            }
            else
            {
                if (TryGetComponent(out _title))
                    _title.text = text;
            }
        }
    }
}
