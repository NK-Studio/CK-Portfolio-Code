using Animation;
using AutoManager;
using Character.USystem.Hook.Model;
using Character.USystem.Hook.View;
using Enemys;
using Managers;
using Platform;
using Settings;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Utility;
using Zenject;
using EHookState = Character.USystem.Hook.Model.EHookState;

namespace Character.USystem.Hook.Presenter
{
    public class HookSystemPresenter : MonoBehaviour
    {
        private HookSystemView _view;
        private HookSystemModel _model;

        private CharacterSettings _settings;

        [Inject] private DiContainer _container;

        private void Awake()
        {
            _model = GetComponent<HookSystemModel>();
            _view = GetComponent<HookSystemView>();
            _settings = Manager.Get<GameManager>().characterSettings;
        }

        private void Start()
        {
            _view.HideSystemRope();

            //훅 시스템 상태에 따라 훅이 날아가거나 되돌아오는 역할을 합니다.
            this.FixedUpdateAsObservable()
                .Subscribe(_ => _view.OnMoveHook(_model.HookState, _model.RopeState, _model.TargetPosition))
                .AddTo(this);

            this.LateUpdateAsObservable()
                .Where(_ => _model.HookState == EHookState.Forward)
                .Where(_ => _model.RopeState == ERopeState.MoveToTarget)
                .Subscribe(_ => _view.TestCode())
                .AddTo(this);

            //거리에 따라 타겟을 놓습니다.
            this.FixedUpdateAsObservable()
                .Where(_ => _model.RopeState == ERopeState.Pull)
                .Subscribe(_ => PutTargetByDistance())
                .AddTo(this);

            //선 길이가 최종 길이가 되면 멈춤니다.
            this.FixedUpdateAsObservable()
                .Where(_ => _model.HookState == EHookState.Forward)
                .Subscribe(_ => LineMaxControl())
                .AddTo(this);

            #region 충돌 처리

            #region Pull

            //타겟과 닿았을 경우
            _view.OnTriggerEnterTargetObservable()
                .Where(_ => _model.HookState == EHookState.Forward)
                .Where(_ => _model.RopeState == ERopeState.Pull)
                .Subscribe(OnCollisionStyleEnterTarget)
                .AddTo(this);

            #endregion

            #region MoveToTarget

            //훅샷 상태로 날아갈 때, 목표 타겟(호롱)에 거리가 0.5이하로 가까워졌을 때, 닿은 것으로 처리한다.
            _view.OnTriggerEnterTargetObservable()
                .Where(_ => _model.HookState == EHookState.Forward)
                .Where(_ => _model.RopeState == ERopeState.MoveToTarget)
                .Subscribe(_ =>
                {
                    GameObject ropePangEffect = _container.ResolveId<GameObject>(EffectType.RopePang);
                    Instantiate(ropePangEffect, _view.GetEndHandleTransform().position, Quaternion.identity);

                    _model.TargetPosition = _view.GetEndHandleTransform().position;
                    _model.HookState = EHookState.Stop;
                })
                .AddTo(this);


            // //훅샷 상태로 날아갈 때, 목표 타겟(호롱)에 거리가 0.5이하로 가까워졌을 때, 닿은 것으로 처리한다.
            // this.FixedUpdateAsObservable()
            //     .Where(_ => _model.HookState == EHookState.Forward)
            //     .Where(_ => _model.RopeState == ERopeState.MoveToTarget)
            //     .Subscribe(OnDistanceStyleEnterTarget)
            //     .AddTo(this);

            #endregion

            #endregion

            _model.HookStateObservable.Subscribe(state =>
            {
                if (state == EHookState.BackOrMoveTarget)
                {
                    if (_view.HasGrabTarget())
                        _view.SetActiveDragEffect(true);
                }
                else if (state == EHookState.Idle)
                {
                    _view.SetActiveDragEffect(false);
                    
                    //_model.TargetDirection = Vector3.zero;
                    //_model.TargetPosition = Vector3.zero;
                }
            }).AddTo(this);

            this.UpdateAsObservable()
                .Where(_ => _model.HookState == EHookState.Idle)
                .Subscribe(_=>_view.ResetHandlePosition())
                .AddTo(this);
        }

        private void OnDistanceStyleEnterTarget(Unit _)
        {
            RaycastHit handRaycast = _view.HandRaycast();

            if (handRaycast.collider)
            {
                //이펙트 실행
                //_view.OnRopePang();

                _model.TargetPosition = handRaycast.point;
                _model.HookState = EHookState.Stop;
            }
        }

        /// <summary>
        /// 타겟과 닿았을 경우 타겟을 그랩 오브젝트의 자식으로 변경하고,
        /// 벽과 충돌시 다시 되돌아오도록 설정합니다.
        /// </summary>
        /// <param name="coll"></param>
        private void OnCollisionStyleEnterTarget(Collider coll)
        {
            //당기기 모드일 경우
            if (_model.RopeState == ERopeState.Pull)
            {
                //적(별사탕) 또는 당기기 오브젝트에 닿으면 타겟을 그랩 타겟으로 설정한다.
                if (coll.CompareTag("Enemy"))
                {
                    _view.SetGrabTarget(coll.transform);
                    // @그랩 부착
                    print("적 당기기 로프 부착");
                    Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[8], transform.position);
                }

                if (coll.CompareTag("CrackTree"))
                {
                    coll.GetComponentInParent<TreePlatform>().OnTriggerAnimation();
                    // @그랩 부착
                    print("부서지는 나무 로프 부착");
                    Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[8], transform.position);
                }

                if (coll.CompareTag("WitchStatue"))
                {
                    coll.GetComponentInParent<WitchStatue>().OnTriggerAnimation();
                    // @그랩 부착
                    print("석상 로프 부착");
                    Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[8], transform.position);
                }
            }

            //끈끈이 젤리를 멈춤 상태로 만든다.
            _model.HookState = EHookState.Stop;
        }

        /// <summary>
        /// 거리에 따라 타겟을 놓습니다.
        /// </summary>
        private void PutTargetByDistance()
        {
            Transform startHandle = _view.GetStartHandleTransform();
            Transform endHandle = _view.GetEndHandleTransform();

            switch (_model.HookState)
            {
                case EHookState.BackOrMoveTarget:
                {
                    float distance = Vector3.Distance(transform.position, endHandle.position);

                    #region 그랩에 걸려있는 타겟을 놓습니다.

                    //해당 거리까지 오면 타겟을 놓는다.
                    if (distance < _settings.hookTargetPlaceDistance)
                    {
                        //타겟을 가져옵니다.
                        Transform target = _view.GetGrabTarget();

                        //타겟이 있을 경우
                        if (target)
                        {
                            //스타 캔디 컴포넌트를 가지고 있는지 체크합니다.
                            StarCandy starCandy = target.GetComponent<StarCandy>();

                            //스타캔디가 아니면 내려놓습니다.
                            if (!starCandy)
                                _view.PutTarget();
                        }
                    }

                    #endregion

                    #region 로프를 정리합니다.

                    //로프를 정리하는 타이밍
                    const float putDistance = 0.4f; //0.7f

                    //이쯤까지 로프가 되돌아오면 멈춤
                    if (distance < putDistance)
                    {
                        _model.HookState = EHookState.Idle;

                        startHandle.localPosition = Vector3.zero;
                        endHandle.localPosition = Vector3.zero;
                    }

                    #endregion

                    break;
                }
            }
        }

        /// <summary>
        /// 라인이 최종 길이가 되면 훅을 멈춥니다.
        /// </summary>
        private void LineMaxControl()
        {
            Transform startHandle = _view.GetStartHandleTransform();
            Transform endHandle = _view.GetEndHandleTransform();

            //Z값만 추출
            Vector3 ropeLocalPosition = endHandle.localPosition;
            ropeLocalPosition.x = 0;
            ropeLocalPosition.y = 0;

            float length = Vector3.Distance(startHandle.localPosition, ropeLocalPosition);

            //특정 길이 만큼 늘어나면 멈춘다.
            if (length > _settings.hookLengthMax)
                _model.HookState = EHookState.Stop;
        }
    }
}