using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Utility;

namespace Effect
{
    public class TrailRendererRoot : MonoBehaviour
    {
        
        [field: SerializeField, ReadOnly]
        public List<TrailRenderer> TrailRenderers { get; private set; } = new();

        private void Start()
        {
            if (TrailRenderers == null || TrailRenderers.IsEmpty())
            {
                BindTrailRenderers();
            }
        }

        private void OnEnable()
        {
            Clear();
        }

        [Button("Trail Renderer 사전 바인드")]
        private void BindTrailRenderers()
        {
            TrailRenderers.Clear();
            var trs = GetComponentsInChildren<TrailRenderer>();
            foreach (var tr in trs)
            {
                TrailRenderers.Add(tr);
            }
        }

        public bool RendererEnabled
        {
            set
            {
                foreach (var tr in TrailRenderers)
                {
                    tr.enabled = value;
                }
            }
        }

        public void Clear()
        {
            foreach (var tr in TrailRenderers)
            {
                tr.Clear();
            }
        }
    }
}