using Sirenix.OdinInspector;
using UnityEngine;

namespace Enemys
{
    [RequireComponent(typeof(Animator))]
    public class CoreTriggerWithRootMotion : MonoBehaviour
    {
        [ValidateInput("@enemyController != null", "컨트롤러가 비어있습니다."), SerializeField]
        private EnemyController enemyController;

        private Animator _animator;
        private Vector3 _velocity;

        private void Awake() => _animator = GetComponent<Animator>();

        [field: SerializeField]
        public float Gravity { get; set; } = 30f;
        public bool IsActiveCore { get; set; } = true;

        [Title("옵션"), Tooltip("경우에따라 이동 속도를 빠르게 합니다.")]
        public float velocityMultiplier = 1f;

        private void OnAnimatorMove()
        {
            float delta = Time.deltaTime;
            Vector3 deltaPosition = _animator.deltaPosition;
            deltaPosition.y = 0;
            _velocity = deltaPosition / delta;
        }

        private void FixedUpdate()
        {
            if (IsActiveCore)
                enemyController.Core(Gravity, _velocity * velocityMultiplier);
            else {
                enemyController.CoreAsRigidbody(Gravity);
            }
        }

        [Button("Auto Binding")]
        private void AutoBinding()
        {
            enemyController = GetComponentInParent<EnemyController>();
        }
    }
}