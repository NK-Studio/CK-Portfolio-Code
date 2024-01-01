using System;
using Micosmo.SensorToolkit;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Tutorial
{
    public class TutorialRangeTrigger : TutorialBase
    {
        public RangeSensor TargetRange;

        [SerializeField, ReadOnly]
        private bool _entered;
        private void Awake()
        {
            if (!TargetRange)
            {
                TargetRange = GetComponent<RangeSensor>();
            }

            if (!TargetRange)
            {
                Debug.LogWarning($"{name} skipped: No RangeSensor", gameObject);
                return;
            }
            TargetRange.OnDetected.AddListener((_, _) =>
            {
                _entered = true;
            });
            TargetRange.enabled = false;
        }

        public override void Enter()
        {
            _entered = false;
            TargetRange.enabled = true;
        }

        public override Result Execute()
        {
            if (!TargetRange)
            {
                Debug.LogWarning($"{name} skipped: No RangeSensor", gameObject);
                return Result.Done;
            }

            return _entered ? Result.Done : Result.Running;
        }

        public override void Exit()
        {
            TargetRange.enabled = false;
        }
    }
}