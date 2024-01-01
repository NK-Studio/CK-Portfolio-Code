using System.Collections.Generic;
using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Utility
{
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class MultipleImpulseSourceHelper : MonoBehaviour
    {

        public List<CinemachineImpulseSource> Sources = new();

        [Button]
        private void AutoFill()
        {
            Sources.Clear();
            Sources.AddRange(GetComponents<CinemachineCollisionImpulseSource>());
        }
            
        [Button]
        public void ImpulseAll()
        {
            foreach (var source in Sources)
            {
                source.GenerateImpulse();
            }
        }
        
    }
}