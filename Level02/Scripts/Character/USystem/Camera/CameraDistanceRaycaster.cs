using UnityEngine;

namespace Character.USystem.Camera
{
    //이 스크립트는 raycast(또는 spherecast)를 사용하여 이 변환과 카메라 사이의 장애물을 감지합니다.;
    //그런 다음 장애물의 근접성을 기반으로 카메라 변환을 이 변환에 더 가깝게 이동합니다.;
    //이 스크립트의 주요 목적은 카메라가 레벨 지오메트리로 클리핑되는 것을 방지하거나 장애물이 플레이어의 시야를 차단하는 것을 방지하는 것입니다.;
    public class CameraDistanceRaycaster : MonoBehaviour
    {
        //카메라의 변환 구성 요소;
        public Transform cameraTransform;

        //카메라 타겟의 Transform 컴포넌트;
        public Transform cameraTargetTransform;

        private Transform _tr;

        //raycast 또는 spherecast가 장애물을 스캔하는 데 사용되는지 여부;
        public CastType castType;

        public enum CastType
        {
            Raycast,
            Spherecast
        }

        //레이캐스팅에 사용되는 레이어 마스크;
        public LayerMask layerMask = ~0;

        //'Raycast 무시' 레이어의 레이어 번호;
        private int _ignoreRaycastLayer;

        //레이캐스팅 시 무시할 충돌기 목록;
        public Collider[] ignoreList;

        //무시 목록에 충돌기 레이어를 저장하는 배열;
        private int[] _ignoreListLayers;

        private float _currentDistance;

        //카메라가 레벨 지오메트리로 클리핑되는 것을 방지하기 위해 레이캐스트의 길이에 추가되는 추가 거리
        //대부분의 경우 기본값 '0.1f'이면 충분합니다.
        //클리핑이 많이 발생하는 경우 이 거리를 약간 늘려볼 수 있습니다.
        //이 값은 'Raycast'가 'castType'으로 선택된 경우에만 사용됩니다. 
        public float minimumDistanceFromObstacles = 0.1f;

        //이 값은 이전 카메라 거리가 새 거리를 향해 얼마나 부드럽게 보간되는지 제어합니다.
        //이 값을 '50f'(또는 그 이상)로 설정하면 (가시적인) 스무딩이 전혀 발생하지 않습니다.
        //이 값을 '1f'(또는 그 이하)로 설정하면 매우 눈에 띄게 평활화됩니다.
        //대부분의 응용 프로그램에서 '25f' 값을 권장합니다. 
        public float smoothingFactor = 25f;

        //Spherecast의 반경, 'Spherecast'가 'castType'으로 선택된 경우에만 사용됩니다.
        public float spherecastRadius = 0.2f;

        private void Awake()
        {
            _tr = transform;

            //무시 목록 레이어를 저장할 설정 배열
            _ignoreListLayers = new int[ignoreList.Length];

            //나중을 위해 레이어 번호 무시 저장
            _ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

            //선택한 레이어 마스크에 'Raycast 무시' 레이어가 포함되어 있지 않은지 확인하십시오.
            if (layerMask == (layerMask | (1 << _ignoreRaycastLayer))) 
                layerMask ^= 1 << _ignoreRaycastLayer;

            if (!cameraTransform)
                Debug.LogWarning("카메라 변환이 할당되지 않았습니다.", this);

            if (!cameraTargetTransform)
                Debug.LogWarning("No camera target transform has been assigned.", this);

            //필요한 변환 참조가 할당되지 않은 경우 이 스크립트를 비활성화하십시오.;
            if (cameraTransform == null || cameraTargetTransform == null)
            {
                enabled = false;
                return;
            }

            //초기 시작 거리 설정;
            _currentDistance = (cameraTargetTransform.position - _tr.position).magnitude;
        }

        private void LateUpdate()
        {
            //마지막 프레임 이후 무시 목록 길이가 변경되었는지 확인;
            if (_ignoreListLayers.Length != ignoreList.Length)
            {
                //그렇다면 새 길이에 맞게 레이어 배열을 무시하도록 설정하십시오.;
                _ignoreListLayers = new int[ignoreList.Length];
            }

            //(일시적으로) 무시 목록의 모든 개체를 'Raycast 무시' 레이어로 이동하고 나중에 사용할 수 있도록 해당 레이어 값을 저장합니다.
            for (int i = 0; i < ignoreList.Length; i++)
            {
                _ignoreListLayers[i] = ignoreList[i].gameObject.layer;
                ignoreList[i].gameObject.layer = _ignoreRaycastLayer;
            }

            //레이캐스트를 캐스팅하여 현재 거리 계산;
            float distance = GetCameraDistance();

            //레이어 재설정;
            for (int i = 0; i < ignoreList.Length; i++)
                ignoreList[i].gameObject.layer = _ignoreListLayers[i];

            //부드러운 전환을 위한 Lerp 'currentDistance';
            _currentDistance = Mathf.Lerp(_currentDistance, distance, Time.deltaTime * smoothingFactor);

            //'cameraTransform'의 새 위치 설정
            Vector3 position = _tr.position;
            cameraTransform.position = position + (cameraTargetTransform.position - position).normalized * _currentDistance;
        }

        /// <summary>
        /// 이 변환에서 카메라 대상 변환으로 광선(또는 구)을 캐스팅하여 최대 거리를 계산합니다.
        /// </summary>
        /// <returns></returns>
        private float GetCameraDistance()
        {
            RaycastHit hit;

            //캐스팅 방향 계산
            Vector3 castDirection = cameraTargetTransform.position - _tr.position;

            if (castType == CastType.Raycast)
            {
                //Cast ray;
                if (Physics.Raycast(new Ray(_tr.position, castDirection), out hit,
                        castDirection.magnitude + minimumDistanceFromObstacles, layerMask,
                        QueryTriggerInteraction.Ignore))
                {
                    //'_hit.distance'에서 'minimumDistanceFromObstacles'를 뺄 수 있는지 확인한 다음 거리를 반환합니다.
                    if (hit.distance - minimumDistanceFromObstacles < 0f)
                        return hit.distance;
                    else
                        return hit.distance - minimumDistanceFromObstacles;
                }
            }
            else
            {
                //캐스트 구체
                if (Physics.SphereCast(new Ray(_tr.position, castDirection), spherecastRadius, out hit, castDirection.magnitude, layerMask, QueryTriggerInteraction.Ignore))
                {
                    //반환 거리;
                    return hit.distance;
                }
            }

            //장애물에 부딪히지 않으면 전체 거리를 반환합니다.
            return castDirection.magnitude;
        }
    }
}