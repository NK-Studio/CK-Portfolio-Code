using Cysharp.Threading.Tasks;
using Enemy.Behavior.Boss;
using Enemy.Task;
using EnumData;
using Managers;
using Settings;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using Utility;

namespace Enemy.Behavior.ShieldMonster
{
    public class BowMonster : Monster
    {
        private BowMonsterSettings _settings;
        public BowMonsterSettings BowSettings => _settings ??= (BowMonsterSettings)base.Settings;

        [SerializeField, BoxGroup("원거리 몬스터")]
        private Transform _shootPosition;
        public float ArrowSpeed => BowSettings.ArrowSpeed;

        public void StartShootArrow()
        {
            var effect = EffectManager.Instance.Get(EffectType.SeahorseCharging);
            if (effect.TryGetComponent(out FakeChild f))
            {
                f.TargetParent = _shootPosition;
            }
        }
        
        public void ShootArrow()
        {
            var shootPosition = _shootPosition.position;
            var direction = PlayerView.transform.position - shootPosition;
            direction.y = 0f;
            direction.Normalize();

            var obj = EffectManager.Instance.Get(BowSettings.ArrowEffectType, shootPosition, Quaternion.LookRotation(direction));
            if (!obj.TryGetComponent(out EnemyProjectile projectile))
            {
                DebugX.Log("cannot get component EnemyProjectile");
                return;
            }
            projectile.Initialize(direction, ArrowSpeed, gameObject,
                () => BowSettings.AttackPower
            );
        }
        [BoxGroup("원거리 몬스터")]
        public BossShootSettings BossShootSettings;
        public void ShootBossProjectile()
        {
            if (!BossShootSettings)
            {
                return;
            }   
            
            var shootPosition = _shootPosition.position;
            BossShootSettings.Shoot(null, shootPosition, PlayerPresenter.transform, destroyCancellationToken).Forget();
        }
        
        

    }
}