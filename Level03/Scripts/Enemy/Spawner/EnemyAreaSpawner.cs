using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Character.Presenter;
using Doozy.Runtime.Common.Extensions;
using Enemy.Behavior;
using EnumData;
using Managers;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Utility;
using Random = UnityEngine.Random;

namespace Enemy.Spawner
{
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyAreaSpawner : MonoBehaviour
    {

        [Serializable]
        public struct EnemyAreaSpawnData
        {
            [Tooltip("입장 뒤 소환될 시간입니다."), ValidateInput("@Time >= 0", "소환 시간은 양수여야 합니다.")]
            public int Time;

            [Tooltip("소환 시 소환되는 몬스터 수입니다."), ValidateInput("@Amount >= 0", "소환 갯수는 양수여야 합니다.")]
            public int Amount;

            [Tooltip("소환 시 사용될 프리팹입니다."), ValidateInput("@SpawnEnemyType != EnemyType.None", "소환 대상 EnemyType이 비어있습니다.")]
            public EnemyType SpawnEnemyType;

            [Tooltip("이 소환이 실행될 때 호출되는 이벤트입니다.")]
            public UnityEvent OnExecuted;
        }

        [Header("전투 구역 설정")] [Tooltip("이 구역이 전투 구역인지 설정합니다. 아닐 경우 여기서 소환된 몬스터는 전투 구역으로 설정된 구역으로 먼저 이동합니다.")]
        public bool IsBattleArea = true;

        [Tooltip("전투 구역 입장 시 시간에 따라 소환되는 몬스터 목록입니다. 시간 순서대로 정렬되어 있어야 정상적으로 작동합니다.")]
        public List<EnemyAreaSpawnData> EnemyAreaSpawnDataList;

        [Header("Sector 설정"), Tooltip("이 리스트는 하단의 자동생성기를 통해 만드는 것이 정신건강에 이롭습니다.")]
        public List<EnemySpawnSector> Sectors = new();

        [Tooltip("각 Sector와 플레이어 사이 거리가 이 수치보다 가까우면 해당 Sector는 스폰 대상에 제외됩니다.")]
        public float PlayerAvoidDistance = 10f;

        private PlayerPresenter _player;
        private BattleArea _parent;
        private int _areaSpawnDataIndex;

        // 스폰할 때 Sector 리스트에서 제외하고 섞을 때 사용하는 버퍼 리스트
        private List<EnemySpawnSector> _sectorCache;

        private void Start()
        {
            // TODO 플레이어 어디서 얻어오지 ..
            _sectorCache = new List<EnemySpawnSector>(Sectors.Count);
            _areaSpawnDataIndex = 0;
            
        }

        [Button("Rigidbody 초기화")]
        private void InitializeRigidbody()
        {
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        public void Initialize(BattleArea parent, PlayerPresenter player)
        {
            _player = player;
            _parent = parent;
            
            var spawnCount = EnemyAreaSpawnDataList.Sum(it => it.Amount);
            _parent.RegisterMonsterCount(spawnCount);
        }

        /// <summary>
        /// PC 진입 후 매 초 BattleArea에 의해 호출됩니다.
        /// </summary>
        /// <param name="count"></param>
        public void OnTimerUpdate(int count)
        {
            if (_areaSpawnDataIndex >= EnemyAreaSpawnDataList.Count)
            {
                return;
            }

            for (int i = _areaSpawnDataIndex; i < EnemyAreaSpawnDataList.Count; i++)
            {
                var data = EnemyAreaSpawnDataList[i];
                // 지금 읽는 line의 시간이 현재 초시간보다 높을 경우 중지
                if (data.Time > count)
                {
                    _areaSpawnDataIndex = i;
                    break;
                }

                // 지정된 갯수만큼 소환
                Spawn(data.SpawnEnemyType, data.Amount);
                data.OnExecuted?.Invoke();
                ++_areaSpawnDataIndex;
            }
        }

        private List<Monster> _dummyList = new();
        /// <summary>
        /// 설정된 Prefab을 임의의 Sector들에 Instantiate합니다. 
        /// </summary>
        /// <param name="type">소환될 EnemyType입니다.</param>
        /// <param name="amount">소환될 Prefab 갯수입니다.</param>
        public void Spawn(EnemyType type, int amount)
        {
            Spawn(type, amount, ref _dummyList);
            _dummyList.Clear();
        }
        public void Spawn(EnemyType type, int amount, ref List<Monster> list)
        {
            if (!_player)
                return;

            _sectorCache.Clear();

            Vector3 playerPosition = _player.transform.position;
            playerPosition.y = 0f;

            float avoidDistanceSquared = PlayerAvoidDistance * PlayerAvoidDistance;

            //모든 세터에 접근
            foreach (var sector in Sectors)
            {
                // 플레이어 거리가 
                Vector3 betweenToPlayer = sector.transform.position;
                betweenToPlayer.y = 0f;
                betweenToPlayer -= playerPosition;
                float distanceSquared = betweenToPlayer.sqrMagnitude;

                // 범위 안이거나, 설정된 AvoidDistance보다 적으면 제외
                if (distanceSquared <= sector.RadiusSquared || distanceSquared <= avoidDistanceSquared)
                {
                    continue;
                }

                _sectorCache.Add(sector);
            }

            // 랜덤으로 섞음
            Extensions.Shuffle(_sectorCache);
            var cacheCount = _sectorCache.Count;
            
            // 예외처리: Count 0이면 아무거나 하나 랜덤으로 추가
            if (_sectorCache.Count <= 0)
            {
                _sectorCache.Add(Sectors.GetRandomItem());
            }
            
            var cacheIndex = 0;

            list.Capacity = Math.Max(list.Count + amount, list.Capacity);
            // 필요한 갯수만큼 선택된 Sector들에서 생성
            for (int i = 0; i < amount; i++)
            {
                cacheIndex = (cacheIndex + 1) % cacheCount;

                var sector = _sectorCache[cacheIndex];
                //xz 평면 랜덤 원
                Vector3 randomCircle = Random.insideUnitCircle;
                randomCircle.z = randomCircle.y;
                randomCircle.y = 0;
                Vector3 spawnPosition = sector.transform.position + randomCircle * sector.Radius;

                GameObject monsterObject = EnemyPoolManager.Instance.Get(type);
                if (monsterObject.TryGetComponent(out NavMeshAgent agent))
                {
                    agent.Warp(spawnPosition);
                }
                else
                {
                    monsterObject.transform.position = spawnPosition;
                }
                Debug.Log($"monster {name}({type}) spawned at {spawnPosition}");

                Monster monster = monsterObject.GetComponent<Monster>();
                if (monster)
                {
                    monster.Initialize(_parent);
                    _parent.RegisterSpawnedMonster(monster);
                    
                    list.Add(monster);
                }
            }
        }

        [field: SerializeField] public List<Collider> Colliders { get; private set; } = new();

#if UNITY_EDITOR

        private struct SimulatedPoint
        {
            public Vector3 Top;
            public Vector3 Point;
            public float X, Z;
        }

        private Bounds _calculatedBounds;
        private readonly List<SimulatedPoint> _simulatedPoints = new();

        [Header("Sector 생성기 관련 설정")] 
        public int GeneratorSectorMaxCount = -1;
        public LayerMask GeneratorGroundMask;
        public float GenerateDistancePeriod = 1f;
        public float GenerateRadiusMultiplier = 0.5f;
        public float GenerateRaycastError = 2f;
        public float GenerateDebugLineTime = 5f;
        public Color GenerateDebugLineInvalidColor = Color.red;

        public Color GenerateDebugLineValidColor = Color.green;

        // https://github.com/halak/unity-editor-icons
        public string GenerateSectorIcon = "sv_label_1";

        [Button("시뮬레이션 데이터 초기화")]
        private void ClearSimulatedData()
        {
            _calculatedBounds = new Bounds();
            Colliders.Clear();
            _simulatedPoints.Clear();
        }

        [Button("생성된 Sector 오브젝트 제거")]
        private void ClearSectorObjects()
        {
            // 기존 Sector 삭제
            Sectors.ForEach(it => DestroyImmediate(it.gameObject));
            Sectors.Clear();
        }

        [Button("Bounds 계산 & 시뮬레이션")]
        private void CalculateBoundsAndCollectColliders()
        {
            ClearSimulatedData();

            // 모든 Collider들의 Bound 합
            var colliders = GetComponentsInChildren<Collider>();
            if (colliders.IsEmpty())
            {
                Debug.LogWarning("수집한 Collider가 없습니다. 자식 오브젝트에 콜라이더를 생성해 주세요.");
                return;
            }

            foreach (var c in colliders)
            {
                DebugX.Log($"Collider: {c.name}");
                _calculatedBounds.EncapsulateOrSet(c.bounds);
                Colliders.Add(c);
            }

            var leftCount = GeneratorSectorMaxCount;

            // 최대 Bound를 XZ축 정렬하게 순회 (단 collider에 포함된 지점이어야 함)
            var size = _calculatedBounds.size;
            var start = _calculatedBounds.min.Copy(y: _calculatedBounds.max.y);
            for (var x = 0f; x <= size.x; x += GenerateDistancePeriod)
            {
                for (var z = 0f; z <= size.z; z += GenerateDistancePeriod)
                {
                    var point = start.Copy(x: start.x + x, z: start.z + z);

                    // collider에 포함되고, Raycast to ground 성공해야 함
                    if (!Physics.Raycast(
                        point, 
                        Vector3.down, 
                        out var hitInfo, 
                        (size.y + GenerateRaycastError), 
                        GeneratorGroundMask.value)
                        || Colliders.All(it => !it.ContainsXZ(hitInfo.point))
                    ) {
                        DebugX.DrawLine(point, point + Vector3.down * (size.y + GenerateRaycastError),
                            GenerateDebugLineInvalidColor, GenerateDebugLineTime);
                        continue;
                    }

                    _simulatedPoints.Add(new SimulatedPoint { Top = point, Point = hitInfo.point, X = x, Z = z, });
                    --leftCount;

                    if (leftCount == 0)
                    {
                        break;
                    }
                }

                if (leftCount == 0)
                {
                    break;
                }
            }
        }

        [Button("자식에 있는 Sector 오브젝트 연결")]
        private void BindSectorsFromChildren()
        {
            Sectors.Clear();
            var childSectors = transform.GetComponentsInChildren<EnemySpawnSector>();
            foreach (var sector in childSectors)
            {
                Sectors.Add(sector);
            }
        }

        [Button("시뮬레이션 기반으로 Sector 오브젝트 생성")]
        private void GenerateSectorsFromSimulatedPoints()
        {
            ClearSectorObjects();
            if (Colliders.IsEmpty() || _simulatedPoints.IsEmpty())
            {
                Debug.LogWarning("Calculate Bounds & Collect Colliders를 먼저 실행하세요.");
                return;
            }

            var iconContent = EditorGUIUtility.IconContent(GenerateSectorIcon);
            var radius = GenerateDistancePeriod * 0.5f * GenerateRadiusMultiplier;
            foreach (var simulatedPoint in _simulatedPoints)
            {
                var point = simulatedPoint.Point;

                // Sector 생성
                var sectorObject = new GameObject($"SpawnSector({simulatedPoint.X},{simulatedPoint.Z})");
                EditorGUIUtility.SetIconForObject(sectorObject, (Texture2D)iconContent.image);

                var sectorTransform = sectorObject.transform;
                sectorTransform.position = point;
                sectorTransform.SetParent(transform, true);

                var sector = sectorObject.AddComponent<EnemySpawnSector>();
                sector.Radius = radius;
                Sectors.Add(sector);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Bounds 그리기
            if (_calculatedBounds.extents.sqrMagnitude > 0f)
            {
                Gizmos.color = Color.cyan;
                _calculatedBounds.DrawBox(Gizmos.DrawLine);
            }

            // 시뮬레이션된 Sector들 그리는 수직선
            Gizmos.color = GenerateDebugLineValidColor;
            var isSectorsEmpty = Sectors.IsEmpty();
            var radius = GenerateDistancePeriod * 0.5f * GenerateRadiusMultiplier;
            foreach (var simulatedPoint in _simulatedPoints)
            {
                Gizmos.DrawLine(simulatedPoint.Top, simulatedPoint.Point);
                if (isSectorsEmpty)
                {
                    DrawUtility.DrawCircle(simulatedPoint.Point, radius, Vector3.up, 16, Gizmos.DrawLine);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (_player && _areaSpawnDataIndex < EnemyAreaSpawnDataList.Count)
            {
                Gizmos.color = Color.red;
                DrawUtility.DrawCircle(_player.transform.position, PlayerAvoidDistance, Vector3.up, 16,
                    Gizmos.DrawLine);
            }
        }
#endif
    }
}