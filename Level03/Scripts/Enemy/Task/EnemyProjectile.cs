using System;
using Character.Presenter;
using Cysharp.Threading.Tasks;
using Enemy.Behavior;
using EnumData;
using Managers;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace Enemy.Task
{
    public class EnemyProjectile : MonoBehaviour
    {
        public float DestroyAfter = 5f;
        public bool Disable = true;
        public EffectType HitEffectType = EffectType.None;
        public DamageReaction DefaultReaction = DamageReaction.Normal;
        protected Rigidbody _rigidbody;
        protected virtual void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        protected Vector3 _direction;
        protected float _speed;
        protected float _timeToLive;
        protected GameObject _source;
        protected Func<float> _damageSupplier;
        protected Func<DamageReaction> _reactionSupplier;
        protected float DefaultDamageSupplier() => 10f;
        protected DamageReaction DefaultReactionSupplier() => DefaultReaction;
        public virtual void Initialize(Vector3 direction, float speed, GameObject source,
            Func<float> damageSupplier = null,
            Func<DamageReaction> reactionSupplier = null
        )
        {
            direction.Normalize();
            _direction = direction;
            _speed = speed;
            _source = source;
            _damageSupplier = damageSupplier;
            _reactionSupplier = reactionSupplier;
            _timeToLive = DestroyAfter;
        }
        
        protected virtual void FixedUpdate()
        {
            _rigidbody.MovePosition(_rigidbody.position + _direction * (_speed * Time.deltaTime));
        }

        protected virtual void Update()
        {
            if (DestroyAfter > 0f)
            {
                if (_timeToLive <= 0f)
                {
                    if (Disable)
                    {
                        gameObject.SetActive(false);
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                    return;
                }
            
                _timeToLive -= Time.deltaTime;
            }
        }

        protected virtual void OnTriggerEnter(Collider c)
        {
            bool createHitEffect = false;
            if (c.gameObject.TryGetComponent(out PlayerPresenter player))
            {
                player.Damage(
                    (_damageSupplier ??= DefaultDamageSupplier)(), 
                    _source,
                    (_reactionSupplier ??= DefaultReactionSupplier)()
                );
                //player.Damage(10f, gameObject);
                DebugX.Log("플레이어 피격 by 투사체");

                createHitEffect = true;
            }
            // 빙결되지 않은 적은 Trigger 판정 X
            else if (c.gameObject.CompareTag("Enemy") && c.gameObject.TryGetComponent(out Monster m))
            {
                if (!m.IsFreeze)
                {
                    return;
                }
                createHitEffect = true;
            }

            if (createHitEffect && HitEffectType != EffectType.None)
            {
                var effect = EffectManager.Instance.Get(HitEffectType);
                effect.transform.position = transform.position;
            }

            if (Disable)
            {
                gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}