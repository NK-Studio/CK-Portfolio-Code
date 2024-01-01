using Damage;
using EnumData;
using Micosmo.SensorToolkit;
using UnityEngine;
using Logger = NKStudio.Logger;

namespace Level
{
    public class SpikeTrap : MonoBehaviour
    {
        private Animator _animator;
        [SerializeField] private RangeSensor _sensorRange;
        [SerializeField] private RangeSensor _damageRange;

        public float DamageAmount = 1;
        public KnockBackInfo KnockBackInfo;
        
        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _sensorRange ??= GetComponent<RangeSensor>();
            _damageRange ??= GetComponent<RangeSensor>();

            var obj = gameObject;
            if(!_animator)
                Logger.LogWarning($"{obj}에 Animator 없음", obj);
            if(!_sensorRange)
                Logger.LogWarning($"{obj}에 Sensor Range 없음", obj);
            if(!_damageRange)
                Logger.LogWarning($"{obj}에 Damage Range 없음", obj);
        }

        private void Start()
        {
            _sensorRange.OnDetected.AddListener((obj, sensor) =>
            {
                _animator.SetTrigger(OnDetected);
            });
        }

        private DamageInfo _damageInfo = null;
        private static readonly int OnDetected = Animator.StringToHash("OnDetected");

        public void Damage()
        {
            if (_damageInfo == null)
            {
                _damageInfo = new DamageInfo
                {
                    Amount = DamageAmount, KnockBack = KnockBackInfo,
                    Mode = DamageMode.Normal, Reaction = DamageReaction.Normal,
                    Source = gameObject
                };
            }
            _damageRange.Pulse();
            foreach (var obj in _damageRange.Detections)
            {
                if (!obj.CompareTag("Enemy") && !obj.CompareTag("Player"))
                {
                    continue;
                }

                if (!obj.TryGetComponent(out IEntity entity))
                {
                    continue;
                }

                entity.Damage(_damageInfo);
            }
        }
    }
}