using UnityEngine;

namespace Utility
{
	//이것은 벡터(플로트 값 뿐만 아니라)를 계산하고 수정하기 위한 다양한 방법을 제공하는 정적 도우미 클래스입니다.
	public static class VectorMath {

		/// <summary>
		/// '_vector_1'과 '_vector_2' 사이의 부호 있는 각도(-180 ~ +180 범위)를 계산합니다.
		/// </summary>
		/// <param name="vector1"></param>
		/// <param name="vector2"></param>
		/// <param name="planeNormal"></param>
		/// <returns></returns>
		public static float GetAngle(Vector3 vector1, Vector3 vector2, Vector3 planeNormal)
		{
			//각도 및 기호 계산;
			float angle = Vector3.Angle(vector1,vector2);
			float sign = Mathf.Sign(Vector3.Dot(planeNormal,Vector3.Cross(vector1,vector2)));

			//각도와 기호 결합;
			float signedAngle = angle * sign;

			return signedAngle;
		}

		/// <summary>
		/// '_direction'(즉, 내적)과 같은 방향을 가리키는 벡터 부분의 길이를 반환합니다.
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="direction"></param>
		/// <returns></returns>
		public static float GetDotProduct(Vector3 vector, Vector3 direction)
		{
			//Normalize vector if necessary;
			if(direction.sqrMagnitude != 1)
				direction.Normalize();
				
			float length = Vector3.Dot(vector, direction);

			return length;
		}
		
		/// <summary>
		/// '_direction'과 같은 방향을 가리키는 벡터의 모든 부분을 제거합니다.
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="direction"></param>
		/// <returns></returns>
		public static Vector3 RemoveDotVector(Vector3 vector, Vector3 direction)
		{
			//필요한 경우 벡터 정규화
			if(direction.sqrMagnitude != 1)
				direction.Normalize();
			
			float amount = Vector3.Dot(vector, direction);
			
			vector -= direction * amount;
			
			return vector;
		}
		
		/// <summary>
		/// 벡터에서 '_direction'과 같은 방향을 가리키는 부분을 추출하여 반환
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="direction"></param>
		/// <returns></returns>
		public static Vector3 ExtractDotVector(Vector3 vector, Vector3 direction)
		{
			//Normalize vector if necessary;
			if(direction.sqrMagnitude != 1)
				direction.Normalize();
			
			float amount = Vector3.Dot(vector, direction);
			
			return direction * amount;
		}

		/// '_planeNormal'에 의해 정의된 평면으로 벡터 회전 
		public static Vector3 RotateVectorOntoPlane(Vector3 vector, Vector3 planeNormal, Vector3 upDirection)
		{
			//회전 계산;
			Quaternion rotation = Quaternion.FromToRotation(upDirection, planeNormal);

			//벡터에 회전 적용;
			vector = rotation * vector;
			
			return vector;
		}

		/// <summary>
		/// '_lineStartPosition' 및 '_lineDirection'으로 정의된 선에 점 투영
		/// </summary>
		/// <param name="lineStartPosition"></param>
		/// <param name="lineDirection"></param>
		/// <param name="point"></param>
		/// <returns></returns>
		public static Vector3 ProjectPointOntoLine(Vector3 lineStartPosition, Vector3 lineDirection, Vector3 point)
		{		
			//'_lineStartPosition'에서 'point'까지 가리키는 벡터 계산;
			Vector3 projectLine = point - lineStartPosition;
	
			float dotProduct = Vector3.Dot(projectLine, lineDirection);
	
			return lineStartPosition + lineDirection * dotProduct;
		}

		/// <summary>
		/// '_speed' 및 '_deltaTime'을 사용하여 대상 벡터를 향해 벡터를 증가시킵니다.
		/// </summary>
		/// <param name="currentVector"></param>
		/// <param name="speed"></param>
		/// <param name="deltaTime"></param>
		/// <param name="targetVector"></param>
		/// <returns></returns>
		public static Vector3 IncrementVectorTowardTargetVector(Vector3 currentVector, float speed, float deltaTime, Vector3 targetVector)
		{
			return Vector3.MoveTowards(currentVector, targetVector, speed * deltaTime);
		}

		/// <summary>
		/// Y축 기준으로 벡터를 반시계방향 회전합니다. 
		/// </summary>
		/// <param name="v">대상 벡터입니다.</param>
		/// <param name="angleInRadian">회전할 라디안 단위 각도입니다. 반시계 방향으로 회전합니다.</param>
		/// <returns>자신의 벡터를 반환합니다.</returns>
		public static Vector3 RotateAroundY(this ref Vector3 v, float angleInRadian) {
			float cos = Mathf.Cos(angleInRadian);
			float sin = Mathf.Sin(angleInRadian);
			float x = v.x * cos - v.z * sin;
			float z = v.z * cos + v.x * sin;
			v.x = x;
			v.z = z;
			return v;
		}

		/// <summary>
		/// Y축 기준으로 벡터를 반시계방향 회전한 벡터를 복사해서 반환합니다.
		/// </summary>
		/// <param name="v">대상 벡터입니다.</param>
		/// <param name="angleInRadian">회전할 라디안 단위 각도입니다. 반시계 방향으로 회전합니다.</param>
		/// <returns>복사본 벡터를 반환합니다.</returns>
		public static Vector3 RotatedAroundY(this in Vector3 v, float angleInRadian) {
			Vector3 newVector = new Vector3(v.x, v.y, v.z);
			newVector.RotateAroundY(angleInRadian);
			return newVector;
		}
	}
}
