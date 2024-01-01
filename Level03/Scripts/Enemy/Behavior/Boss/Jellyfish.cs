using System;
using System.Collections.Generic;
using Character.Presenter;
using Cysharp.Threading.Tasks;
using Damage;
using Effect;
using EnumData;
using Managers;
using Micosmo.SensorToolkit;
using Settings.Boss;
using UnityEngine;
using Utility;

namespace Enemy.Behavior.Boss
{
    public class Jellyfish : MonoBehaviour
    {
        public BossAquusSettings Settings;
        public RangeSensor Range;

        public List<Renderer> Renderers = new();
        public List<GameObject> NonRenderers = new();

        public bool RendererEnabled
        {
            get => Renderers[0].enabled;
            set
            {
                foreach (var r in Renderers)
                {
                    r.enabled = value;
                }

                foreach (var nr in NonRenderers)
                {
                    nr.gameObject.SetActive(value);
                }
            }
        }

        private PlayerPresenter _player;
        private Vector3 _startPosition;

        private void OnEnable()
        {
            _player = GameManager.Instance.Player;
            RendererEnabled = true;
        }

        public void ThrowStart()
        {
            _startPosition = transform.position;
            Sequence().Forget();
        }

        private async UniTaskVoid Sequence()
        {
            var tr = transform;
            var playerPosition = _player.transform.position;
            // 상승
            {
                var curve = Settings.JellyfishThrowCurve;
                float t = 0f;
                float length = curve.GetLength();
                while (t < length)
                {
                    var y = curve.Evaluate(t);
                    tr.position = tr.position.Copy(y: _startPosition.y + y);
                    await UniTask.Yield();
                    t += Time.deltaTime;
                }
            }

            GameObject decalObj;
            // 하강
            {
                var curve = Settings.JellyfishFallCurve;
                var alphaCurve = Settings.JellyfishDecalAlphaCurve;
                var offsetCurve = Settings.JellyfishDecalOffsetCurve;
                tr.position = playerPosition.Copy(y: playerPosition.y + curve[0].value);
                decalObj = EffectManager.Instance.Get(EffectType.AquusJellyfishDecal, tr.position, Quaternion.Euler(90f, 0f, 0f));
                var decal = decalObj.GetComponent<DecalEffect>();
                decalObj.SetActive(true);
                float t = 0f;
                float length = curve.GetLength();
                while (t < length)
                {
                    var y = curve.Evaluate(t);
                    var alpha = alphaCurve.Evaluate(t);
                    var offset = offsetCurve.Evaluate(t);
                    tr.position = tr.position.Copy(y: playerPosition.y + y);
                    decal.Opacity = alpha;
                    decal.Progress = offset;
                    await UniTask.Yield();
                    t += Time.deltaTime;
                }
            }
            tr.position = tr.position.Copy(y: playerPosition.y);

            var explosionEffect = EffectManager.Instance.Get(EffectType.AquusJellyfishExplosion);
            explosionEffect.transform.position = tr.position + Vector3.up * 0.5f;
            decalObj.SetActive(false);
            RendererEnabled = false; 
            for (int pulseCount = 0; pulseCount < Settings.JellyfishPulseCount; pulseCount++)
            {
                PulseAndDamage();
                await UniTask.Delay(TimeSpan.FromSeconds(Settings.JellyfishPulseInterval));
            }
            gameObject.SetActive(false);
        }

        private void PulseAndDamage()
        {
            Range.Pulse();
            foreach (var obj in Range.Detections)
            {
                if (obj.CompareTag("Player") && obj.TryGetComponent(out PlayerPresenter p))
                {
                    p.Damage(PlayerDamageInfo.Get(
                        Settings.JellyfishDamage, gameObject, DamageMode.Normal, DamageReaction.Stun, 
                        enemyAttackType: EnemyAttackType.BossJellyfish
                    ));
                }
            } 
        }
    }
}