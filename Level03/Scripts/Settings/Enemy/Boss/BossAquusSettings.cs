using System;
using System.Collections.Generic;
using EnumData;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace Settings.Boss
{
    [CreateAssetMenu(fileName = "BossAquusSettings", menuName = "Settings/Boss/Aquus Settings", order = 0)]
    public class BossAquusSettings : EnemySettings
    {
        // 일반 설정
        
        [field: SerializeField, FoldoutGroup("일반", true), LabelText("회전 속도"), Tooltip("RotationSpeed")]
        public float RotationSpeed { get; private set; } = 360f;
        
        
        [field: SerializeField, FoldoutGroup("일반", true), LabelText("빙결 피격 피해량"), Tooltip("DamageOnIceSlipCollide")]
        public float DamageOnIceSlipCollide { get; private set; } = 5f;
        
        // 실드
        
        [field: SerializeField, FoldoutGroup("실드", true), LabelText("최대 실드량"), Tooltip("MaxShield")]
        public float MaxShield { get; private set; } = 3;

        [field: SerializeField, FoldoutGroup("실드/페이즈 2", true), LabelText("실드 체력 범위"), Tooltip("Phase2ShieldRange")]
        [field: MinMaxSlider(1f, "MaxShield", true)]
        public Vector2Int Phase2ShieldRange { get; private set; } = new Vector2Int(3, 5);
        
        [field: SerializeField, FoldoutGroup("실드/페이즈 2", true), LabelText("실드 파괴 후 대기시간"), Tooltip("Phase2ShieldWaitTimeRange")]
        [field: MinMaxSlider(1f, 300f, true)]
        public Vector2 Phase2ShieldWaitTimeRange { get; private set; } = new Vector2(8, 15);
        
        
        [field: SerializeField, FoldoutGroup("전투/피격/냉동"), Tooltip("빙결 해동 시 단계별로 걸리는 시간")]
        public float FreezeLevelDownTime { get; protected set; } = 0.5f;
        
        // Pattern: Harp Strike
        
        [field: FoldoutGroup("공격", true)]
        [field: SerializeField, FoldoutGroup("공격/Harp Strike", true), LabelText("피해량"), Tooltip("HarpStrikeDamage")]
        public float HarpStrikeDamage { get; private set; } = 1f;
        
        [field: SerializeField, FoldoutGroup("공격/Harp Strike", true), LabelText("실행 범위"), Tooltip("HarpStrikeExecuteRange")]
        public float HarpStrikeExecuteRange { get; private set; } = 8f;
        
        [field: SerializeField, FoldoutGroup("공격/Harp Strike", true), LabelText("데칼 투명도 커브"), Tooltip("HarpStrikeDecalAlphaCurve")]
        public AnimationCurve HarpStrikeDecalAlphaCurve { get; private set; } = AnimationCurve.Linear(0f, 0f, 1.033f, 1f);
        
        // Pattern: Jellyfish
        
        [field: SerializeField, FoldoutGroup("공격/Jellyfish", true), LabelText("상승 높이 커브"), Tooltip("JellyfishThrowCurve")]
        public AnimationCurve JellyfishThrowCurve { get; private set; } = AnimationCurve.Linear(0f, 0f, 1f, 10f);
        
        [field: SerializeField, FoldoutGroup("공격/Jellyfish", true), LabelText("하강 높이 커브"), Tooltip("JellyfishFallCurve")]
        public AnimationCurve JellyfishFallCurve { get; private set; } = AnimationCurve.Linear(15f, 0f, 3f, 0f);
        
        [field: SerializeField, FoldoutGroup("공격/Jellyfish", true), LabelText("피해량"), Tooltip("JellyfishDamage")]
        public float JellyfishDamage { get; private set; } = 1f;
            
        [field: SerializeField, FoldoutGroup("공격/Jellyfish", true), LabelText("펄스 횟수"), Tooltip("JellyfishPulseCount")]
        public int JellyfishPulseCount { get; private set; } = 4;
        
        [field: SerializeField, FoldoutGroup("공격/Jellyfish", true), LabelText("펄스 주기"), Tooltip("JellyfishPulseInterval")]
        public float JellyfishPulseInterval { get; private set; } = 1f;
        
        [field: SerializeField, FoldoutGroup("공격/Jellyfish", true), LabelText("데칼 투명도 커브"), Tooltip("JellyfishFallCurve")]
        public AnimationCurve JellyfishDecalAlphaCurve { get; private set; } = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        
        [field: SerializeField, FoldoutGroup("공격/Jellyfish", true), LabelText("데칼 Offset값 커브"), Tooltip("JellyfishDecalOffsetCurve")]
        public AnimationCurve JellyfishDecalOffsetCurve { get; private set; } = AnimationCurve.Linear(0f, 0.5f, 1f, 1f);
        
        // Pattern: Bow
        
        [field: SerializeField, FoldoutGroup("공격/Bow", true), LabelText("피해량"), Tooltip("BowAttackDamage")]
        public float BowAttackDamage { get; private set; } = 1f;
        
        [field: SerializeField, FoldoutGroup("공격/Bow", true), LabelText("투사체 속도"), Tooltip("BowAttackSpeed")]
        public float BowAttackSpeed { get; private set; } = 5f;
        
        [field: SerializeField, FoldoutGroup("공격/Bow", true), LabelText("데칼 투명도 커브"), Tooltip("BowAttackDecalAlphaCurve")]
        public AnimationCurve BowAttackDecalAlphaCurve { get; private set; } = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        
        [field: SerializeField, FoldoutGroup("공격/Bow", true), LabelText("데칼 거리 커브"), Tooltip("BowAttackDecalDistanceCurve")]
        public AnimationCurve BowAttackDecalDistanceCurve { get; private set; } = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        
        // Pattern: Scream

        [Serializable, InlineProperty, LabelWidth(100f)]
        public class ScreamSettings
        {
        
            [field: SerializeField, LabelText("피해량"), Tooltip("Damage")]
            public float Damage { get; private set; } = 1f;
            
            [field: SerializeField, LabelText("낙하물 피해 범위"), Tooltip("DamageRange")]
            public float DamageRange { get; private set; } = 1f;
            
            [field: SerializeField, LabelText("낙하물 갯수"), Tooltip("StructureCount"), MinMaxSlider(0f, 30f, ShowFields = true)]
            public Vector2Int StructureCount { get; private set; } = new Vector2Int(6, 8);
            
            [field: SerializeField, LabelText("낙하물 종류"), Tooltip("StructureTypes")]
            public List<EffectType> StructureTypes { get; private set; } = new()
            {
                EffectType.AquusScreamStructure01,
            };
            
            [field: SerializeField, LabelText("낙하 딜레이"), Tooltip("StructureFallDelay"), MinMaxSlider(0f, 5f, ShowFields = true)]
            public Vector2 StructureFallDelay { get; private set; } = new Vector2(0f, 1f);
            
            [field: SerializeField, LabelText("낙하 속도"), Tooltip("StructureFallSpeed"), MinMaxSlider(1f, 30f, ShowFields = true)]
            public Vector2 StructureFallSpeed { get; private set; } = new Vector2(12f, 16f);
            
            [field: SerializeField, LabelText("데칼 투명도 커브"), Tooltip("StructureDecalAlphaCurve")]
            public AnimationCurve StructureDecalAlphaCurve { get; private set; } = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            
            [field: SerializeField, LabelText("데칼 Offset값 커브"), Tooltip("StructureDecalOffsetCurve")]
            public AnimationCurve StructureDecalOffsetCurve { get; private set; } = AnimationCurve.Linear(0f, 0.5f, 1f, 1f);
            
            [field: SerializeField, LabelText("낙하물간 최소 거리"), Tooltip("MinDistance")]
            public float MinDistance { get; private set; } = 2.1f;
            
            [field: SerializeField, LabelText("무효화 시간"), Tooltip("InvalidationTime, 이 시간 이상 지나면 자동 비활성화")]
            public float InvalidationTime { get; private set; } = 10f;
        
        }

        [field: SerializeField, FoldoutGroup("공격/Scream", true), InlineProperty]
        public ScreamSettings DefaultScreamSettings { get; private set; } = new();
        
        // Phase Transition
        
        [field: SerializeField, FoldoutGroup("공격/페이즈 전환", true)]
        public bool PhaseTransitionShuffleSwimPathOrder { get; private set; } = false;
        [field: SerializeField, FoldoutGroup("공격/페이즈 전환", true), InlineProperty]
        public ScreamSettings PhaseTransitionScreamSettings { get; private set; } = new();

    }
}