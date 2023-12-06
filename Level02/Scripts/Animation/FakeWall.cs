using System;
using System.Threading;
using AutoManager;
using Character.Controllers;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Sirenix.OdinInspector;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Animation
{
    public class FakeWall : MonoBehaviour
    {
        [Title("매쉬")] public GameObject Origin;
        public GameObject Crack;

        [Title("이펙트")] public GameObject DustEffect;

        [Tooltip("폭발시 먼지 이펙트를 재생합니다.")] public bool ShowDustEffect = true;

        [Title("스타 캔디")] [AssetsOnly, Tooltip("프리팹으로 만들어진 스타 캔디를 바인딩합니다.")]
        public GameObject StarCandyPrefab;

        [SceneObjectsOnly, Tooltip("씬에 배치된 스타 캔디를 바인딩합니다.")]
        public GameObject StarCandy;

        [SceneObjectsOnly, Tooltip("씬에 배치된 스타 캔디를 바인딩합니다.")]
        public Transform SpawnArea;

        [Title("디버그")]
        public bool DebugMode;

        public EventReference[] SFXClips;
        
        public Vector3 SpawnPoint
        {
            get
            {
                Vector3 p;
                p.x = Random.Range(-0.5f, 0.5f);
                p.y = Random.Range(-0.5f, 0.5f);
                p.z = Random.Range(-0.5f, 0.5f);
                return SpawnArea.TransformPoint(p);
            }
        }

        private bool _isExplosion;

        [Inject] private DiContainer _container;

        private Transform playerTransform;
        
        private void Awake()
        {
            playerTransform = GameObject.FindWithTag("Player").transform;
        }

        private void Start()
        {
            Origin.OnTriggerEnterAsObservable()
                .Where(other => other.CompareTag("ExplosionRange"))
                .Subscribe(_ =>
                {
                    Vector3 directionToTarget = transform.position - playerTransform.position;
                    float angel = Vector3.Angle(transform.forward, directionToTarget);
                    bool isBack = Mathf.Abs(angel) < 90 || Mathf.Abs(angel) > 270;
        
                    if (!isBack)
                    {
                        _isExplosion = true;
                        Origin.SetActive(false);
                        Crack.SetActive(true);
                        print("벽 부서짐");
                        // @ 벽 부서지는 사운드
                        Manager.Get<AudioManager>().PlayOneShot(SFXClips[0], transform.position);

                        if (ShowDustEffect)
                            DustEffect.SetActive(true);
                    }
                })
                .AddTo(this);

            //
            DelayCreateStarCandy().Forget();
        }

        private async UniTask DelayCreateStarCandy()
        {
            CancellationToken ct = this.GetCancellationTokenOnDestroy();

            //스타 캔디가 사라지는 것을 트래킹
            await UniTask.WaitUntil(() => StarCandy == null, cancellationToken: ct);

            //1초를 기다린다.
            await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: ct);

            //부셔졌으면 return,
            if (_isExplosion) return;

            //안부셔졌을 경우
            Vector3 randomPosition = Vector3.zero;
            randomPosition.x = SpawnPoint.x;
            randomPosition.y = SpawnArea.position.y;
            randomPosition.z = SpawnPoint.z;
            StarCandy = _container.InstantiatePrefab(StarCandyPrefab, randomPosition, Quaternion.identity,
                null);

            DelayCreateStarCandy().Forget();
        }

        [Button("스타 캔디 생성", ButtonSizes.Large), PropertySpace(20)]
        private void TestCreateStarCandy()
        {
            //안부셔졌을 경우
            Vector3 randomPosition = Vector3.zero;
            randomPosition.x = SpawnPoint.x;
            randomPosition.y = SpawnArea.position.y;
            randomPosition.z = SpawnPoint.z;
            StarCandy = _container.InstantiatePrefab(StarCandyPrefab, randomPosition, Quaternion.identity,
                null);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!DebugMode) return;
            
            Gizmos.color = Color.cyan;
            if (SpawnArea == null) return;

            Gizmos.matrix = SpawnArea.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
#endif
    }
}