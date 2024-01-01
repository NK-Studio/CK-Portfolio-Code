using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Utility
{
    [RequireComponent(typeof(Rigidbody))]
    public class CustomGravity : MonoBehaviour
    {
        // Gravity Scale editable on the inspector
        // providing a gravity scale per object
        
        public float GravityScale = 1.0f;
        private float _initialGravityScale;
        [HideInInspector]
        public Rigidbody Rigidbody;
        private float _gravity;

        private void Awake()
        {
            _initialGravityScale = GravityScale;
        }

        private void OnEnable ()
        {
            Rigidbody = GetComponent<Rigidbody>();
            Rigidbody.useGravity = false;
            GravityScale = _initialGravityScale;
            _gravity = Physics.gravity.magnitude;
        }

        private void FixedUpdate ()
        {
            var gravity = (-_gravity * GravityScale) * Vector3.up;
            Rigidbody.AddForce(gravity, ForceMode.Acceleration);
        }
    }
}