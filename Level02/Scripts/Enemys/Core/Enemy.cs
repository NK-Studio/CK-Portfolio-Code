using System;
using Character.Controllers;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

namespace Enemys
{
    [Serializable]
    public class TargetInfo
    {
        public bool hasTarget;
        public Vector3 position;

        public void Reset()
        {
            hasTarget = false;
            position = Vector3.zero;
        }
        
        public void SetTarget(Vector3 target)
        {
            hasTarget = true;
            position = target;
        }
    }

    public class Enemy : MonoBehaviour
    {
        [ReadOnly, Min(0)] public float HP;

        protected Animator EnemyAnimator;
        protected PlayerController PlayerController;
        protected Transform PlayerTransform;
        protected EnemyController EnemyController;
        
        private const string Death = "Dead";

        [Tooltip("유저에게 공격을 해도 대미지를 주지 않습니다.")]
        public bool IsAttackPlayerAfterDontDamage;
        
        protected virtual void Awake()
        {
            PlayerController = FindObjectOfType<PlayerController>();
            PlayerTransform = PlayerController.transform;

            EnemyController = GetComponent<EnemyController>();
            EnemyAnimator = GetComponentInChildren<Animator>();
        }

        /// <summary>
        /// 플레이어에게 대미지를 줍니다.
        /// </summary>
        /// <param name="knockbackPower"></param>
        /// <param name="playHitEffect"></param>
        public virtual void ApplyDamageToPlayer(float knockbackPower, int damage, bool playHitEffect)
        {
            PlayerController.TakeDamageWithKnockBack(damage, transform.position, knockbackPower, playHitEffect);
        }

        /// <summary>
        /// 점프합니다.
        /// </summary>
        public virtual void AddJump(float jumpPower)
        {
            EnemyController.OnJump = () =>
            {
                EnemyController.CurrentVerticalSpeed = jumpPower;
                EnemyController.IsGrounded = false;
            };
        }

        public Transform GetPlayerTransform()
        {
            return PlayerTransform;
        }

        /// <summary>
        /// 데미지를 받습니다.
        /// </summary>
        /// <param name="damage">피해량입니다.</param>
        /// <param name="from">피해를 준 오브젝트입니다.</param>
        public virtual void TakeDamage(float damage, GameObject from)
        {
            if (HP == 0) return;

            HP -= damage;

            if (HP < 1)
                CustomEvent.Trigger(gameObject, Death);
        }

        public EnemyController GetEnemyController() => EnemyController;
    }
}