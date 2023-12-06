using System;
using AutoManager;
using Cinemachine;
using DG.Tweening;
using FMODUnity;
using GameplayIngredients;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Platform
{
    public class TreePlatform : MonoBehaviour
    {
        [Title("오브젝트 연결")] [ValidateInput("@TreeGFX != null", "TreeGFX가 비어있습니다.")]
        public Transform TreeGFX;

        [ValidateInput("@Target != null", "Target 위치가 비어있습니다.")]
        public Transform Target;

        [ValidateInput("@ThrowTarget != null", "ThrowTarget이 비어있습니다.")]
        public GameObject ThrowTarget;

        [ValidateInput("@GroundCollision != null", "GroundCollision이 비어있습니다.")]
        public GameObject GroundCollision;

        [Title("간격")] public float duration = 1f;
        public float ShakeStartTime = 0.8f;

        [Title("애니메이션 스타일")] public Ease AnimationStyle = Ease.Linear;

        [SerializeField, ShowIf("@AnimationStyle == Ease.Unset"), Tooltip("애니메이션 전환(Easing) 시 사용될 그래프입니다.")]
        private AnimationCurve easeCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        [Title("OFFScreen"), ValidateInput("@offScreenSystem != null", "OffScreenSystem이 비어있습니다.")]
        public OffScreenSystem offScreenSystem;

        [Title("카메라"), Tooltip("나무가 쓰러졌을 때 화면을 흔듭니다.")]
        public bool CameraShake = true;

        [Title("머티리얼")] [Tooltip("빛나는 머티리얼"), ValidateInput("@LightMaterial != null", "LightMaterial가 비어있습니다.")]
        public Material LightMaterial;

        [Tooltip("일반 머티리얼"), ValidateInput("@NormalMaterial != null", "NormalMaterial가 비어있습니다.")]
        public Material NormalMaterial;

        [Title("매쉬 렌더러")] [ValidateInput("@Renderer != null", "쓰러지는 나무에 렌더러가 비어있습니다.")]
        public MeshRenderer Renderer;

        public EventReference[] SFXClips;

        [Inject] private OffScreenSystemManager _offScreenSystemManager;
        private CinemachineImpulseSource _impulseListener;


        private void Awake()
        {
            ThrowTarget.SetActive(true);
            GroundCollision.SetActive(false);
            _impulseListener = GetComponent<CinemachineImpulseSource>();
        }

        private void Start()
        {
            Renderer.sharedMaterial = LightMaterial;
        }

        [Button, HideInEditorMode]
        public void OnTriggerAnimation()
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Insert(0f, TreeGFX.DOMove(Target.position, duration));
            sequence.Insert(0f, TreeGFX.DORotate(Target.eulerAngles, duration));

            if (CameraShake)
                sequence.InsertCallback(ShakeStartTime, () =>
                {
                    Renderer.sharedMaterial = NormalMaterial;

                    _impulseListener.GenerateImpulse();

                    // @ 나무 쓰러지기 시작 사운드
                    Manager.Get<AudioManager>().PlayOneShot(SFXClips[1], transform.position);
                });

            if (AnimationStyle == Ease.Unset)
                sequence.SetEase(easeCurve);
            else
                sequence.SetEase(AnimationStyle);

            RemoveOffScreenSystem();
            ThrowTarget.SetActive(false);
            GroundCollision.SetActive(true);

            // @ 나무 쓰러지는 사운드
            Manager.Get<AudioManager>().PlayOneShot(SFXClips[0], transform.position);
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