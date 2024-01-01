using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using Character.Core;
using EnumData;
using Sirenix.OdinInspector;
using UnityEngine;
using Utility;

namespace Settings.Boss
{
    [CreateAssetMenu(fileName = "BossYorugamiSettings", menuName = "Scriptable Object/BossSettings", order = 0)]
    public class BossYorugamiSettings : EnemySettings
    {
        [Serializable]
        public struct WaveAttackPatternData
        {
            public int WaitTime;
            public float Distance;
            public float MoveTime;
            public Quaternion Rotation;
            public Vector3 HalfExtents;
            public int AdditionalNavMeshCheckerCount;
            public float AdditionalNavMeshCheckerRadius;
        }

        [field: FoldoutGroup("전투/패턴", true)]
        [field: SerializeField, FoldoutGroup("전투/패턴/근접 공격 패턴", true)]
        public float NearAttack01Damage { get; private set; } = 10f;

        [field: SerializeField, FoldoutGroup("전투/패턴/근접 공격 패턴", true)]
        public float NearAttack02Damage { get; private set; } = 10f;

        [field: SerializeField, FoldoutGroup("전투/패턴/근접 공격 패턴", true)]
        public float NearAttack03Damage { get; private set; } = 10f;

        [field: SerializeField, FoldoutGroup("전투/패턴/근접 공격 패턴", true)]
        public float NearAttackMoveForwardPower { get; private set; } = 5f;

        [field: SerializeField, FoldoutGroup("전투/패턴/근접 공격 패턴", true)]
        public float NearAttackMoveForwardTime { get; private set; } = 0.1f;

        [field: SerializeField, FoldoutGroup("전투/패턴/근접 공격 패턴", true)]
        public float NearAttackGiveUpTime { get; private set; } = 5f;

        [field: SerializeField, FoldoutGroup("전투/패턴/파도 공격 패턴", true)]
        public List<WaveAttackPatternData> WaveAttackSpawnSequence { get; private set; }

        [field: SerializeField, FoldoutGroup("전투/패턴/파도 공격 패턴", true)]
        public float WaveAttackDamage { get; private set; } = 10f;

        [field: SerializeField, FoldoutGroup("전투/패턴/점멸", true), Tooltip("점멸을 최우선하게 되는 거리")]
        public float ForcedFlashRange { get; private set; } = 20f;

        [field: SerializeField, FoldoutGroup("전투/패턴/점멸", true), Tooltip("점멸 시 목표로부터 떨어질 거리")]
        public float FlashDistanceFromTarget { get; private set; } = 2f;

        [Serializable]
        public struct BossFlashAttackSettings
        {
            public float BeforeHideDelay;
            public float HideTime;
            public float BeforeDoAttackDelay;
        }

        [field: SerializeField, FoldoutGroup("전투/패턴/점멸/연계", true),
                Tooltip("점멸 & 근접공격 연계 관련 세팅. \n0번째: 2페이즈 \n1~3번째: 3페이즈")]
        public List<BossFlashAttackSettings> FlashAttackSettings { get; private set; } =
            new List<BossFlashAttackSettings>()
            {
                new() { BeforeHideDelay = 15f, HideTime = 15f, BeforeDoAttackDelay = 10f }, // 0: 2페이즈
                new() { BeforeHideDelay = 8f, HideTime = 15f, BeforeDoAttackDelay = 5f }, // 1: 3페이즈 1타
                new() { BeforeHideDelay = 8f, HideTime = 15f, BeforeDoAttackDelay = 5f }, // 2: 3페이즈 2타
                new() { BeforeHideDelay = 8f, HideTime = 15f, BeforeDoAttackDelay = 5f }, // 3: 3페이즈 3타
            };

        [field: SerializeField, FoldoutGroup("전투/패턴/돌진 공격", true), Tooltip("돌진 속도")]
        public float RushSpeed { get; private set; } = 5f;

        [field: SerializeField, FoldoutGroup("전투/패턴/돌진 공격", true), Tooltip("돌진 거리")]
        public float RushDistance { get; private set; } = 17f;

        [field: SerializeField, FoldoutGroup("전투/패턴/돌진 공격", true), Tooltip("돌진 피해량")]
        public float RushDamage { get; private set; } = 10f;

        [field: SerializeField, FoldoutGroup("전투/패턴/돌진 공격", true), Tooltip("돌진 무기 차징 커브")]
        public AnimationCurve RushSwordChargingCurve { get; private set; } = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [field: SerializeField, FoldoutGroup("전투/패턴/원거리 공격", true), Tooltip("투사체 프리팹")]
        public GameObject ProjectilePrefab { get; private set; }

        [field: SerializeField, FoldoutGroup("전투/패턴/원거리 공격", true), Tooltip("투사체 소환 장소")]
        public float ProjectileSpeed { get; private set; } = 5f;

        [field: SerializeField, FoldoutGroup("전투/패턴/폭탄 생성 공격", true), Tooltip("프리팹")]
        public GameObject BombPrefab { get; private set; }

        [field: SerializeField, FoldoutGroup("전투/패턴/폭탄 생성 공격", true), Tooltip("프리팹 폭발 시간")]
        public float BombExplosionTime { get; private set; } = 5f;

        [field: SerializeField, FoldoutGroup("전투/패턴/폭탄 생성 공격", true), Tooltip("폭발 피해량")]
        public float BombExplosionDamage { get; private set; } = 10f;

        [field: SerializeField, FoldoutGroup("전투/패턴/부채꼴 공격", true), Tooltip("부채꼴 공격 시 뜨는 높이")]
        public float SectorAttackLeapHeight { get; private set; } = 5f;

        [field: SerializeField, FoldoutGroup("전투/패턴/부채꼴 공격", true), Tooltip("부채꼴 공격 시 공중에 뜰 떄 커브")]
        public AnimationCurve SectorAttackLeapStartCurve { get; private set; } =
            AnimationCurve.EaseInOut(0f, 0f, 0.23f, 1f);

        [field: SerializeField, FoldoutGroup("전투/패턴/부채꼴 공격", true), Tooltip("부채꼴 공격 시 지면에 내릴 때 커브")]
        public AnimationCurve SectorAttackLeapEndCurve { get; private set; } =
            AnimationCurve.EaseInOut(0f, 1f, 0.7f, 0f);

        [field: SerializeField, FoldoutGroup("전투/패턴/부채꼴 공격", true), Tooltip("부채꼴 이펙트 소환 후 폭발 시간")]
        public float SectorAttackExplosionDelay { get; private set; } = 0.6f;

        [field: SerializeField, FoldoutGroup("전투/패턴/부채꼴 공격", true), Tooltip("부채꼴 이펙트 사이 간격 시간")]
        public float SectorAttackWaitTime { get; private set; } = 5f;

        [field: SerializeField, FoldoutGroup("전투/패턴/부채꼴 공격", true), Tooltip("부채꼴 공격 피해량")]
        public float SectorAttackDamage { get; private set; } = 10f;

        [field: SerializeField, FoldoutGroup("전투/패턴/부채꼴 공격", true), Tooltip("부채꼴 공격 사거리")]
        public float SectorAttackRange { get; private set; } = 10f;

        [field: SerializeField, FoldoutGroup("전투/패턴/검기 발사 패턴", true)]
        public float RangedAttackDamage { get; private set; } = 10f;

        [field: SerializeField, FoldoutGroup("전투/패턴/검기 발사 패턴", true)]
        public float RangedAttackSpeed { get; private set; } = 10f;

        [Serializable]
        public struct BossPhaseTransition
        {
            [Serializable]
            public struct BossPhaseSpawnData
            {
                public EnemyType Type;
                public int Amount;
            }

            public float TargetHealth;
            public List<BossPhaseSpawnData> SpawnDatas;
        }

        [field: SerializeField, FoldoutGroup("전투/패턴/찍기 패턴", true)]
        public float DownAttackWaitTime { get; private set; } = 3f;

        [field: SerializeField, FoldoutGroup("전투/패턴/찍기 패턴", true)]
        public float DownAttackRange { get; private set; } = 6f;

        [field: SerializeField, FoldoutGroup("전투/패턴/찍기 패턴", true)]
        public float DownAttackDamage { get; private set; } = 10f;

        [field: SerializeField, FoldoutGroup("전투/패턴/찍기 패턴", true)]
        public float DownAttackEffectFallTime { get; private set; } = 0.12f;

        [field: SerializeField, FoldoutGroup("전투/패턴/찍기 패턴", true)]
        public float DownAttackFallAnimationTime { get; private set; } = 8f / 30f;

        [field: SerializeField, FoldoutGroup("전투/패턴/찍기 패턴", true)]
        public AnimationCurve DownAttackHeightCurve { get; private set; } = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [field: SerializeField, FoldoutGroup("전투/패턴/찍기 패턴", true)]
        public CameraShakeSettings DownAttackFallShake { get; private set; } = new();

        [field: SerializeField, FoldoutGroup("전투/페이즈", true)]
        public float PhaseTransitionHeight { get; private set; } = 10f;

        [field: SerializeField, FoldoutGroup("전투/페이즈", true)]
        public AnimationCurve PhaseTransitionHeightCurve { get; private set; } =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [field: SerializeField, FoldoutGroup("전투/페이즈", true)]
        public List<BossPhaseTransition> PhaseTransitions { get; private set; } = new();

        [field: SerializeField, FoldoutGroup("전투/페이즈", true)]
        public int PhaseTransitionSpawnAreaIndex { get; private set; } = 1;

        [field: SerializeField, FoldoutGroup("전투/페이즈", true)]
        public float PhaseTransitionHealthRegeneration { get; private set; } = 5f;

        [field: SerializeField, FoldoutGroup("기타", true)]
        public AnimationCurve GroggyKnockBackVelocityCurve { get; private set; } =
            AnimationCurve.Linear(0f, 1f, 0.4f, 0f);

        #region 사운드

        [field: SerializeField, FoldoutGroup("사운드", expanded: false)]
        public AudioPathByString Sounds { get; set; } = new();

        #endregion
    }


    public class SharedBossSettings : SharedVariable<BossYorugamiSettings>
    {
        public static implicit operator SharedBossSettings(BossYorugamiSettings value)
        {
            return new SharedBossSettings { mValue = value };
        }
    }
}