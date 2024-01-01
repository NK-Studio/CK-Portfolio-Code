using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;
using Utility;

namespace UI
{
    public class CombinableOffScreenUIController : MonoBehaviour
    {
        public bool DebugMode = false;
        public CombinedOffScreenUI CombinedUIPrefab;
        public List<CombinedOffScreenUI> CombinedUIList = new List<CombinedOffScreenUI>(16);
        
        public CombinableOffScreenUIPool Pool;
        
        // 등록된 모든 Combinable
        public HashSet<CombinableOffScreenUI> Combinable = new();
#if UNITY_EDITOR
        [field: SerializeField, ReadOnly, MultiLineProperty(Lines = 8)]
        private string _combinableSetPrinter;

        private void UpdateCombinableSetPrinter()
        {
            _combinableSetPrinter = Combinable.ToList()
                .JoinToString("\n", it => it.name + (it.Group != null ? $" (group={it.Group?.PoolIndex:00})" : ""));
        }
#endif
        // 형성된 Group
        public Dictionary<int, CombinedGroup> Groups = new();
        
        public CombinableOffScreenUI RegisterNew()
        {
            var ui = Pool.Get();
            Register(ui);
            return ui;
        }
        public void Register(CombinableOffScreenUI c)
        {
            Combinable.Add(c);
#if UNITY_EDITOR
            UpdateCombinableSetPrinter();
#endif
        }

        public void ReleaseAndUnregister(CombinableOffScreenUI c)
        {
            c.TargetObject = null;
            Pool.Release(c);
            Unregister(c);
        }
        public void Unregister(CombinableOffScreenUI c)
        {
            Combinable.Remove(c);
#if UNITY_EDITOR
            UpdateCombinableSetPrinter();
#endif
        }

        private void Awake()
        {
            for (int i = 0; i < CombinedUIList.Capacity; i++)
            {
                var combined = Instantiate(CombinedUIPrefab, transform);
                CombinedUIList.Add(combined);
                combined.gameObject.SetActive(false);
            }
        }

        private void Log(string message)
        {
            if(DebugMode) Debug.Log(message);
        }
        
        private void LateUpdate()
        {
            Log("<color=white>Controller::LateUpdate() ============================</color>");
            
            // 0. 이전 프레임의 그룹 초기화
            foreach (var (poolIndex, group) in Groups)
            {
                group.Release();
            }
            Groups.Clear();

            bool hasOnScreen = false;
            // 1. 각 Combinable 갱신
            foreach (var c in Combinable)
            {
                c.CombineMarked = false;
                c.Group = null;
                c.UpdatePosition();

                if (c.State == CombinableOffScreenUI.OffScreenState.OnScreen)
                {
                    hasOnScreen = true;
                }
            }

            if (hasOnScreen)
            {
                foreach (var c in Combinable)
                {
                    c.IsVisible = false;
                }

                foreach (var c in CombinedUIList)
                {
                    c.gameObject.SetActive(false);
                }
                return;
            }
            
            // 2. 갱신된 Combinable 위치 기반으로 결합 -> Group 생성
            foreach (var c in Combinable)
            {
                if (!c.IsValid)
                {
                    continue;
                }
                // 이미 다른 Combinable에 의해 접근된 Combinable은 결합 시도하지 않음
                if (c.CombineMarked)
                {
                    continue;
                }
                c.CombineMarked = true;
                
                var range = c.CombineRange;
                if (!range)
                {
                    continue;
                }
                range.Pulse();
                // Log($"{c.name} Creating Group ...");
                int count = 0;
                int me = c.gameObject.GetInstanceID();
                foreach (var obj in range.Detections)
                {
                    if (obj.GetInstanceID() == me)
                    {
                        continue;
                    }
                    if (!obj.TryGetComponent(out CombinableOffScreenUI other))
                    {
                        continue;
                    }
                    ++count;

                    // 상대 접근 표시
                    other.CombineMarked = true;
                    
                    // 내 그룹이 있으면
                    if (c.Group != null)
                    {
                        // 상대 그룹도 있으면
                        if (other.Group != null)
                        {
                            if (other.Group == c.Group)
                            {
                                Log($"{c.name} => {other.name}[{count}] same group, skipped");
                            }
                            else
                            {
                                int otherIndex = other.Group.PoolIndex;
                                Log($"{c.name} => {other.name}[{count}] me={c.Group.PoolIndex}, other={otherIndex} merged into {c.Group.PoolIndex}");
                                Groups.Remove(otherIndex);
                                // 그룹 병합!
                                c.Group.Merge(other.Group);
                                Log($"  <color=yellow>GROUPS merged</color>: (removed {otherIndex})");
                                foreach (var (index, g) in Groups)
                                {
                                    Log($"  - {index}: {g.Children.JoinToString(", ", it => it.name)}");
                                }
                            }
                        }
                        // 상대는 솔로면
                        else
                        {
                            Log($"{c.name} => {other.name}[{count}] me={c.Group.PoolIndex}, other is solo, added other");
                            // 상대를 내 그룹에 추가
                            c.Group.Add(other);
                        }
                    }
                    // 내가 솔로면
                    else
                    {
                        // 상대가 이미 어떤 범위에 속한 경우: 거기에 들어감
                        if (other.Group != null)
                        {
                            Log($"{c.name} => {other.name}[{count}] me is solo, other={other.Group.PoolIndex}, added me");
                            other.Group.Add(c);
                        }
                        // 둘 다 솔로인 경우: 새로운 Group을 만듬
                        else
                        {
                            var group = CombinedGroup.Get();
                            group.Add(c);
                            group.Add(other);
                            Groups.Add(group.PoolIndex, group);
                            Log($"{c.name} => {other.name}[{count}] both are solo new group({group.PoolIndex})");
                            Log("  <color=lime>GROUPS added:</color>");
                            foreach (var (index, g) in Groups)
                            {
                                Log($"  - {index}: {g.Children.JoinToString(", ", it => it.name)}");
                            }
                        }
                    }
                }

                // 주변 돌았는데 나 밖에 없으면 나만 그림 ..
                if (count <= 0)
                {
                    Log($"{c.name} solo visible");
                    c.IsVisible = true;
                }
            }

            // 3. 그룹들 내 위치 및 방향 평균 구해서 병합된 UI 렌더링
            int combinedListIndex = 0;
            foreach (var (poolIndex, group) in Groups)
            {
                if(group.Children.Count <= 0) continue;
                Log($"<color=yellow>[{combinedListIndex}]</color> group({group.PoolIndex}) : [{group.Children.JoinToString(", ", it => it.name)}]");
                var mean = Vector2.zero;
                foreach (var c in group.Children)
                {
                    c.IsVisible = false;
                    mean += (Vector2) c.TargetUI.position;
                }
                mean *= (1f / group.Children.Count);

                var combined = CombinedUIList[combinedListIndex];
                combined.gameObject.SetActive(true);
                combined.MeanPosition = mean;
                combined.UpdatePosition();
                combined.SetEnemyCount(group.Children.Count);
                
                ++combinedListIndex;
            }

            for (int i = combinedListIndex; i < CombinedUIList.Count; i++)
            {
                CombinedUIList[i].gameObject.SetActive(false);
            }

#if UNITY_EDITOR
            UpdateCombinableSetPrinter();
#endif
        }
    }

    [Serializable]
    public class CombinedGroup
    {
        public int PoolIndex { get; }
        public CombinedGroup(int poolIndex)
        {
            PoolIndex = poolIndex;
        }
        
        /// <summary>
        /// 이 그룹에 단일 Combinable을 추가합니다.
        /// </summary>   
        public void Add(CombinableOffScreenUI combinable)
        {
            combinable.Group = this;
            Children.Add(combinable);   
        }
        
        /// <summary>
        /// 이 그룹에 other 그룹을 병합하고 other 그룹을 Release합니다.
        /// </summary>
        /// <param name="other">병합 대상입니다.</param>
        public void Merge(CombinedGroup other)
        {
            foreach (var c in other.Children)
            {
                c.Group = this;
            }
            Children.AddRange(other.Children);
            other.Release(false);
        }


        public List<CombinableOffScreenUI> Children = new();
            
        public void Release(bool removeReference = true)
        {
            if (removeReference) foreach (var c in Children)
                c.Group = null;
            
            Children.Clear();
            Pool.Release(this);
        }

        public static CombinedGroup Get()
        {
            return Pool.Get();
        }

        private const int PoolCapacity = 30;
        private static int PoolSize = 0;
        private static readonly ObjectPool<CombinedGroup> Pool = new ObjectPool<CombinedGroup>(
            () => new CombinedGroup(PoolSize++),
            it => it.Children.Clear(),
            it => { },
            it => { },
            true,
            PoolCapacity
        );

        static CombinedGroup()
        {
            var dummies = new CombinedGroup[PoolCapacity];
            for (int i = 0; i < PoolCapacity; i++)
            {
                dummies[i] = Pool.Get();
            }
            for (int i = 0; i < PoolCapacity; i++)
            {
                Pool.Release(dummies[i]);
            }
        }
    }
}