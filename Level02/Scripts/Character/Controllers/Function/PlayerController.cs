using System;
using AutoManager;
using Character.Animation;
using Character.USystem.Hook.View;
using Character.View;
using Cysharp.Threading.Tasks;
using Enemys;
using Items;
using Managers;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utility;

namespace Character.Controllers
{
    [RequireComponent(typeof(PlayerView))]
    public partial class PlayerController
    {
        /// <summary>
        /// 넉백을 받습니다.
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="power"></param>
        public void KnockBack(Vector3 startPosition, float power) =>
            KnockBack((transform.position - startPosition).normalized * power);

        public void KnockBack(Vector3 force)
        {
            _playerView.OnTriggerAnimation(PlayerAnimation.OnHit);
            AddMomentum(force);
        }

        /// <summary>
        /// 인자로 들어온 데미지만큼 HP를 깍습니다.
        /// </summary>
        /// <param name="damage"></param>
        /// <returns>죽으면 true를 반환합니다.</returns>
        public bool TakeDamage(int damage)
        {
            //죽음 상태라면 리턴한다.
            if (_playerModel.OtherState == EOtherState.Death) return false;

            //일반 상태이면 HP를 깍고 못움지이도록 한다.
            var nowInvincible = _playerModel.InvincibleState;
            if (nowInvincible == InvincibleState.Original)
            {
                //체력 깍고 못움직이도록 설정
                Manager.Get<GameManager>().HP -= damage;
                _playerModel.IsStop = true;

                // @플레이어 피격 사운드
                Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[14], transform.position);
            }

            //체력이 0이하면 죽음 상태로 변경
            if (Manager.Get<GameManager>().HP <= 0)
                return true;

            //넉백 및 무적
            _playerModel.InvincibleState = InvincibleState.Invincible;
            return false;
        }

        /// <summary>
        /// 인자로 들어온 데미지만큼 HP를 깍습니다.
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="startPosition"></param>
        /// <param name="power"></param>
        /// <param name="playHitEffect">Hit 이펙트를 재생할 것인가?</param>
        public void TakeDamageWithKnockBack(int damage, Vector3 startPosition, float power, bool playHitEffect = false)
        {
            if (Manager.Get<GameManager>().IsNotAttack) return;
            
            //무적상태라면 아무처리도 하지 않는다.
            if (_playerModel.InvincibleState == InvincibleState.Invincible) return;

            StateReset();

            //죽음 상태라면 리턴한다.
            if (_playerModel.OtherState == EOtherState.Death) return;

            bool isDead = TakeDamage(damage);

            //이펙트를 재생한다.
            //참고, 초콜릿 히트 이펙트 X
            if (playHitEffect)
                AfterEffect().Forget();

            if (isDead) return;
            KnockBack(startPosition, power);
        }

        private async UniTaskVoid AfterEffect()
        {
            //히트 이펙트 재생
            await UniTask.Delay(TimeSpan.FromMilliseconds(100));
            _playerView.OnHitEffect();
        }

                /// <summary>
        /// 상태를 초기화합니다.
        /// </summary>
        private void RopeCancel()
        {
            _hookSystemModel.HookState = USystem.Hook.Model.EHookState.Stop;
            // #region 데이터 초기화
            //
            // _playerModel.OtherState = EOtherState.Nothing;
            // _playerModel.DirectionModeTime = 0;
            // _playerModel.hookShotPosition = Vector3.zero;
            // _playerModel.UseWeight = false;
            // _playerModel.IsStop = false;
            // _playerModel.targetInfo.hasTarget = false;
            // _playerModel.targetInfo.position = Vector3.zero;
            //
            // _hookSystemModel.HookState = USystem.Hook.Model.EHookState.Idle;
            // _hookSystemModel.TargetDirection = Vector3.zero;
            // _hookSystemModel.TargetPosition = Vector3.zero;
            //
            // #endregion
            //
            // _playerView.CurrentAnimator().Play("Idle");
            // _playerView.throwSystem.SetActiveThrow(false);
            // _playerView.ChangeZoomCamera(false, true);
            // _playerView.FreezeRotationCamera(false);
            // _hookSystemView.ChangeWeaponActiveType(WeaponType.Normal);
            // _hookSystemView.HideSystemRope();
            // _hookSystemView.ResetRopeLength();
        }
        
        /// <summary>
        /// 상태를 초기화합니다.
        /// </summary>
        private void StateReset()
        {
            #region 데이터 초기화

            _playerModel.OtherState = EOtherState.Nothing;
            _playerModel.DirectionModeTime = 0;
            _playerModel.hookShotPosition = Vector3.zero;
            _playerModel.UseWeight = false;
            _playerModel.targetInfo.hasTarget = false;
            _playerModel.targetInfo.position = Vector3.zero;

            _hookSystemModel.HookState = USystem.Hook.Model.EHookState.Idle;
            _hookSystemModel.TargetDirection = Vector3.zero;
            _hookSystemModel.TargetPosition = Vector3.zero;

            #endregion

            var target = _hookSystemView.GetGrabTarget();

            if (target)
            {
                StarCandy starCandy = target.GetComponent<StarCandy>();

                Transform grabTransform = _hookSystemView.GetGrabTransform();

                //젤리 끝에서 생성된다.
                StarCandyBomb starCandyBomb =
                    _playerView.CreateStarCandyBomb(starCandy.starCandyThrowable, grabTransform);

                //StarCandy AI를 제거합니다.
                Destroy(target.gameObject);

                starCandyBomb.transform.parent = null;
                starCandyBomb.OnTriggerPhysics();
                starCandyBomb.ChangePivot(PivotStyle.CenterBottom);
                starCandyBomb.OnTriggerExplosion().Forget();
            }
            //자식으로 무언가 있을 경우
            else if (_playerView.GetDummyTransform.childCount > 0)
            {
                Transform targetPoint = _playerView.GetDummyTransform.GetChild(0);

                //손가락 본 트랜스폼
                Transform handTransform = _playerView.GetDummyTransform;

                bool isStarCandy = targetPoint.gameObject.layer == LayerMask.NameToLayer("StarCandy");

                //스타 캔디
                if (isStarCandy)
                {
                    StarCandy starCandy = targetPoint.GetComponent<StarCandy>();

                    //손가락 본 위치에 생성시킵니다.
                    StarCandyBomb starCandyBomb =
                        _playerView.CreateStarCandyBomb(starCandy.starCandyThrowable, handTransform);

                    //StarCandy AI를 제거합니다.
                    Destroy(targetPoint.gameObject);

                    starCandyBomb.transform.parent = null;
                    starCandyBomb.OnTriggerPhysics();
                    starCandyBomb.ChangePivot(PivotStyle.CenterBottom);
                    starCandyBomb.OnTriggerExplosion().Forget();
                }
                //열쇠
                else
                {
                    KeyObject keyObject = targetPoint.GetComponent<KeyObject>();
                    keyObject.transform.parent = null;
                    keyObject.OnApplyPhysics();

                    //땅에 떨궈질 때 예쁘게 떨어지기
                    Transform keyObjectTransform = keyObject.transform;
                    keyObjectTransform.rotation = Quaternion.identity;
                    keyObjectTransform.localScale = Vector3.one;
                }
            }

            _playerView.throwSystem.SetActiveThrow(false);
            _playerView.ChangeZoomCamera(false, true);
            _playerView.FreezeRotationCamera(false);
            _hookSystemView.ResetHandlePosition();
            _hookSystemView.ChangeWeaponActiveType(WeaponType.Normal);
            _hookSystemView.HideSystemRope();
        }

        /// <summary>
        /// 플레이어가 On
        /// </summary>
        /// <param name="coll"></param>
        private void OnHit(Collider coll)
        {
            _playerView.OnChocolateBomb(coll.transform.position);
            TakeDamageWithKnockBack(1, coll.transform.position, _settings.bulletKnockBackPower);
            Destroy(coll.gameObject);
        }

        private async UniTaskVoid InvincibleTimer()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_settings.invincibleRevertTime));
            _playerModel.InvincibleState = InvincibleState.Original;
        }

        /// <summary>
        /// 적들에게 대미지를 줍니다.
        /// </summary>
        public void OnAttack()
        {
            int layer = 1 << LayerMask.NameToLayer("Enemy") | 1 << LayerMask.NameToLayer("StarCandy") |
                        1 << LayerMask.NameToLayer("Boss");

            Collider[] enemys = Physics.OverlapBox(
                _playerView.AttackGuide.center + _playerView.AttackGuide.transform.position,
                _playerView.AttackGuide.size / 2, Quaternion.identity, layer);

            foreach (Collider enemy in enemys)
                enemy.GetComponent<Enemy>().TakeDamage(1, gameObject);
        }

        public void ShowDeathUI()
        {
            _playerView.OpenDeathUI();
            _playerView.SetActiveCursor(false);
            _playerView.FreezeRotationCamera(true);
        }

        /// <summary>
        /// 앞에 열쇠 캔디가 있는지 체크하고 있다면 열쇠를 줍도록하며 애니메이션을 실행합니다.
        /// </summary>
        public void OnTriggerCatchToCandyKey()
        {
            int layer = 1 << LayerMask.NameToLayer("CatchObject");

            Collider[] Keys = Physics.OverlapBox(
                _playerView.CatchGuide.center + _playerView.CatchGuide.transform.position,
                _playerView.CatchGuide.size / 2, Quaternion.identity, layer);

            if (Keys.Length == 0) return;
            _playerModel.Key = Keys[0].transform;

            _playerModel.IsStop = true;
            _playerModel.OtherState = EOtherState.Catch;
            _playerView.OnTriggerAnimation(PlayerAnimation.OnCatch);
            StartFightMode();
        }

        /// <summary>
        /// 애니메이션에서 이벤트를 받아서 실제로 들고있는 연출을 하도록 합니다.
        /// </summary>
        public void OnCatchToCandyKey()
        {
            if (_playerModel.Key == null) return;

            _playerModel.Key.parent = _playerView.PlayerHandDummyBoneTransform;
            _playerModel.Key.localPosition = Vector3.zero;
            _playerModel.Key.localRotation = Quaternion.Euler(new Vector3(90, 0, 0));

            //잡기 상태로 전환
            var keyObject = _playerModel.Key.GetComponent<KeyObject>();
            keyObject.OnCatch();

            _playerView.throwSystem.throwObject = keyObject.GetThrowable();
            _playerView.ChangeZoomCamera(true);
            _playerModel.OtherState = EOtherState.Catch;
            _hookSystemView.ChangeWeaponActiveType(WeaponType.AllHide); //무기를 제거합니다.
        }

        /// <summary>
        /// 내 앞에 스텐드가 있으면 true를 반환하도록 합니다.
        /// </summary>
        /// <returns></returns>
        private bool IsBeFoundStand(Transform candyTransform)
        {
            int layer = 1 << LayerMask.NameToLayer("Stand");

            Collider[] Keys = Physics.OverlapBox(
                _playerView.CatchGuide.center + _playerView.CatchGuide.transform.position,
                _playerView.CatchGuide.size / 2, Quaternion.identity, layer);

            bool isFindStand = Keys.Length != 0;

            if (isFindStand)
            {
                var keyColor = candyTransform.GetComponent<KeyObject>().MyKeyColor;
                var stand = Keys[0].transform.GetComponent<Stand>();
                var standColor = stand.MyStandColor;

                if ((int)keyColor == (int)standColor)
                {
                    stand.OnSuccess?.Invoke();
                    _playerModel.TargetStand = Keys[0].transform;
                    return true;
                }
                else
                {
                    stand.OnFail?.Invoke();
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// 스탠드에 캔디 키를 내려놓습니다.
        /// </summary>
        public void OnPutDownStand()
        {
            //손에 있는 별사탕을 제거 합니다.
            Transform targetPoint = _playerView.PlayerHandDummyBoneTransform.GetChild(0);
            targetPoint.parent = _playerModel.TargetStand.GetChild(1);

            //위치 재 조정
            targetPoint.localPosition = new Vector3(0, 0.739f, 0);
            targetPoint.localRotation = Quaternion.identity;
            targetPoint.localScale = Vector3.one;
            targetPoint.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        /// <summary>
        /// 공격을 끝냅니다.
        /// </summary>
        public void OnFinishAttack()
        {
            //공격 상태가 아니면 return
            if (_playerModel.OtherState != EOtherState.Attack) return;

            _playerModel.IsStop = false;
            _playerModel.OtherState = EOtherState.Nothing;
            _hookSystemView.ChangeWeaponActiveType(WeaponType.Normal);
            _playerView.FreezeRotationCamera(false);
        }

        /// <summary>
        /// 손에 스타 캔디를 가지고 있으면 true를 반환합니다.
        /// </summary>
        /// <returns></returns>
        private bool IsHandCatchToStarCandy()
        {
            if (_playerView.PlayerHandDummyBoneTransform.childCount == 0) return false;

            //손에 있는 별사탕을 제거 합니다.
            Transform targetPoint = _playerView.PlayerHandDummyBoneTransform.GetChild(0);

            bool isStarCandy = targetPoint.gameObject.layer == LayerMask.NameToLayer("StarCandy");

            return isStarCandy;
        }

        /// <summary>
        /// 공중 로프를 실행합니다.
        /// </summary>
        public void OnAirShotRope()
        {
            //공중 로프 발사
            OnHookShot();
            _hookSystemView.ChangeWeaponActiveType(WeaponType.System);
            _playerModel.isLockGravity = false;
            _playerModel.IsJumping = false;
            _playerModel.DoubleJumpCount = 0;

            //  @공중 로프 발사 사운드
            Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[7], transform.position);
        }

        /// <summary>
        /// 로프의 상태를 전환합니다.
        /// </summary>
        /// <param name="value"></param>
        private void OnChangeRopeState(float value)
        {
            switch (value)
            {
                case > 0:
                    ChangeHookState();
                    break;
                case < 0:
                    ChangeHookState();
                    break;
            }

            void ChangeHookState()
            {
                _playerModel.targetInfo.Reset();
                _playerModel.RopeState = _playerModel.RopeState switch
                {
                    ERopeState.MoveToTarget => ERopeState.Pull,
                    ERopeState.Pull => ERopeState.MoveToTarget,
                    _ => _playerModel.RopeState
                };

                if (_hookSystemView)
                    _hookSystemView.ChangeColorByState(_playerModel.RopeState);
                else
                    DebugX.LogWarning("_hookSystemView is null");
            }
        }

        private void OnChangeRopeState(ERopeState value)
        {
            _playerModel.RopeState = value;
            if (_hookSystemView)
                _hookSystemView.ChangeColorByState(_playerModel.RopeState);
            else
                DebugX.LogWarning("_hookSystemView is null");
        }

        private Vector3 MoveToTargetPosition;
        
        /// <summary>
        /// 훅샷을 하기위해 타겟을 바라보는 상태로 전환시키는 함수입니다.
        /// </summary>
        private void OnChangeStateToHookShotRotate(Unit _, bool isAir = false)
        {
            //훅을 던질 수 있는 상태가 아니라면 return
            bool isActiveHookShot = _playerModel.targetInfo.hasTarget;

            if (!isActiveHookShot) return;

            MoveToTargetPosition = _playerModel.targetInfo.position;

            //hookPoint가 카메라 앞에 찍혀 있는 경우
            bool isCamera = CheckObjectInCamera(MoveToTargetPosition);

            if (!isCamera)
                return;

            if (isAir)
            {
                //캐릭터를 공중에서 멈춤 상태로 전환
                _playerModel.isLockGravity = true;
                SetMomentum(Vector3.zero);
            }

            _playerModel.hookShotPosition = MoveToTargetPosition;
            _playerModel.OtherState = EOtherState.HookShotRotate;
            _playerView.SetCameraStyle(ECameraStyle.Velocity);

            //타겟으로 날아가는 방향
            Vector3 targetDirection = (MoveToTargetPosition - transform.position).normalized;

            //플레이어가 타겟을 향해 날아가는 방향을 적용합니다.
            _hookSystemModel.TargetDirection = targetDirection;
        }

        private bool CheckObjectInCamera(Vector3 position)
        {
            Vector3 screenPoint = _camera.WorldToViewportPoint(position);
            bool isOnScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 &&
                              screenPoint.y < 1;

            return isOnScreen;
        }

        private void InitPlayerState()
        {
            _playerModel.OtherState = EOtherState.Nothing;
            _playerModel.CurrentControllerState = ControllerState.Falling;
            _playerModel.targetInfo.Reset();
            _playerModel.hookShotPosition = Vector3.zero;
            _playerModel.isLockGravity = false;
            _playerModel.IsStop = false;
            _playerModel.DirectionModeTime = 0;
            _playerModel.UseWeight = false;
            _playerModel.DoubleJumpCount = 0;
            _playerModel.InvincibleState = InvincibleState.Original;
            _playerModel.isLockGravity = false;
            _playerModel.IsJumping = false;

            _hookSystemModel.HookState = USystem.Hook.Model.EHookState.Idle;
            _hookSystemModel.TargetPosition = Vector3.zero;
            _hookSystemModel.TargetDirection = Vector3.zero;
            _hookSystemView.ResetHandlePosition();
            _hookSystemView.ChangeWeaponActiveType(WeaponType.Normal);
            _playerView.CurrentAnimator().Play("Idle");
            _playerView.FreezeRotationCamera(false);

            SetMomentum(Vector3.zero);

            string currentScene = SceneManager.GetActiveScene().name;

            if (currentScene == "Stage_1")
            {
                SpawnPointManager spawnPointManager = FindObjectOfType<SpawnPointManager>();
                spawnPointManager.Respawn();
            }
            else if (currentScene == "Stage_2")
            {
                SpawnPointManager spawnPointManager = FindObjectOfType<SpawnPointManager>();
                int index = spawnPointManager.GetIndex();

                if (index is >= 0 and <= 7)
                    spawnPointManager.Respawn();
                else if (index == -1)
                {
                    transform.position = new Vector3(4.93f, 9.09f, 40.4f);
                }
            }
        }

        private void ShowDefaultOccluded(Unit _)
        {
            try
            {
                //UI를 전부 숨깁니다.
                _offScreenSystemManager.AllHide();

                //주변에 훅 트리거들을 모두 가져옵니다.
                Collider[] pullObjectPoints = Physics.OverlapSphere(transform.position, _settings.pullObjectFindRadius,
                    _settings.pullLayerMask);

                //주변에 훅 트리거들을 모두 가져옵니다.
                Collider[] moveToTargetPoints = Physics.OverlapSphere(transform.position, _settings.hookShotFindRadius,
                    _settings.moveToTargetLayerMask);

                foreach (Collider moveToPoint in moveToTargetPoints)
                {
                    EHookState testDepthTest = GetPossibleHookShot(moveToPoint);

                    switch (testDepthTest)
                    {
                        case EHookState.Occluded:
                        case EHookState.Possible:
                            _offScreenSystemManager.UpdateTargetUI(moveToPoint, EHookState.Occluded);
                            break;
                        case EHookState.Impossible:
                            _offScreenSystemManager.UpdateTargetUI(moveToPoint, EHookState.Hide);
                            break;
                    }
                }

                foreach (Collider pullObjectPoint in pullObjectPoints)
                {
                    EHookState testDepthTest = GetPossibleHookShot(pullObjectPoint);

                    switch (testDepthTest)
                    {
                        case EHookState.Occluded:
                        case EHookState.Possible:
                            _offScreenSystemManager.UpdateTargetUI(pullObjectPoint, EHookState.Occluded);
                            break;
                        case EHookState.Impossible:
                            _offScreenSystemManager.UpdateTargetUI(pullObjectPoint, EHookState.Hide);
                            break;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void OnFindCatchObjectPoint(Unit _)
        {
            try
            {
                //주변에 훅 트리거들을 모두 가져옵니다.
                Collider[] pullObjectPoints = Physics.OverlapSphere(transform.position, _settings.pullObjectFindRadius,
                    _settings.pullLayerMask);

                //하나도 없으면 리턴
                if (pullObjectPoints.Length < 1)
                    return;

                foreach (Collider pullObjectPoint in pullObjectPoints)
                {
                    EHookState testDepthTest = GetPossibleHookShot(pullObjectPoint);

                    switch (testDepthTest)
                    {
                        case EHookState.Occluded:
                            _offScreenSystemManager.UpdateTargetUI(pullObjectPoint, EHookState.Occluded);
                            break;
                        case EHookState.Impossible:
                            _playerModel.targetInfo.Reset();
                            _offScreenSystemManager.UpdateTargetUI(pullObjectPoint, EHookState.Hide);
                            break;
                    }
                }

                //제일 가까운 훅 트리거를 계산합니다.
                Collider hit = ComputeFirstHookPoint(pullObjectPoints);

                bool isHookPointRangeCenter = ComputeHookPointAndVCenter(hit, _settings.pullPossibleCenterDistance);

                //훅 트리거까지 거리를 계산합니다.
                float distance = Vector3.Distance(hit.transform.position, transform.position);

                //훅을 걸 수 있는 사정거리
                bool isPossibleDistance = distance < _settings.hookLengthMax;

                EHookState hookState = GetPossibleHookShot(hit);

                //훅을 던질 수 있는 상태였지만, 거리가 맞지 않는 다면,
                if (!isHookPointRangeCenter || hookState == EHookState.Possible && !isPossibleDistance)
                {
                    _playerModel.targetInfo.Reset();
                    _offScreenSystemManager.UpdateTargetUI(hit, EHookState.DistanceImpossible);
                }
                else if (hookState == EHookState.Possible)
                {
                    if (hit.gameObject.layer == LayerMask.NameToLayer("StarCandy"))
                        _playerModel.targetInfo.SetTarget(hit.transform.GetChild(1).position);

                    else if (hit.gameObject.layer == LayerMask.NameToLayer("CrackTree"))
                        _playerModel.targetInfo.SetTarget(hit.transform.position);

                    _offScreenSystemManager.UpdateTargetUI(hit, EHookState.Possible);
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        ///  훅샷 포인트를 찾고, 상태를 반영합니다.
        /// </summary>
        private void OnFindHookShotPoint(Unit _)
        {
            try
            {
                //주변에 훅 트리거들을 모두 가져옵니다.
                Collider[] moveToTargetPoints = Physics.OverlapSphere(transform.position, _settings.hookShotFindRadius,
                    _settings.moveToTargetLayerMask);

                //하나도 없으면 리턴
                if (moveToTargetPoints.Length < 1)
                    return;

                //거리에 들어오는 녀석만 처리
                foreach (Collider moveToPoint in moveToTargetPoints)
                {
                    EHookState testDepthTest = GetPossibleHookShot(moveToPoint);

                    switch (testDepthTest)
                    {
                        case EHookState.Occluded:
                            _offScreenSystemManager.UpdateTargetUI(moveToPoint, EHookState.Occluded);
                            break;
                        case EHookState.Impossible:
                            _playerModel.targetInfo.Reset();
                            _offScreenSystemManager.UpdateTargetUI(moveToPoint, EHookState.Hide);
                            break;
                    }
                }

                //제일 가까운 훅 트리거를 계산합니다.
                Collider hit = ComputeFirstHookPoint(moveToTargetPoints);

                bool isHookPointRangeCenter = ComputeHookPointAndVCenter(hit, _settings.hookShotPossibleCenterDistance);

                //훅 트리거까지 거리를 계산합니다.
                float distance = Vector3.Distance(hit.transform.position, transform.position);

                //훅을 걸 수 있는 사정거리
                bool isPossibleDistance = distance < _settings.hookShotPossibleDistance;
                bool isPaddingDistance = distance > 4;

                EHookState hookState = GetPossibleHookShot(hit);

                //훅을 던질 수 있는 상태였지만, 거리가 맞지 않는 다면,
                if (!isHookPointRangeCenter || hookState == EHookState.Possible && !isPossibleDistance)
                {
                    _playerModel.targetInfo.Reset();
                    _offScreenSystemManager.UpdateTargetUI(hit, EHookState.DistanceImpossible);
                }
                else if (hookState == EHookState.Occluded)
                {
                    _playerModel.targetInfo.Reset();
                    _offScreenSystemManager.UpdateTargetUI(hit, EHookState.Occluded);
                }
                else if (isPaddingDistance && hookState == EHookState.Possible)
                {
                    _offScreenSystemManager.UpdateTargetUI(hit, EHookState.Possible);
                    _playerModel.targetInfo.SetTarget(hit.transform.position);
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 히트된 타겟이 화면 중앙과 거리를 체크하여 훅을 걸 수 있는지를 반환합니다.
        /// </summary>
        /// <param name="hit"></param>
        /// <param name="centerDistance"></param>
        /// <returns></returns>
        private bool ComputeHookPointAndVCenter(Collider hit, float centerDistance)
        {
            Vector3 pointViewScreen = _camera.WorldToViewportPoint(hit.transform.position) -
                                      new Vector3(0.5f, 0.5f, 0);
            pointViewScreen.z = 0;
            float distance = Vector3.Distance(pointViewScreen, Vector3.zero);

            //0 이하면 무조건 true 반환
            if (centerDistance <= 0)
                return true;

            return distance * 100 < centerDistance;
        }

        /// <summary>
        /// 화면과 가까운 녀석을 반환합니다.
        /// </summary>
        /// <param name="hockShotPoints"></param>
        /// <returns></returns>
        private Collider ComputeFirstHookPoint(Collider[] hockShotPoints)
        {
            HookShotPoint[] indexDistance = new HookShotPoint[hockShotPoints.Length];

            for (int i = 0; i < hockShotPoints.Length; i++)
            {
                Vector3 pointViewScreen = _camera.WorldToViewportPoint(hockShotPoints[i].transform.position) -
                                          new Vector3(0.5f, 0.5f, 0);
                pointViewScreen.z = 0;
                float distance = Vector3.Distance(pointViewScreen, Vector3.zero);
                indexDistance[i] = new HookShotPoint(i, i, distance);
            }

            //가운데 중심과 가까운 포인트를 찾습니다.
            USorting.QuickSort(indexDistance);

            //가운데 중심과 가까운 hockShotPoint를 반환합니다.
            return hockShotPoints[indexDistance[0].ColliderIndex];
        }

        /// <summary>
        /// 훅샷 포인터에서 플레이어로 재 계산했을 때, 상태를 반환합니다.
        /// </summary>
        /// <param name="hit"></param>
        /// <returns></returns>
        private EHookState GetPossibleHookShot(Collider hit)
        {
            Vector3 origin = transform.position + _collider.center;
            Vector3 hitPosition = hit.transform.position;
            Vector3 playerDirection = (origin - hitPosition).normalized;
            int layerMask = 1 << LayerMask.NameToLayer("Default") | 1 << LayerMask.NameToLayer("Player");

            bool raycast = Physics.Raycast(hitPosition, playerDirection, out RaycastHit playerHit,
                Mathf.Infinity, layerMask);

            if (raycast)
            {
                if (playerHit.collider.CompareTag("Player"))
                    return EHookState.Possible;

                if (playerHit.collider.CompareTag("DisableHookTrigger"))
                    return EHookState.Occluded;
            }

            return EHookState.Impossible;
        }

        /// <summary>
        ///  훅을 걸 수 있는 방향을 바라보도록 플레이어를 회전시킵니다.
        /// </summary>
        private void OnHookShotRotate(Unit _)
        {
            //타겟 방향으로 회전
            Vector3 hookShotDirection = (MoveToTargetPosition - _tr.position).normalized;
            _playerView.RotateTarget(hookShotDirection);

            //해당 타겟을 플레이어가 바라보는데, 얼마나 바라보았는지 0~1로 표현해야한다.
            Vector3 hookPointYEqualPlayerY = MoveToTargetPosition;
            hookPointYEqualPlayerY.y = _tr.position.y;

            //훅 포인트가 플레이어를 바라보는 방향
            Vector3 hookPointToPlayerDirection = (hookPointYEqualPlayerY - _tr.position).normalized;

            float angle = Vector3.Dot(hookPointToPlayerDirection, _playerView.GetForward());

            //날아감
            if (angle > 0.99f)
                _playerModel.OtherState = EOtherState.HookShotFlying;

            //모멘텀(추진력)을 리셋합니다.
            SetMomentum(Vector3.zero);
        }

        private Vector3 PullPosition;

        /// <summary>
        /// 손가락 끈끈이를 던지는 상태로 전환합니다
        /// </summary>
        private void OnStateChangeThrowRope()
        {
            PullPosition = _playerModel.targetInfo.position;

            bool initFightMode = StartFightMode();

            if (initFightMode)
                return;

            //스톱 상태로 만듭니다.
            _playerModel.IsStop = true;

            //카메라를 멈춥니다.
            _playerView.FreezeRotationCamera(true);

            //상태를 던지는 상태로 만듬
            _playerModel.OtherState = EOtherState.ThrowRope;

            //공격 무기로 전환합니다.
            _hookSystemView.ChangeWeaponActiveType(WeaponType.Bending);

            //손가락 끈끈이를 던지는 애니메이션 재생
            _playerView.OnPlayTriggerRope(_playerModel.RopeState);
        }

        /// <summary>
        /// 훅을 던집니다.
        /// </summary>
        private void OnThrowHook(Unit _)
        {
            //IK 타겟이 본을 
            _playerView.SetTwoBoneIKTargetPosition(PullPosition);

            //닿은 오브젝트의 위치를 받아옵니다.
            _hookSystemModel.TargetPosition = PullPosition;

            //Rig에 Weight를 줍니다.
            _playerModel.UseWeight = true;

            //타겟으로 날아가는 방향
            Vector3 targetDirection = (PullPosition - transform.position).normalized;

            //플레이어가 타겟을 향해 날아가는 방향을 적용합니다.
            _hookSystemModel.TargetDirection = targetDirection;

            //로프 발사
            _hookSystemModel.ShotRope(_playerModel.RopeState);
        }

        /// <summary>
        /// 타겟을 향해 날아갑니다.
        /// </summary>
        private void OnFlyToTarget()
        {
//            _hookSystemModel.TargetPosition = _playerModel.hookShotPosition;
            Vector3 targetPosition = MoveToTargetPosition;

            var lerp = Mathf.Lerp(1f, 0f, _hookSystemView.GetDistanceToTargetNormalize());
            var speed = _hookSystemView.MoveToTargetCurve.Evaluate(lerp);


            //Debug.Log((_settings.flyToTargetSpeed + speed));

            try
            {
                Vector3 dir = (targetPosition - transform.position).normalized;
                SetMomentum(dir * (_settings.flyToTargetSpeed + speed));
                //SetMomentum(dir * (_settings.flyToTargetSpeed ));
            }
            catch (NullReferenceException)
            {
                DebugX.LogError("손가락 끈끈이에 target이 비어있습니다.");
            }
        }

        /// <summary>
        /// 거리에 따라 타겟을 향해 날아가는 것을 멈춥니다.
        /// </summary>
        private void OnFlyStop()
        {
            Vector3 targetPosition = MoveToTargetPosition;
            float distance = Vector3.Distance(targetPosition, transform.position);

            if (distance < _settings.placePlayerByHookFly)
            {
                if (_playerModel.OtherState == EOtherState.ThrowRope)
                {
                    _hookSystemModel.HookState = USystem.Hook.Model.EHookState.Idle;
                    _hookSystemView.ResetHandlePosition();
                    _hookSystemView.ChangeWeaponActiveType(WeaponType.Normal);
                    _playerModel.UseWeight = false;
                    _playerView.SetBehaviourID(2);
                }
                else
                {
                    _playerView.SetBehaviourID(2);
                    _playerModel.IsStop = false;
                    _playerModel.UseWeight = false;
                    _playerModel.OtherState = EOtherState.Nothing;
                    _hookSystemModel.HookState = USystem.Hook.Model.EHookState.Idle;
                    _hookSystemView.ResetHandlePosition();
                    _hookSystemView.ChangeWeaponActiveType(WeaponType.Normal);
                    _playerModel.IsJumping = true;
                }

                //_offScreen.SetTarget(null);
                SetMomentum(Vector3.zero);

                Vector3 targetDirection = _hookSystemModel.TargetDirection;
                AddMomentum(targetDirection * _settings.bounceOffPower);
            }
        }

        /// <summary>
        /// 공격모드로 진입하거나, 이미 공격모드라면 FightMode 시간을 연장합니다.
        /// </summary>
        /// <returns>공격 모드를 처음 실행하면 true, 실행 중이라면 false를 반환합니다.</returns>
        private bool StartFightMode()
        {
            switch (_playerModel.DirectionModeTime)
            {
                case 0:
                    _playerModel.DirectionModeTime = _settings.FightReleaseTime;
                    return true;
                case > 0:
                    _playerModel.DirectionModeTime = _settings.FightReleaseTime;
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 훅 상태에서 원래대로 되돌립니다.
        /// </summary>
        private void OnClearState()
        {
            _playerModel.IsStop = false;
            _playerModel.OtherState = EOtherState.Nothing;
            _playerView.FreezeRotationCamera(false);
            _playerView.ResetBehaviourAnimation();
            SetMomentum(Vector3.zero);
        }

        public void OnHookShot()
        {
            Vector3 targetPosition = MoveToTargetPosition;

            //IK 타겟이 본을 
            _playerView.SetTwoBoneIKTargetPosition(targetPosition);

            //타겟 목표를 등불로 처리합니다.
            _hookSystemModel.TargetPosition = targetPosition;

            //Rig에 Weight를 줍니다.
            _playerModel.UseWeight = true;

            _hookSystemView.RefreshRope(_hookSystemModel.TargetPosition);

            //로프 발사
            _hookSystemModel.ShotRope(_playerModel.RopeState);
        }


        /// <summary>
        /// 위치를 강제로 욺깁니다.
        /// </summary>
        /// <param name="position"></param>
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void OnJumpOrDoubleJump()
        {
            //점프 상태이고, 더블 점프 카운트가 넘어가지 않았을 때
            if (_playerModel.IsJumping && _playerModel.DoubleJumpCount < _settings.DoubleJumpCount)
            {
                DoubleJumping();
                return;
            }

            Jumping();
        }

        private void ResetOnJump(Unit _)
        {
            //점프를 초기화합니다.
            _playerModel.IsJumping = false;
            _playerModel.DoubleJumpCount = 0;
            _playerView.ResetBehaviourAnimation();
        }
    }
}