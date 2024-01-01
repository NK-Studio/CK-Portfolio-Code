using System;
using System.Collections.Generic;
using Character.Behaviour.State;
using Damage;
using Effect;
using Enemy.Behavior;
using EnumData;
using Settings.Player;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Utility;
using Random = UnityEngine.Random;

namespace Character.Core.Weapon
{
    public class PlayerMissileBullet : PlayerBullet
    {
        private PlayerMissileBulletSettings _missileBulletSettings = null;
        public PlayerMissileBulletSettings MissileSettings => _missileBulletSettings ??= Settings as PlayerMissileBulletSettings;

        [SerializeField, ReadOnly] private float _time;
        [SerializeField, ReadOnly] private bool _isGuide;
        [SerializeField, ReadOnly] private Transform _target;

        private Vector3 _targetPosition;
        public override void SetPositionAndRotation(PlayerShootState state, Vector3 position, Quaternion rotation)
        {
            // 부채꼴 내 랜덤 방향으로 설정
            var halfAngle = MissileSettings.CompensationAngle * 0.5f;
            var randomizedRotation = Quaternion.AngleAxis(Random.Range(-halfAngle, halfAngle), Vector3.up) * rotation;
            
            // 마우스 투영 실패 시 그냥 직선 발사  . .. 
            if (!state.Behaviour.MouseRaycastGround(state.View, out var hitPosition))
            {
                Debug.LogWarning("failed MouseRaycastGround", gameObject);
                _targetPosition = position + rotation * (Vector3.forward * 10f);
                base.SetPositionAndRotation(state, position, rotation);
                return;
            }

            _targetPosition = hitPosition.Copy(y: position.y);
            base.SetPositionAndRotation(state, position, randomizedRotation);
        }

        public override void Initialize(PlayerBulletSettings settings, Vector3 position, float maxDistance)
        {
            base.Initialize(settings, position, maxDistance);
            _time = 0f;
            _isGuide = false;
            _target = null;

            if (MissileSettings.GuideStartDelay <= 0)
            {
                UpdateTargetPosition();
            }
        }

        private void UpdateTargetPosition()
        {
            _isGuide = true;
            // 범위 내 몬스터 찾기
            var count = Physics.OverlapSphereNonAlloc(
                _targetPosition, 
                MissileSettings.GuideRangeFromTargetPosition, 
                _colliders, 
                EnemyMask
            );

            if (count <= 0)
            {
                return;
            }

            Collider nearest = _colliders[0];
            Vector3 nearestPosition = nearest.transform.position;
            float nearestDistanceSquared = nearestPosition.DistanceSquared(_targetPosition);
            for (int i = 1; i < count; i++)
            {
                var c = _colliders[i];
                if (!c.CompareTag("Enemy"))
                {
                    continue;
                }

                var p = c.transform.position;
                var distanceSquared = p.DistanceSquared(_targetPosition);
                if (distanceSquared < nearestDistanceSquared)
                {
                    nearestDistanceSquared = distanceSquared;
                    nearestPosition = p;
                    nearest = c;
                }
            }

            _target = nearest.transform;
            _targetPosition = nearestPosition.Copy(y: _targetPosition.y);
        }
        
        [FormerlySerializedAs("MouseGuideMask")] public LayerMask EnemyMask;
        private Collider[] _colliders = new Collider[16];
        protected override void FixedUpdate()
        {
            var t = transform;
            var dt = Time.deltaTime;
            var delta = Settings.Speed * dt;
            
            _time += dt;
            var rigidbodyPosition = Rigidbody.position;
            
            // 일정 시간동안은 비유도 직선 발사
            bool isBeforeGuideStartDelay = _time - dt < MissileSettings.GuideStartDelay;
            if (!_isGuide || isBeforeGuideStartDelay)
            {
                Rigidbody.MovePosition(rigidbodyPosition + transform.forward * delta);

                if (isBeforeGuideStartDelay && _time >= MissileSettings.GuideStartDelay)
                {
                    UpdateTargetPosition();
                }
                return;
            }

            // 실시간 추적 목표가 있으면 목표 지점 갱신
            if (_target)
            {
                _targetPosition = _target.position.Copy(y: _targetPosition.y);

            }
            
            // 일정 거리 이내 도달하면 유도 중단 
            if (rigidbodyPosition.Distance(_targetPosition.Copy(y: rigidbodyPosition.y)) <= MissileSettings.GuideDisableRangeFromTarget)
            {
                _target = null;
                _isGuide = false;
            }

            // 목표 지점으로 유도
            var direction = (_targetPosition - rigidbodyPosition).Copy(y: 0f).normalized;
            var oldRotation = Rigidbody.rotation;
            var newRotation = Quaternion.LookRotation(direction, Vector3.up);
            switch (MissileSettings.GuideRotationType)
            {
                case PlayerMissileBulletSettings.GuideRotationMethod.Linear:
                {
                    Rigidbody.MoveRotation(Quaternion.RotateTowards(
                        oldRotation, 
                        newRotation,
                        MissileSettings.GuideRotationSpeed * dt
                    ));
                    break;
                }
                case PlayerMissileBulletSettings.GuideRotationMethod.Slerp:
                {
                    Rigidbody.MoveRotation(Quaternion.Slerp(
                        oldRotation, 
                        newRotation,
                        MissileSettings.GuideRotationSpeed * dt
                    ));
                    break;
                }
            }
            Rigidbody.MovePosition(rigidbodyPosition + (Rigidbody.rotation * Vector3.forward) * delta);
            
            var position = Rigidbody.position;
            var moved = (position - ShootPosition).magnitude;
            if (moved >= MaxDistance)
            {
                gameObject.SetActive(false);
                OnDisabled.Invoke();
            }
        }

        protected override EntityHitResult HitEntity(IEntity entity, Collider c)
        {
            Explode();
            return EntityHitResult.Success;
        }

        private void Explode()
        {
            // 범위 내 전체 몬스터 피격
            var count = Physics.OverlapSphereNonAlloc(
                Rigidbody.position, 
                MissileSettings.HitRange, 
                _colliders, 
                EnemyMask
            );

            for (int i = 0; i < count; i++)
            {
                var c = _colliders[i];
                if ((!c.CompareTag("Enemy") && !c.CompareTag("Destructible")) || !c.TryGetComponent(out IEntity e))
                {
                    continue;
                }
                base.HitEntity(e, null);
            }
        }
    }
}