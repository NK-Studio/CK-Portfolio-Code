using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Enemys
{
    [CreateAssetMenu(fileName = "New StarCandySettings", menuName = "Settings/Enemy/StarCandySettings", order = 10)]
    public class StarCandySettings : EnemySettings
    { 
        [field: FoldoutGroup("StarCandy", true), SerializeField, Tooltip("폭발 딜레이")]
        public float ExplodeDelay { get; private set; } = 2f;
        
        [field: Tooltip("플레이어에게 박치기 했을 때 플레이어가 받는 넉백 파워")]
        [field: FoldoutGroup("StarCandy", true), SerializeField]
        public float KnockbackPowerToPlayer { get; private set; } = 2f;
        
        [field: Tooltip("플레이어를 쫏다가 너무 멀면 포기할 준비를 하는 시간")]
        [field: FoldoutGroup("HariboSoldier", true), SerializeField]
        public float TrackingGiveUpTime { get; private set; } = 1f;

        public EventReference[] SFXClips;
    }
}