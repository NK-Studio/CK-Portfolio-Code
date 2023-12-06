using FMODUnity;
using UnityEngine;

namespace Enemys
{
    [CreateAssetMenu(fileName = "New ChocolateFrogSettings", menuName = "Settings/Enemy/ChocolateFrogSettings",
        order = 10)]
    public class ChocolateFrogSettings : EnemySettings
    {
        [Tooltip("탄이 날아가는 속도")]
        [field: SerializeField]
        public float BulletSpeed { get; set; } = 2f;

        [Tooltip("탄이 살아있는 시간(초 단위)")]
        [field: SerializeField]
        public float BulletLifeTime { get; set; } = 3f;
        
        [Tooltip("턴 속도")]
        [field: SerializeField]
        public float TurnSpeed { get; set; } = 5f;

        public EventReference[] SFXClips;
    }
}