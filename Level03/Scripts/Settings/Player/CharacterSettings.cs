using System;
using System.Collections.Generic;
using Character.Core;
using Dummy.Scripts;
using EnumData;
using Managers;
using Settings.Item;
using Settings.Player;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Utility;

namespace Settings
{
    [Serializable]
    public class KeyAudioPathDictionary : UnitySerializedDictionary<string, string>
    {
    }

    [CreateAssetMenu(fileName = "New CharacterSettings", menuName = "Settings/CharacterSettings", order = 2)]
    public class CharacterSettings : ScriptableObject
    {
        [SerializeField] private bool showDebug;

        #region 기본

        [field: SerializeField, FoldoutGroup("기본", true), Tooltip("최대 체력")]
        public float MaximumHealth { get; private set; } = 3f;
            
        [field: SerializeField, FoldoutGroup("기본", true), Tooltip("체력 아이템 자동 획득 범위")]
        public float HealthItemRange { get; private set; } = 1f;

        [field: SerializeField, FoldoutGroup("기본", true), Tooltip("체력 커스터마이저")]
        public ItemDropTablePercentageCustomizer HealthItemDropCustomizer { get; private set; }

        [field: SerializeField, FoldoutGroup("기본", true), Tooltip("체력 커스터마이저 그래프. 가로로는 체력, 세로로는 확률")]
        public AnimationCurve HealthItemDropCustomizerMappingCurve { get; private set; } = new AnimationCurve(
            new Keyframe(0.0f, 30),
            new Keyframe(0.5f, 0.30f),
            new Keyframe(1.0f, 0.25f),
            new Keyframe(1.5f, 0.20f),
            new Keyframe(2.0f, 0.15f),
            new Keyframe(2.5f, 0.10f),
            new Keyframe(3.0f, 0.05f)
        );
        
        [field: SerializeField, FoldoutGroup("기본", true), Tooltip("이동 속도")]
        public float MovementSpeed { get; private set; } = 7;

        [field: SerializeField, FoldoutGroup("기본", true), Tooltip("GFX 회전 속도")]
        public float GFXRotateSpeed { get; private set; } = 10;

        [field: SerializeField, FoldoutGroup("기본", true), Tooltip("GFX 각도에 따른 회전 속도 조절. 시간 0 ~ 1은 각각 각도 0 ~ 180º에 대응됨")]
        public AnimationCurve GFXRotateCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [field: SerializeField, FoldoutGroup("기본/카메라 줌 인·아웃", true), Tooltip("카메라 줌 인/아웃 속도")]
        public float CameraZoomSpeed { get; private set; } = 60f;

        [field: SerializeField, FoldoutGroup("기본/카메라 줌 인·아웃", true), Tooltip("카메라 줌 인/아웃 따라가는 속도")]

        public float CameraZoomFollowSpeed { get; private set; } = 30f;

        [field: SerializeField, FoldoutGroup("기본/카메라 줌 인·아웃", true), Tooltip("카메라 줌 인/아웃 거리 범위")]
        public FloatRange CameraZoomDistanceRange { get; private set; } = new FloatRange(4f, 16f);

        [field: SerializeField, FoldoutGroup("기본/카메라 줌 인·아웃", true), Tooltip("카메라 줌 인/아웃 회전 커브")]
        public AnimationCurve CameraZoomRotationCurve { get; private set; } = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [field: SerializeField, FoldoutGroup("기본/카메라 줌 인·아웃", true), Tooltip("카메라 줌 인/아웃 회전 범위")]
        public FloatRange CameraZoomRotationRange { get; private set; } = new FloatRange(0f, 45f);

        [field: SerializeField, FoldoutGroup("기본", true), Tooltip("이동 시 BlendTree 애니메이션 인자 Lerp 속도")]
        public float BlendTreeLerpSpeed { get; private set; } = 10f;

        [field: SerializeField, FoldoutGroup("기본/카메라 Panning", true), Tooltip("패닝 Lerp 속도")]
        public float CameraPanningLerpSpeed { get; private set; } = 10f;

        [field: SerializeField, FoldoutGroup("기본/카메라 Panning", true), Tooltip("수평 패닝 커브")]
        public AnimationCurve CameraPanningHorizontalCurve { get; private set; } =
            AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [field: SerializeField, FoldoutGroup("기본/카메라 Panning", true), Tooltip("수직 패닝 커브")]
        public AnimationCurve CameraPanningVerticalCurve { get; private set; } = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        //착륙 애니메이션을 위한 속도 임계값;
        //애니메이션은 하향 속도가 이 임계값을 초과하는 경우에만 트리거됩니다.
        public float LandVelocityThreshold = 5f;

        #endregion

        #region 공격

        [Serializable, LabelWidth(150f)]
        public class PlayerAttackSettings
        {
            public DamageSettings Damage = 1;
            public KnockBackSettings KnockBack = new();
            [Tooltip("빙결 여부")] public bool Freeze = false;

            [Tooltip("빙결 수치: 보스에게 적용되는 빙결 저항 감소 수치. 빙결 여부 = true인 경우에만 적용")]
            public float FreezeFactorToBoss = 10f;
        }

        #region (Archived) 근접 공격

        // [field: SerializeField, FoldoutGroup("공격", true), Tooltip("1타 공격")]
        public PlayerAttackSettings Attack01Settings { get; private set; } = new()
        {
            Damage = 1, KnockBack = new KnockBackSettings(50f, 0.2f), Freeze = false
        };

        // [field: SerializeField, FoldoutGroup("공격", true), Tooltip("2타 공격")]
        public PlayerAttackSettings Attack02Settings { get; private set; } = new()
        {
            Damage = 1, KnockBack = new KnockBackSettings(50f, 0.2f), Freeze = true
        };

        // [field: SerializeField, FoldoutGroup("공격", true), Tooltip("3타 공격")]
        public PlayerAttackSettings Attack03Settings { get; private set; } = new()
        {
            Damage = 1, KnockBack = new KnockBackSettings(150f, 0.2f), Freeze = false
        };

        // [field: SerializeField, FoldoutGroup("공격", true), Tooltip("4타 공격")]
        public PlayerAttackSettings Attack04Settings { get; private set; } = new()
        {
            Damage = 1, KnockBack = new KnockBackSettings(150f, 0.2f), Freeze = true
        };

        #endregion


        [field: SerializeField, FoldoutGroup("빙결", true), Tooltip("빙결 시간: 빙결 공격에 피격된 적에게 생성되는 빙하의 지속 시간입니다.")]
        public float FreezeTime { get; private set; } = 2f;

        [field: SerializeField, FoldoutGroup("빙결", true),
                Tooltip("빙하 파괴 추가 피해량: 빙결 상태의 적을 공격 시에 빙하를 파괴하며 가하는 추가 데미지입니다.")]
        public float FreezeBreakDamage { get; private set; } = 9999f;

        [field: SerializeField, FoldoutGroup("빙결/동상", true), Tooltip("동상 세기: 동상에 의해 모든 행동에 배속이 적용되는 수치입니다.")]
        public float FrostbiteAnimationSpeed { get; private set; } = 0.7f;

        [field: SerializeField, FoldoutGroup("빙결/동상", true), Tooltip("동상 시간: 동상이 지속되는 시간입니다. 동상 중에는 빙결되지 않습니다.")]
        public float FrostbiteTime { get; private set; } = 5f;

        // [field: SerializeField, FoldoutGroup("공격/카메라 흔들림", true)]
        public CameraShakeSettings AttackCameraShake { get; private set; } = new(
            new FloatRange(-0.2f, 0.2f),
            new FloatRange(-0.2f, 0.2f),
            1f
        );

        [Serializable, InlineProperty]
        public class DamageSettings
        {
            [HorizontalGroup(), LabelWidth(50f)] public float Amount = 10f;

            public static implicit operator DamageSettings(float value)
            {
                return new DamageSettings { Amount = value };
            }

            public static implicit operator float(DamageSettings settings)
            {
                return settings.Amount;
            }
        }

        [Serializable, InlineProperty]
        public class KnockBackSettings
        {
            [HorizontalGroup(Width = 0.4f), LabelWidth(50f)]
            public float Amount;

            [HorizontalGroup(Width = 0.3f), LabelWidth(30f)]
            public float Time;

            [HorizontalGroup(Width = 0.3f), LabelWidth(30f)]
            public float FreezeFactor = 0.1f;

            public float FreezeSlippingAmount => Amount * FreezeFactor;

            public KnockBackSettings(float amount = 10f, float time = 0.2f, float freezeFactor = 0.1f)
            {
                Amount = amount;
                Time = time;
                FreezeFactor = freezeFactor;
            }

            public void Deconstruct(out float amount, out float time, out float freezeFactor)
            {
                amount = Amount;
                time = Time;
                freezeFactor = FreezeFactor;
            }
        }

        [Serializable, InlineProperty, LabelWidth(150f)]
        public class GeneralSkillSettings
        {
            public float CoolTime = 1f;
            public DamageSettings Damage = 10f;
            public float InvincibleTime = 3f;
        }

        [field: FoldoutGroup("공격", true)]
        [field: SerializeField]
        public PlayerBulletSettings DefaultBulletSettings { get; private set; }

        [Serializable]
        public class HammerSkillSettings : GeneralSkillSettings
        {
            public AnimationCurve CameraMultiplierCurve = AnimationCurve.Constant(0f, 1f, 0.7f);
            public float TimeScaleDuration = 0.3f;
            public float TimeScale = 0.25f;
            public AnimationCurve TimeScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            public AnimationCurve RadialBlurIntensityCurve = AnimationCurve.Constant(0f, 1f, 1f);
            public AnimationCurve ChromaticAberrationIntensityCurve = AnimationCurve.Constant(0f, 1f, 1f);
            public AnimationCurve ScreenFillerAlphaCurve = AnimationCurve.Constant(0f, 1f, 1f);
            public KnockBackSettings KnockBack = new(50f, 0.2f);
            public CameraShakeSettings CameraShakeSettings = new();
            public const float AttackAngle = 180f;
            public float AdditionalAngle = 0f;
            public AnimationCurve HammerAngleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            public float InvincibleTimeAfterHammerSwing = 0.1f;
            public GamePadManager.RumbleSettings RumbleOnHitFrozenEnemy = new(1f, 0.5f, 0.15f);

            public AnimationCurve DashSpeedCurveByTime = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            public float DashSpeed = 8f;
            public float DashErrorTargetFrame = 1f/60f;
            public float DashErrorThreshold = 1f;
        }

        [field: SerializeField, FoldoutGroup("공격/Hammer", true)]
        public HammerSkillSettings HammerSettings { get; private set; } = new()
        {
            CoolTime = 0f, Damage = 0f
        };

        [field: SerializeField, FoldoutGroup("공격/Hammer", true)]
        public HammerSkillSettings SpecialHammerSettings { get; private set; } = new()
        {
            CoolTime = 0f, Damage = 0f
        };

        #region (Archived) 스킬

        [Serializable]
        public class IceSpikeSkillSettings : GeneralSkillSettings
        {
            public const int MaximumCount = 1;
            public float Speed = 10f;
            public float MaxDistance = 50f;

            public bool Explode => true;
            // public bool Explode => PrototypeSettings.Instance.Shoot == PrototypeSettings.ShootType.Explode;
        }

        // [field: SerializeField, FoldoutGroup("공격/스킬/IceSpike", true)]
        public IceSpikeSkillSettings IceSpikeSettings { get; private set; } = new()
        {
            Damage = 0, InvincibleTime = 0.5f
        };


        [Serializable]
        public class IceSpraySkillSettings : GeneralSkillSettings
        {
            public KnockBackSettings KnockBack = new(50f, 0.2f);
            public CameraShakeSettings CameraShakeSettings = new();
        }

        // [field: SerializeField, FoldoutGroup("공격/스킬/IceSpray", true)]
        public IceSpraySkillSettings IceSpraySettings { get; private set; } = new()
        {
            CoolTime = 5f, Damage = 30f, InvincibleTime = 25f / 30f
        };


        #region Deprecated Skills(SwordAura, SectorAttack, ZSlash, FlashAttack)

        [Serializable]
        public class ZSlashSkillSettings : GeneralSkillSettings
        {
            public DamageSettings PopAllDamage = 30f;
            public KnockBackSettings PopAllKnockBack = new();
            public float Distance = 8f;
            public float ForwardRayStartHeight = 5f;
            public float RayBackAmount = 5f;
            public AnimationCurve CameraFollowCurve = new(); // TODO
            public CameraShakeSettings CameraShakeSettings = new();
        }

        // [field: SerializeField, FoldoutGroup("공격/스킬/ZSlash", true)]
        public ZSlashSkillSettings ZSlashSettings { get; private set; } = new()
        {
            CoolTime = 15f, Damage = 10f, PopAllDamage = 30f, InvincibleTime = 50f / 30f
        };

        public float ZSlashCoolTime => ZSlashSettings.CoolTime;
        public float ZSlashDistance => ZSlashSettings.Distance;
        public float ZSlashForwardRayStartHeight => ZSlashSettings.ForwardRayStartHeight;
        public float ZSlashForwardRayBackAmount => ZSlashSettings.RayBackAmount;
        public AnimationCurve ZSlashCameraFollowCurve => ZSlashSettings.CameraFollowCurve;

        /////////////////////////////////////

        [Serializable]
        public class SwordAuraSkillSettings : GeneralSkillSettings
        {
            public KnockBackSettings KnockBack = new(100f, 0.1f);
            public float RotateAngleInDegrees = 40f;
            public CameraShakeSettings CameraShakeSettings = new();
        }

        // [field: SerializeField, FoldoutGroup("공격/스킬/SwordAura", true)]
        public SwordAuraSkillSettings SwordAuraSettings { get; private set; } = new()
        {
            CoolTime = 8f, Damage = 15f, InvincibleTime = 50f / 30f
        };

        public float SwordAuraRotateAngleInDegrees => SwordAuraSettings.RotateAngleInDegrees;
        public float SwordAuraCoolTime => SwordAuraSettings.CoolTime;

        /////////////////////////////////////

        [Serializable]
        public class SectorAttackSkillSettings : GeneralSkillSettings
        {
            public KnockBackSettings KnockBack = new(50f, 0.2f);
            public CameraShakeSettings CameraShakeSettings = new();
        }

        // [field: SerializeField, FoldoutGroup("공격/스킬/SectorAttack", true)]
        public SectorAttackSkillSettings SectorAttackSettings { get; private set; } = new()
        {
            CoolTime = 5f, Damage = 30f, InvincibleTime = 25f / 30f
        };

        public float SectorAttackCoolTime => SectorAttackSettings.CoolTime;

        ///////////////////////////////////// 

        [Serializable]
        public class FlashAttackSkillSettings : GeneralSkillSettings
        {
            public KnockBackSettings KnockBack = new(50f, 0.2f);
            public float MaxDistance = 10f;
            public AnimationCurve CameraFollowCurve = new();
            public CameraShakeSettings CameraShakeSettings = new();
        }

        // [field: SerializeField, FoldoutGroup("공격/스킬/FlashAttack", true)]
        public FlashAttackSkillSettings FlashAttackSettings { get; private set; } = new()
        {
            Damage = 30f, CoolTime = 9f, InvincibleTime = 1f
        };

        public float FlashAttackCoolTime => FlashAttackSettings.CoolTime;
        public float FlashAttackMaxDistance => FlashAttackSettings.MaxDistance;
        public AnimationCurve FlashAttackCameraFollowCurve => FlashAttackSettings.CameraFollowCurve;

        ///////////////////////////////////// 

        [Serializable]
        public class TimeCutterSkillSettings : GeneralSkillSettings
        {
            public const int MaximumCount = 3;
            public KnockBackSettings KnockBack = new(50f, 0.2f);
            public CameraShakeSettings CameraShakeSettings = new();
            public float TimeScale = 0f;
            public float TimeScaleDuration = 6f;
            public int InitialCount = MaximumCount;
            public FloatRange DistanceRange = new(2f, 10f);
            public float RushSpeed = 10f;
        }

        // [field: SerializeField, FoldoutGroup("공격/스킬/TimeCutter", true)]
        public TimeCutterSkillSettings TimeCutterSettings { get; private set; } = new()
        {
            Damage = 10f, InvincibleTime = 0.5f, CoolTime = 6f
        };

        [Serializable]
        public class CircleAttackSkillSettings : GeneralSkillSettings
        {
            public DamageSettings PopAllDamage = 30f;
            public KnockBackSettings PopAllKnockBack = new(100f, 0.2f);
            public float TimeScale = 0f;
            public CameraShakeSettings CameraShakeSettings = new();
        }

        // [field: SerializeField, FoldoutGroup("공격/스킬/CircleAttack", true)]
        public CircleAttackSkillSettings CircleAttackSettings { get; private set; } = new()
        {
            Damage = 10f, PopAllDamage = 30f, InvincibleTime = 4f
        };

        public float CircleAttackTimeScale => CircleAttackSettings.TimeScale;

        #endregion

        #endregion

        #endregion

        #region 점멸

        [field: SerializeField, FoldoutGroup("대시", true), Tooltip("점멸 거리")]
        public float FlashDistance { get; private set; } = 5f;

        [field: SerializeField, FoldoutGroup("대시", true), Tooltip("점멸 쿨타임")]
        public float FlashCooldown { get; private set; } = 3;

        [field: SerializeField, FoldoutGroup("대시", true), Tooltip("점멸 게이지 차는 시간 (0 -> 1)")]
        public float FlashFillTime { get; private set; } = 5; // 5s

        [field: SerializeField, FoldoutGroup("대시", true), Tooltip("점멸 연속 사용 횟수")]
        public int FlashCount { get; private set; } = 3;

        public float FlashUseGaugeAmount => 1f / FlashCount; // 33.3%
        public float FlashFillSpeed => 1f / FlashFillTime; // 20%/s

        [field: SerializeField, FoldoutGroup("대시/경계선 보정", true), Tooltip("SamplePosition 횟수")]
        public int EdgeDashSamplePositionCount { get; private set; } = 3;
        [field: SerializeField, FoldoutGroup("대시/경계선 보정", true), Tooltip("SamplePosition 횟수")]
        public float EdgeDashSamplePositionRange { get; private set; } = 2f;
        [field: SerializeField, FoldoutGroup("대시/경계선 보정", true), Tooltip("SamplePosition 횟수")]
        public float EdgeDashSamplePositionLargeRange { get; private set; } = 5f;
        [field: SerializeField, FoldoutGroup("대시/경계선 보정", true), Tooltip("SamplePosition 횟수")]
        public float EdgeDashSamplePositionLimitAngle { get; private set; } = 10f;
#if UNITY_EDITOR
        public float EdgeDashSamplePositionLimitAngleInCos => 
#else
        private float? _edgeDashSamplePositionLimitAngleInCos = null;
        public float EdgeDashSamplePositionLimitAngleInCos => _edgeDashSamplePositionLimitAngleInCos ??=
#endif
            Mathf.Cos(EdgeDashSamplePositionLimitAngle * Mathf.Deg2Rad);

        [field: SerializeField, FoldoutGroup("대시/세부 설정"), Tooltip("플레이어 정면으로 발사하는 광선의 시작 높이")]
        public float FlashForwardRayStartHeight { get; private set; } = 3f;

        [field: SerializeField, FoldoutGroup("대시/세부 설정"), Tooltip("플레이어 정면으로 발사하는 광선이 닿으면 반대 방향으로 살짝 이동하는 거리입니다.")]
        public float FlashForwardRayBackAmount { get; private set; } = 0.5f;

        [field: SerializeField, FoldoutGroup("대시/세부 설정"), Tooltip("점멸 시 카메라가 조금 느리게 따라오도록 하는 시간입니다.")]
        public float FlashCameraFollowDelay { get; private set; } = 0.01f;

        [field: SerializeField, FoldoutGroup("대시/세부 설정"), Tooltip("점멸 이동 시간입니다.")]
        public float FlashDuration { get; private set; } = 12f / 30f; // 6프레임

        [field: SerializeField, FoldoutGroup("대시/세부 설정"), Tooltip("정규화된 시간에 따른 위치 커브입니다.")]
        public AnimationCurve FlashPositionCurve { get; private set; } = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [field: SerializeField, FoldoutGroup("대시/세부 설정"), Tooltip("점멸 시 메시가 다시 보일 때 까지 걸리는 시간입니다.")]
        public float FlashMeshHideTime { get; private set; } = 0.416f; // 기본값: 60Hz 기준 25프레임

        [field: SerializeField, FoldoutGroup("대시/테스트용 세부 설정"), Tooltip("선딜")]
        public float FlashTestBeforeDelay { get; private set; } = 0.25f;

        [field: SerializeField, FoldoutGroup("대시/테스트용 세부 설정"), Tooltip("후딜")]
        public float FlashTestAfterDelay { get; private set; } = 0.25f;

        #endregion

        #region 신력

        // [field: SerializeField, FoldoutGroup("신력", true), Tooltip("몬스터 1타 공격 당 신력 채워지는 양(%)")]
        public float SoulAmountByAttackMonster { get; private set; } = 1f;

        #endregion

        #region 이동 물리

        // [field: SerializeField, FoldoutGroup("이동 물리", expanded: false), Tooltip("컨트롤러의 변환을 기준으로 운동량을 계산하고 적용할지 여부입니다.")]
        public bool UseLocalMomentum { get; private set; }

        #region 공중 제어

        //플레이어가 땅에 닿은 경우 'GroundFriction'이 대신 사용됩니다.
        // [field: SerializeField, FoldoutGroup("이동 물리/공중 제어"), Tooltip("컨트롤러가 공중에서 얼마나 빨리 운동량을 잃는지 결정합니다.")]
        public float AirFriction { get; private set; } = 0.5f;

        // [field: SerializeField, FoldoutGroup("이동 물리/공중 제어")]
        public float GroundFriction { get; private set; } = 100f;

        //값이 높을수록 더 많은 공기 제어가 가능합니다.
        // [field: SerializeField, FoldoutGroup("이동 물리/공중 제어"), Tooltip("컨트롤러가 공중에서 얼마나 빨리 방향을 바꿀 수 있는지 결정합니다.")]
        public float AirControlRate { get; private set; } = 2f;

        #endregion

        #region 중력 및 경사면

        //하향 중력의 양;
        // [field: SerializeField, FoldoutGroup("이동 물리/중력 및 경사면")]
        public float Gravity { get; private set; } = 30f;

        // [field: SerializeField, FoldoutGroup("이동 물리/중력 및 경사면"), Tooltip("캐릭터가 가파른 비탈을 얼마나 빨리 미끄러지는가?")]
        public float SlideGravity { get; private set; } = 5f;

        // [field: SerializeField, FoldoutGroup("이동 물리/중력 및 경사면"), Tooltip("허용 가능한 경사 각도 제한")]
        public float SlopeLimit { get; private set; } = 80f;

        #endregion

        #region 점프 (Archived)

        // [field: SerializeField, FoldoutGroup("이동 물리/점프 (Archived)", expanded: false)]
        public float JumpSpeed { get; private set; } = 10;

        // [field: SerializeField, FoldoutGroup("이동 물리/점프 (Archived)", expanded: false), Tooltip("점프를 진행하는 시간")]
        public float JumpDuration { get; private set; } = 0.2f;

        #endregion

        #endregion

        #region 피격

        [field: SerializeField, FoldoutGroup("피격", expanded: false), Tooltip("피격 시 무적 시간")]
        public float HitInvincibleTime { get; set; } = 1.0f;
        
        [field: SerializeField, FoldoutGroup("피격", expanded: false), Tooltip("경직 시 무적 시간")]
        public float HitStunInvincibleTime { get; set; } = 1.0f;

        // [field: SerializeField, FoldoutGroup("피격", expanded: false), Tooltip("피격 시 넉백 무적 시간")]
        // public float HitKnockBackInvincibleTime { get; set; } = 1.0f;

        [field: SerializeField, FoldoutGroup("피격", expanded: false), Tooltip("피격 당했을 때 캐릭터를 칠할 시간입니다.")]
        public float HitTintTime { get; set; } = 0.3f;

        [field: SerializeField, FoldoutGroup("피격", expanded: false), Tooltip("피격 당했을 때 캐릭터를 칠할 컬러입니다.")]
        public Color HitTintColor { get; set; } = new(0.7f, 0.7f, 0.7f);

        [field: SerializeField, FoldoutGroup("피격", true)]
        public CameraShakeSettings HitCameraShake { get; private set; } = new(
            new FloatRange(-0.2f, 0.2f),
            new FloatRange(-0.2f, 0.2f),
            1f
        );

        [Serializable]
        public struct InvincibleTimeByAttackTypeSettings
        {
            public EnemyAttackType Type;
            public float Time;
        }

        [field: SerializeField, FoldoutGroup("피격/무적 시간", expanded: false)]
        public float DefaultInvincibleTime { get; private set; } = 1f;

        [field: SerializeField, FoldoutGroup("피격/무적 시간", expanded: false)]
        public List<InvincibleTimeByAttackTypeSettings> InvincibleTimeByAttackTypeSettingsList { get; private set; } =
            new();

        private float[] _invincibleTimeByAttackTypeSettings = null;

        public float GetInvincibleTimeByAttackType(EnemyAttackType type)
        {
            if (_invincibleTimeByAttackTypeSettings == null || _invincibleTimeByAttackTypeSettings.Length <= 0)
            {
                _invincibleTimeByAttackTypeSettings = new float[(int)EnemyAttackType._TypeCount];
                if (showDebug)
                    Debug.Log(
                        $"_invincibleTimeByAttackTypeSettings initialized with size of {_invincibleTimeByAttackTypeSettings.Length}");
                for (int i = 0; i < _invincibleTimeByAttackTypeSettings.Length; i++)
                {
                    _invincibleTimeByAttackTypeSettings[i] = DefaultInvincibleTime;
                }

                foreach (var settings in InvincibleTimeByAttackTypeSettingsList)
                {
                    _invincibleTimeByAttackTypeSettings[(int)settings.Type] = settings.Time;
                    Debug.Log($"Invincible Time of {settings.Type}: {settings.Time}s");
                }
            }

            if (showDebug)
                Debug.Log(
                    $"GetInvincibleTimeByAttackType({type.ToString()}) => {(int)type}, size of {_invincibleTimeByAttackTypeSettings.Length}");
            return _invincibleTimeByAttackTypeSettings[(int)type];
        }

        #endregion

        #region 낙하
        
        [field: SerializeField, FoldoutGroup("낙하", true), Tooltip("낙하 사망 판정 시간")]
        public float FallDeadTime { get; private set; } = 2f;

        #endregion
        
        #region 사운드

        [field: SerializeField, FoldoutGroup("사운드", expanded: false)]
        public AudioPathByString Sounds { get; set; } = new();

        #endregion
    }
}