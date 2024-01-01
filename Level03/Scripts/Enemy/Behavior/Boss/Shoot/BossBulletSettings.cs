using Sirenix.OdinInspector;
using UnityEngine;

namespace Enemy.Behavior.Boss
{
    [CreateAssetMenu(fileName = "BossBulletSettings", menuName = "Settings/Boss/Bullet Settings", order = 0)]
    public class BossBulletSettings : ScriptableObject
    {
        public enum Type
        {
            Straight, Guided,
        }

        public enum GuideTargetUpdateType
        {
            [InspectorName("None (초기 좌표 사용)")]
            None,
            [InspectorName("Every Frame (실시간 갱신)")]
            EveryFrame,
            [InspectorName("Periodically (주기적 갱신)")]
            Periodically,
            
        }

        [field: SerializeField, BoxGroup("일반 탄환"), Tooltip("탄환 종류입니다.")]
        public Type BulletType { get; private set; } = Type.Straight;
        
        [field: SerializeField, BoxGroup("일반 탄환"), Tooltip("탄환 속도입니다.")]
        public float BulletSpeed { get; private set; } = 10f;
        
        [field: SerializeField, BoxGroup("일반 탄환"), Tooltip("피해량입니다.")]
        public float BulletDamage { get; private set; } = 1f;
        
        [field: SerializeField, BoxGroup("일반 탄환"), Tooltip("빙결 무시 여부입니다. 체크 시 얼지 않습니다.")]
        public bool PreventFreeze { get; private set; } = false;
        
        [field: SerializeField, BoxGroup("일반 탄환"), Tooltip("최대 사거리입니다. 이를 넘어가면 자동으로 비활성화됩니다.")]
        public float BulletRange { get; private set; } = 50f;
        
        // 유도
        
        [field: SerializeField, BoxGroup("유도 탄환", VisibleIf = "@BulletType == Type.Guided"),
                Tooltip("목표 내에 접근하면 유도가 중단되는 거리입니다."), ShowIf("@BulletType == Type.Guided")]
        public float GuideDisableRangeFromTarget { get; private set; } = 1f;
        
        [field: SerializeField, BoxGroup("유도 탄환", VisibleIf = "@BulletType == Type.Guided"),
                Tooltip("발사 방향과 벌어지면 유도가 중단되는 각도입니다."), ShowIf("@BulletType == Type.Guided")]
        public float GuideDisableAngleFromShootDirection { get; private set; } = 45f;
#if UNITY_EDITOR
        public float GuideDisableAngleFromShootDirectionInCos => 
#else
        private float? _guideDisableAngleFromShootDirectionInCos = null;
        public float GuideDisableAngleFromShootDirectionInCos => _guideDisableAngleFromShootDirectionInCos ??=
#endif
                Mathf.Cos((GuideDisableAngleFromShootDirection) * Mathf.Deg2Rad);
        
        [field: SerializeField, BoxGroup("유도 탄환", VisibleIf = "@BulletType == Type.Guided"),
                Tooltip("유도가 시작되는 시간입니다."), ShowIf("@BulletType == Type.Guided")]
        public float GuideStartDelay { get; private set; } = 0f;
        
        [field: SerializeField, BoxGroup("유도 탄환", VisibleIf = "@BulletType == Type.Guided"),
                Tooltip("유도 시 회전하는 속도입니다."), ShowIf("@BulletType == Type.Guided")]
        public float GuideRotationSpeed { get; private set; } = 50f;

        [field: SerializeField, BoxGroup("유도 탄환", VisibleIf = "@BulletType == Type.Guided"),
                Tooltip("유도 시 목표 갱신 방법을 선택합니다."), ShowIf("@BulletType == Type.Guided")]
        public GuideTargetUpdateType GuideUpdateType { get; private set; } = GuideTargetUpdateType.EveryFrame;

        [field: SerializeField, BoxGroup("유도 탄환", VisibleIf = "@BulletType == Type.Guided"),
                Tooltip("유도 목표 갱신 방법이 주기적"), ShowIf("@GuideUpdateType == GuideTargetUpdateType.Periodically")]
        public float GuideUpdatePeriod { get; private set; } = 2f;


    }
}