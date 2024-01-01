using System;
using Micosmo.SensorToolkit;
using UnityEngine;

namespace Utility
{
    [RequireComponent(typeof(RangeSensor))]
    public class RangeSensorVisualizer : MonoBehaviour
    {
        public float DestroyAfter = 0.1f;
        
        private RangeSensor _sensor;
        private GameObject _cache;
        private void Start()
        {
            if (!TryGetComponent(out _sensor))
            {
                return;
            }

            if (!GetPrimitive(_sensor, out _cache))
            {
                return;
            }

            _sensor.OnPulsed += () =>
            {
                Visualize(DestroyAfter);
            };
        }

        private bool GetPrimitive(RangeSensor sensor, out GameObject obj)
        {
            switch (sensor.Shape)
            {
                case RangeSensor.Shapes.Box:
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.transform.localScale = sensor.Box.HalfExtents * 2f;
                    break;
                case RangeSensor.Shapes.Sphere:
                    obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    obj.transform.localScale = Vector3.one * (sensor.Sphere.Radius * 2f);
                    break;
                default:
                    obj = null;
                    return false;
            }

            if (obj.TryGetComponent<Collider>(out var c))
                c.enabled = false;
            
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.SetActive(false);
            
            return true;
        }
        
        private float _visualizeTime = 0f;
        private void Visualize(float time)
        {
            _visualizeTime = time;
            if (time > 0)
            {
                _cache.SetActive(true);
            }
        }

        private void Update()
        {
            if (_visualizeTime > 0f)
            {
                _visualizeTime -= Time.deltaTime;
                return;
            }

            if (_cache)
                if (_cache.activeSelf)
                    _cache.SetActive(false);
        }
    }
}