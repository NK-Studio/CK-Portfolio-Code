using System;
using System.Collections.Generic;
using Micosmo.SensorToolkit;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Effect
{
    public class EffectRange : MonoBehaviour
    {
        [field: SerializeField, ReadOnly]
        public List<RangeSensor> Sensors { get; private set; } = new List<RangeSensor>();
        
        private void Awake()
        {
            if (Sensors.Count <= 0)
            {
                BindSensors();
            }
        }

        [Button("RangeSensor 사전 바인드")]
        private void BindSensors()
        {
            var sensorsArray = GetComponentsInChildren<RangeSensor>();
            Sensors.Clear();
            Sensors.AddRange(new List<RangeSensor>(sensorsArray));
        }



        public bool IsEmpty() => Sensors.Count <= 0;
        
        public RangeSensor this[int index]
        {
            get => Sensors[index];
            set => Sensors[index] = value;
        }

    }
}