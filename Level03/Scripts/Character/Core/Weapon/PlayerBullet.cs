using System;
using Character.Behaviour.State;
using Cinemachine;
using Damage;
using Enemy.Behavior;
using EnumData;
using FMODPlus;
using Managers;
using Settings;
using Settings.Player;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.ProBuilder.MeshOperations;

namespace Character.Core.Weapon
{
    public class PlayerBullet : MonoBehaviour
    {
        public PlayerBulletSettings Settings { get; set; } = null;


        public UnityEvent OnDisabled;
        protected Rigidbody Rigidbody;
        protected CinemachineImpulseSource ImpulseSource;
        protected float MaxDistance;

        protected virtual void Awake()
        {
            TryGetComponent(out Rigidbody);
            TryGetComponent(out ImpulseSource);
        }

        protected Vector3 ShootPosition;

        public virtual void SetPositionAndRotation(PlayerShootState state, Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
        }
        
        public virtual void Initialize(PlayerBulletSettings settings, Vector3 position, float maxDistance)
        {
            Settings = settings;
            ShootPosition = position;
            MaxDistance = maxDistance;
            OnDisabled.RemoveAllListeners();
            Rigidbody.isKinematic = true;
            Rigidbody.position = position;
            // _rigidbody.velocity = Vector3.zero;
            // _rigidbody.angularVelocity = Vector3.zero;
            
            if (ImpulseSource)
            {
                ImpulseSource.m_ImpulseDefinition = settings.ImpulseDefinition;
                ImpulseSource.GenerateImpulseWithVelocity(settings.ImpulseVelocity);
            }
            
            AudioManager.Instance.PlayOneShot(settings.ShootSound);
        }
        

        protected virtual void FixedUpdate()
        {
            var t = transform;
            Rigidbody.MovePosition(Rigidbody.position + t.forward * (Settings.Speed * Time.deltaTime));
            // _rigidbody.velocity = t.forward * Settings.Speed;

            var position = t.position;
            var moved = (position - ShootPosition).magnitude;
            if (moved >= MaxDistance)
            {
                gameObject.SetActive(false);
                OnDisabled.Invoke();
            }
        }

        protected virtual EntityHitResult HitEntity(IEntity entity, Collider c)
        {
            return entity.Damage(EnemyDamageInfo.Get(
                Settings.DamageToBoss, gameObject, DamageMode.Normal, DamageReaction.Freeze,
                playerAttackType: PlayerState.PlayerBullet, freezeFactor: Settings.FreezePower, collider: c
            ));
        }
        
        // 어쨌든 충돌했을 때 (벽, 적)
        protected virtual void OnTriggerEnter(Collider other)
        {
            bool spawnHitEffect;
            // 엔티티 타격
            if((other.CompareTag("Enemy") || other.CompareTag("Destructible")) 
               && (other.TryGetComponent(out IEntity entity) || other.attachedRigidbody.TryGetComponent(out entity)))
            {
                var result = HitEntity(entity, other);
                if (result == EntityHitResult.Ignored)
                {
                    // 관통
                    return;
                }
                spawnHitEffect = result is EntityHitResult.Success or EntityHitResult.Invincible;
            }
            else
            {
                spawnHitEffect = true;
            }

            if(spawnHitEffect)
                EffectManager.Instance.Get(EffectType.EnemyHitProjectile).transform.position = transform.position + Vector3.up * 0.5f;
            // IceSpike도 이펙트 취급이니 일단 disable
            gameObject.SetActive(false);
            OnDisabled.Invoke();
        }
    }
}