using System;
using System.Collections.Generic;
using Damage;
using Effect;
using Enemy.Behavior;
using EnumData;
using Managers;
using Settings.Player;
using UnityEngine;
using Utility;

namespace Character.Core.Weapon
{
    public class PlayerBreatheBullet : PlayerBullet
    {
        private PlayerBreatheBulletSettings _breatheBulletSettings = null;
        public PlayerBreatheBulletSettings BreatheSettings => _breatheBulletSettings ??= Settings as PlayerBreatheBulletSettings;

        public SectorRangeSensorFilter Range;
        protected override void Awake()
        {
        }

        private struct MonsterByForwardDot
        {
            public IEntity Monster;
            public float Dot;
        }
        private readonly List<MonsterByForwardDot> _enemiesInRange = new(15);
        private float _time;
        private float _curveLength;
        private bool _collected = false;
        private bool _end;
        public override void Initialize(PlayerBulletSettings settings, Vector3 position, float maxDistance)
        {
            // Debug.Log("PlayerBreatheBullet::Initialize");
            Settings = settings;
            ShootPosition = position;
            OnDisabled.RemoveAllListeners();

            transform.position = position;
            _collected = false;
            _end = false;
            
            AudioManager.Instance.PlayOneShot(settings.ShootSound);
            
            
        }

        private void SpawnCollideEffect(Vector3 position)
        {
            var effect = EffectManager.Instance.Get(BreatheSettings.CollideEffectType);
            effect.transform.position = position;
        }

        private readonly List<IEntity> _enemiesInDot = new();
        private void Update()
        {
            if (_end)
            {
                return;
            }
            var t = transform;
            var forward = t.forward;
            var right = t.right;
            var position = t.position;
            
            if (!_collected)
            {
                _collected = true;
                // 부채꼴 범위 내의 오브젝트 저장
                _enemiesInRange.Clear();
                foreach (var obj in Range.FilteredPulse())
                {
                    // Debug.Log($"PlayerBreatheBullet::FilteredPulse - {obj}");
                    if ((!obj.CompareTag("Enemy") && !obj.CompareTag("Destructible")) || !obj.TryGetComponent(out IEntity e))
                    {
                        continue;
                    }

                    var pair = new MonsterByForwardDot()
                    {
                        Monster = e,
                        // 앞쪽 내적 값을 사용
                        Dot = Vector3.Dot(forward, obj.transform.position - position)
                    };
                    _enemiesInRange.Add(pair);
                    Debug.Log($"{e.name} - {pair.Dot}");
                }
                // Hammer와 비슷하게 뒤에서 짜르기 위해 내림차순 정렬
                _enemiesInRange.Sort((x, y) => -x.Dot.CompareTo(y.Dot));
                _time = 0f;
                _curveLength = BreatheSettings.ForwardRangeCurve.GetLength();
                return;
            }
            if (_time >= _curveLength)
            {
                SprayCollideEffect();
                _end = true;
                return;
            }
            _time += Time.deltaTime;
            // 이번 절취선
            var currentDot = Range.Radius * BreatheSettings.ForwardRangeCurve.Evaluate(_time);

            DebugX.DrawLine(
                ShootPosition - right * Range.Radius + forward * currentDot,
                ShootPosition + right * Range.Radius + forward * currentDot,
                Color.yellow, 2f
            );

            _enemiesInDot.Clear();
            while (_enemiesInRange.Count > 0 && _enemiesInRange[^1].Dot < currentDot)
            {
                // Debug.Log($"[{_time:F3}] {currentDot}: {_enemiesInRange[^1].Monster} ({_enemiesInRange[^1].Dot})");
                _enemiesInDot.Add(_enemiesInRange[^1].Monster);
                _enemiesInRange.RemoveAt(_enemiesInRange.Count - 1); // pop
            }

            foreach (var m in _enemiesInDot)
            {
                m.Damage(EnemyDamageInfo.Get(
                    Settings.DamageToBoss, gameObject, DamageMode.Normal, DamageReaction.Freeze,
                    playerAttackType: PlayerState.PlayerBullet, freezeFactor: BreatheSettings.FreezePower
                ));
                SpawnCollideEffect(m.transform.position + Vector3.up * 0.5f);
            }
        }

        protected override void FixedUpdate()
        {
            // Nothing on FixedUpdate
        }

        protected override void OnTriggerEnter(Collider other)
        {
            // No trigger
        }

        private void SprayCollideEffect()
        {
            var t = transform;
            var forward = t.forward;
            var position = t.position + Vector3.up * 0.5f;

            var rayCount = BreatheSettings.CollideEffectDivision;
            var leftRotation = Quaternion.AngleAxis(-Range.Angle * 0.5f, Vector3.up); 
            var left = leftRotation * forward;
            var rotator = Quaternion.AngleAxis(Range.Angle / rayCount, Vector3.up);

            var ray = new Ray(position, left);
            var distance = Range.Radius - BreatheSettings.CollideEffectRangeThreshold;
            var mask = BreatheSettings.CollideEffectMask.value;
            for (int i = 0; i < rayCount; i++)
            {
                if (Physics.Raycast(ray, out var hitInfo, distance, mask))
                {
                    Debug.DrawRay(ray.origin, ray.direction * hitInfo.distance, Color.green, 3f, false);
                    SpawnCollideEffect(hitInfo.point);
                }
                else
                {
                    Debug.DrawRay(ray.origin, ray.direction * distance, Color.red, 3f, false);
                }

                ray.direction = rotator * ray.direction;
            }

        }   
    }
}