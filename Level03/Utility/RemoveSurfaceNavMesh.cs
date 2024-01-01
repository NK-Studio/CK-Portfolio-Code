using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Utility
{
    public class RemoveSurfaceNavMesh : MonoBehaviour
    {
        [Button]
        public void RemoveAll()
        {
            NavMesh.RemoveAllNavMeshData();
        }
    }
}
