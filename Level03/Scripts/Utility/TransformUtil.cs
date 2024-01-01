using UnityEngine;

namespace Utility
{
    public class TransformUtil : MonoBehaviour
    {
        public void ResetLocalPosition()
        {
            transform.localPosition = Vector3.zero;
        }
        public void ResetLocalRotation()
        {
            transform.localRotation = Quaternion.identity;
        }
        public void ResetLocalRotationAndPosition()
        {
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }
}