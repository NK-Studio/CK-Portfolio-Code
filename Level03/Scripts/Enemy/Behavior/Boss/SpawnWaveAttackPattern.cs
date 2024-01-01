using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Character.Presenter;
using Cysharp.Threading.Tasks;
using EnumData;
using Managers;
using ManagerX;
using Settings.Boss;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.AI;
using Utility;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace Enemy.Behavior.Boss
{
    [TaskDescription("Wave Attack 패턴을 소환합니다. " +
                     "패턴 진행 시간동안 Running 상태로 대기합니다. " +
                     "패턴이 끝나면 Success를 반환합니다.")]
    public class SpawnWaveAttackPattern : Action
    {
        public SharedBossSettings Settings;
        public SharedGameObject Target;
        public SharedBool UseRunningWait = false;
        public SharedBool RunningEndFlag;

        private List<BossYorugamiSettings.WaveAttackPatternData> WavePatterns => Settings.Value.WaveAttackSpawnSequence;
        private static Vector3[] StandardBasis => BossYorugami.StandardBasis;

        private NavMeshAgent _agent;
        private float _runningTime;
        public override void OnStart()
        {
            _agent = GetComponent<NavMeshAgent>();
            var target = Target.Value;

            var stackedTime = 0f;
            var longest = 0f;
            foreach (var pillar in Settings.Value.WaveAttackSpawnSequence)
            {
                stackedTime += pillar.WaitTime;
                if (longest < stackedTime + pillar.MoveTime)
                {
                    longest = stackedTime + pillar.MoveTime;
                }
            }
            _runningTime = longest;
            RunningEndFlag.Value = true;
            
            // 큐브 생성 안 됐으면 생성
            if (_cubes.Count <= 0)
            {
                CreateCube(target);
            }
            // 이미 있으면 리셋
            else
            {
                // Debug.Log("ResetCubePositions()");
                ResetCubePositions();
            }
            // Debug.Log($"PlayPattern({target})");
            PlayPattern(target).Forget();
        }

        public override TaskStatus OnUpdate()
        {
            if (UseRunningWait.Value && _runningTime > 0f)
            {
                _runningTime -= Time.deltaTime;
                _agent?.LookTowards(Target.Value.transform.position);
                RunningEndFlag.Value = false;
                return TaskStatus.Running;
            }

            // Running을 사용하지 않는 경우라도 RunningEndFlag는 설정하도록
            if (!UseRunningWait.Value)
            {
                RunningFlagSequence().Forget();
            }
            return TaskStatus.Success;
        }

        private async UniTaskVoid RunningFlagSequence()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_runningTime));
            RunningEndFlag.Value = false;
        }

        private readonly List<GameObject> _cubes = new();
        private void ClearCube()
        {
            _cubes.ForEach(it => GameObject.DestroyImmediate(it?.gameObject));
            _cubes.Clear();
        }

        private List<Vector3> _navMeshCheckerCache = new(6);
        
        private void CreateCube(GameObject target)
        {
            ClearCube();
        
            var origin = target.transform.position;
        
            var index = 0;
            foreach (var pillar in WavePatterns)
            {
            
                foreach (var standardBasis in StandardBasis)
                {
                    var basisIndex = 0;
                
                    var forward = pillar.Rotation * standardBasis;
                    var position = forward * pillar.Distance;
                    var right = Vector3.Cross(Vector3.up, forward);
                    right.Normalize();

                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    var t = cube.transform;
                    cube.name = $"{index}::{basisIndex}";
                
                    var collider = cube.GetComponent<BoxCollider>();
                    collider.isTrigger = true;

                    var renderer = cube.GetComponent<MeshRenderer>();
                    renderer.enabled = false;
                
                    // t.SetParent(transform, true);
                    t.position = position; // + Vector3.up * pillar.HalfExtents.y;
                    t.forward = -forward;
                    t.localScale = pillar.HalfExtents * 2;

                    cube.SetActive(false);
                    // Debug.Log($"cube {cube.name} disabled by CreateCube");

                    cube.OnTriggerEnterAsObservable()
                        .Subscribe(OnCubeTrigger)
                        .AddTo(cube.GetCancellationTokenOnDestroy());
                    
                    _cubes.Add(cube);
                }

                ++index;
            }
        }

        private void OnCubeTrigger(Collider collider)
        {
            if (collider.TryGetComponent(out PlayerPresenter player))
            {
                // DebugX.Log("플레이어 피격 by Four Pillars");
                player.Damage(Settings.Value.WaveAttackDamage, collider.gameObject, DamageReaction.Stun);
            }
        }

        private void ResetCubePositions()
        {
            int index = 0;
            foreach (var pillar in WavePatterns)
            {

                int basisIndex = 0;
                foreach (var standardBasis in StandardBasis)
                {
                    var cube = _cubes[index * 4 + basisIndex];
                    if(!cube) continue;
                
                    var t = cube.transform;
                    var forward = pillar.Rotation * standardBasis;
                    var position = forward * pillar.Distance;

                    t.position = position; // + Vector3.up * pillar.HalfExtents.y;
                    t.forward = -forward;
                    t.localScale = pillar.HalfExtents * 2;
                
                    cube.SetActive(false);
                    // Debug.Log($"cube {cube.name} disabled by ResetCubePositions");
                
                    ++basisIndex;
                }

                ++index;
            }
        }

        private async UniTaskVoid PlayPattern(GameObject target)
        {
            // Debug.Log($"PlayPattern({target}) start");
            var index = 0;
            foreach (var pillar in WavePatterns)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(pillar.WaitTime));

                MoveCubes(target, index, pillar.MoveTime).Forget();
            
                ++index;
            }
        }

        private async UniTaskVoid MoveCubes(GameObject target, int index, float moveTime)
        {
            Settings.Value.Sounds.TryGetValue("WaveAttack", out var sound);
            AutoManager.Get<AudioManager>().PlayOneShot(sound, "BossWaveAttackPhase", index);
            var wavePattern = WavePatterns[index];
            var start = index * 4;
            var end = (index + 1) * 4;

            var origin = target.transform.position;
            for (int i = start; i < end; i++)
            {
                var cube = _cubes[i];
                var cubeTransform = cube.transform;
                cubeTransform.position += origin;
                var forward = wavePattern.Rotation * StandardBasis[i % 4];
                var position = cubeTransform.position; // + Vector3.down * wavePattern.HalfExtents.y;
                var right = Vector3.Cross(Vector3.up, forward);

                // NavMesh 체크
                _navMeshCheckerCache.Clear();
                var axisX = right * wavePattern.HalfExtents.x;
                var axisZ = forward * wavePattern.HalfExtents.z;

                var innerRight = position + axisZ + axisX;
                var innerLeft  = position + axisZ - axisX;
                var outerRight = position - axisZ + axisX;
                var outerLeft  = position - axisZ - axisX;
                
                _navMeshCheckerCache.Add(innerLeft);
                _navMeshCheckerCache.Add(innerRight);
                _navMeshCheckerCache.Add(outerLeft);
                _navMeshCheckerCache.Add(outerRight);

                var length = wavePattern.HalfExtents.x * 2f / (wavePattern.AdditionalNavMeshCheckerCount + 1);
                for (int k = 0; k < wavePattern.AdditionalNavMeshCheckerCount; k++)
                {
                    var innerAdditionalChecker = innerLeft + right * (length * (k + 1));
                    var outerAdditionalChecker = outerLeft + right * (length * (k + 1));

                    _navMeshCheckerCache.Add(innerAdditionalChecker);
                    _navMeshCheckerCache.Add(outerAdditionalChecker);
                }

                var isInNavMesh = false;
                foreach (var checker in _navMeshCheckerCache)
                {
                    if (!NavMesh.SamplePosition(checker, out _, wavePattern.AdditionalNavMeshCheckerRadius,
                            NavMesh.AllAreas))
                    {
                        DrawUtility.DrawWireSphere(checker, wavePattern.AdditionalNavMeshCheckerRadius, 16, 
                            (a, b) => DebugX.DrawLine(a, b, Color.red, 5f)
                        );
                        continue;
                    }

                    DrawUtility.DrawWireSphere(checker, wavePattern.AdditionalNavMeshCheckerRadius, 16, 
                        (a, b) => DebugX.DrawLine(a, b, Color.green, 5f)
                    );
                    isInNavMesh = true;
                    break;
                }

                if (!isInNavMesh)
                {
                    continue;
                }
            
            
                cube.SetActive(true);
                // Debug.Log($"cube {cube.name} enabled by MoveCubes");

                // 이펙트 부착
                var waveObject = EffectManager.Instance.Get(EffectType.BossWaveAttack);
                var wave = waveObject.GetComponent<WaveAttackEffect>();
                wave.SetWidth(wavePattern.HalfExtents.x * 2f);
                wave.SetFollowTarget(cube);
                
                waveObject.transform.SetPositionAndRotation(
                    cubeTransform.position,
                    cubeTransform.rotation
                );
            }
        
            float leftTime = moveTime;
            float deltaTime = 0f;

            var speed = wavePattern.Distance / moveTime;
        
            while (leftTime >= 0f)
            {
                deltaTime = Time.deltaTime;
                leftTime -= deltaTime;
            
                for (int i = start; i < end; i++)
                {
                    var cube = _cubes[i];
                    if(!cube || !cube.activeInHierarchy) continue;
                    var t = cube.transform;

                    t.position += t.forward * (deltaTime * speed);
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        
            for (int i = start; i < end; i++)
            {
                var cube = _cubes[i];
                if(!cube) continue;
                cube.SetActive(false);
                // Debug.Log($"cube {cube.name} disabled by MoveCubes End");
            }
        }
        
        
    }
}