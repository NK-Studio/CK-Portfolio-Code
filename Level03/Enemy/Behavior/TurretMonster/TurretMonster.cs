using System;
using Settings;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Serialization;
using Utility;

namespace Enemy.Behavior.TurretMonster
{
    public class TurretMonster : Monster
    {
        private TurretSettings _settings;
        public new TurretSettings Settings => _settings ??= (TurretSettings)base.Settings;
        
        [field: SerializeField, FoldoutGroup("공격/설치형 몬스터", true), Tooltip("발사 위치입니다.")]
        public Transform ShootPosition { get; set; }
        
        #region SharedProperty

        public float ProjectileFlyTime => Settings.ProjectileFlyTime;
        public GameObject ProjectilePrefab => Settings.ProjectilePrefab;

        #endregion

        protected override void Start()
        {
            base.Start();

            AttackStartRangeRay.WorldSpace = true;
            this.UpdateAsObservable()
                // 플레이어가 사거리 안에 있다면?
                // Ray가 플레이어를 따라감
                .Subscribe(_ =>
                {
                    if (!PlayerView) return;
                    var t = transform;
                    var forward = t.forward;
                    forward.y = 0f; forward.Normalize();
                    
                    var playerCenterPoint = PlayerView.CenterPoint.position;
                    var toPlayerHorizontal = playerCenterPoint - t.position;
                    toPlayerHorizontal.y = 0f;
                    var distanceSquared = toPlayerHorizontal.sqrMagnitude;
                    if (distanceSquared > Settings.AttackStartRangeSquared)
                    {
                        return;
                    }
                    var direction = toPlayerHorizontal.normalized;

                    if (Vector3.Dot(forward, direction) < Settings.AttackStartRangeSightHalfAngleInCos)
                    {
                        return;
                    }
                    AttackStartRangeRay.Direction = (playerCenterPoint - AttackStartRangeRay.transform.position).normalized;
                }).AddTo(this);
        }


        protected override void OnDrawGizmos()
        {
            // base.OnDrawGizmos();
            var settings = Settings;
            if(!settings) return;

            var t = transform;
            var range = settings.AttackStartRange;
            var angle = settings.AttackStartRangeSightAngle;

            var rotator = Quaternion.AngleAxis(angle * 0.5f, Vector3.up);

            var origin = ShootPosition?.position ?? t.position;
            var forward = t.forward;
            var left = rotator * forward;
            var right = Quaternion.Inverse(rotator) * forward;
            
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(origin, origin + left * range);
            Gizmos.DrawLine(origin, origin + right * range);

            const int segment = 10;
            rotator = Quaternion.AngleAxis(angle / segment, Vector3.up);
            var drawer = right * range;
            for (int i = 0; i < segment; i++)
            {
                var start = drawer;
                var end = rotator * drawer;
                Gizmos.DrawLine(origin + start, origin + end);
                drawer = end;
            }
        }

    }
}