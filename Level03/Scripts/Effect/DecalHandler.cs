using UnityEngine;
using UnityEngine.Rendering.Universal;
using Utility;

namespace Effect
{
    [RequireComponent(typeof(DecalProjector))]
    public class DecalHandler : MonoBehaviour
    {
        private static readonly Quaternion BaseRotation = Quaternion.Euler(90f, 0f, 0f);
        public DecalProjector DecalProjector { get; private set; }
        private void Start()
        {
            DecalProjector = GetComponent<DecalProjector>();
            if (!DecalProjector)
            {
                DebugX.LogError($"cannot find DecalProjector on {name}", this);
            }
            transform.rotation = BaseRotation;
        }

        public void InitializeCircle(float size)
        {
            if (!DecalProjector)
            {
                DecalProjector = GetComponent<DecalProjector>();
            }

            DecalProjector.size = DecalProjector.size.Copy(x: size, y: size);
        }
        
        public void SetForward(Vector3 position, Vector3 forward, float distance, float projectorHeight = 5f, float dampingHeight = 1f)
        {
            if (!DecalProjector)
            {
                DecalProjector = GetComponent<DecalProjector>();
            }

            DecalProjector.size = DecalProjector.size.Copy(y: distance);
            DecalProjector.pivot = DecalProjector.pivot.Copy(y: distance * 0.5f);
            transform.SetPositionAndRotation(
                position + Vector3.up * (projectorHeight - dampingHeight), 
                Quaternion.LookRotation(forward) * BaseRotation
            );
        }

        public void SetPosition(Vector3 position, float projectorHeight = 5f)
        {
            transform.position = position + Vector3.up * projectorHeight;
        }
        
    }
}