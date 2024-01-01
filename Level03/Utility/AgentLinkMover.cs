using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Utility
{
    /// <summary>
    /// https://github.com/Unity-Technologies/NavMeshComponents/blob/master/Assets/Examples/Scripts/AgentLinkMover.cs
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class AgentLinkMover : MonoBehaviour
    {
        private NavMeshAgent _agent;
        
        public AnimationCurve Curve = new AnimationCurve(
            new Keyframe(0f, 0f), 
            new Keyframe(0.5f, 1f), 
            new Keyframe(1f, 0f)
        );

        public float Duration = 0.5f;
        public float Cooldown = 1f;

        public UnityEvent OnStart;
        public UnityEvent OnEnd;

        private float _currentCooldown = 0f;
        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.autoTraverseOffMeshLink = false;
        }
        private void OnEnable()
        {
            Task().Forget();
        }

        private void Update()
        {
            if (_currentCooldown > 0f)
            {
                _currentCooldown -= Time.deltaTime;
            }
        }

        private async UniTaskVoid Task()
        {
            while (true)
            {
                await UniTask.Yield();
                if (!this || !gameObject || !gameObject.activeInHierarchy || !enabled || !_agent)
                {   
                    return;
                }

                if (_agent.isOnOffMeshLink && _currentCooldown <= 0f)
                {
                    await CurveMove();
                    if(_agent.enabled && !_agent.isStopped)
                        _agent.CompleteOffMeshLink();
                    _currentCooldown = Cooldown;
                }
            }
        }

        private async UniTask CurveMove()
        {
            var oldUpdateRotation = _agent.updateRotation;
            _agent.updateRotation = false;
            var data = _agent.currentOffMeshLinkData;
            var startPosition = _agent.transform.position;
            var endPosition = data.endPos + Vector3.up * _agent.baseOffset;
            var direction = (endPosition - startPosition).Copy(y: 0f).normalized;
            transform.forward = direction;
            OnStart?.Invoke();
            var normalizedTime = 0f;
            while (normalizedTime < 1f)
            {
                if (!_agent.enabled || _agent.isStopped)
                {
                    if(_agent.enabled)
                        _agent.updateRotation = oldUpdateRotation;
                    OnEnd?.Invoke();
                    return;
                }
                var yOffset = Curve.Evaluate(normalizedTime);
                _agent.transform.position =
                    Vector3.Lerp(startPosition, endPosition, normalizedTime) + yOffset * Vector3.up;
                normalizedTime += Time.deltaTime / Duration;
                await UniTask.Yield();
            }
            _agent.updateRotation = oldUpdateRotation;
            OnEnd?.Invoke();
        }
    }
}