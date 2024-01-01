using Character.Presenter;
using Cinemachine;
using EnumData;
using Managers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Utility;

public class AnchoredBlendingCamera : MonoBehaviour
{

    /// <summary>
    /// 카메라가 Blending되는 Curve입니다.
    /// </summary>
    public AnimationCurve BlendingCurve;
    /// <summary>
    /// 직접 움직일 VCam입니다. 이 VCam의 초기 위치를 기반으로 설정됩니다. 
    /// </summary>
    public CinemachineVirtualCamera Camera;
    /// <summary>
    /// 플레이어 추적 VCam입니다.
    /// </summary>
    public CinemachineVirtualCamera PlayerFollowCamera;
    /// <summary>
    /// 이 오브젝트의 어떤 축으로 Blending할지 결정합니다.
    /// </summary>
    public Axis BlendAxis;

    public Volume TargetLocalVolume;
    public AnimationCurve DOFCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float TargetDOFFocusDistance;
    [field: SerializeField]
    public bool UseDOFFocusAdjustment { get; set; } = true;
    private DepthOfField _dof;
    private float _initialDofFocusDistance;
    
    private PlayerPresenter _player;
    private BoxCollider _collider;
    private Camera _camera;

    private struct TransformData
    {
        public Vector3 position;
        public Quaternion rotation;

        public TransformData(Transform t)
        {
            position = t.position;
            rotation = t.rotation;
        }
    }
    
    [ReadOnly]
    private float _fov;
    private TransformData _targetTransform;

    private void Start()
    {
        _player = GameManager.Instance.Player;
        _camera = UnityEngine.Camera.main;
        if(!PlayerFollowCamera)
            PlayerFollowCamera = _player.View.VirtualCamera;

        _collider = GetComponent<BoxCollider>();

        _fov = Camera.m_Lens.FieldOfView;
        _targetTransform = new TransformData(Camera.transform);

        if (TargetLocalVolume)
        {
            TargetLocalVolume.profile.TryGet(out _dof);
            if (_dof)
            {
                _initialDofFocusDistance = _dof.focusDistance.value;
                // DebugX.Log($"DOF: {_dof}, Distance: {_initialDofFocusDistance}");
            }
        }
    }

    // 거리 구하기
    private float DistanceFromOrigin()
    {
        var playerPosition = _player.transform.position;
        var closestPoint = _collider.ClosestPoint(playerPosition);
        // 콜라이더 밖에 있으면 무한으로 판정
        if ((closestPoint - playerPosition).sqrMagnitude > Vector3.kEpsilonNormalSqrt)
        {
            return float.PositiveInfinity;
        }
        var originToPlayer = playerPosition - transform.position;
        // BoxCollider의 원하는 로컬 축에 투영
        var projection = Vector3.Dot(originToPlayer, transform.GetLocalAxis(BlendAxis));
        // 투영된 길이가 원하는 길이
        return Mathf.Abs(projection);  
    }
    // 거리 정규화
    private float NormalizedDistanceFromOrigin() 
        => DistanceFromOrigin() / (_collider.size.Get(BlendAxis) * 0.5f * transform.localScale.Get(BlendAxis));
    
    private void Update()
    {
        var normalizedDistance = NormalizedDistanceFromOrigin();
        // 거리 바깥에 있을 때는 카메라 끄기
        if (normalizedDistance > 1f)
        {
            if (Camera.gameObject.activeInHierarchy)
            {
                Camera.gameObject.SetActive(false);
            }
            return;
        }

        // 카메라 키기
        if (!Camera.gameObject.activeInHierarchy)
        {
            Camera.gameObject.SetActive(true);
        }

        // (1 - 거리) 를 x값으로 삼음 : 0 ~ 1 ~ 0이 되도록
        var t = BlendingCurve.Evaluate(1f - normalizedDistance);
        
        // Curve 함숫값에 따라 보간
        var position = Vector3.Lerp(
            PlayerFollowCamera.transform.position, 
            _targetTransform.position, 
            t
        );
        var rotation = Quaternion.Slerp(
            PlayerFollowCamera.Follow.rotation, // CameraRoot 회전 얻어오기 
            _targetTransform.rotation, 
            t
        );
        var fov = Mathf.Lerp(PlayerFollowCamera.m_Lens.FieldOfView, _fov, t);

        // 자식 카메라에 반영
        // Lerp값이 normalizedDistance?: 멀리 있을 때는 빠릿하게, 중심에 가까울 때는 느슨하게
        var cameraTransform = Camera.transform;
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, position, normalizedDistance);
        cameraTransform.rotation = Quaternion.Lerp(cameraTransform.rotation, rotation, normalizedDistance);
        Camera.m_Lens.FieldOfView = Mathf.Lerp(Camera.m_Lens.FieldOfView, fov, normalizedDistance);


        if (UseDOFFocusAdjustment && TargetLocalVolume)
        {
            var dof = DOFCurve.Evaluate(t);
            // var b = Vector3.Distance(_camera.transform.position, _player.transform.position); 
            var b = Mathf.Min(
                Vector3.Distance(_camera.transform.position, _player.transform.position),
                TargetDOFFocusDistance
            );
            // _dof.focusDistance.value = b;
            _dof.focusDistance.value = Mathf.Lerp(
                _player.Model.PlayerFollowCameraDistance, 
                b, 
                dof
            );
            DebugX.Log($"dof: {_dof.focusDistance.value} (a: {_initialDofFocusDistance}, b: {b}, t: {dof})");
        }
        
    }

#if UNITY_EDITOR
    // 시각화
    private void OnDrawGizmos()
    {
        if (!_collider)
        {
            _collider = GetComponent<BoxCollider>();
        }
        const int divide = 20;
        float x = -1f;
        var localAxis = transform.GetLocalAxis(BlendAxis);
        var axisExtent = transform.localScale.Get(BlendAxis) * _collider.size.Get(BlendAxis) * 0.5f;
        var pos = transform.position - localAxis * axisExtent;
        var dx = 1f / divide;
        var mover = localAxis * (axisExtent * dx);
        for (int i = 0; i <= divide * 2; i++)
        {
            x += dx;
            var y = BlendingCurve.Evaluate(1f - Mathf.Abs(x));
            DrawUtility.DrawWireSphere(pos + Vector3.up * y, 0.1f, 8, (a, b) => DebugX.DrawLine(a, b, Color.yellow));
            pos += mover;
        }
    }
#endif
}
