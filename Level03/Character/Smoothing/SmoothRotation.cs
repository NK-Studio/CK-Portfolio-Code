using UnityEngine;

namespace Character.Smoothing
{
	//이 스크립트는 게임 오브젝트의 회전을 부드럽게 합니다.;
	public class SmoothRotation : MonoBehaviour {

		//회전 값이 복사 및 스무딩되는 대상 변환;
		public Transform Target;
		private Transform _tr;

		private Quaternion _currentRotation;

		//현재 회전이 목표 회전을 향해 부드럽게 되는 속도를 제어하는 속도;
		public float SmoothSpeed = 20f;

		//스무딩으로 인한 지연을 보상하기 위해 회전 값을 외삽할지 여부;
		public bool ExtrapolateRotation;

		/// <summary>
		/// 'UpdateType'은 스무딩 함수가 'Update' 또는 'LateUpdate'에서 호출되는지 여부를 제어합니다.
		/// </summary>
		public enum EUpdateType
		{
			Update,
			LateUpdate
		}
		
		public EUpdateType UpdateType;

		//Awake;
		private void Awake () {

			//대상이 선택되지 않은 경우 이 변환의 상위를 대상으로 선택하십시오.
			if(!Target)
				Target = transform.parent;

			Transform transform1 = transform;
			_tr = transform1;
			
			_currentRotation = transform1.rotation;
		}

		//OnEnable;
		private void OnEnable()
		{
			//마지막 회전에서 원치 않는 보간을 방지하기 위해 게임 개체가 다시 활성화될 때 현재 회전을 재설정합니다.
			ResetCurrentRotation();
		}

		private void Update () {
			if(UpdateType == EUpdateType.LateUpdate)
				return;
			
			SmoothUpdate();
		}

		private void LateUpdate () {
			if(UpdateType == EUpdateType.Update)
				return;
			
			SmoothUpdate();
		}

		private void SmoothUpdate()
		{
			//부드러운 전류 회전;
			_currentRotation = Smooth (_currentRotation, Target.rotation, SmoothSpeed);

			//Set rotation;
			_tr.rotation = _currentRotation;
		}

		/// <summary>
		/// 'smoothTime'을 기준으로 목표 회전 방향으로 회전을 부드럽게 합니다.
		/// </summary>
		/// <param name="currentRotation"></param>
		/// <param name="targetRotation"></param>
		/// <param name="smoothSpeed"></param>
		/// <returns></returns>
		private Quaternion Smooth(Quaternion currentRotation, Quaternion targetRotation, float smoothSpeed)
		{
			//'extrapolateRotation'이 'true'로 설정된 경우 새 대상 회전을 계산합니다.;
			if (ExtrapolateRotation && Quaternion.Angle(currentRotation, targetRotation) < 90f) {
				Quaternion difference = targetRotation * Quaternion.Inverse (currentRotation);
				targetRotation *= difference;
			}

			//Slerp rotation and return;
			return Quaternion.Slerp (currentRotation, targetRotation, Time.deltaTime * smoothSpeed);
		}

		/// <summary>
		/// 저장된 회전을 재설정하고 대상의 회전과 일치하도록 이 게임 개체를 회전합니다.
		/// 타겟이 방금 회전되었고 보간이 발생하지 않아야 하는 경우(즉시 회전) 이 함수를 호출합니다.
		/// </summary>
		public void ResetCurrentRotation()
		{
			_currentRotation = Target.rotation;
		}
								
	}
}