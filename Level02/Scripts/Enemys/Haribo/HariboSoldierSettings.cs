using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Enemys
{
    [CreateAssetMenu(fileName = "New HariboSoldierSettings", menuName = "Settings/Enemy/HariboSoldierSettings",
        order = 10)]
    public class HariboSoldierSettings : EnemySettings
    {
        [field: Tooltip("돌진할 때 속도")]
        [field: FoldoutGroup("HariboSoldier", true), SerializeField]
        public float DashSpeed { get; private set; } = 5f;

        [field: Tooltip("박치기 판정 범위")]
        [field: FoldoutGroup("HariboSoldier", true), SerializeField,]
        public float ButtRange { get; private set; } = 1f;

        [field: Tooltip("플레이어에게 맞았을 때 넉백")]
        [field: FoldoutGroup("HariboSoldier", true), SerializeField]
        public float KnockBackPower { get; private set; } = 2f;
        
        [field: Tooltip("플레이어에게 박치기 했을 때 플레이어가 받는 넉백 파워")]
        [field: FoldoutGroup("HariboSoldier", true), SerializeField]
        public float KnockbackPowerToPlayer { get; private set; } = 2f;
        
        [field: Tooltip("점프 파워")]
        [field: FoldoutGroup("HariboSoldier", true), SerializeField]
        public float JumpPower { get; private set; }


        [field: Tooltip("플레이어를 쫏다가 너무 멀면 포기할 준비를 하는 시간")]
        [field: FoldoutGroup("HariboSoldier", true), SerializeField]
        public float TrackingGiveUpTime { get; private set; } = 1f;

        public EventReference[] SFXClips;
    }
}