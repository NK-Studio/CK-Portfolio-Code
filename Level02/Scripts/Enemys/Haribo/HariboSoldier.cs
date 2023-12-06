using System;
using System.Collections;
using AutoManager;
using FMODUnity;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using Unity.VisualScripting;
using UnityEngine;
using Utility;
using Zenject;
using Random = UnityEngine.Random;

namespace Enemys
{
    public class HariboSoldier : Enemy
    {
        public enum HariboAnimation
        {
            Move, //점프
            Attack, //더블 점프
            Death
        }

        private static readonly int IsMove = Animator.StringToHash("isMove");
        private static readonly int OnAttack = Animator.StringToHash("OnAttack");
        private static readonly int OnDeath = Animator.StringToHash("OnDead");

        public HariboSoldierSettings Settings;

        [Min(0)] public float MoveRandomizeRange = 1f;

        [Min(0)] public float AttackAfterIdleCoolTimeRandomizeRange = 0.5f;

        [Title("디버그 모드")] [SerializeField] private bool isDebugMode;

        public float TurnTrackingToIdle { get; set; }

        [Title("컬러 재질"), SerializeField] private Material[] ColorMaterials;
        private Material _material;

        [Title("스킨니드 렌더러"), ValidateInput("@skinnedRenderer != null", "하리보에 스키니드 렌더러를 연결해야합니다."), SerializeField]
        private SkinnedMeshRenderer skinnedRenderer;

        [Inject] private DiContainer _container;

        [Title("옵션"), SerializeField] private bool ShowWaterEffect;

        [ShowIf("@ShowWaterEffect")] public float WaterY;

        [SerializeField] private EventReference WaterFall;

        protected override void Awake()
        {
            base.Awake();
            HP = Settings.HPMax;

            //랜덤한 컬러
            int randomColor = Random.Range(0, ColorMaterials.Length);
            _material = Instantiate(ColorMaterials[randomColor]);

            //재질 변경
            skinnedRenderer.material = _material;
        }

        private void Start()
        {
            #region 별사탕에 맞아서 사망

            this.OnTriggerEnterAsObservable()
                .Where(coll => coll.CompareTag("ExplosionRange"))
                .Subscribe(_ => TakeDamage(1, gameObject))
                .AddTo(this);

            #endregion
        }

        private void FixedUpdate()
        {
            EnemyController.Core(Settings.gravity);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!isDebugMode) return;
            Vector3 position = transform.position;
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(position, Settings.ButtRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(position, Settings.TrackingRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(position, Settings.AttackRange);
        }
#endif

        /// <summary>
        /// 플레이어한테 선형적으로 돌진
        /// </summary>
        public void DashToPlayer()
        {
            MoveToPlayer(Settings.DashSpeed);
        }

        public override void AddJump(float jumpPower)
        {
            EnemyController.OnJump = () =>
            {
                EnemyController.CurrentVerticalSpeed = jumpPower;
                EnemyController.IsGrounded = false;
                StartCoroutine(CheckGround());
            };
        }

        /// <summary>
        /// 박치기 범위 내 검사 
        /// </summary>
        /// <returns></returns>
        public bool IsPlayerInButtRange()
        {
            float distanceFromPlayer = EnemyController.GetDistanceFromTarget(PlayerTransform);

            bool isPlayerInButtRange = distanceFromPlayer <= Settings.ButtRange;
            return isPlayerInButtRange;
        }

        /// <summary>
        /// 옵션에 따라서 공격을 적용하거나 적용하지 않는다.
        /// </summary>
        /// <param name="damage">IsAttackPlayerAfterDontDamage가 false이면 해당 damage를 적용합니다.</param>
        /// <returns></returns>
        public int ApplyDamageByOption(int damage)
        {
            if (IsAttackPlayerAfterDontDamage)
                return 0;

            return damage;
        }

        /// <summary>
        /// 플레이어에게 이동합니다.
        /// </summary>
        public void MoveToPlayer()
        {
            if (!PlayerTransform) return;
            EnemyController.MoveTo(PlayerTransform.position, Settings.MoveSpeed);
        }

        /// <summary>
        /// 지정한 속도로 플레이어에게 이동합니다.
        /// </summary>
        public void MoveToPlayer(float moveSpeed)
        {
            if (!PlayerTransform) return;
            EnemyController.MoveTo(PlayerTransform.position, moveSpeed);
        }

        /// <summary>
        /// 땅에 닿으면 딜레이 플로우로 넘깁니다.
        /// </summary>
        /// <returns></returns>
        public IEnumerator CheckGround()
        {
            yield return new WaitUntil(() => EnemyController.IsGrounded);
            CustomEvent.Trigger(gameObject, "CoolTime");
        }

        /// <summary>
        /// 뒤로 날려갑니다.
        /// </summary>
        public void KnockBack(Transform target, float knockBackPower, ForceMode mode)
        {
            //생성할 이펙트
            GameObject effect = _container.ResolveId<GameObject>(EffectType.MonsterHit);

            //생성 위치
            Vector3 position = transform.position;
            position.y += 0.43f;

            //히트 이펙트 생성
            Instantiate(effect, position, Quaternion.identity);

            Vector3 direction = (transform.position - target.position).normalized;
            EnemyController.AddForce(direction * knockBackPower, mode);
        }


        #region Thredule

        public void AddTurnTrackingToIdle(float deltaTime)
        {
            TurnTrackingToIdle += deltaTime;
        }

        /// <summary>
        /// moveRandomizeRange를 음수에서 양수 사이로 랜덤하게 반환합니다.
        /// </summary>
        /// <returns></returns>
        public float GetMoveRandomize()
        {
            return Random.Range(-MoveRandomizeRange, MoveRandomizeRange);
        }

        /// <summary>
        /// attackAfterIdleCoolTimeRandomizeRange를 음수에서 양수 사이로 랜덤하게 반환합니다.
        /// </summary>
        /// <returns></returns>
        public float GetAttackAfterIdleCoolTimeRandomize()
        {
            return Random.Range(-AttackAfterIdleCoolTimeRandomizeRange, AttackAfterIdleCoolTimeRandomizeRange);
        }

        #endregion

        #region Animation

        /// <summary>
        /// 애니메이션을 트리거 합니다.
        /// </summary>
        /// <param name="hariboAnimation"></param>
        /// <param name="active"></param>
        public void OnTriggerAnimation(HariboAnimation hariboAnimation, bool active = false)
        {
            switch (hariboAnimation)
            {
                case HariboAnimation.Move:
                    EnemyAnimator.SetBool(IsMove, active);
                    break;
                case HariboAnimation.Attack:
                    EnemyAnimator.SetTrigger(OnAttack);
                    //  @하리보 공격 사운드
                    Manager.Get<AudioManager>().PlayOneShot(Settings.SFXClips[1], transform.position);
                    break;
                case HariboAnimation.Death:
                    EnemyAnimator.SetTrigger(OnDeath);
                    //  @하리보 사망 사운드
                    Manager.Get<AudioManager>().PlayOneShot(Settings.SFXClips[2], transform.position);
                    break;
            }
        }

        #endregion

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Water"))
            {
                //물에 닿으면 사망
                Destroy(gameObject);

                //이펙트가 켜져있으면 처리한다.
                if (!ShowWaterEffect) return;
                Vector3 position = transform.position;
                position.y = WaterY;

                Manager.Get<AudioManager>().PlayOneShot(WaterFall, transform.position);
                GameObject effect = _container.ResolveId<GameObject>(EffectType.WaterSplash);
                Instantiate(effect, position, Quaternion.identity);
            }
        }
    }
}