using System;
using UnityEngine;

namespace Platform_Experimental_
{
    [RequireComponent(typeof(BoxCollider))]
    public class SpawnPoint : MonoBehaviour
    {
        public Vector3 position;
        
        private void OnTriggerEnter(Collider other)
        {
            
        }
    }
}
