using Dummy.Scripts;
using EnumData;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Settings.Player
{
    [CreateAssetMenu(fileName = "PlayerBreatheBulletSettings", menuName = "Settings/Player Bullet/Player Breathe Bullet", order = 0)]
    public class PlayerBreatheBulletSettings : PlayerBulletSettings
    {
        [field: SerializeField, BoxGroup("방사기"), Tooltip("방사가 매 프레임당 전진 시 사용되는 범위입니다. " +
                                                         "시간 축을 그대로 사용하고, 값 축은 [0, 1]로 제한되어 1의 경우 반지름까지 도달합니다.")]
        public AnimationCurve ForwardRangeCurve { get; private set; }

        [field: SerializeField, BoxGroup("방사기/충돌 이펙트"), Tooltip("충돌 이펙트 타입")]
        public EffectType CollideEffectType { get; private set; } = EffectType.EnemyHitProjectile;
        
        [field: SerializeField, BoxGroup("방사기/충돌 이펙트"), Tooltip("충돌 이펙트 발생 레이어 마스크")]
        public LayerMask CollideEffectMask { get; private set; }

        [field: SerializeField, BoxGroup("방사기/충돌 이펙트"), Tooltip("충돌 이펙트 거리값: 콜라이더 평균값으로 해두면 될 듯")]
        public float CollideEffectRangeThreshold { get; private set; } = 0.5f;
        
        [field: SerializeField, BoxGroup("방사기/충돌 이펙트"), Tooltip("충돌 이펙트 레이 분할 수")]
        public int CollideEffectDivision { get; private set; } = 15;

    }
}