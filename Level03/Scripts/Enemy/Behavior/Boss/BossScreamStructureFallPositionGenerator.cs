using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Utility;
using Logger = NKStudio.Logger;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Enemy.Behavior.Boss
{
    public class BossScreamStructureFallPositionGenerator : MonoBehaviour
    {
        public List<Transform> TargetPositionObjects = new();

        [ReadOnly] public List<Transform> SelectedPositions = new(); 
        [ReadOnly] public List<Transform> SelectablePositions = new();
        
        [field: SerializeField] public List<Collider> Colliders { get; private set; } = new();

        [BoxGroup("선택 시뮬레이션")]
        public int SimulationCount = 5;
        [BoxGroup("선택 시뮬레이션")]
        public float SimulationRadius = 2f;


        [BoxGroup("선택 시뮬레이션"), Button("시뮬레이션")]
        private bool SimulateSelectRandomPositions() => SelectRandomPositions(SimulationCount, SimulationRadius);

        [BoxGroup("선택 시뮬레이션"), Button("검증")]
        private bool Verify(int repeatCount = 100)
        {
            Debug.Log($"count={SimulationCount}, radius={SimulationRadius}에 대한 {repeatCount}회 검증 시작 ...");
            int minimumSelectableCount = int.MaxValue;
            for (int i = 0; i < repeatCount; i++)
            {
                if (!SelectRandomPositions(SimulationCount, SimulationRadius))
                {
                    Debug.LogWarning($"[{i+1:000}] 검증 실패! 갯수를 만족할 수 없는 경우가 있음!!");
                    return false;
                }
                Debug.Log($"[{i+1:000}] 남은 수={SelectablePositions.Count}");
                minimumSelectableCount = Mathf.Min(minimumSelectableCount, SelectablePositions.Count);
            }
            Debug.Log($"{repeatCount}회 검증 완료. 최소 남은 수={minimumSelectableCount}");
            return true;
        }
        
        /// <summary>
        /// 생성된 위치 중 무작위로 반지름 내에 겹치지 않는 지점들을 구합니다.
        /// </summary>
        /// <param name="count">지점 갯수입니다.</param>
        /// <param name="radius">지점간 최소 거리입니다.</param>
        /// <returns>성공 여부입니다.</returns>
        public bool SelectRandomPositions(int count, float radius)
        {
            SelectedPositions.Clear();
            SelectablePositions.Clear();
            SelectablePositions.AddRange(TargetPositionObjects);

            // 섞기
            SelectablePositions.Shuffle();
            float radiusSquared = radius * radius;
            for (int i = 0; i < count; i++)
            {
                if (SelectablePositions.Count <= 0)
                {
                    Logger.LogWarning($"더 이상 남은 선택 가능한 위치가 없습니다! (count={count}, radius={radius})");
                    return false;
                }
                
                // 마지막에 있는 원소 하나 선택
                var selection = SelectablePositions[^1];
                SelectedPositions.Add(selection);

                var origin = selection.position;
                // 수평 거리가 일정 이내인 원소 제거
                SelectablePositions.RemoveAll(it => (it.position - origin).Copy(y: 0f).sqrMagnitude <= radiusSquared);
            }

            return true;
        }

        [BoxGroup("선택 시뮬레이션"), Button("시뮬레이션 초기화")]
        private void ClearSimulation()
        {
            SelectedPositions.Clear();
            SelectablePositions.Clear();
        }
        
#if UNITY_EDITOR

        
        
        private struct SimulatedPoint
        {
            public Vector3 Top;
            public Vector3 Point;
            public float X, Z;
        }
        
        private Bounds _calculatedBounds;
        private readonly List<SimulatedPoint> _simulatedPoints = new();

        [BoxGroup("생성기 설정")] public int GeneratorSectorMaxCount = -1;
        [BoxGroup("생성기 설정")] public LayerMask GeneratorGroundMask = 1 << 7;
        [BoxGroup("생성기 설정")] public float GenerateDistancePeriod = 1f;
        [BoxGroup("생성기 설정")] public float GenerateRadiusMultiplier = 0.5f;
        [BoxGroup("생성기 설정")] public float GenerateRaycastError = 2f;
        [BoxGroup("생성기 설정")] public float GenerateDebugLineTime = 5f;
        [BoxGroup("생성기 설정")] public Color GenerateDebugLineInvalidColor = Color.red;
        [BoxGroup("생성기 설정")] public Color GenerateDebugLineValidColor = Color.green;
        

        // https://github.com/halak/unity-editor-icons
        [BoxGroup("생성기 설정")] public string GenerateSectorIcon = "sv_label_1";

        [BoxGroup("생성기 설정"), Button("시뮬레이션 데이터 초기화")]
        private void ClearSimulatedData()
        {
            _calculatedBounds = new Bounds();
            // Colliders.Clear();
            _simulatedPoints.Clear();
        }

        [BoxGroup("생성기 설정"), Button("생성된 오브젝트 제거")]
        private void ClearSectorObjects()
        {
            // 기존 Sector 삭제
            TargetPositionObjects.ForEach(it => DestroyImmediate(it.gameObject));
            TargetPositionObjects.Clear();
            SelectablePositions.Clear();
            SelectedPositions.Clear();
        }
        
        [BoxGroup("생성기 설정"), Button("Bounds 계산 & 시뮬레이션")]
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
            Colliders.Clear();
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

                    bool raycastResult = Physics.Raycast(
                        point,
                        Vector3.down,
                        out var hitInfo,
                        (size.y + GenerateRaycastError),
                        GeneratorGroundMask.value
                    );
                    if (!raycastResult)
                    {
                        DebugX.DrawLine(point, point + Vector3.down * (size.y + GenerateRaycastError),
                            GenerateDebugLineInvalidColor, GenerateDebugLineTime);
                        continue;
                    }
                    
                    bool isInCollider = Colliders.All(it => !it.ContainsXZ(hitInfo.point)); 
                    if (isInCollider) {
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

        [BoxGroup("생성기 설정"), Button("자식에 있는 오브젝트 연결")]
        private void BindSectorsFromChildren()
        {
            TargetPositionObjects.Clear();
            var childSectors = transform.GetComponentsInChildren<Transform>();
            foreach (var sector in childSectors)
            {
                if(sector == transform) continue;
                if (sector.TryGetComponent(out Collider c) && Colliders.Contains(c))
                {
                    continue;
                }
                TargetPositionObjects.Add(sector);
            }
        }

        [BoxGroup("생성기 설정"), Button("시뮬레이션 기반으로 오브젝트 생성")]
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
                
                TargetPositionObjects.Add(sectorTransform);
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
            var isSectorsEmpty = TargetPositionObjects.IsEmpty();
            var radius = GenerateDistancePeriod * 0.5f * GenerateRadiusMultiplier;
            foreach (var simulatedPoint in _simulatedPoints)
            {
                Gizmos.DrawLine(simulatedPoint.Top, simulatedPoint.Point);
                if (isSectorsEmpty)
                {
                    DrawUtility.DrawCircle(simulatedPoint.Point, radius, Vector3.up, 16, Gizmos.DrawLine);
                }
            }

            if (SelectablePositions.Count > 0 || SelectedPositions.Count > 0)
            {
                float radiusHalf = SimulationRadius * 0.5f;
                Gizmos.color = Color.red.Copy(a: 0.3f);
                foreach (var t in TargetPositionObjects)
                {
                    if(!t) continue;
                    var position = t.position;
                    DrawUtility.DrawCircle(position, radiusHalf, Vector3.up, 16, Gizmos.DrawLine);
                    Gizmos.DrawLine(position, position + Vector3.up * 2f);
                }
                Gizmos.color = Color.white.Copy(a: 0.2f);
                foreach (var t in SelectablePositions)
                {
                    if(!t) continue;
                    var position = t.position;
                    DrawUtility.DrawCircle(position, radiusHalf, Vector3.up, 16, Gizmos.DrawLine);
                    Gizmos.DrawLine(position, position + Vector3.up * 2f);
                }
                Gizmos.color = Color.green;
                foreach (var t in SelectedPositions)
                {
                    if(!t) continue;
                    var position = t.position;
                    DrawUtility.DrawCircle(position, radiusHalf, Vector3.up, 16, Gizmos.DrawLine);
                    Gizmos.DrawLine(position, position + Vector3.up * 2f);
                }
            }
            else
            {
                float radiusHalf = SimulationRadius * 0.5f;
                Gizmos.color = Color.white;
                foreach (var t in TargetPositionObjects)
                {
                    if(!t) continue;
                    var position = t.position;
                    DrawUtility.DrawCircle(position, radiusHalf, Vector3.up, 16, Gizmos.DrawLine);
                    Gizmos.DrawLine(position, position + Vector3.up * 2f);
                }
            }
        }
        
#endif
        
        
    }
}