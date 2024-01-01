using Enemy.Behavior.TurretMonster;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tutorial
{
    public class TutorialFallProjectile : TurretMonsterProjectile
    {
        [field: SerializeField] 
        protected GameObject _rangeProjector;
        
        protected override void Start()
        {
            ProjectileRangeProjector = _rangeProjector;
            base.Start();

            if (TryGetComponent(out Rigidbody r))
            {
                // TutorialFallProjectile은 transform move
                r.isKinematic = true;
            }
        }
    }
}