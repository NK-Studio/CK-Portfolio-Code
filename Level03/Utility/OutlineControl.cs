using System;
using Character.View;
using Managers;
using ManagerX;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class OutlineControl : MonoBehaviour
{
    [Tooltip("가까이 가면 보이게하는 거리입니다. (미터 단위)")]
    public float DistanceToOutline = 5f;

    [field: SerializeField] public bool Interaction { get; private set; }

    private bool _isShow;
    private bool _isMouseHovering;

    public LayerMask LayerMask;
    public UnityEvent OnInteraction;

    private Transform _playerTransform;
    private NKOutline _outline;
    private Camera _camera;

    private void Awake()
    {
        _camera = Camera.main;
        _playerTransform = FindAnyObjectByType<PlayerView>().transform;
        TryGetComponent(out _outline);
    }

    private void Start()
    {
        _outline.SetActiveOutline(false);

        // 캐릭터의 거리에 따라 처리
        this.UpdateAsObservable()
            .Where(_ => Interaction)
            .Subscribe(_ => _isShow = CheckPlayerDistance())
            .AddTo(this);

        // 마우스를 올리면 처리가 된다.
        MouseEnter()
            .Subscribe(_ => ShowOutline(true))
            .AddTo(this);

        // 마우스를 내리면 처리가 된다.
        MouseExit()
            .Subscribe(_ => ShowOutline(false))
            .AddTo(this);

        // 가까이 있을 때 상호작용 키를 누르면 처리
        DownKeyInteraction()
            .Where(_ => _isMouseHovering)
            .Subscribe(_ => OnInteraction?.Invoke())
            .AddTo(this);
    }

    private IObservable<Unit> MouseEnter() =>
        this.UpdateAsObservable().Where(_ => _isShow).Where(_ => IsMouseHoveringTopView());

    private IObservable<Unit> MouseExit() =>
        this.UpdateAsObservable().Where(_ => _isShow).Where(_ => !IsMouseHoveringTopView());

    private IObservable<Unit> DownKeyInteraction() => this.UpdateAsObservable().Where(_ => _isShow).Where(_ =>
        AutoManager.Get<InputManager>()
            .Controller.System.Interection.WasPerformedThisFrame());

    /// <summary>
    /// 인자에 따라 아웃라인을 보여줍니다.
    /// </summary>
    /// <param name="showOutline"></param>
    private void ShowOutline(bool showOutline)
    {
        _isMouseHovering = showOutline;
        _outline.SetActiveOutline(showOutline);
    }

    /// <summary>
    /// DistanceToOutline보다 가까이 있으면 true를 반환합니다.
    /// </summary>
    private bool CheckPlayerDistance()
    {
        bool result;
        float distance = Vector3.Distance(_playerTransform.position, transform.position);

        if (distance < DistanceToOutline)
            result = true;
        else
        {
            result = false;
            _outline.SetActiveOutline(false);
        }

        return result;
    }

    /// <summary>
    /// 마우스를 올리면 true를 반환합니다.
    /// </summary>
    /// <returns></returns>
    public bool IsMouseHoveringTopView()
    {
        // 마우스의 스크린 좌표를 월드 좌표로 변환
        Ray ray = _camera.ScreenPointToRay(Mouse.current.position.value);

        // Raycast로 충돌 검사
        return Physics.Raycast(ray, out RaycastHit _, 100f, LayerMask);
    }

    public void TestLog()
    {
        Destroy(gameObject);
    }
}