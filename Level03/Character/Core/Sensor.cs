using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Character.Core
{
	//이 스크립트는 광선 및 구체 투사를 담당합니다.
	//런타임 시 'Mover' 구성 요소에 의해 인스턴스화됩니다.
	[System.Serializable]
	public class Sensor {

		//기본 레이캐스트 매개변수
		public float CastLength = 1f;
		public float SphereCastRadius = 0.2f;

		//(ray-)cast의 시작점;
		private Vector3 _origin = Vector3.zero;

		//레이캐스팅의 방향으로 사용되는 로컬 변환 축을 설명하는 열거형.
		public enum CastDirection
		{
			Forward,
			Right,
			Up,
			Backward, 
			Left,
			Down
		}

		private CastDirection _castDirection;

		//Raycast 적중 정보 변수;
		private bool _hasDetectedHit;
		private Vector3 _hitPosition;
		private Vector3 _hitNormal;
		private float _hitDistance;
		private List<Collider> _hitColliders = new();
		private List<Transform> _hitTransforms = new();

		//spherecast를 사용할 때 특정 엣지 케이스에 사용되는 백업 노멀;
		private Vector3 _backupNormal;

		//첨부된 구성 요소에 대한 참조
		private Transform _tr;
		private Collider _col;

		//다양한 유형의 지상 탐지 방법을 설명하는 열거형.
		[SerializeField]
		public enum ECastType
		{
			Raycast,
			RaycastArray,
			Spherecast
		}

		public ECastType CastType = ECastType.Raycast;
		public LayerMask layermask = 255;

		//'Raycast 무시' 레이어의 레이어 번호입니다.
		private int _ignoreRaycastLayer;

		//실제 표면 법선을 얻기 위해 추가 광선을 던집니다.;
		public bool CalculateRealSurfaceNormal;
		
		//지면까지의 실제 거리를 얻으려면 추가 광선을 투사하십시오.
		public bool CalculateRealDistance;

		//Array raycast settings;

		//모든 행의 광선 수
		public int ArrayRayCount = 9;
		
		//중심 광선 주위의 행 수입니다.
		public int ArrayRows = 3;
		
		//다른 모든 행을 오프셋할지 여부입니다.
		public bool OffsetArrayRows;

		//모든 배열 레이캐스트 시작 위치를 포함하는 배열(로컬 좌표).
		private Vector3[] _raycastArrayStartPositions;

		//레이캐스팅 시 무시할 콜라이더의 선택적 목록입니다.
		private Collider[] _ignoreList;

		//무시 목록에 충돌체 레이어를 저장하기 위한 배열.
		private int[] _ignoreListLayers;

		//에디터에서 디버그 정보(적중 위치, 적중 법선...)를 그릴지 여부.
		public bool IsInDebugMode;

		private List<Vector3> _arrayNormals = new();
		private List<Vector3> _arrayPoints = new();

		//Constructor;
		public Sensor (Transform transform, Collider collider)
		{
			_tr = transform;

			if(collider == null)
				return;

			_ignoreList = new Collider[1];

			//목록을 무시하기 위해 충돌기 추가;
			_ignoreList[0] = collider;

			//나중을 위해 "Raycast 무시" 레이어 번호 저장;
			_ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

			//무시 목록 레이어를 저장할 설정 배열;
			_ignoreListLayers = new int[_ignoreList.Length];
		}

		//레이캐스트 히트에 대한 정보 저장과 관련된 모든 변수를 재설정합니다.
		private void ResetFlags()
		{
			_hasDetectedHit = false;
			_hitPosition = Vector3.zero;
			_hitNormal = -GetCastDirection();
			_hitDistance = 0f;

			if(_hitColliders.Count > 0)
				_hitColliders.Clear();
			if(_hitTransforms.Count > 0)
				_hitTransforms.Clear();
		}

		//입력 인수를 기반으로 모든 배열 광선(로컬 좌표)의 시작 위치를 포함하는 배열을 반환합니다.
		public static Vector3[] GetRaycastStartPositions(int sensorRows, int sensorRayCount, bool offsetRows, float sensorRadius)
		{
			//위치를 저장하는 데 사용되는 목록을 초기화합니다.
			List<Vector3> positions = new();

			//목록에 중앙 시작 위치 추가
			Vector3 startPosition = Vector3.zero;
			positions.Add(startPosition);

			for(int i = 0; i < sensorRows; i++)
			{
				//이 행의 모든 위치에 대한 반경 계산
				float rowRadius = (float)(i+1)/sensorRows; 

				for(int j = 0; j < sensorRayCount * (i + 1); j++)
				{
					//이 개별 위치에 대한 각도(도) 계산
					float angle = (360f/(sensorRayCount * (i + 1))) * j;	

					//'offsetRows'가 'true'로 설정되면 다른 모든 행이 오프셋됩니다.
					if(offsetRows && i % 2 == 0)	
						angle += (360f/(sensorRayCount * (i + 1)))/2f;

					//반경과 각도를 하나의 위치로 결합하고 목록에 추가
					float x = rowRadius * Mathf.Cos(Mathf.Deg2Rad * angle);
					float y = rowRadius * Mathf.Sin(Mathf.Deg2Rad * angle);

					positions.Add(new Vector3(x, 0f, y) * sensorRadius);
				}
			}
			//목록을 배열로 변환하고 배열을 반환합니다.
			return positions.ToArray();
		}

		//충돌체를 확인하기 위해 광선(또는 구 또는 광선 배열)을 캐스트합니다.
		public void Cast()
		{
			ResetFlags();

			//세계 좌표에서 광선의 원점과 방향 계산
			Vector3 worldDirection = GetCastDirection();
			Vector3 worldOrigin = _tr.TransformPoint(_origin);

			//마지막 프레임 이후 무시 목록 길이가 변경되었는지 확인
			if(_ignoreListLayers.Length != _ignoreList.Length)
			{
				//그렇다면 새 길이에 맞게 레이어 배열을 무시하도록 설정하십시오.
				_ignoreListLayers = new int[_ignoreList.Length]; 
			}

			//(일시적으로) 무시 목록의 모든 개체를 'Raycast 무시' 레이어로 이동
			for(int i = 0; i < _ignoreList.Length; i++)
			{
				_ignoreListLayers[i] = _ignoreList[i].gameObject.layer;
				_ignoreList[i].gameObject.layer = _ignoreRaycastLayer;
			}

			//선택한 감지 모드에 따라 다른 함수를 호출하여 충돌체 확인
			switch (CastType)
			{
				case ECastType.Raycast:
					CastRay(worldOrigin, worldDirection);
					break;
				case ECastType.Spherecast:
					CastSphere(worldOrigin, worldDirection);
					break;
					case ECastType.RaycastArray:
					CastRayArray(worldOrigin, worldDirection);
					break;
				default:
					_hasDetectedHit = false;
					break;
			}

			//ignoreList에서 콜라이더 레이어 재설정
			for(int i = 0; i < _ignoreList.Length; i++) 
				_ignoreList[i].gameObject.layer = _ignoreListLayers[i];
		}

		//광선 배열을 '_direction'으로 캐스팅하고 '_origin'을 중심으로 합니다.
		private void CastRayArray(Vector3 origin, Vector3 direction)
		{
			//세계 좌표에서 광선의 원점과 방향 계산
			Vector3 rayDirection = GetCastDirection();

			//마지막 프레임의 결과 지우기;
			_arrayNormals.Clear();
			_arrayPoints.Clear();

			//캐스트 배열;
			for(int i = 0; i < _raycastArrayStartPositions.Length; i++)
			{
				//광선 시작 위치 계산
				Vector3 rayStartPosition = origin + _tr.TransformDirection(_raycastArrayStartPositions[i]);

				if(Physics.Raycast(rayStartPosition, rayDirection, out RaycastHit hit, CastLength, layermask, QueryTriggerInteraction.Ignore))
				{
				
					if(IsInDebugMode)
						Debug.DrawRay(hit.point, hit.normal, Color.red, Time.fixedDeltaTime * 1.01f);

					_hitColliders.Add(hit.collider);
					_hitTransforms.Add(hit.transform);
					_arrayNormals.Add(hit.normal);
					_arrayPoints.Add(hit.point);
				}
			}

			//결과 평가
			_hasDetectedHit = (_arrayPoints.Count > 0);

			if(_hasDetectedHit)
			{
				//Calculate average surface normal;
				Vector3 averageNormal = Vector3.zero;
				for(int i = 0; i < _arrayNormals.Count; i++) 
					averageNormal += _arrayNormals[i];

				averageNormal.Normalize();

				//Calculate average surface point;
				Vector3 averagePoint = Vector3.zero;
				for(int i = 0; i < _arrayPoints.Count; i++) 
					averagePoint += _arrayPoints[i];

				averagePoint /= _arrayPoints.Count;
				
				_hitPosition = averagePoint;
				_hitNormal = averageNormal;
				_hitDistance = VectorMath.ExtractDotVector(origin - _hitPosition, direction).magnitude;
			}
		}

		//단일 광선을 '_origin'에서 '_direction'으로 투사
		private void CastRay(Vector3 origin, Vector3 direction)
		{
			_hasDetectedHit = Physics.Raycast(origin, direction, out RaycastHit hit, CastLength, layermask, QueryTriggerInteraction.Ignore);

			if(_hasDetectedHit)
			{
				_hitPosition = hit.point;
				_hitNormal = hit.normal;

				_hitColliders.Add(hit.collider);
				_hitTransforms.Add(hit.transform);

				_hitDistance = hit.distance;
			}
		}

		//'_origin'에서 '_direction'으로 구체를 캐스팅합니다.
		private void CastSphere(Vector3 origin, Vector3 direction)
		{
			_hasDetectedHit = Physics.SphereCast(origin, SphereCastRadius, direction, out RaycastHit hit, CastLength - SphereCastRadius, layermask, QueryTriggerInteraction.Ignore);

			if(_hasDetectedHit)
			{
				_hitPosition = hit.point;
				_hitNormal = hit.normal;
				_hitColliders.Add(hit.collider);
				_hitTransforms.Add(hit.transform);

				_hitDistance = hit.distance;

				_hitDistance += SphereCastRadius;

				//Calculate real distance;
				if(CalculateRealDistance)
				{
					_hitDistance = VectorMath.ExtractDotVector(origin - _hitPosition, direction).magnitude;
				}

				Collider col = _hitColliders[0];

				//Calculate real surface normal by casting an additional raycast;
				if(CalculateRealSurfaceNormal)
				{
					if(col.Raycast(new Ray(_hitPosition - direction, direction), out hit, 1.5f))
					{
						if(Vector3.Angle(hit.normal, -direction) >= 89f)
							_hitNormal = _backupNormal;
						else
							_hitNormal = hit.normal;
					}
					else
						_hitNormal = _backupNormal;
					
					_backupNormal = _hitNormal;
				}
			}
		}

		//이 게임 개체의 변환 구성 요소의 로컬 축을 기반으로 세계 좌표의 방향을 계산합니다.
		private Vector3 GetCastDirection()
		{
			switch(_castDirection)
			{
			case CastDirection.Forward:
				return _tr.forward;

			case CastDirection.Right:
				return _tr.right;

			case CastDirection.Up:
				return _tr.up;

			case CastDirection.Backward:
				return -_tr.forward;

			case CastDirection.Left:
				return -_tr.right;

			case CastDirection.Down:
				return -_tr.up;
			default:
				return Vector3.one;
			}
		}

		//편집기에서 디버그 정보 그리기(적중 위치 및 지면 노멀)
		public void DrawDebug()
		{
			if(_hasDetectedHit && IsInDebugMode)
			{
				const float markerSize = 0.2f;
				Debug.DrawRay(_hitPosition, _hitNormal, Color.red, Time.deltaTime);
				Debug.DrawLine(_hitPosition + Vector3.up * markerSize, _hitPosition - Vector3.up * markerSize, Color.green, Time.deltaTime);
				Debug.DrawLine(_hitPosition + Vector3.right * markerSize, _hitPosition - Vector3.right * markerSize, Color.green, Time.deltaTime);
				Debug.DrawLine(_hitPosition + Vector3.forward * markerSize, _hitPosition - Vector3.forward * markerSize, Color.green, Time.deltaTime);
			}
		}

		//Getters;

		//센서가 무언가에 부딪혔는지 여부를 반환합니다.
		public bool HasDetectedHit() => _hasDetectedHit;

		//충돌기를 치기 전에 레이캐스트가 도달한 거리를 반환합니다.
		public float GetDistance() => _hitDistance;

		//레이캐스트가 적중한 콜라이더의 표면 법선을 반환합니다.
		public Vector3 GetNormal() => _hitNormal;

		//레이캐스트가 충돌체에 부딪힌 세계 좌표의 위치를 반환합니다.
		public Vector3 GetPosition() => _hitPosition;

		//레이캐스트에 맞은 콜라이더에 대한 참조를 반환합니다.
		public Collider GetCollider() => _hitColliders[0];

		//레이캐스트에 맞은 콜라이더에 부착된 변환 구성 요소에 대한 참조를 반환합니다.;
		public Transform GetTransform() => _hitTransforms[0];

		//Setters;

		//레이캐스트가 시작할 위치를 설정합니다.
		//입력 벡터 '_origin'은 로컬 좌표로 변환됩니다.
		public void SetCastOrigin(Vector3 origin)
		{
			if(!_tr)
				return;
			
			_origin = _tr.InverseTransformPoint(origin);
		}

		//이 게임 오브젝트의 변환 축을 레이캐스트의 방향으로 사용하도록 설정합니다.
		public void SetCastDirection(CastDirection direction)
		{
			if(!_tr)
				return;

			_castDirection = direction;
		}

		//레이캐스트 배열의 시작 위치 다시 계산
		public void RecalibrateRaycastArrayPositions() => 
			_raycastArrayStartPositions = GetRaycastStartPositions(ArrayRows, ArrayRayCount, OffsetArrayRows, SphereCastRadius);
	}
}