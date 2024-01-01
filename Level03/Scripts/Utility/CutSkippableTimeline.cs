using System;
using System.Collections.Generic;
using Managers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace Utility
{
    public class CutSkippableTimeline : MonoBehaviour
    {
        // 특정 조건에 따라 스킵 가능해진 타임라인 Set (타임라인 에셋 이름 기준)
        private static readonly HashSet<string> SkippableTimelines = new();
        
        public string TimelineName => Timeline.playableAsset.name;
        // 다른 트리거에서 이 함수 호출하면 앞으로 스킵 가능한 타임라인이 됨
        public void MakeSkippable()
        {
            SkippableTimelines.Add(TimelineName);
        }
        
        public PlayableDirector Timeline;
        public double[] SkipTimings;
        [field: SerializeField]
        public bool CanSkip { get; set; } = true;

        private void Start()
        {
            // 스킵 가능하도록 변경해주는 부분
            if (!CanSkip && SkippableTimelines.Contains(TimelineName))
            {
                CanSkip = true;
            }
            InputManager.Instance.Controller.System.Skip.performed += Run;
        }

        private void OnDestroy()
        {
            InputManager.Instance.Controller.System.Skip.performed -= Run;
        }

        private void Run(InputAction.CallbackContext ctx)
        {
            Skip();
        }

        private bool Skip()
        {
            if (!CanSkip)
            {
                return false;
            }
            var time = Timeline.time;
            // 스킵이 없거나 마지막 스킵 가능한 타이밍 넘었으면 스킵 불가
            if (SkipTimings.Length <= 0 || SkipTimings[^1] <= time)
            {
                return false;
            }

            for (int i = 0; i < SkipTimings.Length; i++)
            {
                if(time > SkipTimings[i])
                {
                    continue;
                }

                Timeline.time = SkipTimings[i];
                return true;
            }

            return false;
        }
    }
}