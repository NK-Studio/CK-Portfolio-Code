using System;
using Character.Presenter;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Effect;
using EnumData;
using Managers;
using Micosmo.SensorToolkit;
using Settings;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace Enemy.Behavior.TurretMonster
{
    public class TurretMonsterProjectile : MonoBehaviour
    {
        
        [ValidateInput("@Settings != null")]
        public TurretSettings Settings;
        
        public RangeSensor Sensor;
        public EffectType ExplosionEffectType = EffectType.TurretMonsterCannonExplosion;

        public float AffectRange => Settings.ProjectileAffectRange;
        protected virtual GameObject ProjectileRangeProjector { get; set; }
        
        protected virtual void Start()
        {
            Sensor.Shape = RangeSensor.Shapes.Sphere;
            Sensor.Sphere.Radius = AffectRange;
            this.OnCollisionEnterAsObservable().Subscribe(c =>
            {
                DebugX.Log($"TurretMonsterProjectile collide with {c.collider.name}");
                Explode();
            }).AddTo(this);
            
            Sensor.OnDetected.AddListener((obj, _) =>
            {
                if (obj.TryGetComponent(out PlayerPresenter player))
                {
                    player.Damage(Settings.AttackPower, gameObject);
                }
                Debug.Log($"{obj.name} damaged from {gameObject.name}");
            });
        }

        public void Explode()
        {
            Sensor.Pulse();
            var effect = EffectManager.Instance.Get(ExplosionEffectType);
            effect.transform.position = transform.position;
            Destroy(gameObject);
        }

        protected DecalEffect _projector;
        private GameObject _projectorObject;
        private float _flyTime = 0f;
        private float _maxFlyTime;
        public void Initialize(Vector3 start, Vector3 target, float flyTime)
        {
            if (!ProjectileRangeProjector) 
                ProjectileRangeProjector = Settings.ProjectileRangeProjector;

            _projectorObject = Instantiate(ProjectileRangeProjector, target, Quaternion.identity);
            
            _projector = _projectorObject.GetComponent<DecalEffect>();
            _projector.Radius = Settings.ProjectileAffectRange;
            _projector.Progress = 0f;
            _projector.Opacity = 0f;
            DOTween.To(
                () => _projector.Opacity, 
                value => _projector.Opacity = value, 
                1f, 0.5f
            ).Play();
            _flyTime = 0f;
            _maxFlyTime = flyTime;
            this.UpdateAsObservable().Subscribe(_ =>
            {
                if (_flyTime > _maxFlyTime)
                {
                    Explode();
                    return;
                }
                
                _flyTime += Time.deltaTime;
                _projector.Progress = Mathf.Clamp01(_flyTime / _maxFlyTime);
            }).AddTo(this);
        }

        private void OnDestroy()
        {
            if (_projectorObject)
            {
                Destroy(_projectorObject);
                _projector = null;
                _projectorObject = null;
            }
        }
    }
}