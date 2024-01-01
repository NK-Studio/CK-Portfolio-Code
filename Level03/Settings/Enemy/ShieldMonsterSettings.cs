using Sirenix.OdinInspector;
using UnityEngine;

namespace Settings
{
    [CreateAssetMenu(fileName = "ShieldMonsterSettings", menuName = "Scriptable Object/ShieldMonsterSettings", order = 0)]
    public class ShieldMonsterSettings : EnemySettings
    {
        
        [field: SerializeField, FoldoutGroup("전투/방패 몬스터", true), Tooltip("돌진 이동 속도")]
        public float RushMovementSpeed { get; private set; } = 5f;
        
        [field: SerializeField, FoldoutGroup("전투/방패 몬스터", true), Tooltip("돌진 애니메이션 프레임 수")]
        public int RushAnimationFrames { get; private set; } = 75 - 41;

        [field: SerializeField, FoldoutGroup("전투/방패 몬스터", true), Tooltip("사거리 대비 추가 돌진 거리")]
        public float RushRangeAdditionFromAttackRange { get; private set; } = 5f;
        
        // 돌진 거리 == 공격 시작 범위
        public float RushRange => AttackStartRange + RushRangeAdditionFromAttackRange;
    }
}