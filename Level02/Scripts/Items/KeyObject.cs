using Character.USystem.Throw;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Items
{
    public class KeyObject : MonoBehaviour
    {
        public enum KeyColor
        {
            Red,
            Green,
            Yellow
        }

        [field: SerializeField, Title("색깔")] public KeyColor MyKeyColor { get; private set; }
        public GameObject PinEffect;


        private Rigidbody _rigid;
        private SphereCollider _collider;
        private Throwable _throwable;

        private void Awake()
        {
            _rigid = GetComponent<Rigidbody>();
            _collider = GetComponent<SphereCollider>();
            _throwable = GetComponent<Throwable>();
        }

        /// <summary>
        /// 잡기 상태가 되기 위해 물리를 억제합니다.
        /// </summary>
        public void OnCatch()
        {
            _rigid.isKinematic = true;
            _collider.enabled = false;

            if (PinEffect)
                PinEffect.SetActive(false);
        }

        /// <summary>
        /// 내려놓기 상태가 되기 위해 물리를 적용합니다.
        /// </summary>
        public void OnApplyPhysics()
        {
            _rigid.isKinematic = false;
            _rigid.useGravity = true;
            _collider.enabled = true;
        }

        /// <summary>
        /// 던지기 기능을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Throwable GetThrowable()
        {
            return _throwable;
        }

        /// <summary>
        /// 땅에 붙도록 처리합니다.
        /// </summary>
        public void FloorSnap()
        {
            bool isHit = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 10f,
                LayerMask.GetMask("Ground"));

            if (isHit)
                transform.position = hit.point;
        }
    }
}