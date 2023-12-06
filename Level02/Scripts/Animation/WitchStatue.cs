using System;
using AutoManager;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using RayFire;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Animation
{
    public class WitchStatue : MonoBehaviour
    {
        // 오브젝트 시작지점
        [Title("오브젝트 연결")] [ValidateInput("@ObjectFrom != null", "ObjectFrom가 비어있습니다.")]
        public Transform ObjectFrom;

        // 오브젝트 목표지점
        [ValidateInput("@ObjectTarget != null", "ObjectTarget 위치가 비어있습니다.")]
        public Transform ObjectTarget;

        // 로프 던지는 위치
        [ValidateInput("@ThrowTarget != null", "ThrowTarget이 비어있습니다.")]
        public GameObject ThrowTarget;

        // 바닥에 닿는 판정 콜라이더
        [ValidateInput("@GroundCollision != null", "GroundCollision이 비어있습니다.")]
        public GameObject GroundCollision;

        public GameObject Bomb;

        [Title("카메라 흔들림")] [Tooltip("나무가 쓰러졌을 때 화면을 흔듭니다.")]
        public bool CameraShake = true;

        public float ShakeStartTime = 0.8f;

        [Title("애니메이션 지속시간")] public float duration = 1f;

        [Title("애니메이션 스타일")] public Ease AnimationStyle = Ease.Linear;

        [SerializeField, ShowIf("@AnimationStyle == Ease.Unset"), Tooltip("애니메이션 전환(Easing) 시 사용될 그래프입니다.")]
        private AnimationCurve easeCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        [Title("OFFScreen"), ValidateInput("@offScreenSystem != null", "OffScreenSystem이 비어있습니다.")]
        public OffScreenSystem offScreenSystem;

        public EventReference[] SFXClips;

        [Inject] private OffScreenSystemManager _offScreenSystemManager;
        private CinemachineImpulseSource _impulseListener;

        // from FakeWall

        [Title("파괴효과")] [Tooltip("원본 모델")] public GameObject Origin;
        [Tooltip("부숴진 모델(At Start)")] public GameObject Crack;

        private bool _hasCracked;
        public bool HasCracked => _hasCracked;

        [Inject] private DiContainer _container;
        public RayfireRigid _root;
        public RayfireBomb bomb;

        [Button("Active")]
        private void pp()
        {
            _root.Activate();
            bomb.gameObject.SetActive(true);
        }

        [Button("init")]
        private void initti()
        {
            _root.Initialize();
        }

        [Button("Fade")]
        private void go()
        {
            _root.Fade();
        }

        private void Awake()
        {
            ThrowTarget.SetActive(true);
            _impulseListener = GetComponent<CinemachineImpulseSource>();
        }

        private void Start()
        {
            Bomb.gameObject.SetActive(false);
        }

        public int Index { get; set; } = -1;
        public float DestroyTime = 10f;

        public void Explode()
        {
            if (_sequence != null)
            {
                _sequence.Kill();
                _sequence = null;
            }
            
            Origin.SetActive(false);
            Crack.SetActive(true);
            GroundCollision.gameObject.SetActive(false);
            Bomb.gameObject.SetActive(true);
            _root.Fade();
            
            SelfDestroy().Forget();
            
            // @ 석상 파괴 사운드
            Manager.Get<AudioManager>().PlayOneShot(SFXClips[0], transform.position);
        }

        private async UniTaskVoid SelfDestroy()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(DestroyTime),
                cancellationToken: this.GetCancellationTokenOnDestroy());
            
            Destroy(gameObject);
        }

        private Sequence _sequence;

        [Button, HideInEditorMode]
        public void OnTriggerAnimation()
        {
            // 시퀀스 구성 (움직임 & 회전 애니메이션)
            Sequence sequence = _sequence = DOTween.Sequence();
            sequence.Insert(0f, ObjectFrom.DOMove(ObjectTarget.position, duration));
            sequence.Insert(0f, ObjectFrom.DORotateQuaternion(ObjectTarget.rotation, duration));

            // 시퀀스 끝나면 화면 흔듬
            if (CameraShake)
                sequence.InsertCallback(ShakeStartTime, () => _impulseListener.GenerateImpulse());


            // 암튼 애니메이션이 끝났을 때? (중단의 경우에도)
            sequence.OnComplete(Explode);

            // 애니메이션 커브 설정
            if (AnimationStyle == Ease.Unset)
                sequence.SetEase(easeCurve);
            else
                sequence.SetEase(AnimationStyle);

            // 로프 마커 제거
            RemoveOffScreenSystem();
            ThrowTarget.SetActive(false);

            // 바닥 콜라이더 활성화
            GroundCollision.SetActive(true);

            _hasCracked = true;
        }

        /// <summary>
        /// OffScreenSystem을 제거합니다.
        /// </summary>
        private void RemoveOffScreenSystem()
        {
            Image pointer = offScreenSystem.GetPointer();
            _offScreenSystemManager.Remove(offScreenSystem);
            Destroy(pointer.gameObject);
        }
    }
}