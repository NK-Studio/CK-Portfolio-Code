using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Enemy.Spawner;
using EnumData;
using Sirenix.OdinInspector;
using UnityEngine;
using Utility;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Enemy.Behavior.Boss
{
    public class BossEnemySpawner : SerializedMonoBehaviour
    {
        [Serializable]
        public class EnemySpawnerController
        {
            public EnemySpawner Spawner { get; }
            public bool IsSpawning = false;

            public EnemySpawnerController(EnemySpawner spawner)
            {
                Spawner = spawner;
                IsSpawning = false;
            }
            
        }
        [Serializable]
        public class SpawnSettings
        {

            [LabelText("생성기")]
            public EnemySpawner Spawner;
            
#if UNITY_EDITOR
            [Button("즉시 소환"), ShowIf("@Spawner == null"), HorizontalGroup(GroupID = "button")]
            public void GenerateSingleSpawner()
            {
                if(Spawner) return;
                
                var manager = Selection.activeGameObject;
                var root = new GameObject($"Spawner");
                root.transform.SetParent(manager.transform, false);

                var spawner = root.AddComponent<EnemySpawner>();
                Spawner = spawner;
                
                Selection.SetActiveObjectWithContext(root, null);
            }
            
            [Button("포물선 소환"), ShowIf("@Spawner == null"), HorizontalGroup(GroupID = "button")]
            public void GenerateParabolaSpawner()
            {
                if(Spawner) return;

                var manager = Selection.activeGameObject;
                var root = new GameObject($"ParabolaSpawner");
                root.transform.SetParent(manager.transform, false);

                var start = new GameObject($"ParabolaSpawner_Start");
                start.transform.SetParent(root.transform, false);
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
        
        [SerializeField, LabelText("생성기 목록")]
        private List<SpawnSettings> _spawners = new();
        [field: SerializeField, LabelText("동작중인 생성기"), ReadOnly]
        public List<EnemySpawnerController> Spawners { get; private set; } = new();
        private List<int> _spawnerIndices = new();

        [field: SerializeField, LabelText("종류별 최대 개체 수")]
        public Dictionary<EnemyType, int> MaximumEnemyCountByType { get; private set; } = new()
        {
            { EnemyType.ClubMonster, 2 },
            { EnemyType.SeahorseMonster, 2 },
            { EnemyType.SeahorseMonster02, 2 },
            { EnemyType.SeahorseMonster03, 2 },
            { EnemyType.JellyfishMonster, 2 },
            { EnemyType.StingrayMonster, 2 },
        };
        
        [LabelText("총 개체 수")]
        public int MaximumEnemyCount = 5;
        
        [LabelText("소환 트리거 주기")]
        public float Period = 5;

        [LabelText("주기당 최대 소환 개체 수")]
        public int MaximumSpawnCountOnce = 5;
        
        [LabelText("소환 딜레이 범위"), MinMaxSlider(0f, 5f)]
        public Vector2 DelayRange = new Vector2(0.7f, 2f); 

        [field: SerializeField, LabelText("킬 카운트"), ReadOnly]
        public int KillCounter { get; private set; } = 0;
        
        [SerializeField, LabelText("활성화 여부")] 
        private bool _enabled = false;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (!_enabled && value)
                {
                    KillCounter = 0;
                    _updateTime = 0;
                }
                _enabled = value;
            }
        }

        // 현재 소환된 몬스터
        public HashSet<Monster> SpawnedEnemies { get; } = new();

        private void Start()
        {
            foreach (var spawner in _spawners)
            {
                Spawners.Add(new EnemySpawnerController(spawner.Spawner));
            }
            _spawnerIndices.Clear();
            _spawnerIndices.Capacity = Spawners.Count;
            for (int i = 0; i < Spawners.Count; i++)
            {
                _spawnerIndices.Add(i);
            }
        }

        [SerializeField, ReadOnly]
        private float _updateTime = 0f;
        private void Update()
        {
            if(!Enabled) return;

            if (_updateTime > 0f)
            {
                _updateTime -= Time.deltaTime;
                return;
            }

            _updateTime = Period;
            UpdateDemand();
            ResolveDemand();
#if UNITY_EDITOR
            _spawnQueuePrinter = _spawnQueue.ToList().JoinToString("\n", type => type.ToString());
#endif
        }

        // 소환 대기중인 몬스터
        private Queue<EnemyType> _spawnQueue = new();
#if UNITY_EDITOR
        [field: SerializeField, ReadOnly, MultiLineProperty(Lines = 8)]
        private string _spawnQueuePrinter;
#endif
        /// <summary>
        /// 수요 해결: 사용중이지 않은 스포너들에 현재 queue에 쌓인 물량 해결
        /// </summary>
        private void ResolveDemand()
        {
            _spawnerIndices.Shuffle();
            Debug.Log($"[{_spawnerIndices.JoinToString(", ", it => it.ToString())}]");
            int spawnedCount = 0;
            for (int i = 0; i < _spawnerIndices.Count; i++)
            {
                if (spawnedCount >= MaximumSpawnCountOnce)
                {
                    return;
                }
                var controller = Spawners[_spawnerIndices[i]];
                
                // 소환 중인 스포너 제외 queue 해결
                if (controller.IsSpawning)
                {
                    continue;
                }

                if (!_spawnQueue.TryDequeue(out var type))
                {
                    return;
                }

                var spawnedMonster = controller.Spawner.Spawn(type, DelayRange.Random());
                SpawnedEnemies.Add(spawnedMonster);
                spawnedMonster.OnDeadEvent.AddListener(OnSpawnedMonsterDead);
                // 소환 중인 스포너는 소환 못하도록 holding
                SpawnerSequence(controller, spawnedMonster).Forget();

                ++spawnedCount;
            }
        }

        private static async UniTaskVoid SpawnerSequence(EnemySpawnerController controller, Monster m)
        {
            controller.IsSpawning = true;
            await UniTask.WaitWhile(() => m.IsRunningSpawnSequence);
            controller.IsSpawning = false;
        }
        
        private void OnSpawnedMonsterDead(Monster monster)
        {
            monster.OnDeadEvent.RemoveListener(OnSpawnedMonsterDead);
            SpawnedEnemies.Remove(monster);
            ++KillCounter;
        }
        
        private Dictionary<EnemyType, int> _spawnableEnemyCountByType = new();
        private List<EnemyType> _spawnableTypes = new();

        /// <summary>
        /// 수요 갱신: 현재 부족한 몬스터 수만큼 queue에 올려놓기
        /// </summary>
        private void UpdateDemand()
        {
            // 남은 총 개체량: 최대 개체량 - (현재 소환된 수 + 소환 대기 수) 
            int spawnCount = MaximumEnemyCount - (SpawnedEnemies.Count + _spawnQueue.Count);
            if(spawnCount <= 0) return;
            
            // 종류별 현재 소환 가능한 수 얻어오기
            _spawnableEnemyCountByType.Clear();
            foreach (var (type, count) in MaximumEnemyCountByType)
            {
                _spawnableEnemyCountByType.Add(type, count);
            }
            // Dictionary에서 소환된 몬스터들 갯수만큼 하나씩 빼기
            foreach (var m in SpawnedEnemies)
            {
                var type = m.Settings.Type;
                if (_spawnableEnemyCountByType.TryGetValue(type, out var count))
                {
                    if (count <= 1)
                    {
                        _spawnableEnemyCountByType.Remove(type);
                    }
                    else
                    {
                        _spawnableEnemyCountByType[type] = count - 1;
                    }
                }
            }

            if (_spawnableEnemyCountByType.IsEmpty() 
                // 종류별 갯수 합이 필요 소환 수보다 적은경우 ??
                || _spawnableEnemyCountByType.Sum(it => it.Value) < spawnCount)
            {
                Debug.LogWarning($"필요 개체수가 {spawnCount}인데 소환 가능한 적 종류가 부족합니다.");
                return;
            }

            // EnemyType을 리스트로 갯수만큼 배열
            _spawnableTypes.Clear();
            foreach (var (type, amount) in _spawnableEnemyCountByType)
            {
                for (int i = 0; i < amount; i++)
                {
                    _spawnableTypes.Add(type);
                }
            }
            
            // 섞고 나서 하나씩 뒤에서 빼서 queue
            _spawnableTypes.Shuffle();
            for (int i = 0; i < spawnCount; i++)
            {
                var type = _spawnableTypes[^1];
                _spawnableTypes.RemoveAt(_spawnableTypes.Count - 1);
                
                // Queue에 넣기. ResolveDemand()에서 소환 예정
                _spawnQueue.Enqueue(type);
            }
        }

#if UNITY_EDITOR
        
        private void OnDrawGizmos()
        {
            foreach (var settings in _spawners)
            {
                if (settings.Spawner)
                {
                    settings.Spawner.DrawGizmosOnValid();
                }
            }
        }
#endif
    }
}