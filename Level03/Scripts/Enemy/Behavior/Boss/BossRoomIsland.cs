using System;
using Character.Presenter;
using Cysharp.Threading.Tasks;
using Damage;
using Micosmo.SensorToolkit;
using RayFire;
using UnityEngine;
using UnityEngine.Events;

namespace Enemy.Behavior.Boss
{
    public class BossRoomIsland : MonoBehaviour
    {
        public float Delay = 3f;
        public RayfireRigid Rigid;
        public RangeSensor KillRange;
        public UnityEvent Event;

        private bool _exploded;
        public void Explode()
        {
            _exploded = false;
            ExplodeSequence().Forget();
        }
        
        private async UniTaskVoid ExplodeSequence()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(Delay));
            if(_exploded) return;
            ExplodeInternal();
        }

        public void ExplodeInternal()
        {
            if(_exploded) return;
            _exploded = true;
            Event?.Invoke();
            
            var rigid = Rigid;
            rigid.Initialize();
            rigid.Fade();
            if (rigid.TryGetComponent(out RayfireBomb bomb))
            {
                bomb.Explode(0f);
            }

            if (KillRange)
            {
                KillRange.Pulse();
                foreach (var obj in KillRange.Detections)
                {
                    if (obj.CompareTag("Player") && obj.TryGetComponent(out PlayerPresenter player))
                    {
                        player.Health = 0f;
                    }else if (obj.CompareTag("Enemy") && obj.TryGetComponent(out Monster m))
                    {
                        if (m.IsFreeze)
                        {
                            m.OnFreezeBreak();
                        }
                        else
                        {
                            m.Damage(EnemyDamageInfo.Get(m.Health, KillRange.gameObject));
                        }
                    }
                }
            }
        }
    }
}