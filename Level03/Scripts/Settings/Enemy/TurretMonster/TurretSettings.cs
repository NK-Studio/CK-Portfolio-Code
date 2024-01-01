using Enemy.Behavior.TurretMonster;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Settings
{
    [CreateAssetMenu(fileName = "TurretSettings", menuName = "Scriptable Object/TurretSettings", order = 0)]
    public class TurretSettings : EnemySettings
    {
        [field: SerializeField, FoldoutGroup("전투/설치형 몬스터", true), Tooltip("투사체 피해 범위")]
        public float ProjectileAffectRange { get; private set; } = 10f;
        
        [field: SerializeField, FoldoutGroup("전투/설치형 몬스터", true), Tooltip("투사체 체공 시간")]
        public float ProjectileFlyTime { get; private set; } = 3;
        
        [field: SerializeField, FoldoutGroup("전투/설치형 몬스터", true), Tooltip("공격 사거리 시야각")]
        public float AttackStartRangeSightAngle { get; private set; } = 60;
        
        [field: SerializeField, FoldoutGroup("전투/설치형 몬스터", true), Tooltip("투사체")]
        public GameObject ProjectilePrefab { get; private set; }
        
        [field: SerializeField, FoldoutGroup("전투/설치형 몬스터", true), Tooltip("투사체 Projector")]
        public GameObject ProjectileRangeProjector { get; private set; }

        private float? _attackStartRangeSightHalfAngleInCos = null;
        public float AttackStartRangeSightHalfAngleInCos
            => _attackStartRangeSightHalfAngleInCos ??= Mathf.Cos(AttackStartRangeSightAngle * Mathf.Deg2Rad * 0.5f);
    }
}