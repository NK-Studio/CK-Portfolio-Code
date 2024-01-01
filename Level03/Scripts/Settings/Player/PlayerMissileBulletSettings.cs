using Dummy.Scripts;
using Enemy.Behavior.Boss;
using EnumData;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Settings.Player
{
    [CreateAssetMenu(fileName = "PlayerMissileBulletSettings", menuName = "Settings/Player Bullet/Player Missile Bullet", order = 0)]
    public class PlayerMissileBulletSettings : PlayerBulletSettings
    {

        [field: SerializeField, BoxGroup("유도 탄환"), Tooltip("마우스 지정한 위치 기준 이 거리 안에 있으면 유도 지정 대상이 됩니다.")]
        public float GuideRangeFromTargetPosition { get; private set; } = 7f;
        
        [field: SerializeField, BoxGroup("유도 탄환"),
                Tooltip("목표 내에 접근하면 유도가 중단되는 거리입니다.")]
        public float GuideDisableRangeFromTarget { get; private set; } = 1f;
        
        [field: SerializeField, BoxGroup("유도 탄환"),
                Tooltip("유도가 시작되는 시간입니다.")]
        public float GuideStartDelay { get; private set; } = 0f;
        
        public enum GuideRotationMethod
        {
            [InspectorName("Linear: deg/s")]
            Linear,
            [InspectorName("Slerp: interpolation percentage per frame")] 
            Slerp
        }

        [field: SerializeField, BoxGroup("유도 탄환"),
                Tooltip("유도 회전 Method를 결정합니다.")]
        public GuideRotationMethod GuideRotationType { get; private set; } = GuideRotationMethod.Linear;
        
        [field: SerializeField, BoxGroup("유도 탄환"),
                Tooltip("유도 시 회전하는 속도입니다. 유도 회전 Method에 따라 적용되는 회전 속도의 단위가 달라집니다.")]
        public float GuideRotationSpeed { get; private set; } = 50f;
    }
}