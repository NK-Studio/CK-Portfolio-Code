using Sirenix.OdinInspector;
using UnityEngine;

namespace Character.USystem.Throw
{
    [RequireComponent(typeof(Rigidbody))]
    public class Throwable : MonoBehaviour
    {
        [SerializeField] private Rigidbody rigid;
        [SerializeField] private SphereCollider sphereCollider;

        [field: SerializeField] public float Power { get; private set; } = 10f;

        [field: SerializeField] public float Radius { get; private set; } = 0.5f;

        [Title("Debug")] [field: SerializeField]
        private Vector3 offset;

        private void Awake()
        {
            rigid = GetComponent<Rigidbody>();
            sphereCollider = GetComponent<SphereCollider>();
        }

        /// <summary>
        /// 던지기를 시전합니다.
        /// </summary>
        /// <param name="direction">날아가는 방향</param>
        /// <param name="power">날아가는 힘</param>
        public void Throw(Vector3 direction, float power = -1)
        {
            float nextPower = Mathf.Approximately(power, -1) ? Power : power;

            // 포물선 계산을 위해, 저항이 0이 되어야 함
            OnThrowTriggerPhysics();

            rigid.velocity = Vector3.zero;
            rigid.AddForce(direction.normalized * nextPower, ForceMode.VelocityChange);
        }


        /// <summary>
        /// 피직스를 제거합니다.
        /// </summary>
        private void OnThrowTriggerPhysics()
        {
            // 포물선 계산을 위해, 저항이 0이 되어야 함
            rigid.mass = 1;
            rigid.useGravity = true;
            rigid.isKinematic = false;
            rigid.drag = 0f;
            rigid.angularDrag = 0f;

            sphereCollider.isTrigger = true;
        }

        /// <summary>
        /// 피직스를 제거합니다.
        /// </summary>
        public void OnNoTriggerPhysics()
        {
            // 포물선 계산을 위해, 저항이 0이 되어야 함
            rigid.mass = 1;
            rigid.useGravity = false;
            rigid.isKinematic = true;
            rigid.drag = 0f;
            rigid.angularDrag = 0f;

            sphereCollider.isTrigger = true;
        }

        /// <summary>
        /// 피직스를 적용합니다.
        /// </summary>
        public void OnTriggerPhysics(bool useGravity = false, float mass = 10000000f, bool isTrigger = false)
        {
            // 포물선 계산을 위해, 저항이 0이 되어야 함
            rigid.mass = mass;
            rigid.useGravity = useGravity;
            rigid.isKinematic = false;
            rigid.drag = 0f;
            rigid.angularDrag = 0.05f;

            sphereCollider.isTrigger = isTrigger;
        }

        /// <summary>
        /// 힘을 더해줍니다.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="mode"></param>
        public void AddForce(Vector3 force, ForceMode mode)
        {
            rigid.AddForce(force, mode);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position + offset, Radius);
        }
#endif
    }
}