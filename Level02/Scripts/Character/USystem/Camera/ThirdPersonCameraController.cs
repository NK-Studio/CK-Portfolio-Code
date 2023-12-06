using Character.Controllers;
using UnityEngine;
using Utility;

namespace Character.USystem.Camera
{
	//이 스크립트는 3인칭 카메라를 사용하는 게임을 위한 일반 'CameraController' 스크립트의 약간 더 전문화된 버전입니다.
	//'turnCameraTowardMovementDirection'을 활성화하면 카메라가 연결된 게임 오브젝트의 현재 이동 방향을 향해 점차적으로 회전합니다.
	//이 회전의 속도와 속도는 'maximumMovementSpeed' 및 'cameraTurnSpeed'를 사용하여 제어할 수 있습니다.
	public class ThirdPersonCameraController : CameraController {

		//카메라가 컨트롤러의 이동 방향으로 회전하는지 여부입니다.
		public bool turnCameraTowardMovementDirection = true;

		public PlayerController controller;

		//이 게임 개체의 최대 예상 이동 속도입니다.
		//이 값은 이 게임 개체가 달성할 수 있는 최대 이동 속도로 설정해야 합니다.
		//현재 이동 속도가 'maximumMovementSpeed'에 가까울수록 카메라가 더 빨리 회전합니다.
		//결과적으로 게임 오브젝트가 느리게 움직이면(즉, 캐릭터의 경우 "달리기" 대신 "걷기") 카메라도 느리게 회전합니다.
		public float maximumMovementSpeed = 7f;

		//카메라가 이동 방향으로 회전하는 일반적인 속도입니다.
		public float cameraTurnSpeed = 120f;

		protected override void Setup()
		{
			if(controller == null)
				DebugX.LogWarning("이 스크립트에 할당된 컨트롤러 참조가 없습니다.");
		}

		protected override void HandleCameraRotation ()
		{
			//일반 카메라 회전 코드 실행
			base.HandleCameraRotation ();

			if(!controller)
				return;

			if(turnCameraTowardMovementDirection && controller)
			{
				//컨트롤러 속도 가져오기;
				Vector3 controllerVelocity = controller.GetVelocity();

				RotateTowardsVelocity(controllerVelocity, cameraTurnSpeed);
			}
		}

		/// <summary>
		/// 이 게임 오브젝트의 위쪽 벡터를 중심으로 '_speed'의 속도로 '_direction'으로 카메라를 회전합니다.
		/// </summary>
		/// <param name="velocity"></param>
		/// <param name="speed"></param>
		public void RotateTowardsVelocity(Vector3 velocity, float speed)
		{
			//원치 않는 방향 구성 요소 제거
			velocity = VectorMath.RemoveDotVector(velocity, GetUpDirection());
			
			//현재 방향과 새로운 방향의 각도 차이 계산
			float angle = VectorMath.GetAngle(GetFacingDirection(), velocity, GetUpDirection());

			//각도의 부호를 계산하십시오.
			float sign = Mathf.Sign (angle);

			//최종 각도 차이 계산;
			float finalAngle =  Time.deltaTime * speed * sign * Mathf.Abs(angle/90f);

			//각도가 90도보다 크면 최종 각도 차이를 다시 계산합니다.
			if(Mathf.Abs(angle) > 90f)
				finalAngle = Time.deltaTime * speed * sign * ((Mathf.Abs (180f - Mathf.Abs(angle)))/90f);

			//계산된 각도 오버슈트 확인
			if(Mathf.Abs (finalAngle) > Mathf.Abs (angle))
				finalAngle = angle;

			//'maximumMovementSpeed'와 비교하여 이동 속도를 고려합니다.
			finalAngle *= Mathf.InverseLerp(0f, maximumMovementSpeed, velocity.magnitude);
		    
            SetRotationAngles(GetCurrentXAngle(), GetCurrentYAngle() + finalAngle);
		}	
	}
}
