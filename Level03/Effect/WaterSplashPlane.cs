using System;
using EnumData;
using Managers;
using Micosmo.SensorToolkit;
using UnityEngine;
using UnityEngine.Serialization;
using Utility;

namespace Effect
{
    [RequireComponent(typeof(RangeSensor))]
    public class WaterSplashPlane : MonoBehaviour
    {
        public EffectType SplashEffectType = EffectType.None;
        public float YOffset = 0.5f;
        public float GravityScaleOnFall = 0.5f;
        public float VerticalVelocityScaleOnFall = 0.5f;
        
        private RangeSensor _sensor;
        private void Start()
        {
            _sensor = GetComponent<RangeSensor>();
            _sensor.OnDetected.AddListener(OnDetected);
        }

        private void OnDetected(GameObject obj, Sensor sensor)
        {
            var effect = EffectManager.Instance.Get(SplashEffectType);
            effect.transform.position = obj.transform.position.Copy(y: transform.position.y + YOffset);

            if (obj.TryGetComponent(out CustomGravity gravity) && gravity.Rigidbody.velocity.y < 0f)
            {
                var velocity = gravity.Rigidbody.velocity;
                gravity.Rigidbody.velocity = velocity.Copy(y: velocity.y * VerticalVelocityScaleOnFall);
                gravity.GravityScale = GravityScaleOnFall;
            }
        }
    }
}