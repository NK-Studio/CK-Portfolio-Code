using System;
using UnityEngine;
using UnityEngine.UI;

namespace NKStudio.Option
{
    public class Circle : MonoBehaviour
    {
        public Sprite Full;
        public Sprite Empty;

        private Image _image;

        private bool _isFill;
        public bool IsFill
        {
            get => _isFill;
            set
            {
                _isFill = value;
                SetFill(_isFill);
            }
        }

        private void Awake()
        {
            TryGetComponent(out _image);
        }

        private void Start()
        {
            SetFill(_isFill);
        }

        /// <summary>
        /// 원을 칠하거나 칠하지 않습니다.
        /// </summary>
        /// <param name="isFill">true시 칠하고, false시 칠하지 않습니다.</param>
        private void SetFill(bool isFill)
        {
            _isFill = isFill;
            
            if (_image)
                _image.sprite = IsFill ? Full : Empty;
            else
            {
                _image = GetComponent<Image>();
                if (_image)
                    _image.sprite = IsFill ? Full : Empty;
            }
        }
    }
}
