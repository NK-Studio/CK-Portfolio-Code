using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Utility;

namespace Effect
{
    public class MaterialRandomPropertyInitializer : MonoBehaviour
    {
        [Serializable]
        public class ShaderProperty
        {
#if UNITY_EDITOR
            private Material _material;
            private Vector2 GetRangeOr01()
            {
                if(!_material) return Vector2.up; // (0, 1)
                var shader = _material.shader;
                var propertyIndex = shader.FindPropertyIndex(Key);
                if (propertyIndex < 0) return Vector2.up;
                try
                {
                    var range = shader.GetPropertyRangeLimits(propertyIndex);
                    return range;
                }
                catch (Exception)
                {
                    return Vector2.up;
                }
            }

            public Material Material
            {
                get => _material;
                set => _material = value;
            }

            [Button(Expanded = true)]
            private void SetRangeAsPropertyValue(float width = 0f)
            {
                if(!_material) return;
                if (!_material.HasFloat(Key))
                {
                    Debug.LogWarning($"{_material}에 {Key}에 해당하는 Property가 없습니다 !!");
                    return;
                }
                var value = _material.GetFloat(Key);
                var validRange = GetRangeOr01();
                Range = new Vector2(
                    Mathf.Max(validRange.x, value - width * 0.5f), 
                    Mathf.Min(validRange.y, value + width * 0.5f)
                );
            }
#endif
            public string Key;
            [MinMaxSlider("@GetRangeOr01()", true)]
            public Vector2 Range;

        }

        [field: SerializeField]
        public Renderer TargetRenderer { get; private set; } = new();
        
        [field: SerializeField]
        public List<ShaderProperty> TargetProperties { get; private set; } = new();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if(!TargetRenderer) return;
            var material = TargetRenderer.sharedMaterial;
            foreach (var property in TargetProperties)
            {
                property.Material = material;
            }
        }
#endif

        [field: SerializeField]
        public bool SetOnAwake { get; private set; } = true;

        private void OnEnable()
        {
            if(SetOnAwake) 
                SetRandomProperty();
        }

        public void SetRandomProperty()
        {
            var material = TargetRenderer.material;
            foreach (var property in TargetProperties)
            {
                var randomValue = property.Range.Random();
                material.SetFloat(property.Key, randomValue);
            }
        }
        
    }
}