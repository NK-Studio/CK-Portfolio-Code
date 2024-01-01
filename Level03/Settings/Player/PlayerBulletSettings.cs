using Character.Core.Weapon;
using Cinemachine;
using EnumData;
using FMODUnity;
using Managers;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Settings.Player
{
    public enum PlayerBulletGrade
    {
        Normal, Rare, Epic,
    }
    
    [CreateAssetMenu(fileName = "New PlayerBulletSettings", menuName = "Settings/Player Bullet/General Player Bullet", order = 0)]
    public class PlayerBulletSettings : ScriptableObject
    {
        [field: SerializeField, BoxGroup("정보")] 
        public PlayerBulletGrade Grade { get; protected set; } = PlayerBulletGrade.Normal;
        
        ///////////////////
        
        [field: SerializeField, BoxGroup("피격"), Tooltip("몬스터 대상 적용되는 빙결 단계. 99는 최대 중첩 및 빙결 대상으로는 즉사시킵니다.")] 
        public float FreezePower { get; protected set; } = 1f; // TODO 적용 필요
        
        [field: SerializeField, BoxGroup("피격"), Tooltip("보스 대상 피해량입니다.")] 
        public float DamageToBoss { get; protected set; } = 1f;
        
        [field: SerializeField, BoxGroup("피격"), Tooltip("피격 범위로 원을 그릴 때 반지름의 길이를 의미합니다.")] 
        public float HitRange { get; protected set; } = 0f;
        
        ///////////////////
        
        [field: SerializeField, BoxGroup("발사"), Tooltip("공격 가능한 최대 횟수입니다.")] 
        public int MaxAmmo { get; protected set; } = 12;
        
        [field: SerializeField, BoxGroup("발사"), Tooltip("1회 공격 시 날아가는 탄환의 수 입니다.")] 
        public int Count { get; protected set; } = 1;
        
        [field: SerializeField, BoxGroup("발사"), Tooltip("최대 사거리. 최대 사거리 도달 시 사라집니다.")] 
        public float MaxDistance { get; protected set; } = 10f;
        
        [field: SerializeField, BoxGroup("발사"), Tooltip("탄속입니다.")] 
        public float Speed { get; protected set; } = 10f;
        
        [field: SerializeField, BoxGroup("발사"), Tooltip("추가 발사 쿨타임입니다. 최종 발사 간격은 애니메이션 클립 재생 시간에 더해집니다.")] 
        public float CoolTime { get; protected set; } = 0.2f;
        
        [field: SerializeField, BoxGroup("발사"), Tooltip("재장전 시간입니다. 음수로 설정 시 재장전이 불가능합니다.")] 
        public float ReloadTime { get; protected set; } = -1f;
        
        [field: SerializeField, BoxGroup("발사/보정"), Tooltip("공격 시 보정 되는 범위를 나타내는 부채꼴 내각을 의미합니다.")] 
        public float CompensationAngle { get; protected set; } = 15f;
        
        [field: SerializeField, BoxGroup("발사/보정"), Tooltip("게임패드 사용 시 보정 범위 내각 배율입니다.")] 
        public float CompensationAngleMultiplierOnStrongCompensate { get; protected set; } = 2f;

        public float CompensationAngleOnStrongCompensate =>
            CompensationAngle * CompensationAngleMultiplierOnStrongCompensate;
        
        
        ///////////////////
        
        [field: SerializeField, BoxGroup("애니메이션"), Tooltip("애니메이션 클립 재생 시간입니다.")] 
        public float AnimationDuration { get; protected set; } = 0.2f;
        
        [field: SerializeField, BoxGroup("애니메이션"), Tooltip("발사 중에 적용되는 MovementSpeed의 배율입니다.")] 
        public float Deceleration { get; protected set; } = 0.8f;
        
        ///////////////////
        
        [field: SerializeField, BoxGroup("이펙트")] 
        public EffectType BulletType { get; protected set; } = EffectType.PlayerIceBullet;
        
        [field: SerializeField, BoxGroup("이펙트")] 
        public EffectType TrailType { get; protected set; } = EffectType.PlayerIceBulletTrail;
        
        [field: SerializeField, BoxGroup("이펙트")] 
        public EffectType MuzzleFlashType { get; protected set; } = EffectType.PlayerMuzzleFlash;

        ///////////////////

        [field: SerializeField, BoxGroup("게임패드")]
        public GamePadManager.RumbleSettings RumbleOnShoot { get; protected set; } = new(.2f, .4f, .2f);

        ///////////////////
        
        [field: SerializeField, BoxGroup("카메라")]
        public CinemachineImpulseDefinition ImpulseDefinition { get; protected set; } = new()
        {
            m_ImpulseChannel = 1,
            m_ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump,
            m_CustomImpulseShape = new AnimationCurve(),
            m_ImpulseDuration = 0.2f,
            m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
            m_DissipationDistance = 100,
            m_DissipationRate = 0.25f,
            m_PropagationSpeed = 343
        };
        [field: SerializeField, BoxGroup("카메라")]
        public Vector3 ImpulseVelocity { get; protected set; } = new(0f, 0f, 1f);
        
        ///////////////////

        [field: SerializeField, BoxGroup("아이템")]
        public ItemType Item { get; private set; } = ItemType.None;
        
        [field: SerializeField, BoxGroup("아이템")]
        public Color ItemMaterialTint { get; protected set; } = Color.yellow;

        public virtual PlayerBulletMagazine CreateMagazine() 
            => new PlayerBulletMagazine(this);
        
        
        [field: SerializeField, BoxGroup("사운드")]
        public EventReference ShootSound { get; protected set; }
        
        [field: SerializeField, BoxGroup("UI")]
        public Sprite TypeIconSprite { get; protected set; }
        
#if UNITY_EDITOR
        // 에디터 값 변경 대응
        public float CompensationAngleInCos => Mathf.Cos(CompensationAngle * Mathf.Deg2Rad * 0.5f);
        public float CompensationAngleOnStrongCompensateInCos => Mathf.Cos(CompensationAngleOnStrongCompensate * Mathf.Deg2Rad * 0.5f);
#else
            private float? _compensationAngleInCos = null;
            private float? _compensationAngleOnStrongCompensateInCos = null;
            public float CompensationAngleInCos => _compensationAngleInCos ??= Mathf.Cos(CompensationAngle * Mathf.Deg2Rad * 0.5f);
            public float CompensationAngleOnStrongCompensateInCos 
                => _compensationAngleOnStrongCompensateInCos ??= Mathf.Cos(CompensationAngleOnStrongCompensate * Mathf.Deg2Rad * 0.5f);
#endif
        
        [field: SerializeField, BoxGroup("발사/보정"), Tooltip("조준 보정 시 단일 대상에 대해 각도 내적값에 따라 얼마나 보정할지를 결정하는 커브입니다.")] 
        public AnimationCurve CompensationCurve { get; protected set; } = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [field: SerializeField, BoxGroup("발사/보정"), Tooltip("다중 보정 시 각도 내적값에 대한 가중치 커브입니다. 시간 값이 1에 가까울수록 높으면 방향에 가까운 쪽에 높은 가중치가 설정됩니다.")] 
        public AnimationCurve MultiCompensationCurve { get; protected set; } = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        

    }
}