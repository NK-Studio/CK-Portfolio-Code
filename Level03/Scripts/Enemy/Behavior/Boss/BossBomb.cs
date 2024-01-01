
using Character.Presenter;
using Effect;
using EnumData;
using Managers;
using Micosmo.SensorToolkit;
using UnityEngine;

namespace Enemy.Behavior.Boss
{
    public class BossBomb : MonoBehaviour
    {
        public DecalEffect DecalProjector;
        public RangeSensor ExplosionRange;
        public AnimationCurve OpacityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private float _endTime = -1f;
        private float _time;
        private float _damage;
        private Material _material;

        private void OnEnable()
        {
            DecalProjector.gameObject.SetActive(true);
            DecalProjector.Opacity = 0f;
            DecalProjector.Progress = 0f;
        }

        public void Initialize(float time, float damage, GameObject spawner)
        {
            _endTime = time;
            _time = 0f;
            _damage = damage;
        }

        private void Update()
        {
            if(_time >= _endTime) return;
            
            _time += Time.deltaTime;
            
            DecalProjector.Opacity = OpacityCurve.Evaluate(_time);
            var t = _time / _endTime;
            DecalProjector.Progress = t;

            if (_time >= _endTime)
            {
                var effect = EffectManager.Instance.Get(EffectType.BossSpawnBombExplosion);
                effect.transform.position = transform.position;
                ExplosionRange.Pulse();
                foreach(var target in ExplosionRange.Detections)
                {
                    if (target.CompareTag("Player") && target.TryGetComponent(out PlayerPresenter player))
                    {
                        player.Damage(_damage, gameObject, DamageReaction.KnockBack);
                    }
                }
                DecalProjector.gameObject.SetActive(false);
            }
        }

        // 내장된 SwordBomb가 꺼질 때 같이 없어지기
        private void OnParticleSystemStopped()
        {
            gameObject.SetActive(false);
        }
    }
}