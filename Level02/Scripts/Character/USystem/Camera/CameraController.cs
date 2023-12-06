using Input.Camera;
using UnityEngine;
using Utility;

namespace Character.USystem.Camera
{
    //이 스크립트는 사용자 입력에 따라 게임 오브젝트를 회전합니다.
    //x축(수직)을 중심으로 한 회전은 'upperVerticalLimit' 및 'lowerVerticalLimit'을 설정하여 제한할 수 있습니다.
    public class CameraController : MonoBehaviour
    {
        //현재 회전 값(도)
        private float _currentXAngle;
        private float _currentYAngle;

        //수직 회전에 대한 상한 및 하한(도 단위)(게임 오브젝트의 로컬 x축을 따라)
        [Range(0f, 90f)] public float upperVerticalLimit = 60f;

        [Range(0f, 90f)] public float lowerVerticalLimit = 60f;

        //보간을 위해 이전 회전 값을 저장하는 변수
        private float _oldHorizontalInput;
        private float _oldVerticalInput;

        //카메라 회전 속도; 
        public float cameraSpeed = 250f;

        //카메라 회전 값을 부드럽게 할지 여부;
        public bool smoothCameraRotation;

        //이 값은 이전 카메라 회전 각도가 새 카메라 회전 각도를 향해 얼마나 부드럽게 보간되는지 제어합니다.
        //이 값을 '50f'(또는 그 이상)로 설정하면 스무딩이 전혀 발생하지 않습니다.
        //이 값을 '1f'(또는 그 이하)로 설정하면 매우 눈에 띄게 평활화됩니다.
        //대부분의 경우 '25f' 값이 권장됩니다.
        [Range(1f, 50f)] public float cameraSmoothingFactor = 25f;

        //현재 향하는 방향 및 위쪽 방향을 저장하기 위한 변수
        private Vector3 _facingDirection;
        private Vector3 _upwardsDirection;

        //변환 및 카메라 구성 요소에 대한 참조;
        private Transform _tr;
        private UnityEngine.Camera _cam;
        private CameraMouseInput _cameraInput;

        [SerializeField, Header("마우스 고정"), Tooltip("마우스를 못움직이도록 고정합니다.")]
        private bool mouseLock = true;

        //설정 참조.
        private void Awake()
        {
            _tr = transform;
            _cam = GetComponent<UnityEngine.Camera>();
            _cameraInput = GetComponent<CameraMouseInput>();

            if (!_cameraInput)
                DebugX.LogWarning("이 게임 개체에 연결된 카메라 입력 스크립트가 없습니다.", gameObject);

            //이 게임 객체에 카메라 구성 요소가 연결되지 않은 경우 변환의 자식을 검색합니다.;
            if (!_cam)
                _cam = GetComponentInChildren<UnityEngine.Camera>();

            //각도 변수를 이 변환의 현재 회전 각도로 설정
            Quaternion localRotation = _tr.localRotation;
            _currentXAngle = localRotation.eulerAngles.x;
            _currentYAngle = localRotation.eulerAngles.y;

            //카메라 회전 코드를 한 번 실행하여 직면 및 위쪽 방향 계산
            RotateCamera(0f, 0f);

            Setup();

            if (mouseLock)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        public void SetActiveCursor(bool active)
        {
            if (active)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.Confined;
            }
        }

        //이 함수는 Awake() 직후에 호출됩니다. 스크립트를 상속하여 재정의할 수 있습니다.
        protected virtual void Setup()
        {
        }

        private void Update()
        {
            HandleCameraRotation();
        }

        //사용자 입력 받기 및 카메라 회전 처리
        //이 메서드는 이 기본 클래스에서 파생된 클래스에서 재정의하여 카메라 동작을 수정할 수 있습니다.
        protected virtual void HandleCameraRotation()
        {
            if (!_cameraInput)
                return;

            //입력 값 가져오기
            float inputHorizontal = _cameraInput.GetHorizontalCameraInput();
            float inputVertical = _cameraInput.GetVerticalCameraInput();

            RotateCamera(inputHorizontal, inputVertical);
        }

        //Rotate camera; 
        private void RotateCamera(float newHorizontalInput, float newVerticalInput)
        {
            if (smoothCameraRotation)
            {
                _oldHorizontalInput = Mathf.Lerp(_oldHorizontalInput, newHorizontalInput,
                    Time.deltaTime * cameraSmoothingFactor);
                _oldVerticalInput = Mathf.Lerp(_oldVerticalInput, newVerticalInput,
                    Time.deltaTime * cameraSmoothingFactor);
            }
            else
            {
                //이전 입력을 직접 교체
                _oldHorizontalInput = newHorizontalInput;
                _oldVerticalInput = newVerticalInput;
            }

            //카메라 각도에 입력 추가
            _currentXAngle += _oldVerticalInput * cameraSpeed * Time.deltaTime;
            _currentYAngle += _oldHorizontalInput * cameraSpeed * Time.deltaTime;

            //클램프 수직 회전
            _currentXAngle = Mathf.Clamp(_currentXAngle, -upperVerticalLimit, lowerVerticalLimit);

            UpdateRotation();
        }

        /// <summary>
        /// x 및 y 각도를 기반으로 카메라 회전 업데이트
        /// </summary>
        private void UpdateRotation()
        {
            //나중을 위해 'facingDirection' 및 'upwardsDirection' 저장
            _facingDirection = _tr.forward;
            _upwardsDirection = _tr.up;

            Quaternion localRotation = Quaternion.Euler(new Vector3(_currentXAngle, _currentYAngle, 0));
            _tr.localRotation = localRotation;
        }

        /// <summary>
        /// x 및 y 각도를 직접 설정
        /// </summary>
        /// <param name="xAngle"></param>
        /// <param name="yAngle"></param>
        public void SetRotationAngles(float xAngle, float yAngle)
        {
            _currentXAngle = xAngle;
            _currentYAngle = yAngle;

            UpdateRotation();
        }

        /// <summary>
        /// 씬의 월드 위치를 가리키는 회전 방향으로 카메라를 회전합니다.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="lookSpeed"></param>
        public void RotateTowardPosition(Vector3 position, float lookSpeed)
        {
            //대상 모양 벡터 계산;
            Vector3 direction = (position - _tr.position);

            RotateTowardDirection(direction, lookSpeed);
        }

        /// <summary>
        /// 씬에서 뷰 벡터를 향해 카메라 회전
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="lookSpeed"></param>
        public void RotateTowardDirection(Vector3 direction, float lookSpeed)
        {
            //방향 정규화;
            direction.Normalize();

            //대상 모양 벡터를 이 변환의 로컬 공간으로 변환합니다.;
            Transform parent = _tr.parent;
            direction = parent.InverseTransformDirection(direction);

            //(로컬) 현재 보기 벡터 계산; 
            Vector3 currentLookVector = GetAimingDirection();
            currentLookVector = parent.InverseTransformDirection(currentLookVector);

            //x 각도 차이 계산
            float xAngleDifference = VectorMath.GetAngle(new Vector3(0f, currentLookVector.y, 1f),
                new Vector3(0f, direction.y, 1f), Vector3.right);

            //y 각도 차이 계산
            currentLookVector.y = 0f;
            direction.y = 0f;
            float yAngleDifference = VectorMath.GetAngle(currentLookVector, direction, Vector3.up);

            //더 나은 클램핑을 위해 각도 값을 Vector2 변수로 전환;
            Vector2 currentAngles = new(_currentXAngle, _currentYAngle);
            Vector2 angleDifference = new(xAngleDifference, yAngleDifference);

            //정규화된 방향 계산
            float angleDifferenceMagnitude = angleDifference.magnitude;

            if (angleDifferenceMagnitude == 0f)
                return;

            Vector2 angleDifferenceDirection = angleDifference / angleDifferenceMagnitude;

            //오버슈팅 확인
            if (lookSpeed * Time.deltaTime > angleDifferenceMagnitude)
                currentAngles += angleDifferenceDirection * angleDifferenceMagnitude;
            else
                currentAngles += angleDifferenceDirection * lookSpeed * Time.deltaTime;

            //새로운 각도 설정;
            _currentYAngle = currentAngles.y;

            //클램프 수직 회전;
            _currentXAngle = Mathf.Clamp(currentAngles.x, -upperVerticalLimit, lowerVerticalLimit);

            UpdateRotation();
        }

        public float GetCurrentXAngle() => _currentXAngle;

        public float GetCurrentYAngle() => _currentYAngle;

        /// <summary>
        /// 수직 회전 없이 카메라가 향하고 있는 방향을 반환합니다.
        /// 이 벡터는 이동 관련 목적으로 사용해야 합니다(예: 앞으로 이동).
        /// </summary>
        /// <returns></returns>
        public Vector3 GetFacingDirection() => _facingDirection;

        /// <summary>
        /// Y를 제거한 카메라가 향하고 있는 방향을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetForwardDirection()
        {
            Vector3 nextForward = _tr.forward;
            nextForward.y = 0;
            return nextForward;
        }

        /// <summary>
        /// 이 게임 객체의 '정방향' 벡터를 반환합니다.
        /// 이 벡터는 카메라가 "조준"하는 방향을 가리키며 발사체 또는 레이캐스트를 인스턴스화하는 데 사용할 수 있습니다.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetAimingDirection() => _tr.forward;

        /// <summary>
        /// 이 게임 객체의 '오른쪽' 벡터를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetStrafeDirection() => _tr.right;

        /// <summary>
        /// 이 게임 객체의 '위쪽' 벡터를 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetUpDirection() => _upwardsDirection;
    }
}