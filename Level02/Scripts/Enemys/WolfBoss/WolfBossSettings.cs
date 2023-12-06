using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Enemys.WolfBoss {
    [CreateAssetMenu(fileName = "New WolfBossSettings", menuName = "Settings/Enemy/WolfBossSettings", order = 0)]
    public class WolfBossSettings : EnemySettings {
        
        
        [Tooltip("돌진 공격 폭")]
        [field: FoldoutGroup("WolfBoss - 돌진 공격", true), SerializeField]
        public float RushWidth { get; private set; }
        [Tooltip("돌진 공격 높이")]
        [field: FoldoutGroup("WolfBoss - 돌진 공격", true), SerializeField]
        public float RushHeight { get; private set; }
        [Tooltip("돌진 공격 감지 범위")]
        [field: FoldoutGroup("WolfBoss - 돌진 공격", true), SerializeField]
        public float RushRecognizeRange { get; private set; }
        [Tooltip("돌진 공격 범위")]
        [field: FoldoutGroup("WolfBoss - 돌진 공격", true), SerializeField]
        public float RushAttackRange { get; private set; } = 1f;
        [Tooltip("돌진 공격 속도")]
        [field: FoldoutGroup("WolfBoss - 돌진 공격", true), SerializeField]
        public float RushSpeed { get; private set; }
        
        [Tooltip("돌진 공격 넉백량")]
        [field: FoldoutGroup("WolfBoss - 돌진 공격", true), SerializeField]
        public float RushKnockBackPower { get; private set; } = 5f;
        
        [Tooltip("할퀴기 공격 부채꼴 각도 (호도법, 0 ~ 360)")]
        [field: FoldoutGroup("WolfBoss - 할퀴기 공격", true), SerializeField, Range(0f, 360f)]
        public float ScratchAngle { get; private set; }
        [Tooltip("할퀴기 공격 범위 (부채꼴 반지름)")]
        [field: FoldoutGroup("WolfBoss - 할퀴기 공격", true), SerializeField]
        public float ScratchRange { get; private set; }

        [Tooltip("할퀴기 공격 높이")]
        [field: FoldoutGroup("WolfBoss - 할퀴기 공격", true), SerializeField]
        public float ScratchHeight { get; private set; } = 2f;

        [Tooltip("할퀴기 공격 넉백량")]
        [field: FoldoutGroup("WolfBoss - 할퀴기 공격", true), SerializeField]
        public float ScratchKnockBackPower { get; private set; } = 3f;
        
        [Tooltip("점프 공격 범위 (원 반지름)")]
        [field: FoldoutGroup("WolfBoss - 점프 공격", true), SerializeField]
        public float JumpAttackRange { get; private set; }

        [Tooltip("점프 공격 범위 오프셋")]
        [field: FoldoutGroup("WolfBoss - 점프 공격", true), SerializeField]
        public Vector3 JumpAttackOffset { get; private set; } = Vector3.zero;
        [Tooltip("점프 공격 범위 (인식 범위)")]
        [field: FoldoutGroup("WolfBoss - 점프 공격", true), SerializeField]
        public float JumpAttackRecognizeRange { get; private set; }
        [Tooltip("점프 공격 포물선 최대 높이")]
        [field: FoldoutGroup("WolfBoss - 점프 공격", true), SerializeField]
        public float JumpAttackMaxHeight { get; private set; } = 5f;
        [Tooltip("점프 공격 넉백량")]
        [field: FoldoutGroup("WolfBoss - 점프 공격", true), SerializeField]
        public float JumpAttackKnockBackPower { get; private set; } = 5f;
        
        [Tooltip("그로기 상태 지속 시간")]
        [field: FoldoutGroup("WolfBoss - 그로기", true), SerializeField]
        public float GroggyDuration { get; private set; }

        [Tooltip("그로기 시 별사탕 소환 갯수")]
        [field: FoldoutGroup("WolfBoss - 그로기", true), SerializeField]
        public RangeInt GroggyStarCandySpawnCountRange { get; private set; } = new RangeInt(3, 2);
        [Tooltip("그로기 시 별사탕 소환 힘")]
        [field: FoldoutGroup("WolfBoss - 그로기", true), SerializeField]
        public float GroggyStarCandySpawnPower { get; private set; } = 10f;
        [Tooltip("그로기 시 별사탕 소환 연직방향 힘")]
        [field: FoldoutGroup("WolfBoss - 그로기", true), SerializeField]
        public float GroggyStarCandySpawnPowerUpDirection { get; private set; } = 3f;

        public EventReference[] SFXClips;
    }
}