using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Character.Presenter;
using Settings.Boss;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.AI;
using Utility;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace Enemy.Behavior.Boss
{
    [TaskDescription("원형 파동 패턴을 소환합니다. " +
                     "패턴 진행 시간동안 Running 상태로 대기합니다. " +
                     "패턴이 끝나면 Success를 반환합니다.")]
    public class BossCircularWaveAttack : Action
    {
        public SharedBossSettings Settings;
        public SharedGameObject Target;
        public SharedFloat RunningTime = 5f;
        public SharedFloat StartScale = 1f;
        public SharedFloat EndScale = 10f;
        public SharedFloat Width = 1f;

        public SharedMaterial DebugMaterialOuter;
        public SharedMaterial DebugMaterialInner;

        private NavMeshAgent _agent;
        private float _runningTime;
        private GameObject _outerSphere;
        private GameObject _innerSphere;
        private Vector3 _startScale;
        private Vector3 _endScale;
        public override void OnStart()
        {
            _agent = GetComponent<NavMeshAgent>();
            _runningTime = 0f;
            _startScale = StartScale.Value * Vector3.one;
            _endScale = EndScale.Value * Vector3.one;
            
            if (!_outerSphere)
            {
                _outerSphere = UnityEngine.GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var t = _outerSphere.transform;
                t.position = transform.position;

                if (DebugMaterialOuter.Value)
                {
                    var renderer = _outerSphere.GetComponent<MeshRenderer>();
                    var mats = new List<Material> { DebugMaterialOuter.Value };
                    renderer.SetMaterials(mats);
                }
                _outerSphere.GetComponent<Collider>().isTrigger = true;
                _outerSphere.OnTriggerEnterAsObservable()
                    .Subscribe(OnSphereTriggerEnter)
                    .AddTo(_outerSphere);

            }

            if (!_innerSphere)
            {
                _innerSphere = UnityEngine.GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var t = _innerSphere.transform;
                t.position = transform.position;

                if (DebugMaterialInner.Value)
                {
                    var renderer = _innerSphere.GetComponent<MeshRenderer>();
                    var mats = new List<Material> { DebugMaterialInner.Value };
                    renderer.SetMaterials(mats);
                }
                _innerSphere.GetComponent<Collider>().enabled = false;
            }

            var origin = transform.position;
            _outerSphere.transform.position = origin;
            _innerSphere.transform.position = origin;
            _outerSphere.SetActive(true);
            _innerSphere.SetActive(true);
            
        }

        private void OnSphereTriggerEnter(Collider c)
        {
            var origin = _outerSphere.transform.position;
            origin.y = 0f; 
            var target = c.transform.position;
            target.y = 0f;
            var currentScale = Mathf.Lerp(StartScale.Value, EndScale.Value, _runningTime / RunningTime.Value);
            if (Vector3.Distance(origin, target) <= 0.5 * currentScale - Width.Value)
            {
                // DebugX.Log($"원형파동 {c.name} 피격 무시 - width 벗어남");
                return;
            }
            if (c.CompareTag("Player") && c.TryGetComponent(out PlayerPresenter player))
            {
                DebugX.Log("플레이어 피격 by 원형 파동");
                player.Damage(10f, Owner.gameObject); //임시처리
                //player.Damage(10f, gameObject);
                player.View.Push((c.transform.position - _outerSphere.transform.position).normalized * 10f, ForceMode.Impulse, 0.5f);
            }
        }

        public override TaskStatus OnUpdate()
        {
            if (_runningTime > RunningTime.Value)
            {
                return TaskStatus.Success;
            }
            _runningTime += Time.deltaTime;
            if (Target.Value)
            {
                _agent?.LookTowards(Target.Value.transform.position);
            }

            var outer = _outerSphere.transform;
            outer.localScale = Vector3.Lerp(_startScale, _endScale, _runningTime / RunningTime.Value);
            var outerScale = outer.localScale.x;
            var origin = outer.position;

            var inner = _innerSphere.transform;
            inner.localScale = Vector3.one * (outerScale - Width.Value * 2f);
            DrawUtility.DrawCircle(origin, outerScale * 0.5f, Vector3.up, 32, (a, b) => DebugX.DrawLine(a, b, Color.green));
            DrawUtility.DrawCircle(origin, outerScale * 0.5f - Width.Value, Vector3.up, 32, (a, b) => DebugX.DrawLine(a, b, Color.green));
            return TaskStatus.Running;

        }

        public override void OnEnd()
        {
            _outerSphere.SetActive(false);
            _innerSphere.SetActive(false);
        }
    }
}