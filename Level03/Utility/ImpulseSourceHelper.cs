using Cinemachine;
using UnityEngine;

namespace Utility
{
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class ImpulseSourceHelper : MonoBehaviour
    {
        private CinemachineImpulseSource _source;

        public CinemachineImpulseSource Source => _source ??= GetComponent<CinemachineImpulseSource>();

        private void Awake()
        {
            _source = GetComponent<CinemachineImpulseSource>();
        }

        public void SetDefaultVelocityX(float value) => Source.m_DefaultVelocity = Source.m_DefaultVelocity.Copy(x: value);
        public void SetDefaultVelocityY(float value) => Source.m_DefaultVelocity = Source.m_DefaultVelocity.Copy(y: value);
        public void SetDefaultVelocityZ(float value) => Source.m_DefaultVelocity = Source.m_DefaultVelocity.Copy(z: value);
    }
}