using System;
using System.Collections.Generic;
using Enemy.Behavior;
using EnumData;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Enemy.Spawner
{
    [ExecuteAlways]
    public class StageManager : MonoBehaviour
    {

        /*
        [Button("새로고침"), BoxGroup("유틸", order: -1f)]
        private void OnValidate()
        {
        }
        */
        
        [Serializable]
        public class WaveSettings
        {
            public enum WaveClearCondition
            {
                [InspectorName("모든 적 제거 (KillAllMonsters)")]
                KillAllMonsters,
                [InspectorName("별도 함수 호출 (Custom)")]
                Custom,
            }

            [BoxGroup("기본"), LabelText("웨이브 클리어 조건")]
            public WaveClearCondition Condition = WaveClearCondition.KillAllMonsters;
            [BoxGroup("기본"), LabelText("생성 몬스터 목록")]
            public List<WaveSpawnSettings> SpawnSettings = new();
            
            [BoxGroup("웨이브 스킵 타이머 발생 조건"), LabelText("남은 적 수")]
            public int WaveSkipMonsterCount = 1;
            [BoxGroup("웨이브 스킵 타이머 발생 조건"), LabelText("초 수"), DisableIf("@WaveSkipMonsterCount < 0")]
            public int WaveSkipTime = 5;

            [BoxGroup("디버깅"), LabelText("Gizmo 그리기 여부")]
            public bool DrawGizmo = true;
        }

        [Serializable]
        public class WaveSpawnSettings
        {
            [LabelText("소환할 적 몬스터 종류")]
            public EnemyType EnemyType;

            [LabelText("생성기")]
            public EnemySpawner Spawner;

            [LabelText("소환 딜레이")] 
            public float Delay = 1f;
            
#if UNITY_EDITOR
            [Button("즉시 소환"), ShowIf("@Spawner == null"), HorizontalGroup(GroupID = "button")]
            public void GenerateSingleSpawner()
            {
                if(Spawner) return;
                
                var manager = Selection.activeGameObject ?? GameObject.FindAnyObjectByType<StageManager>()?.gameObject;
                var root = new GameObject($"{EnemyType.ToString()}_Spawner");
                root.transform.SetParent(manager.transform);

                var spawner = root.AddComponent<EnemySpawner>();
                Spawner = spawner;
                
                Selection.SetActiveObjectWithContext(root, null);
            }
            
            [Button("포물선 소환"), ShowIf("@Spawner == null"), HorizontalGroup(GroupID = "button")]
            public void GenerateParabolaSpawner()
            {
                if(Spawner) return;

                var manager = Selection.activeGameObject ?? GameObject.FindAnyObjectByType<StageManager>()?.gameObject;
                var root = new GameObject($"{EnemyType.ToString()}_ParabolaSpawner");
                root.transform.SetParent(manager.transform);

                var start = new GameObject($"{EnemyType.ToString()}_ParabolaSpawner_Start");
                start.transform.SetParent(root.transform);
                start.transform.localPosition = new Vector3(0f, -5f, 5f);

                var spawner = root.AddComponent<EnemyParabolaSpawner>();
                spawner.ParabolaStartPosition = start.transform;
                spawner.ParabolaEndPosition = root.transform;
                spawner.HighestFromLeap = 10f;
                spawner.GenerateParabola();

                Spawner = spawner;
                
                Selection.SetActiveObjectWithContext(root, null);
            }
            
#endif
            
        }

        [BoxGroup("웨이브 설정"), LabelText("Gizmos 항상 그리기")] 
        public bool DrawGizmosAlways = false;
        [BoxGroup("웨이브 설정"), LabelText("웨이브 목록"), ListDrawerSettings(OnBeginListElementGUI = "OnBeginWaveElementGUI")]
        public List<WaveSettings> Waves = new();
        
        [field: SerializeField, BoxGroup("이벤트")] public UnityEvent OnStageStart { get; private set; }
        [field: SerializeField, BoxGroup("이벤트")] public UnityEvent<int> OnWaveStart { get; private set; }
        [field: SerializeField, BoxGroup("이벤트")] public UnityEvent OnStageEnd { get; private set; }
        
        private void Awake()
        {
            
        }

        [field: SerializeField, ReadOnly]
        public int CurrentWaveIndex { get; private set; } = -1;
        public bool IsStarted => CurrentWaveIndex >= 0;
        public bool IsEnded => CurrentWaveIndex >= Waves.Count;
        public bool IsRunning => IsStarted && !IsEnded;
        public WaveSettings CurrentWave => Waves[CurrentWaveIndex];

        [ReadOnly]
        public int LivingEnemyCount => _spawnedMonsters.Count;
        [field: SerializeField, ReadOnly] 
        public float WaveSkipTime { get; private set; } = 0;
        
        /// <summary>
        /// 스테이지를 시작합니다.
        /// </summary>
        /// <param name="forced">강제 실행 여부입니다. false일 시 이미 진행중이거나 완료된 스테이지는 시작되지 않습니다.</param>
        [Button("스테이지 시작")]
        public void StartStage(bool forced = false)
        {
            if(!forced && (IsStarted || IsEnded))
            {
                Debug.LogWarning($"{name}: StartStage()를 호출했지만 이미 이미 진행중이거나 끝난 스테이지입니다.", gameObject);
                return;
            }

            OnStageStart.Invoke();
            CurrentWaveIndex = -1;
            _spawnedMonsters.Clear();
            NextWave();
        }

        private void NextWave()
        {
            // 이전 웨이브 종료 판정
            if (IsStarted)
            {
                EndWave(CurrentWave);
            }
            
            ++CurrentWaveIndex;
            
            // 모든 웨이브 종료
            if (IsEnded)
            {
                EndStage();    
                return;
            }
            
            // 이번 웨이브 시작
            StartWave(CurrentWave);
        }

        private readonly HashSet<Monster> _spawnedMonsters = new();
        private void StartWave(WaveSettings wave)
        {
            WaveSkipTime = wave.WaveSkipMonsterCount > 0 ? wave.WaveSkipTime : -1f;
            
            OnWaveStart.Invoke(CurrentWaveIndex);

            foreach (var setting in wave.SpawnSettings)
            {
                if (setting.EnemyType == EnemyType.None)
                {
                    continue;
                }
                if (!setting.Spawner)
                {
                    Debug.LogWarning($"웨이브 {CurrentWaveIndex}에 {setting.EnemyType} Spawner가 유효하지 않음", gameObject);
                    continue;
                }
                Spawn(setting);
            }
        }

        private void Spawn(WaveSpawnSettings setting)
        {
            var spawnedMonster = setting.Spawner.Spawn(setting.EnemyType, setting.Delay);
            _spawnedMonsters.Add(spawnedMonster);
            spawnedMonster.OnDeadEvent.AddListener(OnSpawnedMonsterDead);
            
            // 마지막 웨이브에 스폰되는 몬스터일 때에만 빙결 밀림 시 사망 판정으로 처리
            if (CurrentWaveIndex == Waves.Count - 1)
            {
                spawnedMonster.OnFreezeSlipEvent.AddListener(OnSpawnedMonsterDead);
            }
        }

        private void OnSpawnedMonsterDead(Monster monster)
        {
            if (!_spawnedMonsters.Remove(monster))
            {
                return;
            }
            monster.OnDeadEvent.RemoveListener(OnSpawnedMonsterDead);
            monster.OnFreezeSlipEvent.RemoveListener(OnSpawnedMonsterDead);
            
            if (IsRunning && _spawnedMonsters.Count <= 0 && CurrentWave.Condition == WaveSettings.WaveClearCondition.KillAllMonsters)
            {
                NextWave();
                return;
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!Application.IsPlaying(gameObject))
            {
                UpdateEditor();
                return;
            }
#endif

            // 실행 중 한정
            if (!IsRunning)
            {
                return;
            }

            // 남은 몬스터 수가 스킵 카운트 이하일 경우
            if (WaveSkipTime > 0f && _spawnedMonsters.Count <= CurrentWave.WaveSkipMonsterCount)
            {
                // 설정된 타이머가 지나면 다음 웨이브 시작
                WaveSkipTime -= Time.deltaTime;
                if (WaveSkipTime <= 0)
                {
                    NextWave();
                }
            }

        }

        private void UpdateEditor()
        {
        }
        
        private void EndWave(WaveSettings wave)
        {
            
        }

        private void EndStage()
        {
            OnStageEnd.Invoke();
        }
        
        ///////////////////
        ///////////////////
        ///////////////////

#if UNITY_EDITOR
        
        private void OnDrawGizmos()
        {
            if(!DrawGizmosAlways) return;
            DrawGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            if(DrawGizmosAlways) return;
            DrawGizmos();
        }

        private void DrawGizmos()
        {
            foreach (var wave in Waves)
            {
                if(!wave.DrawGizmo) continue;
                foreach (var settings in wave.SpawnSettings)
                {
                    if (settings.Spawner)
                    {
                        settings.Spawner.DrawGizmosOnValid();
                    }
                }
            }
        }

        private void OnBeginWaveElementGUI(int index)
        { 
            var style = new GUIStyle
            {
                richText = true,
                fontStyle = FontStyle.Bold
            };
            EditorGUILayout.LabelField($"<color=white>웨이브 {index}</color>", style);
        }
#endif
    }
}