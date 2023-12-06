using Character.Controllers;
using Sirenix.OdinInspector;
using UnityEngine;
using Utility;

namespace Character.Animation
{
    public enum ECameraStyle
    {
        Velocity,
        Direction
    }

    //이 스크립트는 게임 오브젝트를 대상 컨트롤러의 속도 방향으로 돌립니다.
    public class TurnTowardPlayerController : MonoBehaviour
    {
        [field: SerializeField, Title("카메라 스타일")]
        public ECameraStyle cameraStyle { get; set; }

        [Title("VelocityStyle")]

        #region ECameraStyle.Velocity

        //이 게임 오브젝트가 컨트롤러의 속도로 회전하는 속도
        [DisableIf("@this.cameraStyle != ECameraStyle.Velocity")]
        public float turnSpeed = 500f;

        [DisableIf("@this.cameraStyle != ECameraStyle.Velocity")]
        //타겟 컨트롤러
        public PlayerController controller;

        //새 방향을 계산할 때 현재 컨트롤러 운동량(모멘텀)을 무시해야 하는지 여부
        [DisableIf("@this.cameraStyle != ECameraStyle.Velocity")]
        public bool ignoreControllerMomentum;

        #endregion

        [Title("DirectionStyle")]

        #region ECameraStyle.Direction

        [DisableIf("@this.cameraStyle == ECameraStyle.Velocity")]
        public Transform targetTransform;

        #endregion

        private Transform _parentTransform;
        private Transform _tr;

        //이 게임오브젝트의 (로컬) y축을 중심으로 하는 현재(로컬) 회전;
        private float _currentYRotation;

        //현재 방향과 목표 방향 사이의 각도가 'fallOffAngle' 아래로 떨어지면 'turnSpeed'가 점차 느려지고 결국 '0f'에 접근합니다.
        //이것은 회전에 스무딩 효과를 추가합니다.;
        private const float FallOffAngle = 90f;

        private void OnEnable() => _currentYRotation = transform.localEulerAngles.y;

        //Setup;
        private void Start()
        {
            _tr = transform;
            _parentTransform = _tr.parent;

            if (!targetTransform)
                DebugX.LogWarning("타겟 트랜스폼이 참조되지 않았습니다.");

            //컨트롤러가 할당되지 않은 경우 경고 발생
            if (controller == null)
            {
                enabled = false;
                DebugX.LogWarning("이 'TurnTowardPlayerController' 구성요소에 컨트롤러가 할당되지 않았습니다!");
            }
        }

        private void LateUpdate()
        {
            if (cameraStyle == ECameraStyle.Velocity)
                VelocityStyle();
            else
                DirectionStyle();
        }

        /// <summary>
        /// 플레이어의 Velocity기반으로 회전을 계산합니다.
        /// </summary>
        private void VelocityStyle()
        {
            //컨트롤러 속도 가져오기;
            Vector3 velocity = ignoreControllerMomentum
                ? controller.GetMovementVelocity()
                : controller.GetVelocity();

            //상위 변환의 '위쪽' 방향으로 정의된 평면에 속도 투영;
            velocity = Vector3.ProjectOnPlane(velocity, _parentTransform.up);

            const float magnitudeThreshold = 0.001f;

            //속도의 크기가 임계값보다 작으면 리턴합니다.
            if (velocity.magnitude < magnitudeThreshold)
                return;

            //속도 방향 정규화
            velocity.Normalize();

            //현재 'Forward' 벡터 가져오기
            Vector3 currentForward = _tr.forward;

            //속도와 ForwardDirection 사이의 각도 계산;
            float angleDifference = VectorMath.GetAngle(currentForward, velocity, _parentTransform.up);

            //각도 계수 계산;
            float factor = Mathf.InverseLerp(0f, FallOffAngle, Mathf.Abs(angleDifference));

            //이 프레임의 단계를 계산합니다.;
            float step = Mathf.Sign(angleDifference) * factor * Time.deltaTime * turnSpeed;

            //Clamp step;
            if (angleDifference < 0f && step < angleDifference)
                step = angleDifference;
            else if (angleDifference > 0f && step > angleDifference)
                step = angleDifference;

            //현재 y 각도에 단계 추가
            _currentYRotation += step;

            //Clamp y angle;
            if (_currentYRotation > 360f)
                _currentYRotation -= 360f;
            if (_currentYRotation < -360f)
                _currentYRotation += 360f;

            //Quaternion.Euler를 사용하여 변환 회전 설정
            _tr.localRotation = Quaternion.Euler(0f, _currentYRotation, 0f);
        }

        /// <summary>
        /// 플레이어가 바라보고 있는 방향 기준으로 회전을 계산합니다.
        /// </summary>
        private void DirectionStyle()
        {
            if (!targetTransform)
                return;

            //위쪽 및 앞으로 방향 계산;
            Vector3 up = _parentTransform.up;
            Vector3 forwardDirection = Vector3.ProjectOnPlane(targetTransform.forward, up).normalized;

            _tr.rotation = Quaternion.LookRotation(forwardDirection, up);
        }

        /// <summary>
        /// degree(도)값 방향으로 회전 시킵니다.
        /// </summary>
        /// <param name="direction"></param>
        public void SetRotation(Vector3 direction)
        {
            _tr.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        /// <summary>
        /// degree(도)값 방향으로 회전 시킵니다.
        /// </summary>
        /// <param name="angle"></param>
        public void SetRotation(float angle)
        {
            _currentYRotation = angle;
            _tr.localRotation = Quaternion.Euler(0f, _currentYRotation, 0f);
        }

        /// <summary>
        /// degree(도)값 방향으로 회전 시킵니다.
        /// </summary>
        /// <param name="direction"></param>
        public void SetRotationBody(Vector3 direction)
        {
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            rotation.x = 0;
            rotation.z = 0;
            _tr.rotation = rotation;
        }

        /// <summary>
        /// 앞을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetForward() => _tr.forward;
        
        /// <summary>
        /// 오일러 앵글을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetEulerAngle() => _tr.eulerAngles;
    }
}