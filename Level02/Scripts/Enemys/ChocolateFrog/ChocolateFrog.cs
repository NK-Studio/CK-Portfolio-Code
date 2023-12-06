using AutoManager;
using Character.Controllers;
using Character.Core;
using FMODUnity;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using Unity.VisualScripting;
using UnityEngine;
using Utility;
using Zenject;

namespace Enemys
{
    public class ChocolateFrog : Enemy
    {
        public enum AnimationState
        {
            Show,
            Attack,
            Dead,
        }

        [ValidateInput("@settings != null", "초콜릿 개구리 세팅 파일이 빠져있습니다.")] [SerializeField]
        private ChocolateFrogSettings settings;

        [ValidateInput("@_bullet != null", "탄 오브젝트가 비어있습니다."), SerializeField]
        private Bullet _bullet;

        [ValidateInput("@bulletSpawnPosition != null", "탄 생성 위치 오브젝트가 비어있습니다."), SerializeField]
        private Transform bulletSpawnPosition;

        [ValidateInput("@GFXTransform != null", "초콜릿 개구리의 GFX Transform이 연결되지 않았습니다."), SerializeField]
        private Transform GFXTransform;

        public bool LookAtWeight { get; private set; }

        [SerializeField] private EventReference WaterFall;
        
        [Title("디버그 모드")] [SerializeField] private bool isDebugMode;

        [Title("옵션")] [SerializeField] private bool ShowWaterEffect;

        [ShowIf("@ShowWaterEffect"), SerializeField]
        private float WaterY;

        [Inject] private DiContainer _container;

        private Transform _player;

        private float _coolTime;
        private ObservableStateMachineTrigger _stateMachineTrigger;
        private static readonly int OnDead = Animator.StringToHash("OnDead");
        private static readonly int OnAttack = Animator.StringToHash("OnAttack");

        protected override void Awake()
        {
            base.Awake();
            HP = settings.HPMax;
            _stateMachineTrigger = EnemyAnimator.GetBehaviour<ObservableStateMachineTrigger>();

            PlayerController player = FindObjectOfType<PlayerController>();
            if (player)
                _player = player.transform;
        }

        private void Start()
        {
            _stateMachineTrigger.OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("Idle"))
                .Subscribe(_ => CustomEvent.Trigger(gameObject, "Idle"))
                .AddTo(this);

            #region 별사탕에 맞아서 사망

            this.OnTriggerEnterAsObservable()
                .Where(coll => coll.CompareTag("ExplosionRange"))
                .Subscribe(_ => TakeDamage(1, gameObject))
                .AddTo(this);

            #endregion

            #region 물에 풍덩

            this.OnTriggerEnterAsObservable()
                .Where(coll => coll.CompareTag("Water"))
                .Subscribe(_ =>
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
                }).AddTo(this);

            #endregion
        }

        private void FixedUpdate()
        {
            //기본 물리
            EnemyController.Core(settings.gravity);

            //플레이어 바라보기
            if (LookAtWeight)
                LookAtPlayer();
        }

        private void LookAtPlayer()
        {
            Vector3 direction = Quaternion.LookRotation(_player.position - GFXTransform.position).eulerAngles;

            Vector3 destination = new Vector3(0, direction.y, 0);
            GFXTransform.eulerAngles =
                Vector3.Lerp(GFXTransform.eulerAngles, destination, Time.deltaTime * settings.TurnSpeed);
        }

        /// <summary>
        /// 트래킹 범위 안에 있을 경우 
        /// </summary>
        /// <returns></returns>
        public bool IsPlayerInTrackingRange()
        {
            float distanceFromPlayer = EnemyController.GetDistanceFromTarget(PlayerTransform);

            bool isPlayerInButtRange = distanceFromPlayer <= settings.TrackingRange;
            return isPlayerInButtRange;
        }

        /// <summary>
        /// 공격 범위 안에 있을 경우 
        /// </summary>
        /// <returns></returns>
        public bool IsPlayerInAttackRange()
        {
            float distanceFromPlayer = EnemyController.GetDistanceFromTarget(PlayerTransform);

            bool isPlayerInButtRange = distanceFromPlayer <= settings.AttackRange;
            return isPlayerInButtRange;
        }

        /// <summary>
        /// 리그의 무게를 조절합니다.
        /// </summary>
        public void SetRigWeight(bool weight)
        {
            LookAtWeight = weight;
        }

        /// <summary>
        /// 플레이어에게 공격합니다.
        /// </summary>
        public void Attack()
        {
            //딜레이를 지정합니다.
            _coolTime = settings.AttackDelay;

            //플레이어 바라보기를 끝냅니다.
            LookAtWeight = false;

            //탄을 발사합니다.
            ShotBullet();

            // @ 개구리 공격 사운드
            Manager.Get<AudioManager>().PlayOneShot(settings.SFXClips[0], transform.position);
        }

        private void ShotBullet()
        {
            Vector3 playerCenter = PlayerTransform.position;
            playerCenter.y += 0.75f;
            Vector3 direction = (playerCenter - bulletSpawnPosition.position).normalized;
            Bullet bullet = Instantiate(_bullet, bulletSpawnPosition.position, Quaternion.LookRotation(direction));
            bullet.speed = settings.BulletSpeed;
            bullet.lifeTime = settings.BulletLifeTime;
        }

        /// <summary>
        /// 플레이어가 밀어서 이동시킬 수 있게 합니다.
        /// </summary>
        public void Movable()
        {
            if (TryGetComponent(out Rigidbody rigid))
            {
                rigid.freezeRotation = false;
                rigid.mass = 20;
                rigid.useGravity = true;
            }

            if (TryGetComponent(out CapsuleCollider coll))
            {
                coll.center = new Vector3(0, 0.4166975f, 0);
                coll.material = null;
                coll.radius = 0.270886f;
                coll.height = 0.8397679f;
            }

            if (TryGetComponent(out Mover mover))
                mover.enabled = false;

            //자신의 컴포넌트를 비활성화합니다.
            enabled = false;

            EnemyController.enabled = false;
        }

        /// <summary>
        /// 공격 쿨 타임을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public float GetAttackCoolTime() => _coolTime;

        /// <summary>
        /// 쿨 타임이 0 초과이면 카운트 다운을 합니다.
        /// </summary>
        /// <returns></returns>
        public void CountDownAttackCoolTime()
        {
            if (_coolTime > 0)
                _coolTime = Mathf.Clamp(_coolTime - Time.deltaTime, 0, float.MaxValue);
        }

        /// <summary>
        /// 애니메이션 비헤이비어를 설정합니다.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="active"></param>
        public void OnTriggerAnimation(AnimationState state, bool active = false)
        {
            switch (state)
            {
                case AnimationState.Dead:
                    EnemyAnimator.SetTrigger(OnDead);
                    // @ 개구리 사망 사운드
                    Manager.Get<AudioManager>().PlayOneShot(settings.SFXClips[1], transform.position);
                    Manager.Get<AudioManager>().PlayOneShot(settings.SFXClips[3], transform.position);

                    Movable();
                    break;

                case AnimationState.Attack:
                    EnemyAnimator.SetTrigger(OnAttack);
                    break;
            }
        }

        /// <summary>
        /// 히트 이펙트를 재생합니다.
        /// </summary>
        public void PlayHitEffect()
        {
            //생성할 이펙트
            GameObject effect = _container.ResolveId<GameObject>(EffectType.MonsterHit);

            //생성 위치
            Vector3 position = transform.position;
            position.y += 1.4388f;

            //히트 이펙트 생성
            GameObject hitEffect = Instantiate(effect, position, Quaternion.identity);
            hitEffect.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!isDebugMode) return;
            Vector3 position = transform.position;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(position, settings.AttackRange);
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(position, settings.TrackingRange);
        }
#endif
    }
}