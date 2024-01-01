using UnityEngine;
using Utility;

namespace Enemy.Spawner
{
    public class EnemySpawnSector : MonoBehaviour
    {

        public float Radius = 1f;

        public float RadiusSquared { get; private set; }
        
        private void Awake()
        {
            RadiusSquared = Radius * Radius;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var t = transform;
            DrawUtility.DrawCircle(t.position, Radius, t.up, 24, Gizmos.DrawLine);
        }
#endif
        
    }
}