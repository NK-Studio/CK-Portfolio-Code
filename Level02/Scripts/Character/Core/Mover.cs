using UnityEngine;

namespace Character.Core
{
    //이 스크립트는 물리, 충돌 감지 및 지상 감지를 처리합니다.
    //외부 스크립트의 'FixedUpdate' 프레임마다 이동 속도('SetVelocity'를 통해)가 동작합니다.
    public class Mover : MonoBehaviour
    {
        //충돌기 변수;
        [SerializeField, Range(0f, 1f), Header("계단 옵션 : ")]
        private float stepHeightRatio = 0.25f;

        [SerializeField, Header("콜라이더 옵션 :")] private float colliderHeight = 2f;

        [SerializeField] private float colliderThickness = 1f;

        [SerializeField] private Vector3 colliderOffset = Vector3.zero;

        //첨부된 콜라이더들에 대한 참조;
        private BoxCollider _boxCollider;
        private SphereCollider _sphereCollider;
        private CapsuleCollider _capsuleCollider;

        //Sensor variables;
        [SerializeField, Header("센서 옵션 :")] public Sensor.CastType sensorType = Sensor.CastType.Raycast;

        private const float SensorRadiusModifier = 0.8f;
        private int _currentLayer;
        [SerializeField] private bool isInDebugMode;

        [SerializeField, Range(1, 5), Header("센서 배열 옵션")]
        private int sensorArrayRows = 1;

        [SerializeField] [Range(3, 10)] private int sensorArrayRayCount = 6;
        [SerializeField] private bool sensorArrayRowsAreOffset;
        [SerializeField] private LayerMask groundLayer;
        [HideInInspector] public Vector3[] raycastArrayPreviewPositions;

        //지상 감지 변수;
        private bool _isGrounded = true;

        //센서 범위 변수;
        private bool _isUsingExtendedSensorRange = true;
        private float _baseSensorRange;

        //지면과의 정확한 거리를 유지하는 데 필요한 현재 상향(또는 하향) 속도.
        private Vector3 _currentGroundAdjustmentVelocity = Vector3.zero;

        //첨부된 구성 요소에 대한 참조
        private Collider _col;
        private Rigidbody _rig;
        private Transform _tr;
        private Sensor _sensor;

        private void Awake()
        {
            Setup();

            //Initialize sensor;
            _sensor = new Sensor(_tr, _col);
            RecalculateColliderDimensions();
            RecalibrateSensor();
        }

        private void Reset()
        {
            Setup();
        }

        private void OnValidate()
        {
            //콜라이더의 치수를 다시 계산합니다.
            if (gameObject.activeInHierarchy)
                RecalculateColliderDimensions();

            //레이캐스트 배열 미리보기 위치를 다시 계산합니다.
            if (sensorType == Sensor.CastType.RaycastArray)
                raycastArrayPreviewPositions =
                    Sensor.GetRaycastStartPositions(sensorArrayRows, sensorArrayRayCount, sensorArrayRowsAreOffset, 1f);
        }

        //구성 요소에 대한 참조를 설정합니다.
        private void Setup()
        {
            _tr = transform;
            _col = GetComponent<Collider>();

            //이 게임오브젝트에 콜라이더가 연결되어 있지 않다면 콜라이더를 추가하세요.
            if (!_col)
            {
                _tr.gameObject.AddComponent<CapsuleCollider>();
                _col = GetComponent<Collider>();
            }

            _rig = GetComponent<Rigidbody>();

            //이 게임오브젝트에 리지드바디가 연결되어 있지 않다면 리지드바디를 추가하세요.
            if (!_rig)
            {
                _tr.gameObject.AddComponent<Rigidbody>();
                _rig = GetComponent<Rigidbody>();
            }

            _boxCollider = GetComponent<BoxCollider>();
            _sphereCollider = GetComponent<SphereCollider>();
            _capsuleCollider = GetComponent<CapsuleCollider>();

            //리지드바디 회전을 멈추고 리지드바디 중력을 비활성화합니다.
            _rig.freezeRotation = true;
            _rig.useGravity = false;
        }

        //디버그 모드가 활성화된 경우 디버그 정보를 그립니다.
        private void LateUpdate()
        {
            if (isInDebugMode)
                _sensor.DrawDebug();
        }

        //콜라이더 높이 너비 두께 다시 계산
        public void RecalculateColliderDimensions()
        {
            //콜라이더가 이 게임 오브젝트에 연결되어 있는지 확인하십시오.
            if (!_col)
            {
                //Setup()을 호출하여 연결된 충돌기에 대한 참조를 얻으십시오.
                Setup();

                //다시 확인하십시오.
                if (!_col)
                {
                    Debug.LogWarning($"{gameObject.name}에 부착된 충돌기가 없습니다!");
                    return;
                }
            }

            //충돌기 변수를 기반으로 충돌기 치수를 설정합니다.
            if (_boxCollider)
            {
                Vector3 size = Vector3.zero;
                size.x = colliderThickness;
                size.z = colliderThickness;

                _boxCollider.center = colliderOffset * colliderHeight;

                size.y = colliderHeight * (1f - stepHeightRatio);
                _boxCollider.size = size;

                _boxCollider.center += new Vector3(0f, stepHeightRatio * colliderHeight / 2f, 0f);
            }
            else if (_sphereCollider)
            {
                _sphereCollider.radius = colliderHeight / 2f;
                _sphereCollider.center = colliderOffset * colliderHeight;

                _sphereCollider.center += new Vector3(0f, stepHeightRatio * _sphereCollider.radius, 0f);
                _sphereCollider.radius *= (1f - stepHeightRatio);
            }
            else if (_capsuleCollider)
            {
                _capsuleCollider.height = colliderHeight;
                _capsuleCollider.center = colliderOffset * colliderHeight;
                _capsuleCollider.radius = colliderThickness / 2f;

                _capsuleCollider.center += new Vector3(0f, stepHeightRatio * _capsuleCollider.height / 2f, 0f);
                _capsuleCollider.height *= (1f - stepHeightRatio);

                if (_capsuleCollider.height / 2f < _capsuleCollider.radius)
                    _capsuleCollider.radius = _capsuleCollider.height / 2f;
            }

            //새로운 충돌기 치수에 맞게 센서 변수를 재보정합니다.
            if (_sensor != null)
                RecalibrateSensor();
        }

        //센서 변수를 재보정합니다.
        private void RecalibrateSensor()
        {
            //센서 광선의 원점과 방향을 설정합니다.
            _sensor.SetCastOrigin(GetColliderCenter());
            _sensor.SetCastDirection(Sensor.CastDirection.Down);

            //센서 레이어 마스크를 계산합니다.
            RecalculateSensorLayerMask();

            //센서 캐스트 유형 설정;
            _sensor.castType = sensorType;

            //센서 반경 폭을 계산하십시오.
            float radius = colliderThickness / 2f * SensorRadiusModifier;

            //부동 소수점 오류를 보안하기 위해 모든 센서 길이에 'safetyDistanceFactor'를 곱한다.
            const float safetyDistanceFactor = 0.001f;

            //충돌기 높이를 센서 반경에 맞춥니다.
            if (_boxCollider)
                radius = Mathf.Clamp(radius, safetyDistanceFactor,
                    (_boxCollider.size.y / 2f) * (1f - safetyDistanceFactor));
            else if (_sphereCollider)
                radius = Mathf.Clamp(radius, safetyDistanceFactor,
                    _sphereCollider.radius * (1f - safetyDistanceFactor));
            else if (_capsuleCollider)
                radius = Mathf.Clamp(radius, safetyDistanceFactor,
                    (_capsuleCollider.height / 2f) * (1f - safetyDistanceFactor));

            //센서 변수를 설정합니다.

            //센서 반경을 설정하십시오.
            Vector3 localScale = _tr.localScale;
            _sensor.sphereCastRadius = radius * localScale.x;

            //센서 길이를 계산하고 설정합니다.
            float length = 0f;
            length += colliderHeight * (1f - stepHeightRatio) * 0.5f;
            length += colliderHeight * stepHeightRatio;
            _baseSensorRange = length * (1f + safetyDistanceFactor) * localScale.x;
            _sensor.castLength = length * localScale.x;

            //센서 배열 변수를 설정합니다.
            _sensor.arrayRows = sensorArrayRows;
            _sensor.arrayRayCount = sensorArrayRayCount;
            _sensor.offsetArrayRows = sensorArrayRowsAreOffset;
            _sensor.isInDebugMode = isInDebugMode;

            //센서 spherecast 변수를 설정합니다.
            _sensor.calculateRealDistance = true;
            _sensor.calculateRealSurfaceNormal = true;

            //센서를 새 값으로 재보정합니다.
            _sensor.RecalibrateRaycastArrayPositions();
        }

        //현재 물리 설정을 기반으로 센서 레이어 마스크를 다시 계산합니다.
        public void RecalculateSensorLayerMask()
        {
            int layerMask = 0;
            int objectLayer = gameObject.layer;

            //기본 Default는 허용
            const int _Default = 0;
            layerMask |= 1 << _Default;

            //레이어 마스크를 계산합니다.
            //땅을 체크할 수 있도록 합니다.
            layerMask |= groundLayer;

            // for (int i = 0; i < 32; i++)
            // {
            //     if (!Physics.GetIgnoreLayerCollision(objectLayer, i))
            //         layerMask |= (1 << i);
            // }

            //계산된 레이어 마스크에 'Raycast 무시' 레이어가 포함되어 있지 않은지 확인하십시오.
            if (layerMask == (layerMask | (1 << LayerMask.NameToLayer("Ignore Raycast"))))
            {
                layerMask ^= 1 << LayerMask.NameToLayer("Ignore Raycast");
            }

            //센서 레이어 마스크 설정;
            _sensor.layermask = layerMask;

            //현재 레이어를 저장합니다.
            _currentLayer = objectLayer;
        }

        //세계 좌표에서 충돌기의 중심을 반환합니다.
        private Vector3 GetColliderCenter()
        {
            if (!_col)
                Setup();

            return _col.bounds.center;
        }

        //무버가 땅에 닿았는지 확인합니다.
        //나중을 위해 모든 관련 충돌 정보를 저장합니다.
        //지면과의 정확한 거리를 유지하기 위해 필요한 조정 속도를 계산합니다.
        private void Check()
        {
            //지상 조정 속도를 재설정합니다.
            _currentGroundAdjustmentVelocity = Vector3.zero;

            //센서 길이 설정;
            if (_isUsingExtendedSensorRange)
                _sensor.castLength = _baseSensorRange + (colliderHeight * _tr.localScale.x) * stepHeightRatio;
            else
                _sensor.castLength = _baseSensorRange;

            _sensor.Cast();

            //센서가 아무 것도 감지하지 못한 경우 플래그를 설정하고 반환합니다.
            if (!_sensor.HasDetectedHit())
            {
                _isGrounded = false;
                return;
            }

            //지상 감지용 플래그를 설정합니다.
            _isGrounded = true;

            //센서 광선이 도달한 거리를 가져옵니다.
            float distance = _sensor.GetDistance();

            //얼마나 많은 무버를 위 또는 아래로 움직여야 하는지 계산하십시오.
            Vector3 localScale = _tr.localScale;
            float upperLimit = colliderHeight * localScale.x * (1f - stepHeightRatio) * 0.5f;
            float middle = upperLimit + colliderHeight * localScale.x * stepHeightRatio;
            float distanceToGo = middle - distance;

            //다음 프레임에 대한 새로운 지면 조정 속도를 설정합니다.
            _currentGroundAdjustmentVelocity = _tr.up * (distanceToGo / Time.fixedDeltaTime);
        }

        /// <summary>
        /// 무버가 땅에 닿았는지 체크합니다.
        /// </summary>
        public void CheckForGround()
        {
            //마지막 프레임 이후 개체 레이어가 변경되었는지 확인하십시오.
            //그렇다면 센서 레이어 마스크를 다시 계산하십시오.
            if (_currentLayer != gameObject.layer)
                RecalculateSensorLayerMask();

            Check();
        }

        /// <summary>
        /// 무버 속도를 설정합니다.
        /// </summary>
        /// <param name="velocity"></param>
        public void SetVelocity(Vector3 velocity) => _rig.velocity = velocity + _currentGroundAdjustmentVelocity;

        /// <summary>
        /// 무버 힘을 추가합니다.
        /// </summary>
        /// <param name="force"></param>
        /// <param name="mode"></param>
        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)
        {
            _rig.AddForce(force, mode);
        }

        /// <summary>
        /// 무버가 지면에 닿고 hte 'up' 벡터와 지면 법선 사이의 각도가 너무 가파르지 않으면 'true'를 반환합니다.
        /// (예: angle < slope_limit)
        /// </summary>
        /// <returns></returns>
        public bool IsGrounded() => _isGrounded;

        //Setters;

        /// <summary>
        /// 센서 범위를 확장해야 하는지 여부를 설정합니다.
        /// </summary>
        /// <param name="isExtended"></param>
        public void SetExtendSensorRange(bool isExtended)
        {
            _isUsingExtendedSensorRange = isExtended;
        }

        /// <summary>
        /// 충돌기의 높이를 설정합니다.
        /// </summary>
        /// <param name="newColliderHeight"></param>
        public void SetColliderHeight(float newColliderHeight)
        {
            if (colliderHeight == newColliderHeight)
                return;

            colliderHeight = newColliderHeight;
            RecalculateColliderDimensions();
        }

        public float GetColliderHeight() => colliderHeight;

        /// <summary>
        /// 콜라이더의 두께폭을 설정합니다.
        /// </summary>
        /// <param name="newColliderThickness"></param>
        public void SetColliderThickness(float newColliderThickness)
        {
            if (colliderThickness == newColliderThickness)
                return;

            if (newColliderThickness < 0f)
                newColliderThickness = 0f;

            colliderThickness = newColliderThickness;
            RecalculateColliderDimensions();
        }

        /// <summary>
        /// 허용 가능한 계단 높이를 설정하십시오.
        /// </summary>
        /// <param name="newStepHeightRatio"></param>
        public void SetStepHeightRatio(float newStepHeightRatio)
        {
            newStepHeightRatio = Mathf.Clamp(newStepHeightRatio, 0f, 1f);
            stepHeightRatio = newStepHeightRatio;
            RecalculateColliderDimensions();
        }

        //Getters;

        public Vector3 GetGroundNormal() => _sensor.GetNormal();

        public Vector3 GetGroundPoint() => _sensor.GetPosition();

        public Collider GetGroundCollider() => _sensor.GetCollider();

        public Rigidbody GetRigidbody() => _rig;
    }
}