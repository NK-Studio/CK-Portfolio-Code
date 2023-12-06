using Sirenix.OdinInspector;
using UnityEngine;

namespace Enemys
{
    [CreateAssetMenu(fileName = "New EnemySettings", menuName = "Settings/Enemy/EnemySettings", order = 3)]
    public class EnemySettings : ScriptableObject
    {
        [field: Tooltip("이동속도")]
        [field: FoldoutGroup("Enemy 공통", true), SerializeField]
        public float MoveSpeed { get; private set; }

        [field: Tooltip("추적 범위")]
        [field: FoldoutGroup("Enemy 공통", true), SerializeField]
        public float TrackingRange { get; private set; }

        [field: Tooltip("공격 범위")]
        [field: FoldoutGroup("Enemy 공통", true), SerializeField]
        public float AttackRange { get; private set; }

        [field: Tooltip("피해량")]
        [field: FoldoutGroup("Enemy 공통", true), SerializeField]
        public float AttackDamageAmount { get; private set; }

        [field: Tooltip("공격 쿨 다운 (초 단위)")]
        [field: FoldoutGroup("Enemy 공통", true), SerializeField]
        public float AttackDelay { get; private set; } = 3f;
        
        [field: Tooltip("최대 체력")]
        [field: FoldoutGroup("Enemy 공통", true), SerializeField]
        public float HPMax { get; private set; }

        [field: Tooltip("중력")]
        [field: FoldoutGroup("Enemy 공통", true), SerializeField]
        public float gravity = 30;
    }
}