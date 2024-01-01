using System.Collections.Generic;
using EnumData;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Settings
{
    [CreateAssetMenu(fileName = "New FreezableSettings", menuName = "Settings/Freezable Settings", order = 0)]
    public class FreezableSettings : ScriptableObject
    {
        [field: SerializeField, FoldoutGroup("냉동", true), Tooltip("냉동 단계별 둔화 계수")]
        public List<EnemySettings.FrostbiteFactor> FrostbiteByLevel { get; protected set; } = new()
        {
            new() { AnimationSpeed = 1f, ColorThreshold = 0f, }, 
            new() { AnimationSpeed = 0.9f, ColorThreshold = 0.2f, },
            new() { AnimationSpeed = 0.8f, ColorThreshold = 0.5f, },
            new() { AnimationSpeed = 0f, ColorThreshold = 0.5f },
        };
        
        [field: SerializeField, FoldoutGroup("냉동", true), Tooltip("냉동 단계별 이펙트 종류")]
        public List<EffectType> FreezeEffectsByLevel { get; protected set; } = new()
        {
            EffectType.None,
            EffectType.None,
            EffectType.None,
            EffectType.EnemyFreeze05,
            EffectType.EnemyFreeze07,
            EffectType.EnemyFreeze08,
        };
        
        [field: SerializeField, FoldoutGroup("냉동", true), Tooltip("완전 냉동 단계")]
        public int FreezeCompleteLevel { get; protected set; } = 1;
        
        [field: SerializeField, FoldoutGroup("냉동", true)] 
        public LayerMask FreezeSlippingCollideMask { get; protected set; } = new();

        [field: SerializeField, FoldoutGroup("냉동", true), Tooltip("미끄러짐 방향 보정 중 완전 빙결 대상 추가 가중치. 내적값 + 가중치를 기준으로 가장 높은 대상 선택됨")]
        public float SlipCompensateWeightToFreeze { get; protected set; } = 0.35f;
        
        [field: SerializeField, FoldoutGroup("냉동", true), Tooltip("미끄러질 때 회전하는 속도 (deg/s)")]
        public float SlipRotationSpeed { get; protected set; } = 1440f;
        
        [field: SerializeField, FoldoutGroup("냉동", true), Tooltip("최대 냉동 단계")]
        public int MaxFreezeLevel { get; protected set; } = 2;
        [field: SerializeField, FoldoutGroup("냉동", true), Tooltip("빙결 파괴각")]
        public float SlipCollideThresholdAngle { get; protected set; } = 10f;
        
#if UNITY_EDITOR
        public float SlipCollideThresholdAngleInCos => Mathf.Cos((90f + SlipCollideThresholdAngle) * Mathf.Deg2Rad);
#else
        private float? _slipCollideThresholdAngleInCos = null;
        public float SlipCollideThresholdAngleInCos => _slipCollideThresholdAngleInCos ??= Mathf.Cos((90f + SlipCollideThresholdAngle) * Mathf.Deg2Rad);
#endif
    }
}