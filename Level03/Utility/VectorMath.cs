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

		/// <summary>
		/// 특정 축의 값만 수정된 벡터를 반환합니다. float.NaN을 삽입하면 원래 해당 축의 값을 사용합니다.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public static Vector2 Copy(this in Vector2 v, float x = float.NaN, float y = float.NaN)
		{
			Vector2 result;
			result.x = float.IsNaN(x) ? v.x : x;
			result.y = float.IsNaN(y) ? v.y : y;
			return result;
		}

		/// <summary>
		/// 특정 축의 값만 수정된 벡터를 반환합니다. float.NaN을 삽입하면 원래 해당 축의 값을 사용합니다.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public static Vector3 Copy(this in Vector3 v, float x = float.NaN, float y = float.NaN, float z = float.NaN)
		{
			Vector3 result;
			result.x = float.IsNaN(x) ? v.x : x;
			result.y = float.IsNaN(y) ? v.y : y;
			result.z = float.IsNaN(z) ? v.z : z;
			return result;
		}
		
		/// <summary>
		/// Collider 안에 있는지 확인합니다. 원리는 ClosestPoint와 원래 벡터의 거리가 일정 거리 이내면 true를 반환합니다.
		/// </summary>
		/// <param name="collider"></param>
		/// <param name="v"></param>
		/// <returns></returns>
		public static bool Contains(this Collider collider, in Vector3 v)
		{
			var closest = collider.ClosestPoint(v);
			return (v - closest).sqrMagnitude <= Vector3.kEpsilon;
		}

		/// <summary>
		/// Vector의 크기를 조절합니다. Vector3.zero에 사용하지 않게 유의하세요.
		/// 원리는 this *= (newSize / magnitude) 입니다.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="newSize"></param>
		/// <returns>magnitude == 0f인 경우 false, 일반적인 경우 true를 반환합니다.</returns>
		public static bool Resize(this ref Vector3 v, float newSize)
		{
			var length = v.magnitude;
			if (length == 0f)
			{
				return false;
			}
			v *= (newSize / length);
			return true;
		}
		/// <summary>
		/// Vector의 크기를 조절합니다. Vector3.zero에 사용하지 않게 유의하세요.
		/// 원리는 v * (newSize / magnitude) 입니다.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="newSize"></param>
		/// <returns></returns>
		public static Vector3 Resized(this in Vector3 v, float newSize)
		{
			var length = v.magnitude;
			if (length == 0f)
			{
				return Vector3.zero;
			}
			return v * (newSize / length);
		}

		/// <summary>
		/// Vector의 크기를 범위 내로 조절합니다. Vector3.zero에 사용하지 않게 유의하세요.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="minSize"></param>
		/// <param name="maxSize"></param>
		/// <returns>magnitude == 0f인 경우 false, 일반적인 경우 true를 반환합니다.</returns>
		public static bool SizeClamp(this ref Vector3 v, float minSize, float maxSize)
		{
			var length = v.magnitude;
			
			// 이미 범위 내면 암것도 안함
			if (minSize <= length && length <= maxSize)
			{
				return true;
			}
			
			if (length == 0f)
			{
				return false;
			}
			var newLength = Mathf.Clamp(length, minSize, maxSize);
			v *= (newLength / length);
			return true;
		}
		/// <summary>
		/// Vector의 크기를 범위 내로 조절합니다. Vector3.zero에 사용하지 않게 유의하세요.
		/// </summary>
		/// <param name="v"></param>
		/// <param name="minSize"></param>
		/// <param name="maxSize"></param>
		public static Vector3 SizeClamped(this in Vector3 v, float minSize, float maxSize)
		{
			var length = v.magnitude;

			// 이미 범위 내면 암것도 안함
			if (minSize <= length && length <= maxSize)
			{
				return v;
			}
			
			if (length == 0f)
			{
				return Vector3.zero;
			}
			
			length = Mathf.Clamp(length, minSize, maxSize);
			return v.Resized(length);
		}
		
		/// <summary>
		/// XZ축 한정으로 Collider 안에 있는지 확인합니다.
		/// 원리는 ClosestPoint와 원래 벡터의 거리가 일정 거리 이내면 true를 반환합니다.
		/// 단, ClosestPoint와 원래 벡터의 y를 일치시키고 검사합니다.
		/// </summary>
		/// <param name="collider"></param>
		/// <param name="v"></param>
		/// <returns></returns>
		public static bool ContainsXZ(this Collider collider, in Vector3 v)
		{
			var closest = collider.ClosestPoint(v);
			closest.y = v.y;
			return (v - closest).sqrMagnitude <= Vector3.kEpsilon;
		}

		public static bool IsZero(this in Vector2 v) => v.sqrMagnitude <= 0f;
		public static bool IsZero(this in Vector3 v) => v.sqrMagnitude <= 0f;
		
		public static Vector3 ToVectorXZ(this in Vector2 v)
		{
			return new Vector3(v.x, 0f, v.y);
		}

		public static float Distance(this in Vector3 v1, in Vector3 v2)
		{
			return Vector3.Distance(v1, v2);
		}
		public static float DistanceSquared(this in Vector3 v1, in Vector3 v2)
		{
			return (v1 - v2).sqrMagnitude;
		}
	}
}
