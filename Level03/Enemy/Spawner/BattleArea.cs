using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using Character.Presenter;
using Enemy.Behavior;
using Enemy.Spawner;
using Managers;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Enemy
{
    public class BattleArea : MonoBehaviour
    {
        /// <summary>
        /// BattleArea의 Key입니다. 전체 레벨에서 유일합니다.
        /// </summary>
        public string Key => name;

        public enum BattleAreaEndType
        {
            KillAllSpawnedMonsters,
            Custom,
        }

        public BattleAreaEndType EndType = BattleAreaEndType.KillAllSpawnedMonsters;
        public List<EnemyAreaSpawner> Spawners;


#if UNITY_EDITOR
        [Button("자식 오브젝트 Spawner 자동 연결")]
        private void AutoBindSpawners()
        {
            Spawners.Clear();

            foreach (var spawner in GetComponentsInChildren<EnemyAreaSpawner>())
            {
                Spawners.Add(spawner);
            }
        }

        [Button("씬에 배치된(사전 스폰) 몬스터 연결", DisplayParameters = true, Expanded = true)]
        private void AutoBindEnemiesInHierarchy(bool forced = false)
        {
            var monsters = FindObjectsOfType<Monster>();
            foreach (var monster in monsters)
            {
                // 이미 할당된 전투구역 있으면 무시
                if (!forced && monster.TargetBattleArea != null)
                {
                    return;
                }

                // 전투 구역 안에 있는 친구만
                if (!Contains(monster.transform.position, false))
                {
                    return;
                }

                DebugX.Log($"<color=yellow>{monster.name}</color>가 전투 구역 <color=cyan>{name}</color>에 배정됨");
                monster.TargetBattleArea = this;
                EditorUtility.SetDirty(monster);
            }
        }
        
#endif

        private PlayerPresenter _player;

        // 플레이어가 이 전투 구역에 방문 여부
        [ReadOnly, ShowInInspector] private bool _hasPlayerVisited = false;

        public bool HasPlayerVisited => _hasPlayerVisited;

        // 방문 이후 몇 초 흘렀는지
        [ReadOnly, ShowInInspector] private int _timeAfterPlayerVisited = -1;

        [field: Header("이벤트")]
        [field: SerializeField] public UnityEvent OnBattleAreaStart { get; private set; }
        [field: SerializeField] public UnityEvent OnBattleAreaEnd { get; private set; }
        [field: SerializeField] public UnityEvent OnClearedBattleAreaLoad { get; private set; }

        private int _registeredMonsterCount = 0;
        public void RegisterMonsterCount(int count)
        {
            _registeredMonsterCount += count;
        }

        private int _killedMonsterCount = 0;
        private HashSet<Monster> _spawnedMonsters = new();
        public void RegisterSpawnedMonster(Monster monster)
        {
            _spawnedMonsters.Add(monster);
            monster.OnDeadEvent.AddListener(OnSpawnerMonsterDead);
        }

        private void OnSpawnerMonsterDead(Monster monster)
        {
            monster.OnDeadEvent.RemoveListener(OnSpawnerMonsterDead);
            _spawnedMonsters.Remove(monster);
            _killedMonsterCount += 1;

            // 지정된 수만큼 다 죽었으면
            if (EndType == BattleAreaEndType.KillAllSpawnedMonsters && _killedMonsterCount >= _registeredMonsterCount)
            {
                EndBattleArea();
            }
        }

        public void EndBattleArea()
        {
            GameManager.Instance.CurrentCheckPointStorage.AddBattleArea(Key);
            OnBattleAreaEnd?.Invoke();
            DebugX.Log($"Cleared BattleArea {Key}: {GameManager.Instance.CurrentCheckPointStorage}");
            _player.Model.CurrentBattleArea = null;
        }

        private void OnPlayerBattleAreaEnter(Collider _)
        {
            if (_hasPlayerVisited)
            {
                return;
            }

            DebugX.Log($"BattleArea {Key} start : kill {_registeredMonsterCount} monsters");
            _hasPlayerVisited = true;
            OnBattleAreaStart?.Invoke();
            _player.Model.CurrentBattleArea = this;
        }

        // 모든 BattleArea Collider들의 무게중심좌표
        public Vector3 CenterPoint { get; private set; }
        public Vector3 CenterPointOnNavMesh { get; private set; }

        public float TestTime = 1f;
        
        private void Start()
        {
            _player = GameManager.Instance.Player;

            CenterPoint = Vector3.zero;

            // 이미 꺤 구역이면
            var isCleared = GameManager.Instance.CurrentCheckPointStorage.ContainsBattleArea(this);
            if (isCleared)
            {
                DebugX.Log($"BattleArea Loaded But {Key} has Cleared");
                // 이벤트 실행
                OnClearedBattleAreaLoad?.Invoke();
            }
            
            int colliderCount = 0;
            
            foreach (var spawner in Spawners)
            {
                spawner.Initialize(this, _player);
                
                // 자식 Collider들에 접근하면 visited = true
                if (spawner.IsBattleArea)
                {
                    if(!isCleared)
                        spawner.OnTriggerEnterAsObservable()
                        .Where(it => it.gameObject.GetInstanceID() == _player.gameObject.GetInstanceID())
                        .Subscribe(OnPlayerBattleAreaEnter)
                        .AddTo(spawner);

                    foreach (var c in spawner.Colliders)
                    {
                        var center = c.bounds.center;
                        CenterPoint += center;
                        ++colliderCount;
                    }
                }
            }

            if (colliderCount > 0)
            {
                CenterPoint /= colliderCount;
                CenterPointOnNavMesh = CenterPoint;
            }

            // 방문 이후 1초씩 누적, 스크립트 실행
            if(!isCleared)
                Observable.Interval(TimeSpan.FromSeconds(TestTime))
                    .Where(_ => _hasPlayerVisited)
                    .Subscribe(_ =>
                    {
                        ++_timeAfterPlayerVisited;
                        foreach (var spawner in Spawners)
                        {
                            spawner.OnTimerUpdate(_timeAfterPlayerVisited);
                        }
                    }).AddTo(this);
        }


        public bool Contains(Vector3 from, bool onlyBattleArea = true)
        {
            foreach (var spawner in Spawners)
            {
                // Battle Area 한정
                if (onlyBattleArea && !spawner.IsBattleArea)
                {
                    continue;
                }

                foreach (var c in spawner.Colliders)
                {
                    var to = c.ClosestPoint(from);
                    var between = from - to;
                    var distanceSquared = between.sqrMagnitude;
                    if (distanceSquared <= Vector3.kEpsilon)
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        /// <summary>
        /// 자식 Collider들 중에서 가장 가까운 점을 구합니다.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="onlyBattleArea"></param>
        /// <returns></returns>
        public Vector3 ClosestPoint(Vector3 from, bool onlyBattleArea = true)
        {
            var shortestSquared = float.MaxValue;
            var closest = new Vector3(float.NaN, float.NaN, float.NaN);
            foreach (var spawner in Spawners)
            {
                // Battle Area 한정
                if (onlyBattleArea && !spawner.IsBattleArea)
                {
                    continue;
                }

                foreach (var c in spawner.Colliders)
                {
                    var to = c.ClosestPoint(from);
                    var between = from - to;
                    var distanceSquared = between.sqrMagnitude;
                    if (shortestSquared > distanceSquared)
                    {
                        shortestSquared = distanceSquared;
                        closest = to;
                    }
                }
            }

            return closest;
        }
    }

    public class SharedBattleArea : SharedVariable<BattleArea>
    {
        public static implicit operator SharedBattleArea(BattleArea value)
        {
            return new SharedBattleArea { mValue = value };
        }
    }
}