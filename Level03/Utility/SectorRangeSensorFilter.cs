using System.Collections.Generic;
using Micosmo.SensorToolkit;
using UnityEngine;
using UnityEngine.Serialization;

namespace Utility
{
    [RequireComponent(typeof(RangeSensor))]
    public class SectorRangeSensorFilter : MonoBehaviour
    {
        private RangeSensor _sensor;

        public float Angle = 30f;
    
        private void Awake()
        {
            _sensor = GetComponent<RangeSensor>();
        }

        private void Start()
        {
            if (!_sensor || _sensor == null)
            {
                _sensor = GetComponent<RangeSensor>();
            }

            // if (_sensor.Shape != RangeSensor.Shapes.Sphere)
            // {
            //     Logger.LogWarning("SectorRangeSensorFilter의 Range Sensor Shape가 Sphere가 아님", gameObject);
            //     return;
            // }
            UpdateAngleCache();
        }

        private void OnValidate()
        {
            UpdateAngleCache();
        }

        public float HalfAngleInCos { get; private set; }
        private void UpdateAngleCache()
        {
            HalfAngleInCos = Mathf.Cos(Angle * 0.5f * Mathf.Deg2Rad);
        }

        public bool FilterSector(Vector3 position)
        {
            Transform t = transform;
            Vector3 origin = t.position;
            Vector3 forward = t.forward;

            Vector3 toTarget = (position - origin).Copy(y: 0f);
            Vector3 direction = toTarget.normalized;
            return Vector3.Dot(forward, direction) >= HalfAngleInCos;
        }

        public IEnumerable<GameObject> FilteredPulse()
        {
            _sensor.Pulse();
            foreach (var obj in _sensor.Detections)
            {
                if (!FilterSector(obj.transform.position))
                {
                    continue;
                }
                yield return obj;
            }
        }

        public float Radius
        {
            get
            {
                switch (_sensor.Shape)
                {
                    case RangeSensor.Shapes.Sphere:
                        return _sensor.Sphere.Radius;
                    case RangeSensor.Shapes.Box:
                        var extents = _sensor.Box.HalfExtents;
                        return Mathf.Max(extents.x, extents.y, extents.z);
                    case RangeSensor.Shapes.Capsule:
                        return _sensor.Capsule.Radius;
                }

                return float.NaN;
            }
        }


        private void OnDrawGizmosSelected()
        {
            if (!_sensor || _sensor == null)
            {
                _sensor = GetComponent<RangeSensor>();
            }

            float radius = Radius;
            float halfAngle = Angle * 0.5f;
            Quaternion rotator = Quaternion.AngleAxis(halfAngle, Vector3.up);
            Transform t = transform;
            Vector3 origin = t.position;
            Vector3 forward = t.forward;
            Vector3 left = rotator * (forward * radius);
            Vector3 right = Quaternion.Inverse(rotator) * (forward * radius);
            Gizmos.DrawLine(origin, origin + left);
            Gizmos.DrawLine(origin, origin + right);

        }
    }
}