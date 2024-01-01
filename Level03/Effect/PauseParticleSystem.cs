using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Effect
{
    [ExecuteAlways]
    public class PauseParticleSystem : MonoBehaviour
    {
        public enum PauseMode
        {
            [InspectorName("지속 시간 기반")]
            ByDuration,
            [InspectorName("수동 (스크립트 호출)")]
            ByManually,
        }

        [field: SerializeField, LabelText("대상 Particle System")]
        [field: InfoBox("Root 파티클은 Stop Action: Disable이어야 합니다.")]
        public List<ParticleSystem> ParticleSystems { get; private set; } = new();

        [LabelText("대기 시간")]
        public float PauseAfter = 1f;
        [LabelText("일시정지 방식")]
        public PauseMode Mode = PauseMode.ByDuration;
        [LabelText("지속 시간"), EnableIf("@Mode == PauseMode.ByDuration")]
        public float PauseDuration = 1f;
        [LabelText("에디터에서 동작")]
        public bool WorksInEditorMode = true;

        [field: SerializeField, ReadOnly]
        private bool _isAlive = false;

        private void OnEnable()
        {
            _isAlive = false;
        }

        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (!WorksInEditorMode && !Application.IsPlaying(gameObject))
            {
                return;
            }
#endif
            // 활성화 체크
            if (!_isAlive)
            {
                foreach (var ps in ParticleSystems)
                {
                    if (ps.IsAlive())
                    {
                        _isAlive = true;
                        PauseSequence().Forget();
                        return;
                    }
                }
            }
            else
            {
                // 하나라도 살아있으면 alive, 다 죽으면 isAlive = false
                foreach (var ps in ParticleSystems)
                {
                    if (ps.IsAlive())
                    {
                        return;
                    }
                }
                _isAlive = false;
            }
        }

        private void OnDisable()
        {
            _isAlive = false;
        }

        private async UniTaskVoid PauseSequence()
        {
            await UniTask.WaitUntil(() => ParticleSystems[0].time > PauseAfter, cancellationToken: destroyCancellationToken);
            if (!gameObject.activeInHierarchy) return;
            foreach (var ps in ParticleSystems)
            {
                ps.Pause();
            }

            // 수동 실행일 경우 중단
            if (Mode == PauseMode.ByManually) return;

            await UniTask.Delay(TimeSpan.FromSeconds(PauseDuration));
            Resume();
        }

        [Button("(디버그) 일시정지 해제"), EnableIf("@Mode == PauseMode.ByManually")]
        public void Resume()
        {
            if (!gameObject.activeInHierarchy) return;
            foreach (var ps in ParticleSystems)
            {
                ps.Play();
            }
        }
    }
}
