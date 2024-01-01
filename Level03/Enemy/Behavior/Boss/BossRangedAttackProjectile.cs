using System;
using Character.Presenter;
using Enemy.Task;
using EnumData;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Enemy.Behavior.Boss
{
    public class BossRangedAttackProjectile : EnemyProjectile
    {
        private float _range;
        private float _moved = 0f;
        private Action<Vector3> _onExplode;

        public override void Initialize(Vector3 direction, float speed, GameObject source, Func<float> damageSupplier = null,
            Func<DamageReaction> reactionSupplier = null)
        {
            base.Initialize(direction, speed, source, damageSupplier, reactionSupplier);
            
            _rigidbody.MoveRotation(Quaternion.LookRotation(direction));
        }

        public void InitializeBossRangedAttack(float range, Action<Vector3> onExplode)
        {
            // DebugX.Log($"range: {range}");
            _range = range;
            _moved = 0f;
            _onExplode = onExplode;
        }
        
        protected override void FixedUpdate()
        {
            if (_moved >= _range)
            {
                Explode();
                return;
            }
            var delta = _speed * Time.deltaTime;
            _rigidbody.MovePosition(_rigidbody.position + _direction * delta);

            _moved += delta;
            // DebugX.Log($"moved: {_moved} (+ {delta})");

        }

        protected override void OnTriggerEnter(Collider c)
        {
            if (c.gameObject.TryGetComponent(out PlayerPresenter player))
            {
                Explode();
                return;
            }
            gameObject.SetActive(false);
        }

        private void Explode()
        {
            _onExplode?.Invoke(transform.position);
            gameObject.SetActive(false);
            _onExplode = null;
        }
    }
}