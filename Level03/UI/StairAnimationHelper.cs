using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI
{
    [ExecuteAlways]
    public class StairAnimationHelper : MonoBehaviour
    {
        public float Offset = 0f;
        public Image Image;
        private static readonly int OffsetProperty = Shader.PropertyToID("_Offset");

        private void Awake()
        {
            if (!Image)
            {
                Image = GetComponent<Image>();
            }
        }

        private void Update()
        {
            if (!Image || !Image.material)
            {
                Image = GetComponent<Image>();
                return;
            }
            Image.material.SetFloat(OffsetProperty, Offset);
        }
    }
}