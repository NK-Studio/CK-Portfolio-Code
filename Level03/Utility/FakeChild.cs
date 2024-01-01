using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Utility
{
    [ExecuteAlways]
    public class FakeChild : MonoBehaviour
    {
        [Serializable]
        public struct TransformData
        {
            public Vector3 Position;
            public Quaternion Rotation;
            private bool _isWorldTransform;
            
            public TransformData(Transform from, bool world = true)
            {
                _isWorldTransform = world;
                Position = world ? from.position : from.localPosition;
                Rotation = world ? from.rotation : from.localRotation;
                // Debug.Log($"TransformData(p:{Position}, r:{Rotation})");
            }

            public void Apply(Transform to)
            {
                if (_isWorldTransform)
                {
                    to.SetPositionAndRotation(Position, Rotation);
                }
                else
                {
                    to.SetLocalPositionAndRotation(Position, Rotation);
                }
            }

            public void ApplyParent(Transform parent, out Vector3 position, out Quaternion rotation)
            {
                position = parent.position + parent.rotation * Position;
                rotation = parent.rotation * Rotation;
                
                // Debug.Log($"ApplyParent(p:{position}, r:{rotation.eulerAngles})");
            }
        }
        public TransformData LocalTransform = new() { Position = Vector3.zero, Rotation = Quaternion.identity };

        [Flags]
        public enum Mode : byte
        {
            None = 0b00,
            Position = 0b01,
            Rotation = 0b10,
            All = Position | Rotation,
        }

        public enum UpdateMethod : byte
        {
            Update,
            FixedUpdate,
            LateUpdate,
        }
        
        [field: SerializeField] public Mode FollowMode { get; set; } = Mode.All;
        [field: SerializeField] public UpdateMethod UpdateType { get; set; } = UpdateMethod.Update;

        [Button]
        private void BindLocalTransform()
        {
            LocalTransform = new TransformData(transform);
        }
        
        public Transform TargetParent;
        
        private void Update()
        {
            if(UpdateType != UpdateMethod.Update) return;
            Follow();
        }

        private void FixedUpdate()
        {
            if(UpdateType != UpdateMethod.FixedUpdate) return;
            Follow();
        }

        private void LateUpdate()
        {
            if(UpdateType != UpdateMethod.LateUpdate) return;
            Follow();
        }

        public void Follow(bool forced = false)
        {
            if(!forced && (!TargetParent || FollowMode == Mode.None)) return;
            LocalTransform.ApplyParent(TargetParent, out var position, out var rotation);

            switch (FollowMode)
            {
                case Mode.All:
                    transform.SetPositionAndRotation(position, rotation);
                    return;
                case Mode.Position:
                    transform.position = position;
                    return;
                case Mode.Rotation:
                    transform.rotation = rotation;
                    return;
            }
        }
    }
}