using System;
using Dummy.Scripts;
using Enemy.Behavior;
using EnumData;
using Managers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using Utility;

namespace Enemy.Spawner
{
    [ExecuteAlways]
    public class EnemyParabolaSpawner : EnemySpawner
    {
        [BoxGroup, LabelText("포물선 시작 위치")]
        public Transform ParabolaStartPosition;
        [BoxGroup, LabelText("포물선 도착 위치")]
        public Transform ParabolaEndPosition;
        [BoxGroup, LabelText("포물선 높이")]
        public float HighestFromLeap;
        
        [BoxGroup("x축 위치 커브"), LabelText("사용 여부")]
        public bool UseXPositionCurve = true;
        [BoxGroup("x축 위치 커브"), LabelText("커브"), EnableIf("UseXPositionCurve")]
        public AnimationCurve XPositionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        
        [BoxGroup, LabelText("포물선 결과")]
        public ParabolaByMaximumHeight Parabola;

        [SerializeField] private bool ShowDebug;
        
        public override Monster Spawn(EnemyType type, float delay)
        {
            GameObject monsterObject = EnemyPoolManager.Instance.Get(type);
            if (monsterObject.TryGetComponent(out NavMeshAgent agent))
            {
                agent.Warp(ParabolaStartPosition.position);
            }
            else
            {
                monsterObject.transform.position = ParabolaStartPosition.position;
            }

            Monster m = monsterObject.GetComponent<Monster>();
            m.InitializeParabola(Parabola, UseXPositionCurve ? XPositionCurve : null, delay).Forget();

            return m;
        }

        [Button("포물선 생성")]
        public void GenerateParabola()
        {
            var start = ParabolaStartPosition;
            var end = ParabolaEndPosition;
            if (!start)
            {
                // Debug.LogWarning("Parabola: Start 지점이 없습니다!");
                return;
            }
            if (!end)
            {
                // Debug.LogWarning("Parabola: End 지점이 없습니다!");
                return;
            }
            Parabola = new ParabolaByMaximumHeight(start.position, end.position, HighestFromLeap);
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
        }

#if UNITY_EDITOR
        private void UpdateEditor()
        {
            GenerateParabola();
            UpdateSimulateCurve();
        }

        [BoxGroup("x축 위치 커브"), Button("커브 움직임 시뮬레이션"), ShowIf("UseXPositionCurve")]
        private void SimulateCurve()
        {
            if (_simulator)
            {
                DestroyImmediate(_simulator);
            }

            _simulator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _simulator.name = $"{name}_Simulator";
            _simulateTime = Time.realtimeSinceStartup;
        }
        
        private GameObject _simulator;
        [field: SerializeField, BoxGroup("디버깅"), ReadOnly]
        private float _simulateTime;
        private void UpdateSimulateCurve()
        {
            if (!_simulator)
            {
                return;
            }
            var curve = XPositionCurve;
            var length = curve.GetLength();
            var t = Time.realtimeSinceStartup - _simulateTime;
            if (t > length)
            {
                DestroyImmediate(_simulator);
                _simulator = null;
                return;
            }

            var position = Parabola.GetPosition(curve.Evaluate(t));
            _simulator.transform.position = position;
        }

        [field: SerializeField, BoxGroup("디버깅")]
        private float _testFrames = 60f;
        [field: SerializeField, BoxGroup("디버깅")]
        private Color _startColor = Color.green;
        [field: SerializeField, BoxGroup("디버깅")]
        private Color _endColor = Color.cyan;
        [field: SerializeField, BoxGroup("디버깅")]
        private float _verticalLineAlpha = 0.75f;
        
        public override void DrawGizmosOnValid()
        {
            if (!ShowDebug)
                return;

#if UNITY_EDITOR
            if (_simulator && !Application.isPlaying)
            {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                UnityEditor.SceneView.RepaintAll();
            }
#endif
            
            var parabola = Parabola;
            if (!parabola.Valid)
            {
                return;
            }

            var startColor = _startColor;
            var endColor = _endColor;
            Vector3 v = parabola.Start;
            Vector3 endPosition;
            if (UseXPositionCurve)
            {
                var curve = XPositionCurve;
                var length = curve.GetLength();
                endPosition = parabola.GetPosition(1f);
                for (float t = 0f; t <= length; t += 1f/_testFrames)
                {
                    var f = curve.Evaluate(t / length);
                    var p = parabola.GetPosition(f);
                    Gizmos.color = Color.Lerp(startColor, endColor, t);
                    Gizmos.DrawLine(v, p);
                    Gizmos.color = Gizmos.color.Copy(a: _verticalLineAlpha);
                    Gizmos.DrawLine(p, p.Copy(y: endPosition.y));
                    v = p;
                }

            }
            else
            {
                endPosition = parabola.GetPosition(1f);
                for (float f = 0; f <= 1f; f += 0.05f)
                {
                    var p = parabola.GetPosition(f);
                    Gizmos.color = Color.Lerp(startColor, endColor, f);
                    Gizmos.DrawLine(v, p);
                    Gizmos.color = Gizmos.color.Copy(a: _verticalLineAlpha);
                    Gizmos.DrawLine(p, p.Copy(y: endPosition.y));
                    v = p;
                }
            }
            Gizmos.color = endColor;
            Gizmos.DrawLine(v, endPosition);

            var horizontalOne = new Vector3(1f, 0f, 1f);
            Gizmos.DrawLine(endPosition + horizontalOne, endPosition - horizontalOne);
            Gizmos.DrawLine(endPosition + horizontalOne.Copy(x: -1f), endPosition - horizontalOne.Copy(x: -1f));
        }
#endif
    }
}