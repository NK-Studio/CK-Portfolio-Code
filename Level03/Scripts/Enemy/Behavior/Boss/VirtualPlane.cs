using UnityEngine;
using Utility;

namespace Enemy.Behavior.Boss
{
    public class VirtualPlane : MonoBehaviour
    {
        [field: SerializeField]
        public Transform TargetTransform { get; private set; }


        public Vector3 GetProjectedPosition(in Vector3 v)
        {
            var origin = TargetTransform.position;
            var normal = TargetTransform.up;
            var originToV = v - origin;
            var dot = Vector3.Dot(originToV, normal);
            var projectedPosition = v - normal * dot;
            return projectedPosition;
        }

        public Vector3 GetCircleTangentVectorFromProjectedPosition(in Vector3 projectedPosition)
        {
            var origin = TargetTransform.position;
            var normal = TargetTransform.up;
            var originToProjected = (projectedPosition - origin);
            var cross = Vector3.Cross(normal, originToProjected.normalized).normalized;
            return cross;
        }
        
#if UNITY_EDITOR
        [field: SerializeField]
        public Transform TestTransform { get; private set; }
        
        private void OnDrawGizmos()
        {
            if(!TargetTransform) return;

            var vectors = new Vector3[]
            {
                new Vector3(1f, 0f, 1f),
                new Vector3(1f, 0f, -1f),
                new Vector3(-1f, 0f, -1f),
                new Vector3(-1f, 0f, 1f),
            };
            TargetTransform.TransformPoints(vectors);

            int length = vectors.Length;
            for (int i = 0; i <= vectors.Length; i++)
            {
                var from = vectors[i % length]; 
                var to = vectors[(i + 1) % length]; 
                Gizmos.DrawLine(from, to);
            }

            if (TestTransform)
            {
                var testPosition = TestTransform.position;
                var projectedPosition = GetProjectedPosition(testPosition);

                var origin = TargetTransform.position;                
                Gizmos.color = Color.white;
                Gizmos.DrawLine(testPosition, projectedPosition);
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(projectedPosition, origin);

                var originToProjected = (projectedPosition - origin);
                var normal = TargetTransform.up;
                var cross = Vector3.Cross(normal, originToProjected.normalized).normalized;
                Gizmos.color = Color.green;
                Gizmos.DrawLine(projectedPosition, projectedPosition + normal * 3f);
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(projectedPosition, projectedPosition + cross * 3f);
                Gizmos.color = Color.white;
                DrawUtility.DrawCircle(origin, originToProjected.magnitude, normal, 64, DrawUtility.GizmosDrawer());
            }
        }  
#endif
    }
}