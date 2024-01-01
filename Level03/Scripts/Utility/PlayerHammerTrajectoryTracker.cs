using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace Utility
{
    public class PlayerHammerTrajectoryTracker : MonoBehaviour
    {
        public Animator Animator;
        public Transform OriginTransform;
        public Transform Target;
        
        private ObservableStateMachineTrigger _observableStateMachineTrigger;

        public AnimationCurve ResultCurve;

        private bool _track;
        private float _time;
        private void Start()
        {
            _observableStateMachineTrigger =
                Animator.GetBehaviour<ObservableStateMachineTrigger>();

            _time = 0f;
            _track = false;
            
            _observableStateMachineTrigger.OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("HammerUse"))
                .Subscribe(_ =>
                {
                    Time.timeScale = 0.25f;
                    _track = true;
                    _time = 0f;
                })
                .AddTo(this);

            _observableStateMachineTrigger.OnStateExitAsObservable()
                .Where(info => info.StateInfo.IsName("HammerUse"))
                .Subscribe(_ =>
                {
                    Time.timeScale = 1f;
                    _track = false;
                }).AddTo(this);
        }

        public Vector3 Axis = Vector3.down;
        private void Update()
        {
            if(!_track) return;

            var t = OriginTransform;
            var origin = t.position;
            var right = t.right;
            var target = Target.position;

            var direction = (target - origin).Copy(y: 0f).normalized;
            var angle = Vector3.SignedAngle(right, direction, Axis);
            if (angle >= -90f && angle < 0f)
            {
                // angle = 0f;
            }else if (angle >= -180f && angle < -90f)
            {
                angle += 180f;
            }
            ResultCurve.AddKey(_time, angle / 180f);
            Debug.Log($"record ({_time}, {angle})");
            _time += Time.deltaTime;
            
            Debug.DrawLine(origin, target.Copy(y: origin.y), angle > 0f ? Color.white : Color.red, 10f);
        }
    }
}