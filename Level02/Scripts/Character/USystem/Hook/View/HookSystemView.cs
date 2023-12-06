using System;
using AutoManager;
using Managers;
using Settings;
using Sirenix.OdinInspector;
using UniRx.Triggers;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Utility;
using Zenject;
using EHookState = Character.USystem.Hook.Model.EHookState;

namespace Character.USystem.Hook.View
{
    public enum WeaponType
    {
        Normal,
        Bending,
        System,
        AllShow,
        AllHide
    }

    public class HookSystemView : MonoBehaviour
    {
        [SerializeField, FoldoutGroup("그랩"), Tooltip("손가락 젤리가 오브젝트와 닿을 시, 해당 오브젝트는 이 오브젝트의 자식이 됩니다.")]
        private Transform grabGroup;

        [ValidateInput("@rHand != null", "플레이어으로부터 V HandJelly Point를 바인딩 해야합니다."), SerializeField,
         FoldoutGroup("트랜스폼")]
        private Transform rHand;

        [SerializeField, FoldoutGroup("트랜스폼")] private Transform rope; //rope
        [SerializeField, FoldoutGroup("핸들")] private Transform startHandle;
        [SerializeField, FoldoutGroup("핸들")] private Transform endHandle;

        [ValidateInput("@pullModeMaterial != null", "젤리 머티리얼을 바인딩해야합니다.")] [SerializeField, FoldoutGroup("머티리얼")]
        private Material pullModeMaterial;

        private Material _pullModeMaterial;

        [ValidateInput("@moveToTargetModeMaterial != null", "젤리 머티리얼을 바인딩해야합니다.")]
        [SerializeField, FoldoutGroup("머티리얼")]
        private Material moveToTargetModeMaterial;

        private Material _moveToTargetModeMaterial;

        [SerializeField, FoldoutGroup("머티리얼")] private MeshRenderer[] meshes;

        [ValidateInput("@normalWeaponMesh != null", "플레이어으로부터 일반 무기 메쉬를 바인딩해야합니다.")]
        [SerializeField, FoldoutGroup("무기 머티리얼")]
        private SkinnedMeshRenderer normalWeaponMesh;

        [ValidateInput("@attackWeaponMesh != null", "플레이어으로부터 공격 무기 메쉬를 바인딩해야합니다.")]
        [SerializeField, FoldoutGroup("무기 머티리얼")]
        private SkinnedMeshRenderer attackWeaponMesh;

        [ValidateInput("@dragEffect != null", "Drag 이펙트를 바인딩해야합니다.")] [SerializeField, FoldoutGroup("이펙트")]
        private GameObject dragEffect;

        [field: SerializeField, FoldoutGroup("애니메이션")]
        public AnimationCurve PullCurve { get; private set; } = AnimationCurve.Linear(0, 0, 1, 1);

        [field: SerializeField, FoldoutGroup("애니메이션")]
        public AnimationCurve MoveToTargetCurve { get; private set; } = AnimationCurve.Linear(0, 0, 1, 1);

        private CharacterSettings _settings;

        [Inject] private DiContainer _container;

        private void Awake()
        {
            _settings = Manager.Get<GameManager>().characterSettings;

            _pullModeMaterial = Instantiate(pullModeMaterial);
            _moveToTargetModeMaterial = Instantiate(moveToTargetModeMaterial);
            //center.transform.rotation = Quaternion.Euler(Vector3.zero);
        }

        /// <summary>
        /// 시스템 로프를 숨깁니다.
        /// </summary>
        public void HideSystemRope()
        {
            Assert.IsTrue(meshes.Length > 0, "시스템 로프 메쉬가 비어있습니다.");

            foreach (MeshRenderer meshRenderer in meshes)
                meshRenderer.gameObject.SetActive(false);
        }

        /// <summary>
        /// 로프 팡 이펙트
        /// </summary>
        public void OnRopePang()
        {
            // GameObject effect = _container.ResolveId<GameObject>(EffectType.RopePang);
            // Instantiate(effect, handleGFX.position, quaternion.identity);
        }

        /// <summary>
        /// 당기기 이펙트의 활성화를 설정합니다.
        /// </summary>
        /// <param name="active"></param>
        public void SetActiveDragEffect(bool active)
        {
            dragEffect.SetActive(active);
        }

        public RaycastHit HandRaycast()
        {
            Vector3 origin = endHandle.position;
            Vector3 direction = -endHandle.up;
            Physics.Raycast(origin, direction, out RaycastHit hit, 0.5f, _settings.moveToTargetLayerMask);
            return hit;
        }

        [Button("Auto Binding", ButtonSizes.Large)]
        private void BindingRHand()
        {
            rHand = GameObject.Find("MainChacarter_Root R Hand").transform;

            if (GameObject.Find("Stickyjelly_Move").TryGetComponent(out SkinnedMeshRenderer normalMeshRenderer))
                normalWeaponMesh = normalMeshRenderer;

            if (GameObject.Find("Stickyjelly_NoMove").TryGetComponent(out SkinnedMeshRenderer attackMeshRenderer))
                attackWeaponMesh = attackMeshRenderer;
        }

        public void RefreshRope(Vector3 target)
        {
            transform.position = rHand.position;
            transform.LookAt(target);
        }

        /// <summary>
        /// 훅이 날아가거나 되돌아오는 역할을 수행합니다.
        /// </summary>
        /// <param name="hookState"></param>
        /// <param name="ropeState"></param>
        /// <param name="targetPosition"></param>
        public void OnMoveHook(EHookState hookState, ERopeState ropeState, Vector3 targetPosition)
        {
            Assert.IsTrue(endHandle, "startHandle이 비어있습니다.");

            switch (hookState)
            {
                //로프가 앞으로 이동
                case EHookState.Forward:
                    endHandle.Translate(Vector3.forward * (_settings.hookForwardSpeed * Time.deltaTime));
                    break;

                case EHookState.BackOrMoveTarget:

                    if (ropeState == ERopeState.MoveToTarget)
                    {
                        startHandle.position = rHand.position;
                        endHandle.transform.position = targetPosition;
                        OnRenderRope();
                        return;
                    }

                    //뒤로 되돌아오는 속도
                    float backSpeed = HasGrabTarget() ? _settings.hookBackendSpeed : _settings.hookFailBackendSpeed;

                    //ERopeState.MoveToTarget이 아닐 때만 동작
                    endHandle.Translate(-Vector3.forward * (backSpeed * Time.deltaTime));
                    break;
            }

            OnRenderRope();
        }

        public void TestCode()
        {
            rope.position = rHand.position;
        }
        
        /// <summary>
        /// 타겟까지의 거리를 노말라이즈한 값을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public float GetDistanceToTargetNormalize()
        {
            return Vector3.Distance(startHandle.position, endHandle.position) / 15.5f;
        }

        
        /// <summary>
        /// 손가락 끈끈이를 렌더링합니다.
        /// </summary>
        private void OnRenderRope()
        {
            rope.position = rHand.position;

            //초기 선언
            Vector3 nextRopeLength = rope.localScale;
            float ropeLength = Vector3.Distance(startHandle.position, endHandle.position);
            nextRopeLength = new Vector3(nextRopeLength.x, nextRopeLength.y, ropeLength);

            //시작 핸들은 끝 핸들을 트래킹하도록 처리한다.
            //startHandle.LookAt(endHandle);

            //로프 길이와 방향 재계산
            rope.localScale = nextRopeLength;

            //로프 끈은 손바닥의 하단 피벗을 트래킹 처리한다.
            rope.LookAt(endHandle);

            //머티리얼 스케일 재계산
            _pullModeMaterial.mainTextureScale = new Vector2(_pullModeMaterial.mainTextureScale.x, nextRopeLength.z);
            _moveToTargetModeMaterial.mainTextureScale =
                new Vector2(_moveToTargetModeMaterial.mainTextureScale.x, nextRopeLength.z);
        }

        /// <summary>
        /// 훅의 위치와 회전을 컨트롤링합니다.
        /// </summary>
        public void OnRefreshPositionAndRotationByRHand(EHookState state, Vector3 targetPosition)
        {
            if (state is EHookState.Stop or EHookState.BackOrMoveTarget)
                transform.position = rHand.position;
            else
                transform.position = rHand.position;

            // 핸드 끝 부분을 봄
            //startHandle.LookAt(endHandle);
        }

        /// <summary>
        /// 상태에 따라 컬러를 변경합니다.
        /// </summary>
        /// <param name="state"></param>
        public void ChangeColorByState(ERopeState state)
        {
            //TODO: 색상 변경, 캐싱은 나중에 변수명이 재대로 지어지면 그때 합시다.

            //당기는 로프 모드
            if (state == ERopeState.Pull)
            {
                foreach (MeshRenderer mesh in meshes)
                    mesh.material = _pullModeMaterial;

                normalWeaponMesh.material = _pullModeMaterial;
                attackWeaponMesh.material = _pullModeMaterial;
            }
            else if (state == ERopeState.MoveToTarget)
            {
                foreach (MeshRenderer mesh in meshes)
                    mesh.material = _moveToTargetModeMaterial;

                normalWeaponMesh.material = _moveToTargetModeMaterial;
                attackWeaponMesh.material = _moveToTargetModeMaterial;
            }
        }

        /// <summary>
        /// EndHandle에 달려있는 달려있는 타겟을 놓습니다. 
        /// </summary>
        public void PutTarget()
        {
            if (grabGroup.childCount > 0)
                grabGroup.GetChild(0).parent = null;
        }
        
        /// <summary>
        /// 로프가 타겟을 바라보도록 처리합니다.
        /// </summary>
        /// <param name="target"></param>
        public void LookAt(Transform target)
        {
            transform.LookAt(target);
        }

        /// <summary>
        /// 로프가 타겟을 바라보도록 처리합니다.
        /// </summary>
        /// <param name="target"></param>
        public void LookAt(Vector3 target)
        {
            transform.LookAt(target);
        }

        /// <summary>
        /// 무기를 보입니다.
        /// </summary>
        public void ChangeWeaponActiveType(WeaponType weaponType, bool isHideSystemRope = true)
        {
            switch (weaponType)
            {
                case WeaponType.AllShow:
                    normalWeaponMesh.gameObject.SetActive(true);
                    attackWeaponMesh.gameObject.SetActive(true);
                    break;
                case WeaponType.AllHide:
                    normalWeaponMesh.gameObject.SetActive(false);
                    attackWeaponMesh.gameObject.SetActive(false);
                    HideSystemRope();
                    break;
                case WeaponType.Bending:
                    normalWeaponMesh.gameObject.SetActive(false);
                    attackWeaponMesh.gameObject.SetActive(true);
                    break;
                case WeaponType.Normal:
                    normalWeaponMesh.gameObject.SetActive(true);
                    attackWeaponMesh.gameObject.SetActive(false);
                    break;
                case WeaponType.System:

                    normalWeaponMesh.gameObject.SetActive(false);
                    attackWeaponMesh.gameObject.SetActive(false);

                    foreach (MeshRenderer meshRenderer in meshes)
                        meshRenderer.gameObject.SetActive(true);

                    return;
            }

            if (isHideSystemRope)
            {
                foreach (MeshRenderer meshRenderer in meshes)
                    meshRenderer.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 그랩에 걸려있는 타겟을 가져옵니다.
        /// </summary>
        /// <returns></returns>
        public Transform GetGrabTarget()
        {
            return grabGroup.childCount > 0 ? grabGroup.GetChild(0) : null;
        }

        /// <summary>
        /// 인자로 넘어온 타겟을 그랩 자식으로 설정합니다.
        /// </summary>
        /// <param name="target"></param>
        public void SetGrabTarget(Transform target)
        {
            target.parent = grabGroup;

            //내부에서 위치 재조정
            Vector3 newPosition = target.localPosition;
            newPosition.z = 0.369f;
            target.localPosition = newPosition;
        }

        /// <summary>
        /// 연출용 손가락 끈끈이와 시스템용 손가락 끈끈이를 교체합니다.
        /// </summary>
        /// <param name="weaponType">weaponType가 System이면 시스템 로프를 활성화하고, weaponType이 normal이면 일반 로프를 활성화합니다.</param>
        public void SwapOriginWeapon(WeaponType weaponType)
        {
            if (weaponType == WeaponType.System)
            {
                normalWeaponMesh.gameObject.SetActive(false);
                attackWeaponMesh.gameObject.SetActive(false);

                foreach (MeshRenderer meshRenderer in meshes)
                    meshRenderer.gameObject.SetActive(true);
            }
            else
            {
                normalWeaponMesh.gameObject.SetActive(true);
                attackWeaponMesh.gameObject.SetActive(false);

                foreach (MeshRenderer meshRenderer in meshes)
                    meshRenderer.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 핸들 위치를 재설정하여 로프 길이를 초기화합니다.
        /// </summary>
        public void ResetHandlePosition()
        {
            startHandle.localPosition = Vector3.zero;
            endHandle.localPosition = new Vector3(0, 0, 1);
        }

        #region Get

        /// <summary>
        /// 로프에 타겟이 붙잡혀 있으면 true를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public bool HasGrabTarget()
        {
            return grabGroup.childCount > 0;
        }

        /// <summary>
        /// 시작 핸들의 트랜스폼 반환
        /// </summary>
        /// <returns></returns>
        public Transform GetStartHandleTransform()
        {
            return startHandle;
        }

        /// <summary>
        /// 끝 핸들의 트랜스폼 반환
        /// </summary>
        /// <returns></returns>
        public Transform GetEndHandleTransform()
        {
            return endHandle;
        }

        /// <summary>
        /// 그랩 트랜스폼 반환
        /// </summary>
        /// <returns></returns>
        public Transform GetGrabTransform()
        {
            return grabGroup;
        }

        /// <summary>
        /// EndHandle이 오브젝트와 닿았을 때 트리거 되는 이벤트
        /// </summary>
        /// <returns></returns>
        public IObservable<Collider> OnTriggerEnterTargetObservable()
        {
            return grabGroup.OnTriggerEnterAsObservable();
        }

        #endregion
    }
}