using Damage;
using Settings;
using UnityEngine;

namespace Enemy.Behavior.BoxMonster 
{
    public class BoxMonster : Monster
    {
        private BoxMonsterSettings _settings;
        public BoxMonsterSettings BoxSettings => _settings ??= (BoxMonsterSettings)base.Settings;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (Rigidbody)
            {
                Rigidbody.interpolation = RigidbodyInterpolation.None;
                Rigidbody.constraints = RigidbodyConstraints.FreezePosition;
            }
            if (NavMeshAgent)
            {
                NavMeshAgent.enabled = false; // 엄
            }
        }
    }
}