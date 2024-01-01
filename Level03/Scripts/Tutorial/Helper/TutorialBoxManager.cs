using System;
using Enemy.Behavior;
using Enemy.Behavior.BoxMonster;
using Enemy.Spawner;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Tutorial.Helper
{
    public class TutorialBoxManager : MonoBehaviour
    {
        [BoxGroup("대상")] public BoxMonster Target;
        
        [BoxGroup("재생성")] public float RespawnTime = 3f;
        [BoxGroup("재생성")] public EnemyParabolaSpawner Spawner;
        
        // [BoxGroup("키 가이드")] public Image KeyGuide;
        // [BoxGroup("키 가이드")] public Sprite KeyGuideSpriteNormal;
        // [BoxGroup("키 가이드")] public Sprite KeyGuideSpriteFrozen;
        
        [BoxGroup("재생성"), SerializeField, ReadOnly]
        private float _respawnTimer = -1f;
        private void Start()
        {
            if (!Target)
            {
                Debug.LogWarning($"{name} - Target 없음!", gameObject);
                return;
            }
            if (!Spawner)
            {
                Debug.LogWarning($"{name} - Spawner 없음!", gameObject);
                return;
            }
            Target.OnDeadEvent.AddListener(OnTargetDead);
        }

        private void OnTargetDead(Monster _)
        {
            _respawnTimer = RespawnTime;
            Target.gameObject.SetActive(false);
        }

        private void Update()
        {
            // UpdateKeyGuide();
            if (_respawnTimer > 0f)
            {
                _respawnTimer -= Time.deltaTime;
                if (_respawnTimer <= 0f)
                {
                    Target.gameObject.SetActive(true);
                    Target.InitializeParabola(Spawner.Parabola, Spawner.UseXPositionCurve ? Spawner.XPositionCurve : null, 0f).Forget();
                }
                return;
            }
        }

        /*
        private void UpdateKeyGuide()
        {
            // 없음
            if (!Target 
                // 비활성화
                || !Target.isActiveAndEnabled
                // 날아가는 중
                || Target.IsFreezeSlipping 
                || Target.IsFreezeFalling
            ) {
                KeyGuide.gameObject.SetActive(false);
                return;
            }

            KeyGuide.gameObject.SetActive(true);
            
            // 얼어있으면 우클릭 키가이드 
            if (Target.IsFreeze)
            {
                KeyGuide.sprite = KeyGuideSpriteFrozen;
            }
            // 아니면 좌클릭 키가이드
            else
            {
                KeyGuide.sprite = KeyGuideSpriteNormal;
            }
        }
        */

        private void OnDrawGizmos()
        {
            if (!Spawner)
            {
                return;
            }
            Spawner.DrawGizmosOnValid();
        }
    }
}