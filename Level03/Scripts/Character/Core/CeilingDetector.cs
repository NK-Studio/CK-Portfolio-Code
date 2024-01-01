using Sirenix.OdinInspector;
using UnityEngine;

//이 (선택 사항) 구성 요소는 'PlayerBehaviour'가 연결된 게임 개체에 추가할 수 있습니다.
//내부 물리 계산에 의해 감지된 모든 충돌을 지속적으로 확인합니다.
//충돌이 "천장 충돌"(표면 법선 기준) 캐릭터로 적합하면 결과가 저장됩니다.
//그런 다음 'PlayerBehaviour'는 해당 정보를 사용하여 천장 충돌에 반응할 수 있습니다. 
namespace Character.Core
{
	public class CeilingDetector : MonoBehaviour {
		private bool _ceilingWasHit;

		//천정 히트에 대한 각도 제한;
		public float CeilingAngleLimit = 10f;

		//천장 감지 방법;
		//'OnlyCheckFirstContact' - 첫 번째 충돌 접촉만 확인하십시오. 이 옵션은 다른 두 옵션보다 약간 빠르지만 정확도는 떨어집니다.
		//'CheckAllContacts' - 모든 목록을 확인하고 한 목록만 자격이 있는 한 천장 히트를 등록합니다.
		//'CheckAverageOfAllContacts' - 확인할 평균 표면 법선을 계산합니다.
		public enum ECeilingDetectionMethod
		{
			OnlyCheckFirstContact,
			CheckAllContacts,
			CheckAverageOfAllContacts
		}

		[InfoBox("OnlyCheckFirstContact - 첫 번째 충돌 접촉만 확인하십시오. 이 옵션은 다른 두 옵션보다 약간 빠르지만 정확도는 떨어집니다.\n" +
		         "CheckAllContacts - 모든 연락처를 확인하고 한 연락처만 자격이 있는 한 천장 히트를 등록합니다.\n" +
		         "CheckAverageOfAllContacts - 확인할 평균 표면 법선을 계산합니다.")]
		public ECeilingDetectionMethod CeilingDetectionMethod;

		//활성화하면 적중 위치와 적중 법선을 표시하기 위해 디버그 정보를 그립니다.
		public bool IsInDebugMode;
		
		//디버그 정보가 화면에 표시되는 시간
		private const float DebugDrawDuration = 2.0f;

		private Transform _tr;

		private void Awake()
		{
			_tr = transform;
		}

		private void OnCollisionEnter(Collision collision)
		{
			CheckCollisionAngles(collision);	
		}

		private void OnCollisionStay(Collision collision)
		{
			CheckCollisionAngles(collision);	
		}

		/// <summary>
		/// 주어진 충돌이 천장 충돌에 해당하는지 확인합니다.
		/// </summary>
		/// <param name="collision"></param>
		private void CheckCollisionAngles(Collision collision)
		{
			float angle = 0f;

			if(CeilingDetectionMethod == ECeilingDetectionMethod.OnlyCheckFirstContact)
			{
				//히트 노멀과 캐릭터 사이의 각도를 계산합니다.
				angle = Vector3.Angle(-_tr.up, collision.contacts[0].normal);

				//각도가 천장 각도 제한보다 작으면 천장 히트를 등록합니다.
				if(angle < CeilingAngleLimit)
					_ceilingWasHit = true;

				//디버그 정보를 그립니다.
				if(IsInDebugMode)
					Debug.DrawRay(collision.contacts[0].point, collision.contacts[0].normal, Color.red, DebugDrawDuration);
			}
			if(CeilingDetectionMethod == ECeilingDetectionMethod.CheckAllContacts)
			{
				for(int i = 0; i < collision.contacts.Length; i++)
				{
					//히트 노멀과 캐릭터 사이의 각도를 계산합니다.
					angle = Vector3.Angle(-_tr.up, collision.contacts[i].normal);

					//각도가 천장 각도 제한보다 작으면 천장 히트를 등록합니다.
					if(angle < CeilingAngleLimit)
						_ceilingWasHit = true;

					//디버그 정보를 그립니다.
					if(IsInDebugMode)
						Debug.DrawRay(collision.contacts[i].point, collision.contacts[i].normal, Color.red, DebugDrawDuration);
				}
			}
			if(CeilingDetectionMethod == ECeilingDetectionMethod.CheckAverageOfAllContacts)
			{
				for(int i = 0; i < collision.contacts.Length; i++)
				{
					//히트 노멀과 캐릭터 사이의 각도를 계산하고 총 각도 수에 추가합니다.
					angle += Vector3.Angle(-_tr.up, collision.contacts[i].normal);

					//디버그 정보를 그립니다.
					if(IsInDebugMode)
						Debug.DrawRay(collision.contacts[i].point, collision.contacts[i].normal, Color.red, DebugDrawDuration);
				}

				//평균 각도가 천정 각도 제한보다 작으면 천정 히트를 등록합니다.
				if(angle/collision.contacts.Length < CeilingAngleLimit)
					_ceilingWasHit = true;
			}	
		}

		/// <summary>
		/// 마지막 프레임 동안 상한선에 도달했는지 여부를 반환합니다.
		/// </summary>
		/// <returns></returns>
		public bool HitCeiling()
		{
			return _ceilingWasHit;
		}

		/// <summary>
		/// 실링 히트 플래그를 재설정합니다.
		/// </summary>
		public void ResetFlags()
		{
			_ceilingWasHit = false;
		}
	}
}
