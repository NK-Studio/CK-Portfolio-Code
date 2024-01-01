using UnityEngine;

namespace Character.Smoothing
{
    [ExecuteAlways]
    public class SmoothPosition : MonoBehaviour
    {
        //위치 값이 복사되고 매끄럽게 될 대상 변환
        public Transform Target;

        private Transform _tr;
        private Vector3 _currentPosition;

        //'Lerp'를 smoothType으로 선택했을 때 현재 위치가 목표 위치로 얼마나 빨리 스무딩될 것인지를 제어하는 속도
        public float LerpSpeed = 20f;

        //'SmoothDamp'가 smoothType으로 선택되었을 때 현재 위치가 목표 위치로 얼마나 빨리 스무딩될 것인지를 제어하는 시간
        public float SmoothDampTime = 0.02f;

        //평활화로 인한 지연을 보상하기 위해 위치 값을 외삽할지 여부
        public bool ExtrapolatePosition;

        //'UpdateType'은 스무딩 함수가 'Update' 또는 'LateUpdate'에서 호출되는지 여부를 제어합니다.;
        public enum EUpdateType
        {
            Update,
            LateUpdate
        }

        public EUpdateType UpdateType;

        //다른 SmoothTypes는 다른 알고리즘을 사용하여 대상의 위치를 부드럽게 합니다.; 
        public enum ESmoothType
        {
            Lerp,
            SmoothDamp,
        }

        public ESmoothType SmoothType;

        //게임 시작 시 로컬 위치 오프셋;
        private Vector3 _localPositionOffset;

        private Vector3 _refVelocity;

        private void Awake()
        {
            //대상이 선택되지 않은 경우 이 변환의 상위를 대상으로 선택하십시오.;
            if (!Target)
                Target = transform.parent;

            _tr = transform;
            _currentPosition = _tr.position;

            _localPositionOffset = _tr.localPosition;
        }
        
        private void OnEnable()
        {
            //마지막 위치에서 원치 않는 보간을 방지하기 위해 게임 오브젝트가 다시 활성화될 때 현재 위치 재설정;
            ResetCurrentPosition();
        }

        private void Update()
        {
            if (UpdateType == EUpdateType.LateUpdate)
                return;
            
            SmoothUpdate();
        }

        private void LateUpdate()
        {
            if (UpdateType == EUpdateType.Update)
                return;
            
            SmoothUpdate();
        }

        private void SmoothUpdate()
        {
            //부드러운 현재 위치;
            _currentPosition = Smooth(_currentPosition, Target.position, LerpSpeed);

            //Set position;
            _tr.position = _currentPosition;
        }

        private Vector3 Smooth(Vector3 start, Vector3 target, float smoothTime)
        {
            //로컬 위치 오프셋을 세계 좌표로 변환;
            Vector3 offset = Vector3.zero;
            // Vector3 offset = _tr.localToWorldMatrix * _localPositionOffset;

            //'extrapolateRotation'이 'true'로 설정된 경우 새 대상 위치를 계산합니다.;
            if (ExtrapolatePosition)
            {
                Vector3 difference = target - (start - offset);
                target += difference;
            }

            //대상에 로컬 위치 오프셋 추가;
            target += offset;

            //Smooth(선택한 smoothType 기반) 및 반환 위치;
            switch (SmoothType)
            {
                case ESmoothType.Lerp:
                    return Vector3.Lerp(start, target, Time.deltaTime * smoothTime);
                case ESmoothType.SmoothDamp:
                    return Vector3.SmoothDamp(start, target, ref _refVelocity, SmoothDampTime);
                default:
                    return Vector3.zero;
            }
        }

        /// <summary>
        /// 저장된 위치를 재설정하고 이 게임 오브젝트를 대상의 위치로 직접 이동합니다.
        /// 타겟이 더 먼 거리로 이동했고 보간이 발생하지 않아야 하는 경우(텔레포트) 이 함수를 호출합니다.
        /// </summary>
        public void ResetCurrentPosition()
        {
            //로컬 위치 오프셋을 세계 좌표로 변환;
            // Vector3 offset = _tr.localToWorldMatrix * _localPositionOffset;
            Vector3 offset = Vector3.zero;
            
            //위치 오프셋 추가 및 현재 위치 설정;
            _currentPosition = Target.position + offset;
        }
    }
}