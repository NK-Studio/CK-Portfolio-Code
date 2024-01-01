using EnumData;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Settings
{
    [CreateAssetMenu(fileName = "BowMonsterSettings", menuName = "Scriptable Object/BowMonsterSettings", order = 0)]
    public class BowMonsterSettings : EnemySettings
    {
        
        [field: SerializeField, FoldoutGroup("전투/원거리 몬스터", true), Tooltip("화살 종류")]
        public EffectType ArrowEffectType { get; private set; }
        
        [field: SerializeField, FoldoutGroup("전투/원거리 몬스터", true), Tooltip("화살 속도")]
        public float ArrowSpeed { get; private set; }
    }
}