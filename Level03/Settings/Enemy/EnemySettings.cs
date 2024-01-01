using System;
using System.Collections.Generic;
using EnumData;
using FMODPlus;
using FMODUnity;
using Settings.Item;
using Sirenix.OdinInspector;
using UnityEngine;
using Utility;

namespace Settings
{
    [CreateAssetMenu(fileName = "EnemySettings", menuName = "Scriptable Object/EnemySettings")]
    public class EnemySettings : ScriptableObject
    {
        [field: SerializeField, FoldoutGroup("기본", true), Tooltip("종류")]
        public EnemyType Type { get; protected set; }
        
        [field: SerializeField, FoldoutGroup("기본", true), Tooltip("전체 체력")]
        public float Health { get; protected set; } = 100f;
        
        [field: SerializeField, FoldoutGroup("기본", true), Tooltip("이동 속도")]
        public float MovementSpeed { get; protected set; } = 3f;
        
        
        [field: FoldoutGroup("전투", true)]
        [field: SerializeField, FoldoutGroup("전투/공격", true), Tooltip("공격력")]
        public float AttackPower { get; protected set; } = 10f;

        [field: SerializeField, FoldoutGroup("전투/공격", true), Tooltip("공격 사거리 (시작 범위)")]
        public float AttackStartRange { get; protected set; } = 5f;
        private float? _attackStartRangeSquared;
        public float AttackStartRangeSquared => _attackStartRangeSquared ??= AttackStartRange * AttackStartRange;

        [field: SerializeField, FoldoutGroup("전투/공격", true), Tooltip("공격 쿨타임")]
        public float AttackCooldown { get; protected set; } = 5f;
        
        [field: SerializeField, FoldoutGroup("전투/피격", true), Tooltip("기절 시간")]
        public float StunTime { get; protected set; } = 3f;
        
        [field: SerializeField, FoldoutGroup("전투/피격/레거시"), Tooltip("경직 시간")]
        public float StaggerTime { get; protected set; } = 0.5f;
        
        [field: SerializeField, FoldoutGroup("전투/피격/레거시"), Tooltip("PC로부터 피격 넉백 계수")]
        public float KnockBackPower { get; protected set; } = 20f;
        
        [Serializable]
        public struct FrostbiteFactor
        {
            public float AnimationSpeed;
            public float ColorThreshold;
        }
        
        [field: SerializeField, FoldoutGroup("전투/피격/냉동", true), Tooltip("냉동 단계별 둔화 계수")]
        public List<FrostbiteFactor> FrostbiteByLevel { get; protected set; } = new()
        {
            new() { AnimationSpeed = 1f, ColorThreshold = 0f, }, 
            new() { AnimationSpeed = 0.9f, ColorThreshold = 0.2f, },
            new() { AnimationSpeed = 0.8f, ColorThreshold = 0.5f, },
            new() { AnimationSpeed = 0f, ColorThreshold = 0.5f },
        };

        [field: SerializeField, FoldoutGroup("전투/피격/냉동/둔화 계수 자동화", true)] private AnimationCurve _animationSpeedCurve;
        [field: SerializeField, FoldoutGroup("전투/피격/냉동/둔화 계수 자동화", true)] private AnimationCurve _thresholdCurve;
        [Button, FoldoutGroup("전투/피격/냉동/둔화 계수 자동화", true)]
        public void SetFrostbiteByLevelFromCurve()
        {
            AnimationCurve animationSpeedCurve = _animationSpeedCurve;
            AnimationCurve thresholdCurve = _thresholdCurve;
            FrostbiteByLevel.Clear();
            int maxLevel = Mathf.Max((int)animationSpeedCurve.GetLength(), (int)thresholdCurve.GetLength());
            FrostbiteByLevel.Capacity = maxLevel;
            for (int i = 0; i <= maxLevel; i++)
            {
                FrostbiteByLevel.Add(new FrostbiteFactor
                {
                    AnimationSpeed = animationSpeedCurve.Evaluate(i), 
                    ColorThreshold = thresholdCurve.Evaluate(i)
                });
            }
        }        
        [Button, FoldoutGroup("전투/피격/냉동/둔화 계수 자동화", true)]
        public void SetCurveFromFrostbiteByLevel()
        {
            AnimationCurve animationSpeedCurve = _animationSpeedCurve;
            AnimationCurve thresholdCurve = _thresholdCurve;
            animationSpeedCurve.ClearKeys();
            thresholdCurve.ClearKeys();
            int level = 0;
            foreach (var factor in FrostbiteByLevel)
            {
                animationSpeedCurve.AddKey(level, factor.AnimationSpeed);
                thresholdCurve.AddKey(level, factor.ColorThreshold);

                ++level;
            }
            
        }

        [field: SerializeField, FoldoutGroup("전투/피격/냉동", true), Tooltip("냉동 시 얼음 이펙트 종류")]
        public EffectType FreezeEffectType { get; protected set; } = EffectType.EnemyFreezeClub;
        
        // [field: SerializeField, FoldoutGroup("전투/피격/냉동", true), Tooltip("냉동 단계별 이펙트 종류")]
        // public List<EffectType> FreezeEffectsByLevel { get; protected set; } = new()
        // {
            // EffectType.None,
            // EffectType.EnemyFreeze,
            // EffectType.EnemyFreeze03,
            // EffectType.EnemyFreeze05,
            // EffectType.EnemyFreeze07,
            // EffectType.EnemyFreeze08,
        // };
        [field: SerializeField, FoldoutGroup("전투/피격/냉동", true), Tooltip("냉동 단계별 이펙트 Material 종류")]
        public List<Material> FreezeEffectMaterialByLevel { get; protected set; } = new();
        
        [field: SerializeField, FoldoutGroup("전투/피격/냉동", true), Tooltip("완전 냉동 단계")]
        public int FreezeCompleteLevel { get; protected set; } = 3;
        
        [field: SerializeField, FoldoutGroup("전투/피격/냉동", true), Tooltip("최대 냉동 단계")]
        public int MaxFreezeLevel { get; protected set; } = 6;
        
        [field: SerializeField, FoldoutGroup("전투/피격/냉동", true), Tooltip("완전 냉동 시 사운드")]
        public EventReference FreezeCompleteSound { get; protected set; }
        
        [field: SerializeField, FoldoutGroup("전투/피격/냉동", true), Tooltip("완전 냉동 시 전환할 Animation State")]
        public string AnimationStateNameWhenFreezeComplete { get; protected set; } = "Idle";
            
        [field: SerializeField, FoldoutGroup("전투/피격/냉동", true), Tooltip("빙결 시 적용되는 NavMeshAgent 우선순위")]
        public int NavMeshPriorityOnFreeze { get; protected set; } = 40;

        
        [field: SerializeField, FoldoutGroup("전투/피격/냉동/미끄러짐", true)] 
        public LayerMask FreezeSlippingCollideMask { get; protected set; } = new();
        
        [field: SerializeField, FoldoutGroup("전투/피격/냉동/미끄러짐", true), Tooltip("미끄러짐 방향 보정 중 완전 빙결 대상 추가 가중치. 내적값 + 가중치를 기준으로 가장 높은 대상 선택됨")]
        public float SlipCompensateWeightToFreeze { get; protected set; } = 0.35f;
        
        [field: SerializeField, FoldoutGroup("전투/피격/냉동/미끄러짐", true), Tooltip("미끄러질 때 회전하는 속도 (deg/s)")]
        public float SlipRotationSpeed { get; protected set; } = 1440f;

        [field: SerializeField, FoldoutGroup("전투/피격/냉동/미끄러짐", true), Tooltip("빙결 파괴각")]
        public float SlipCollideThresholdAngle { get; protected set; } = 10f;

        [field: SerializeField, FoldoutGroup("전투/피격/냉동/미끄러짐", true), Tooltip("미끄러짐 최대 유효시간")]
        public float SlipMaxTime { get; protected set; } = 3f;

        [field: SerializeField, FoldoutGroup("전투/피격/냉동/미끄러짐", true), Tooltip("미끄러질 때 움직이지 않으면 곱해지는 유효시간")]
        public float SlipTimeMultiplierWhenNotMoving { get; protected set; } = 2f;
        
#if UNITY_EDITOR
        public float SlipCollideThresholdAngleInCos => 
#else
        private float? _slipCollideThresholdAngleInCos = null;
        public float SlipCollideThresholdAngleInCos => _slipCollideThresholdAngleInCos ??=
#endif
            Mathf.Cos((90f + SlipCollideThresholdAngle) * Mathf.Deg2Rad);

        [field: SerializeField, FoldoutGroup("전투/피격/냉동/미끄러짐", true), Tooltip("미끄러짐 시 무조건 파괴 impulse량")]
        public float SlipCollisionImpulseForce { get; protected set; } = 1000f;
        
        [field: SerializeField, FoldoutGroup("전투/피격/냉동", true), Tooltip("공중 빙결 시 즉시 물리현상 적용 여부")]
        public bool UseFallWhenFreezeOnAir { get; protected set; } = true;
        
        [field: SerializeField, FoldoutGroup("전투/피격/냉동", true), Tooltip("완전 빙결 시 유효한 바닥과의 거리")]
        public float FreezeDistanceFromGroundThreshold { get; protected set; } = 3f;
        
        [field: SerializeField, FoldoutGroup("기타", true), Tooltip("개체 높이(HUD 체력에 사용, m단위)")]
        public float Height { get; protected set; } = 1.7f;

        [field: SerializeField, FoldoutGroup("이펙트", true), Tooltip("소환 이펙트 종류")]
        public EffectType SpawnEffectType { get; protected set; } = EffectType.EnemySpawn;
            
        [field: SerializeField, FoldoutGroup("이펙트", true), Tooltip("피격 이펙트 종류")]
        public EffectType HitEffectType { get; protected set; } = EffectType.EnemyHitNormal;
            
        [field: SerializeField, FoldoutGroup("이펙트", true), Tooltip("피격 이펙트 종류")]
        public EffectType StunEffectType { get; protected set; } = EffectType.None;
            
        [field: SerializeField, FoldoutGroup("이펙트", true), Tooltip("피격 이펙트 위치 높이 배수")]
        public float StunEffectHeightMultiplier { get; protected set; } = 1.5f;
        
        [field: SerializeField, FoldoutGroup("이펙트", true), Tooltip("포물선 소환 인디케이터 사용 여부")]
        public bool UseParabolaSpawnIndicator { get; protected set; } = true;
        
        [field: SerializeField, FoldoutGroup("이펙트", true), Tooltip("포물선 소환 인디케이터 이펙트 고정 지속시간")]
        public float ParabolaSpawnIndicatorDuration { get; protected set; } = 2f;

        [field: SerializeField, FoldoutGroup("아이템", true), Tooltip("아이템 드랍 테이블")]
        public ItemDropTable DropTableOnDead { get; protected set; }

        [field: SerializeField, FoldoutGroup("UI", true), Tooltip("결합 가능한 오프스크린 UI 사용 가능 여부")]
        public bool UseCombinedOffScreenUI { get; protected set; } = true;

        // [field: Tooltip("투사체 속도")]
        // [field: SerializeField]
        // public float ProjectileSpeed { get; protected set; } = 5f;

        // TODO 보류
        // [field: Tooltip("영혼 드랍양")]
        // [field: SerializeField]
        // public float Soul { get; protected set; } = 5f;
    }
}