using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Utility;
using Zenject;

namespace System
{
    public class OffScreenSystem : MonoBehaviour
    {
        [Tooltip("기본 캔버스(OFFScreenCanvas)이 배치되어 있어야합니다."), FoldoutGroup("캔버스"), SerializeField]
        private bool defaultCanvas = true;

        [Tooltip("OFFScreen 매니저에 등록하여 관리할 것인가?"), FoldoutGroup("매니저"), SerializeField]
        private bool useRegisterToManager;

        private OffScreenSystemManager _offScreenSystemManager;
        [Inject] private DiContainer _container;

        [SerializeField, HideIf("defaultCanvas"), FoldoutGroup("캔버스")]
        private string customCanvasName;

        [Tooltip("이 컴포넌트를 가진 오브젝트를 타겟으로 합니다."), FoldoutGroup("타겟"), SerializeField]
        private bool selfTarget = true;

        [SerializeField, HideIf("selfTarget"), FoldoutGroup("타겟")]
        private Transform target;

        [SerializeField, FoldoutGroup("UI 지정"), Tooltip("포인터 UI를 자동으로 씬에 배치합니다. (프리팹으로 참조해야합니다.)")]
        private bool autoTargetUI = true;

        [SerializeField, FoldoutGroup("UI 지정"), HideIf("autoTargetUI")]
        private Image pointer;

        [SerializeField, FoldoutGroup("UI 지정"), ShowIf("autoTargetUI")]
        private GameObject pointerPrefab;

        [SerializeField, FoldoutGroup("스프라이트")]
        private Sprite offSprite;

        [SerializeField, FoldoutGroup("스프라이트")]
        private Sprite possibleSprite;

        [SerializeField, FoldoutGroup("스프라이트")]
        private Sprite normalSprite;

        [SerializeField, FoldoutGroup("스프라이트")]
        private Sprite disableSprite;

        [SerializeField, FoldoutGroup("옵션"), Tooltip("포인터가 화면 경계부분에서 패딩을 받습니다.")]
        private Vector2 padding = new(100, 100f);

        [field: SerializeField, FoldoutGroup("옵션")]
        public Vector3 offset = Vector3.zero;

        [SerializeField, FoldoutGroup("옵션"), Range(0f, 360f), Tooltip("포인터를 회전 시킵니다.")]
        private float offSetRotation;

        [field: SerializeField, FoldoutGroup("옵션"), Tooltip("훅 상태를 변경합니다.")]
        public EHookState HookState { get; private set; }

        [field: SerializeField, FoldoutGroup("옵션"), Tooltip("회전을 적용합니다.")]
        public bool useRotation = true;

        [field: SerializeField, FoldoutGroup("옵션"), Tooltip("화면에 보일 때만 UI를 표시하며 OFFScreen기능을 제거합니다.")]
        public bool OnScreenModeOnly;

        private Canvas _canvas;
        private Camera _camera;

        private void Awake()
        {
            if (defaultCanvas)
                _canvas = GameObject.Find("OFFScreenCanvas").GetComponent<Canvas>();
            else
                _canvas = GameObject.Find(customCanvasName).GetComponent<Canvas>();

            _camera = Camera.main;

            if (selfTarget)
                target = transform;
        }

        private void Start()
        {
            if (_canvas)
                if (autoTargetUI)
                    pointer = Instantiate(pointerPrefab, _canvas.transform).GetComponent<Image>();

            if (useRegisterToManager)
            {
                _offScreenSystemManager = _container.Resolve<OffScreenSystemManager>();

                if (_offScreenSystemManager)
                    _offScreenSystemManager.Register(this);
            }
        }

        private void LateUpdate()
        {
            if (!pointer)
            {
                DebugX.LogWarning("pointer != null");
                Destroy(gameObject);
                return;
            }
            
            if (!target)
            {
                pointer.gameObject.SetActive(false);
                return;
            }

            pointer.gameObject.SetActive(true);

            Vector3 position = target.position;

            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

            //스크린 포인트 좌표계로 변환합니다.
            Vector3 targetWorldPoint = _camera.WorldToScreenPoint(position);

            //Z가 기하급수적으로 확장되는 것을 방지합니다.
            if (targetWorldPoint.z > 1) targetWorldPoint.z = 1f;

            //스크린 포인트를 월드 좌표로 변환합니다. (단위 확장)
            Vector3 targetToWorldScreen = targetWorldPoint - screenCenter;

            //포인터를 타겟을 가리키는 각도를 구합니다.
            float calculateAngle =
                ComputeTargetAngle(selfTarget ? transform.position : target.position, targetToWorldScreen);

            float onScreenSnapOffsetX = 22 - Screen.width * 0.024f;
            float onScreenSnapOffsetY = 0f;

            Vector2 sizeDelta = pointer.rectTransform.sizeDelta;
            float scaleFactor = _canvas.scaleFactor;

            //스크린 안쪽 범위
            //targetWorldPoint.z > 0 은 타겟이 카메라 앞이라면,
            bool isOnScreen = targetWorldPoint.z > 0f &&
                              targetWorldPoint.x / scaleFactor > sizeDelta.x * 0.5f &&
                              targetWorldPoint.x < Screen.width - sizeDelta.x * 0.5f + onScreenSnapOffsetX &&
                              targetWorldPoint.y / scaleFactor > sizeDelta.y * 0.5f &&
                              targetWorldPoint.y < Screen.height - sizeDelta.y * 0.5f + onScreenSnapOffsetY;

            //OnScreen,
            if (isOnScreen)
            {
                //크로스헤어 UI로 변경
                if (HookState == EHookState.Possible)
                    pointer.sprite = possibleSprite;

                if (HookState == EHookState.Occluded)
                    pointer.sprite = disableSprite;

                if (HookState is EHookState.DistanceImpossible or EHookState.Impossible)
                    pointer.sprite = normalSprite;

                if (HookState == EHookState.Hide)
                {
                    Color pointerColor = pointer.color;
                    pointerColor.a = 0;
                    pointer.color = pointerColor;
                }
                else
                {
                    Color pointerColor = pointer.color;
                    pointerColor.a = 1;
                    pointer.color = pointerColor;
                }

                //포인터 UI 회전 값 초기화
                pointer.rectTransform.localEulerAngles = Vector3.zero;

                //타켓 위치로 포인트 트래킹
                pointer.rectTransform.position = targetWorldPoint + offset;
            }
            //OffScreen,
            else
            {
                if (OnScreenModeOnly)
                {
                    Color pointerColor = pointer.color;
                    pointerColor.a = 0;
                    pointer.color = pointerColor;
                }
                else
                {
                    if (HookState == EHookState.Hide)
                    {
                        Color pointerColor = pointer.color;
                        pointerColor.a = 0;
                        pointer.color = pointerColor;
                    }
                    else
                    {
                        Color pointerColor = pointer.color;
                        pointerColor.a = 1;
                        pointer.color = pointerColor;
                    }
                }


                pointer.sprite = offSprite;

                //타겟이 카메라 뒤로 가려지면 true
                bool isBehind = targetWorldPoint.z < 0f;

                //포인터 포지션 계산
                Vector2 computePosition = ComputePointerPosition(isBehind);

                //뷰포트 좌표계를 스크린 좌표계로 변환
                Vector2 targetScreenPosition = new Vector2((Screen.width - padding.x) * computePosition.x,
                    (Screen.height - padding.y) * computePosition.y);

                //좌표계를 다시 원래대로 돌려놓음
                targetScreenPosition += new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

                //타켓 위치로 포인트 트래킹
                pointer.rectTransform.position = targetScreenPosition;

                //타켓 회전 처리
                if (useRotation)
                    pointer.transform.localEulerAngles = new Vector3(0, 0, calculateAngle + offSetRotation);
            }
        }

        #region Set

        /// <summary>
        /// 타겟을 바라보도록 UI회전을 적용합니다.
        /// </summary>
        /// <param name="targetPosition">바라볼 타겟</param>
        /// <param name="targetToWorldScreen"></param>
        private float ComputeTargetAngle(Vector3 targetPosition, Vector3 targetToWorldScreen)
        {
            Transform cameraTransform = _camera.transform;

            //targetPosition은 UI 월드 좌표화된 타겟이다.
            Vector3 targetToCameraDirection = (cameraTransform.position - targetPosition).normalized;

            // 대상이 카메라의 뒤에 있는지 또는 앞에 있는지 알 수 있습니다.
            Vector3 cameraForward = cameraTransform.forward;
            float angle = Vector3.Angle(-cameraForward, targetToCameraDirection);

            // 화면 중앙과 대상 사이의 각도를 가져옵니다.
            float rotation = Mathf.Atan2(targetToWorldScreen.y, targetToWorldScreen.x) * Mathf.Rad2Deg;

            //회전을 계산합니다.
            float computedAngle = rotation + (angle > 90f ? 180f : 0f);

            if (computedAngle < 0)
                computedAngle += 360;

            return computedAngle;
        }

        /// <summary>
        /// 타겟을 재설정합니다.
        /// </summary>
        /// <param name="newTarget"></param>
        public void SetTarget(Transform newTarget) => target = newTarget;

        /// <summary>
        /// 포인터 UI를 재설정합니다.
        /// </summary>
        /// <param name="newPointer"></param>
        public void SetPointer(Image newPointer) => pointer = newPointer;

        /// <summary>
        /// ArrowSprite의 Sprite 재설정합니다.
        /// </summary>
        /// <param name="newArrowSprite"></param>
        public void SetArrowSprite(Sprite newArrowSprite) => offSprite = newArrowSprite;

        /// <summary>
        /// crossSprite의 Sprite를 재설정합니다.
        /// </summary>
        /// <param name="newCrossSprite"></param>
        public void SetCrossSprite(Sprite newCrossSprite) => normalSprite = newCrossSprite;

        /// <summary>
        /// 패딩 사이즈를 재설정합니다.
        /// </summary>
        /// <param name="newPaddingSize"></param>
        public void SetPaddingSize(Vector2 newPaddingSize) => padding = newPaddingSize;


        /// <summary>
        /// UI 상태를 설정합니다.
        /// </summary>
        /// <param name="active"></param>
        public void SetActive(EHookState active) => HookState = active;

        #endregion

        #region Get

        /// <summary>
        /// 포인터 위치 좌표를 계산합니다.
        /// </summary>
        /// <returns></returns>
        public Vector2 ComputePointerPosition(bool behind)
        {
            float x;
            Vector2 result;

            //타겟을 뷰포트 좌표계로 변환
            Vector3 targetOnViewportPoint =
                _camera.WorldToViewportPoint(selfTarget ? transform.position : target.position) -
                new Vector3(0.5f, 0.5f, 0f);

            //타겟이 카메라 뒤에 있다면, 좌표계를 뒤집습니다.
            targetOnViewportPoint = behind ? -targetOnViewportPoint : targetOnViewportPoint;

            //밑변 / 높이 -> sin/cos 연산
            float m = targetOnViewportPoint.y / targetOnViewportPoint.x;

            //화면 왼쪽
            if (targetOnViewportPoint.x < 0)
            {
                // 화면 위
                if (targetOnViewportPoint.y > 0)
                {
                    x = 0.5f / m;
                    // 화면 밖에서, 우리는 왼쪽 위 위치를 얻고 있다.
                    result = x < -0.5f ? new Vector2(-0.5f, -0.5f * m) : new Vector2(0.5f / m, 0.5f);
                }
                // 화면 아래
                else
                {
                    x = -0.5f / m;
                    // 화면 밖에서, 우리는 왼쪽 아래 위치를 얻고 있다.
                    result = x < -0.5f ? new Vector2(-0.5f, -0.5f * m) : new Vector2(-0.5f / m, -0.5f);
                }
            }
            // 화면 오른쪽
            else
            {
                // 화면 위
                if (targetOnViewportPoint.y > 0)
                {
                    x = 0.5f / m;
                    // 화면 밖에서, 우리는 오른쪽 위 위치를 얻고 있다.
                    result = x > 0.5f ? new Vector2(0.5f, 0.5f * m) : new Vector2(0.5f / m, 0.5f);
                }
                // 화면 아래
                else
                {
                    // 화면 밖에서, 우리는 오른쪽 아래 위치를 얻고 있다.
                    x = -0.5f / m;
                    result = x > 0.5f ? new Vector2(0.5f, 0.5f * m) : new Vector2(-0.5f / m, -0.5f);
                }
            }

            return result;
        }

        /// <summary>
        /// 타겟을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Transform GetTarget() => target;

        /// <summary>
        /// 훅 상태를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public EHookState GetHookState() => HookState;

        /// <summary>
        /// 포인터를 반환합니다.
        /// </summary>
        public Image GetPointer() => pointer;

        #endregion
    }
}