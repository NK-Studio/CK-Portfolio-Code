using Sirenix.OdinInspector;
using UnityEngine;

namespace Settings
{
    [CreateAssetMenu(fileName = "JellyfishMonsterSettings", menuName = "Scriptable Object/JellyfishMonsterSettings", order = 0)]
    public class JellyfishMonsterSettings : EnemySettings
    {
        
        [field: SerializeField, FoldoutGroup("전투/해파리 몬스터", true), Tooltip("폭발 카운트 다운")]
        public float ExplosionCountdownTime { get; private set; } = 5f;
        
        [field: SerializeField, FoldoutGroup("전투/해파리 몬스터", true), Tooltip("폭발 범위")]
        public float ExplosionRadius { get; private set; } = 4f;
        
        [field: SerializeField, FoldoutGroup("전투/해파리 몬스터", true), Tooltip("플레이어 접근 폭발 범위")]
        public float ExplosionNearRadius { get; private set; } = 1f;

        [field: SerializeField, FoldoutGroup("전투/해파리 몬스터", true), Tooltip("추적 폭발 카운트다운 색상")]
        public Color ExplosionWarningColor { get; private set; } = Color.red;

        [field: SerializeField, FoldoutGroup("전투/해파리 몬스터", true), Tooltip("추적 폭발 카운트다운 커브")]
        public AnimationCurve ExplosionWarningColorCurve { get; private set; } = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    }
}