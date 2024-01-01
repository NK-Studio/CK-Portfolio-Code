using UnityEngine;

namespace Utility
{
    [ExecuteAlways]
    public class ConfineByCollider : MonoBehaviour
    {
        public enum UpdateMethod : byte
        {
            Update,
            FixedUpdate,
            LateUpdate,
        }
        [field: SerializeField] public UpdateMethod UpdateType { get; set; } = UpdateMethod.Update;
        [field: SerializeField] public Collider TargetCollider { get; set; }
        private void Update()
        {
            if(UpdateType != UpdateMethod.Update) return;
            Confine();
        }

        private void FixedUpdate()
        {
            if(UpdateType != UpdateMethod.FixedUpdate) return;
            Confine();
        }

        private void LateUpdate()
        {
            if(UpdateType != UpdateMethod.LateUpdate) return;
            Confine();
        }

        private void Confine()
        {
            if (!TargetCollider) return;
            var rawPosition = transform.position;
            var confinedPosition = TargetCollider.ClosestPoint(rawPosition);
            transform.position = confinedPosition;
        }
    }
}