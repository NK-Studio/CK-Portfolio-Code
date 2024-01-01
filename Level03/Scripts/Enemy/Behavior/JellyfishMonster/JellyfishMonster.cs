using System;
using EnumData;
using Managers;
using Settings;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Enemy.Behavior.ShieldMonster
{
    public class JellyfishMonster : Monster
    {
        private JellyfishMonsterSettings _settings;
        public JellyfishMonsterSettings JellyfishSettings => _settings ??= (JellyfishMonsterSettings)base.Settings;

        [BoxGroup("해파리 몬스터")]
        public bool PrintDebugLog = false;

        [BoxGroup("해파리 몬스터")]
        public Renderer ExplosionWarningTargetRenderer;

        [BoxGroup("해파리 몬스터")] 
        public string ExplosionWarningTargetProperty = "_BaseColor";
        private int _explosionWarningTargetPropertyID;
        private Material _explosionWarningTargetMaterial;
        private Color _explosionWarningTargetMaterialDefaultColor;

        private const float MinimumExplosionDelayFromSpawn = 1f; 
        public float ExplosionDelayFromSpawn { get; set; }
        public float CountdownDuration { get; set; }
        private IDisposable _countdownDisposable;
        public bool IsCountingDown { get; set; }

        private bool _isInRange;

        protected override void Start()
        {
            base.Start();
            
            if (ExplosionWarningTargetRenderer)
            {
                _explosionWarningTargetMaterial = ExplosionWarningTargetRenderer.material;
                _explosionWarningTargetPropertyID = Shader.PropertyToID(ExplosionWarningTargetProperty);
                _explosionWarningTargetMaterialDefaultColor = _explosionWarningTargetMaterial.GetColor(_explosionWarningTargetPropertyID);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            CountdownDuration = JellyfishSettings.ExplosionCountdownTime;
            _isInRange = false;
            ExplosionDelayFromSpawn = MinimumExplosionDelayFromSpawn;
        }

        private void Update()
        {
            if(Health <= 0 || IsRunningSpawnSequence) return;
            if (ExplosionDelayFromSpawn > 0f)
            {
                ExplosionDelayFromSpawn -= Time.deltaTime;
            }
            
            float distance = Vector3.Distance(PlayerView.transform.position, transform.position);
            // 플레이어와 초근접 시 즉시 폭발
            if (ExplosionDelayFromSpawn <= 0f && !IsFreeze && distance <= _settings.ExplosionNearRadius)
            {
                Explosion();
                return;
            }
            
            // 공격 시작 범위에 들어오면 추적
            if (distance <= _settings.AttackStartRange)
            {
                _isInRange = true;
            }
            
            if (_isInRange)
            {
                // 냉동중이 아닐 때에만 카운트다운
                if (!IsFreeze && !NavMeshAgent.isStopped)
                {
                    IsCountingDown = true;
                    if (IsCountingDown)
                    {
                        if(PrintDebugLog)
                            Debug.Log($"{name} 카운트다운 : " + CountdownDuration, gameObject);
                        CountdownDuration -= Time.deltaTime; // 0.1초씩 감소
                    }   
                }

                if (_explosionWarningTargetMaterial)
                {
                    float t = 1f - (CountdownDuration / JellyfishSettings.ExplosionCountdownTime);
                    float curveResult = JellyfishSettings.ExplosionWarningColorCurve.Evaluate(t);
                    _explosionWarningTargetMaterial.SetColor(_explosionWarningTargetPropertyID, Color.Lerp(
                        _explosionWarningTargetMaterialDefaultColor, 
                        JellyfishSettings.ExplosionWarningColor, 
                        curveResult
                    ));
                }
            }

            if (CountdownDuration <= 0)
            {
                Explosion();
            }
        }

        public void Explosion()
        {
            PreventDropItem = true;
            if(PrintDebugLog)
                Debug.Log($"{name} 폭발", gameObject);
            float distance = Vector3.Distance(PlayerView.CenterPoint.position, transform.position);
            if (distance <= JellyfishSettings.ExplosionRadius)
            {
                PlayerPresenter.Damage(JellyfishSettings.AttackPower, gameObject, DamageReaction.KnockBack);
            }

            var effect = EffectManager.Instance.Get(EffectType.JellyfishExplosion);
            effect.transform.position = transform.TransformPoint(effect.transform.position);

            Health = 0f;
        }
        
        private static void SetSharedMaterial(Renderer r, int index, Material material)
        {
            var mats = r.sharedMaterials;
            mats[index] = material;
            r.sharedMaterials = mats;
        }
    }
}