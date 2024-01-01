using System;
using Character.Model;
using Character.Presenter;
using Enemy.Behavior;
using EnumData;
using Managers;
using UI;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Utility;

public class MouseUIController : MonoBehaviour
{
    private enum EMouseStyle
    {
        Default,
        Attack
    }

    private enum EState
    {
        NoPlay,
        Play
    }

    [SerializeField] private EState state;

    [Tooltip("Pivot 오브젝트를 연결해야합니다.")] 
    public RectTransform NormalMouseUI;
    
    // Attack UI
    public RectTransform AttackMouseUI;
    public RectTransform PointerUI;
    public Image PointerArrowUI;
    public Image AttackCrosshairUI;
    public Image AttackOutlineUI;

    private EMouseStyle _mouseStyle;

    public float RotateOffset;

    private Transform _playerTransform;
    private Camera _camera;

    private PlayerPresenter _player;

    public MouseUISetting MouseUISetting;
    public bool UseMouseSpriteSwap = false;

    public Canvas Canvas;
    public float GamepadWorldOffsetY = 1f;
    public float GamepadRadius = 128f;
    private void Start()
    {
        _camera = Camera.main;
        
        // Find Player
        _player = FindAnyObjectByType<PlayerPresenter>();
        if (_player)
            _playerTransform = _player.transform;

        if (!MouseUISetting)
        {
            DebugX.LogWarning("MouseUISetting이 없습니다.");
            return;
        }
        
        // 마우스 포인터의 기본적인 처리
        this.UpdateAsObservable().Subscribe(_ =>
        {
            // 컴퓨터 마우스가 안보이도록 처리
            Cursor.visible = false;

            #region Mouse 포지션 계산

#if ENABLE_INPUT_SYSTEM
            Vector3 mousePosition = Mouse.current.position.value;
#else
            Vector3 mousePosition = Input.mousePosition;
#endif

            #endregion

            #region 마우스 스타일 변경

            switch (state)
            {
                case EState.NoPlay:
                    NormalMouseUI.gameObject.SetActive(true);
                    AttackMouseUI.gameObject.SetActive(false);
                    _mouseStyle = EMouseStyle.Default;
                    break;
                case EState.Play:
                    var gameManager = GameManager.Instance;
                    bool isGameOver = gameManager.IsGameOver();
              
                    if (isGameOver)
                    {
                        NormalMouseUI.gameObject.SetActive(true);
                        AttackMouseUI.gameObject.SetActive(false);
                        _mouseStyle = EMouseStyle.Default;
                    }
                    else
                    {
                        bool isActiveMenu = gameManager.IsActiveMenu; // 메뉴 창 열었을 때
                        bool isPlayingTimeline = gameManager.IsPlayingTimeline; // 재생중일 때
                        NormalMouseUI.gameObject.SetActive(isActiveMenu); // 메뉴 살아있을 때에만 존재
                        AttackMouseUI.gameObject.SetActive(!isPlayingTimeline && !isActiveMenu); // 타임라인 중에는 없음, 메뉴 중에도 없음
                        _mouseStyle = isActiveMenu ? EMouseStyle.Default : EMouseStyle.Attack;  
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            #region 게임패드

            bool isGamepad = ManagerX.AutoManager.Get<InputManager>().CurrentController == ControllerType.Gamepad;
            
            if (isGamepad)
            {
                NormalMouseUI.gameObject.SetActive(false);
                var attackMouseObject = AttackMouseUI.gameObject;
                if (!attackMouseObject.activeSelf)
                {
                    return;
                }
                if (!_playerTransform)
                {
                    return;
                }
                
                var axis = _player.View.GetInput().AimAxis.Value;
                if (axis.IsZero())
                {
                    attackMouseObject.SetActive(false);
                    return;
                }
                
                var playerPositionWS = _playerTransform.position;
                // var offsetWS = axis.ToVectorXZ().normalized * GamepadRadius;
                // var positionSS = (Vector2)_camera.WorldToScreenPoint(playerPositionWS + offsetWS);
                var playerPositionSS = (Vector2)_camera.WorldToScreenPoint(playerPositionWS + Vector3.up * GamepadWorldOffsetY);
                var positionSS = (axis.normalized * (GamepadRadius * Canvas.scaleFactor)) + playerPositionSS;
                AttackMouseUI.position = positionSS;
                return;
            }

            #endregion

            #endregion

            #region 마우스 따라가기

            if (_mouseStyle == EMouseStyle.Attack)
            {
                if (AttackMouseUI)
                    AttackMouseUI.position = mousePosition;
                else
                    DebugX.LogWarning("AttackMouseUI 없습니다.");
            }
            else
            {
                if (NormalMouseUI)
                    NormalMouseUI.position = mousePosition;
                else
                    DebugX.LogWarning("NormalMouseUI가 없습니다.");
            }

            #endregion

            #region 회전

            if (!_playerTransform)
            {
                return;
            }

            Vector3 playerViewportPosition = _camera.WorldToViewportPoint(_playerTransform.position);

            Vector3 pointerViewportPosition = _camera.ScreenToViewportPoint(mousePosition);
            Vector3 direction = playerViewportPosition - pointerViewportPosition;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + RotateOffset;

            if (PointerUI)
                PointerUI.rotation = Quaternion.Euler(0, 0, angle);
            else
            {
#if UNITY_EDITOR
                Debug.LogError("PointerUI가 없습니다.");
#endif
            }

            #endregion
        }).AddTo(this);

        if (_player)
        {
            // 공격 포인트 UI 처리
            _player.Model.OtherStateObservable
                .Where(it => it == PlayerState.Shoot)
                .Subscribe(OnStartShootState)
                .AddTo(this);
        }

        this.UpdateAsObservable()
            .Subscribe(UpdateBattleState)
            .AddTo(this);

        this.UpdateAsObservable()
            .Subscribe(UpdateEnemyTargeting)
            .AddTo(this);
    }

    #region 전투 시간

    private float _battleTime;
    private void OnStartShootState(PlayerState _)
    {
        _battleTime = MouseUISetting.AttackMouseUIDuration;
        UpdateSprites();
    }

    private void UpdateBattleState(Unit _)
    {
        if (_battleTime > 0f)
        {
            _battleTime -= Time.deltaTime;

            if (_battleTime <= 0f)
            {
                UpdateSprites();
            }
            return;
        }
    }

    #endregion

    #region 타게팅

    [SerializeField]
    private LayerMask _enemyLayer;
    [SerializeField] 
    private bool _isEnemyTargeting = false;
    public bool IsEnemyTargeting
    {
        get => _isEnemyTargeting;
        private set
        {
            var old = _isEnemyTargeting;
            _isEnemyTargeting = value;
            if (old != value)
            {
                UpdateSprites();
            }
        }
    }
    
    // 쿼터뷰니까 일단 적게 잡음 ㅎ;
    private const int HitBufferLength = 4;
    private readonly RaycastHit[] _hits = new RaycastHit[HitBufferLength];
    private void UpdateEnemyTargeting(Unit _)
    {
        if(!UseMouseSpriteSwap) return;
        // 마우스 기준 ray 발사
        var ray = _camera.ScreenPointToRay(Mouse.current.position.value);
        int hitCount = Physics.RaycastNonAlloc(ray, _hits, Mathf.Infinity, _enemyLayer.value);
        if (hitCount <= 0)
        {
            IsEnemyTargeting = false;
            return;
        }
        for (int i = 0; i < hitCount; i++)
        {
            var hit = _hits[i];
            // 유효하지 않은 상태의 적은 체크하지 않음
            // 얼어서 날아가고 있거나, 죽었거나 ...
            if (!hit.transform.TryGetComponent(out Monster m) || m.IsFreezeFalling || m.IsFreezeSlipping || m.Health <= 0f)
            {
                continue;
            }

            IsEnemyTargeting = true;
            return;
        }
        // 멀쩡한 적을 하나도 찾지 못하면 사망
        IsEnemyTargeting = false;
    }
    
    #endregion

    #region 스프라이트 교체

    public MouseUISetting.AttackMouseSpriteSettings SpriteSettings => IsEnemyTargeting
        ? MouseUISetting.EnemyTargetMouseSprites
        : MouseUISetting.DefaultMouseSprites;
    
    private void UpdateSprites()
    {
        if(!UseMouseSpriteSwap) return;
        bool isBattleState = _battleTime > 0f;
        if (isBattleState)
        {
            PointerArrowUI.gameObject.SetActive(false);
            AttackCrosshairUI.gameObject.SetActive(true);
            AttackCrosshairUI.sprite = SpriteSettings.Crosshair;
            AttackOutlineUI.sprite = SpriteSettings.OutlineBattle;
        }
        else
        {
            PointerArrowUI.gameObject.SetActive(true);
            PointerArrowUI.sprite = SpriteSettings.DirectionArrow;
            AttackCrosshairUI.gameObject.SetActive(false);
            AttackOutlineUI.sprite = SpriteSettings.OutlineDefault;
        }
    }

    #endregion

}