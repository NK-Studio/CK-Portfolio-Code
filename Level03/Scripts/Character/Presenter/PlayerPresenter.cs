using System;
using System.Collections.Generic;
using Character.Behaviour;
using Character.Input;
using Character.Model;
using Character.View;
using Damage;
using Dummy.Scripts;
using Effect;
using Enemy.Behavior;
using EnumData;
using FMODUnity;
using Level;
using Managers;
using ManagerX;
using Settings;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Utility;
using Logger = NKStudio.Logger;

namespace Character.Presenter
{
    public class PlayerPresenter : MonoBehaviour, IEntity
    {
        [SerializeField]
        private Transform _cameraRoot;

        public Transform CameraRoot => _cameraRoot;
        //View-Model
        private PlayerView _playerView;
        private PlayerModel _playerModel;
        private PlayerBehaviour _playerBehaviour;
        private CharacterInput _characterInput;
        private ObservableStateMachineTrigger _observableStateMachineTrigger;

        private float _velocityXZ;
        private float _velocityY;
        private Vector3 _playerPosition;

        private DOFController _dof;
        
        private Vector3 _rotationDirection;
        private void Awake()
        {
            _playerView = GetComponent<PlayerView>();
            _playerModel = GetComponent<PlayerModel>();
            _playerBehaviour = GetComponent<PlayerBehaviour>();
            _characterInput = GetComponent<CharacterInput>();
            _observableStateMachineTrigger =
                _playerView.CurrentAnimator().GetBehaviour<ObservableStateMachineTrigger>();
            GameManager.Instance.Player = this;
            
            _dof = FindAnyObjectByType<DOFController>();
            //_rotationDirection = _cameraRootMinRotation.eulerAngles - _cameraRoot.transform.rotation.eulerAngles;
        }
        
        /// <summary>
        /// PlayerFollowCameraDistance에 따라서
        /// AnimationCurve를 적용하여
        /// 카메라를 회전합니다
        /// </summary>
        public void CameraRotate(float distance)
        {
            var settings = _playerView.Settings;
            float distanceCurveValue = settings.CameraZoomRotationCurve.Evaluate(Mathf.InverseLerp(
                settings.CameraZoomDistanceRange.Min, 
                settings.CameraZoomDistanceRange.Max, 
                distance
            ));
            var rotationRange = settings.CameraZoomRotationRange;
            float cameraXValue = distanceCurveValue * rotationRange.Max;
            
            cameraXValue = Mathf.Clamp(cameraXValue, rotationRange.Min, rotationRange.Max);
            _cameraRoot.rotation = Quaternion.Euler(cameraXValue, 0, 0);
        }

        // TODO: (숙제) 충분히 길어졌으므로 PlayerBehaviour로 빼는 것 고려
        //TODO 카메라 기본값 가져오게 해야함
        private void CameraZoom(float value)
        {
            var settings = _playerView.Settings;
            float oldDistance = _playerModel.PlayerFollowCameraDistance;
            float newDistance;
            switch (value)
            {
                case < 0: // 축소: 휠을 내릴 때: 음수: 거리가 멀어짐
                    newDistance = oldDistance + settings.CameraZoomSpeed * Time.deltaTime;
                    break;
                case > 0: // 확대: 휠을 올릴 때: 양수: 거리가 가까워짐
                    newDistance = oldDistance - settings.CameraZoomSpeed * Time.deltaTime;
                    break;
                default:
                    return;
            }
            // 카메라 디스턴스 조정
            var zoomDistanceRange = settings.CameraZoomDistanceRange;
            _playerModel.PlayerFollowCameraTargetDistance = Mathf.Clamp(newDistance, zoomDistanceRange.Min, zoomDistanceRange.Max);
            // ScrollY 값 초기화
            _characterInput.ScrollY.Value = 0;
        }

        private void CameraDistanceUpdate()
        {
            _playerModel.PlayerFollowCameraDistance = Mathf.Lerp(
                _playerModel.PlayerFollowCameraDistance, // from
                _playerModel.PlayerFollowCameraTargetDistance, // to
                _playerView.Settings.CameraZoomFollowSpeed * Time.unscaledDeltaTime // speed
            );
            // DOF 조정
            // if (_dof)
                // _dof.FocusDistance = realDistance;
            
            // 카메라 회전
            // CameraRotate(realDistance);
        }

        private void Start()
        {
            CharacterSettings setting = GameManager.Instance.Settings;

            _playerModel.HealthObservable.Subscribe(value =>
            {
                var customizer = setting.HealthItemDropCustomizer;
                var curve = setting.HealthItemDropCustomizerMappingCurve;
                var percentage = curve.Evaluate(value);
                customizer.UpdatePercentage(percentage);
            }).AddTo(this);
            // _playerModel.MaxAmmoCount = setting.PlayerBulletSettings.InitialAmmoCount;
            _playerModel.DefaultMagazine = setting.DefaultBulletSettings.CreateMagazine();
            _playerModel.CurrentTargetPosition = transform.position;
            _playerModel.MovementSpeedObservable.Subscribe(value => _playerView.NavMeshAgent.speed = value);

            _playerModel.PlayerFollowCameraDistanceDefault = _playerView.VirtualCameraPersonFollow.CameraDistance;
            _playerModel.PlayerFollowCameraDistance = _playerModel.PlayerFollowCameraDistanceDefault;
            _playerModel.PlayerFollowCameraTargetDistance = _playerModel.PlayerFollowCameraDistanceDefault;
   
            _playerModel.PlayerFollowCameraDistanceObservable.Subscribe(newValue =>
            {
                _playerView.VirtualCameraPersonFollow.CameraDistance = newValue;
                _playerView.OldVirtualCameraPersonFollow.CameraDistance = newValue;
            }).AddTo(this);

            _playerModel.TimeCutterAvailableQueueCount = setting.TimeCutterSettings.InitialCount;
            
            #region 카메라

            /*
            this.UpdateAsObservable()
                .Subscribe(_ =>
                {
                    var rot = _cameraRoot.rotation.eulerAngles;
                    var speed = Keyboard.current[Key.LeftShift].isPressed ? 50f : 5f;
                    if (Keyboard.current[Key.UpArrow].isPressed)
                    {
                        rot.x += speed * Time.deltaTime;
                    }else if (Keyboard.current[Key.DownArrow].isPressed)
                    {
                        rot.x -= speed * Time.deltaTime;
                    }
                    _cameraRoot.rotation = Quaternion.Euler(rot);
                }).AddTo(this);
            */

            this.UpdateAsObservable()
                .Subscribe(_ => _playerBehaviour.UpdateCameraPanning(_playerView, _playerModel, _cameraRoot)).AddTo(this);
            
            /*
            _playerView.ScrollYKeyObservable()
                .Subscribe(CameraZoom)
                .AddTo(this);
            */
            
            this.UpdateAsObservable()
                .Subscribe(_ => CameraDistanceUpdate())
                .AddTo(this);
            
            #endregion

            #region 이동
            
            this.UpdateAsObservable()
                .Where(_ => _playerModel.CanInput(PlayerModel.InputType.Move) &&
                            _playerView.GetControllerType() is ControllerType.Gamepad or ControllerType.KeyboardWASD)
                .Subscribe(_ => _playerBehaviour.HandleAxisMoveInput(_playerView, _playerModel))
                .AddTo(this);

            this.UpdateAsObservable()
                .Subscribe(_playerView.OnUpdatePush)
                .AddTo(this);

            #endregion

            #region 애니메이션 업데이트

            //땅에 닿았는지를 애니메이션에 반영합니다.

            
            this.UpdateAsObservable()
                .Subscribe(_ => _playerBehaviour.OnUpdateBaseAnimation(_playerView, _playerModel)).AddTo(this);

            /*
            //서클 어택 사운드를 재생합니다.
            _observableStateMachineTrigger
                .OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("ZSlash01"))
                .Subscribe(_ =>
                {
                    setting.Sounds.TryGetValue("ZSlash", out EventReference clip);
                    AutoManager.Get<AudioManager>().PlayOneShot(clip);
                })
                .AddTo(this);

            //플래쉬 어택01 사운드를 재생합니다.
            _observableStateMachineTrigger
                .OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("FlashAttack01"))
                .Subscribe(_ =>
                {
                    setting.Sounds.TryGetValue("FlashAttack", out EventReference clip);
                    AutoManager.Get<AudioManager>().PlayOneShot(clip, "PlayerFlashAttackPhase", 0f);
                })
                .AddTo(this);

            //플래쉬 어택02 사운드를 재생합니다.
            _observableStateMachineTrigger
                .OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("FlashAttack01"))
                .Subscribe(_ =>
                {
                    setting.Sounds.TryGetValue("FlashAttack", out EventReference clip);
                    AutoManager.Get<AudioManager>().PlayOneShot(clip, "PlayerFlashAttackPhase", 1f);
                })
                .AddTo(this);

            //서클 어택 사운드를 재생합니다.
            _observableStateMachineTrigger
                .OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("CircleAttack"))
                .Subscribe(_ =>
                {
                    setting.Sounds.TryGetValue("CircleAttack", out EventReference clip);
                    AutoManager.Get<AudioManager>().PlayOneShot(clip);
                })
                .AddTo(this);

            // 세터 어택 사운드를 재생합니다.
            _observableStateMachineTrigger
                .OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("SectorAttack"))
                .Subscribe(_ =>
                {
                    setting.Sounds.TryGetValue("SectorAttack", out EventReference clip);
                    AutoManager.Get<AudioManager>().PlayOneShot(clip);
                })
                .AddTo(this);

            // 소드 아우라 소리를 재생합니다.
            _observableStateMachineTrigger
                .OnStateEnterAsObservable()
                .Where(info => info.StateInfo.IsName("SwordAura"))
                .Subscribe(_ =>
                {
                    setting.Sounds.TryGetValue("SwordAura", out EventReference clip);
                    AutoManager.Get<AudioManager>().PlayOneShot(clip);
                })
                .AddTo(this);

            */
            #endregion

            #region 무적 시간

            this.UpdateAsObservable().Where(_ => _playerModel.InvincibleTime > 0f).Subscribe(_ =>
            {
                _playerModel.InvincibleTime -= Time.deltaTime;
            });
                
            // 공격 타입별 무적 시간 갱신
            this.UpdateAsObservable().Subscribe(_ =>
            {
                var count = _playerModel.InvincibleTimeByEnemyAttackType.Length;
                var dt = Time.deltaTime;
                for (int i = 0; i < count; i++)
                {
                    if (_playerModel.InvincibleTimeByEnemyAttackType[i] > 0f)
                        _playerModel.InvincibleTimeByEnemyAttackType[i] -= dt;
                }
            });

            #endregion

            #region 치트키

            this.UpdateAsObservable()
                .Where(_ => GameManager.Instance.CheatMode)
                .Subscribe(_ =>
                {
                    if (Keyboard.current[Key.PageUp].wasPressedThisFrame)
                    {
                        Model.SwordAuraCoolTime = 0f;
                        Model.SectorAttackCoolTime = 0f;
                        Model.ZSlashCoolTime = 0f;
                        Model.FlashAttackCoolTime = 0f;
                        Model.Soul = 1f;
                        Model.FlashGauge = 1f;
                        Model.FlashCooldown = 0f;
                    }

                    // 체력 무한(사실상)
                    if (Keyboard.current[Key.I].wasPressedThisFrame)
                    {
                        Model.Health = 999999f;
                    }

                    // 자살치트
                    if (Keyboard.current[Key.L].wasPressedThisFrame)
                    {
                        Damage(Model.Health);
                    }
                }).AddTo(this);

            #endregion
        }

        public float Health
        {
            get => _playerModel.Health;
            set => _playerModel.Health = value;
        }
        public bool IsFreeze => false;
        public float Height => 1f;


        /// <summary>
        /// 플레이어에게 데미지를 입힙니다.
        /// <param name="damage">피해량입니다.</param>
        /// <param name="source">피해를 입힌 오브젝트입니다. 넉백 계산 시 사용됩니다.</param>
        /// <param name="reaction">피해가 일반 피격, 경직, 넉백인지를 구분합니다.</param>
        /// <returns></returns>
        public bool Damage(float damage, GameObject source = null, DamageReaction reaction = DamageReaction.Normal)
            => Damage(PlayerDamageInfo.Get(damage, source, reaction: reaction)) is EntityHitResult.Success or EntityHitResult.Invincible;

        public EntityHitResult Damage(DamageInfo info)
        {
            if (_playerModel.IsDead) return EntityHitResult.Invincible;
            if (_playerModel.IsDamagingState()) return EntityHitResult.Invincible; // 피해 받는 중(경직, 넉백) 피해 무시 
            if (_playerModel.IsInvincibleTime) return EntityHitResult.Invincible; // 무적 시간이 남아있으면 피해 무시
            if (_playerModel.InvincibleFlag) return EntityHitResult.Invincible; // 모종의 이유로 무적 플래그 활성화 시 피해 무시
            if (GameManager.Instance.IsPlayingTimeline) return EntityHitResult.Invincible; // 어쨌든 타임라인 실행 중이면 피해 무시 (최악의 코드)
            
            OnDamageEvent?.Invoke(info);
            _playerBehaviour.OnDamage(_playerModel, _playerView, info);

            return EntityHitResult.Success;
        }

        /// <summary>
        /// 공격을 처리합니다.
        /// </summary>
        public void AttackDamage()
        {
            _playerBehaviour.AttackDamage(-1, DamageMode.Normal, _playerView, _playerModel);
        }

        public void AttackDamage(int index)
        {
            _playerBehaviour.AttackDamage(index, DamageMode.Normal, _playerView, _playerModel);
        }

        public void AttackStack(int index)
        {
            _playerBehaviour.AttackDamage(index, DamageMode.Stack, _playerView, _playerModel);
        }

        public void AttackPopAll(int index)
        {
            _playerBehaviour.AttackDamage(index, DamageMode.PopAll, _playerView, _playerModel);
        }

        /// <summary>
        /// 현재 타겟 위치기반으로 Destination을 업데이트합니다.
        /// </summary>
        public void UpdateDestination()
        {
            _playerView.UpdateDestination(_playerModel.CurrentTargetPosition);
        }

        public UnityAction<DamageInfo> OnDamageEvent { get; set; }


        public void Slide(SlidePlane plane)
        {
            Model.CurrentSlidePlane = plane;
            _playerBehaviour.OnSlide();
        }
        
        public PlayerView View => _playerView;
        public PlayerModel Model => _playerModel;
        public PlayerBehaviour Behaviour => _playerBehaviour;

        private AudioManager AudioManager => AutoManager.Get<AudioManager>();
    }
}