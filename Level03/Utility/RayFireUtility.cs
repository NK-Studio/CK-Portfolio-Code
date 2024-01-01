using RayFire;
using UnityEngine;
using Logger = NKStudio.Logger;

namespace Utility
{
    public class RayFireUtility : MonoBehaviour
    {
        private RayfireRigid _rigid;
        private Vector3[] _initialPositions;
        public MeshRenderer[] SegmentRenderers { get; private set; }

        public RigidbodyInterpolation Interpolation = RigidbodyInterpolation.None;
        public Rigidbody[] Rigidbodies { get; private set; }
        
        private void Awake()
        {
            _rigid = GetComponent<RayfireRigid>();
            if (_rigid && _rigid.objectType == ObjectType.MeshRoot)
            {
                var count = _rigid.fragments.Count;
                _initialPositions = new Vector3[count];
                SegmentRenderers = new MeshRenderer[count];
                Rigidbodies = new Rigidbody[count];
                for (int i = 0; i < count; ++i)
                {
                    _initialPositions[i] = _rigid.fragments[i].transform.localPosition;
                    SegmentRenderers[i] = _rigid.fragments[i].GetComponent<MeshRenderer>();
                    Rigidbodies[i] = _rigid.fragments[i].GetComponent<Rigidbody>();
                }
            }
        }

        public void ResetFragmentTransform()
        {
            if (!_rigid || _rigid.objectType != ObjectType.MeshRoot)
            {
                Logger.LogWarning("Called ResetFragmentTransform() but not RayFireRigid, MeshRoot");
                return;
            }

            for (int i = 0; i < _initialPositions.Length; i++)
            {
                var t = _rigid.fragments[i].transForm;
                t.SetLocalPositionAndRotation(_initialPositions[i], Quaternion.identity);
            }
        }

        public void SetRigidbodyInterpolation()
        {
            for (int i = 0; i < Rigidbodies.Length; i++)
            {
                Rigidbodies[i].interpolation = Interpolation;
            }
        }
        
    }
}