using UnityEngine;
using Utility;


//이 스크립트는 게임 오브젝트를 대상 컨트롤러의 속도 방향으로 돌립니다.
namespace Enemys
{
    public class TurnTowardEnemyController : MonoBehaviour
    {
        public EnemyController enemyController;
    
        //이 게임 오브젝트가 컨트롤러의 속도로 회전하는 속도입니다.
        public float turnSpeed = 500f;

        private Transform _parentTransform;
        private Transform _tr;

        //이 게임오브젝트의 (로컬) y축을 중심으로 하는 현재(로컬) 회전;
        private float _currentYRotation;

        //현재 방향과 목표 방향 사이의 각도가 'fallOffAngle' 아래로 떨어지면 'turnSpeed'가 점차 느려지고 결국 '0f'에 접근합니다.
        //이것은 회전에 스무딩 효과를 추가합니다.;
        private const float FallOffAngle = 90f;
    
        private void Awake()
        {
            _tr = transform;
            _parentTransform = _tr.parent;
        }

        private void LateUpdate()
        {
            //컨트롤러 속도 가져오기
            Vector3 velocity = enemyController.GetVelocity();

            //상위 변환의 '위쪽' 방향으로 정의된 평면에 속도를 투영합니다.
            velocity = Vector3.ProjectOnPlane(velocity, _parentTransform.up);

            const float magnitudeThreshold = 0.001f;

            //속도의 크기가 임계값보다 작으면 반환합니다.
            if (velocity.magnitude < magnitudeThreshold)
                return;

            //속도 방향을 정규화합니다.
            velocity.Normalize();

            //현재 '순방향' 벡터를 가져옵니다.
            Vector3 currentForward = _tr.forward;

            //속도와 순방향 사이의 (부호 있는) 각도를 계산합니다.
            float angleDifference = VectorMath.GetAngle(currentForward, velocity, _parentTransform.up);

            //각도 계수를 계산합니다.
            float factor = Mathf.InverseLerp(0f, FallOffAngle, Mathf.Abs(angleDifference));

            //이 프레임의 단계를 계산합니다.
            float step = Mathf.Sign(angleDifference) * factor * Time.deltaTime * turnSpeed;
        
            if (angleDifference < 0f && step < angleDifference)
                step = angleDifference;
            else if (angleDifference > 0f && step > angleDifference)
                step = angleDifference;

            //현재 y 각도에 단계를 추가합니다.
            _currentYRotation += step;

            //Clamp y 각도;
            if (_currentYRotation > 360f)
                _currentYRotation -= 360f;
            if (_currentYRotation < -360f)
                _currentYRotation += 360f;

            //Quaternion.Euler를 사용하여 변환 회전을 설정합니다.
            _tr.localRotation = Quaternion.Euler(0f, _currentYRotation, 0f);
        }

        private void OnEnable()
        {
            _currentYRotation = transform.localEulerAngles.y;
        }
    }
}