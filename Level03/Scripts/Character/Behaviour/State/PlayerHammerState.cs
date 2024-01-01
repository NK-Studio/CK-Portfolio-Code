using System.Collections.Generic;
using Character.Core.FSM;
using Character.Model;
using Damage;
using Enemy.Behavior;
using Enemy.Behavior.Boss;
using EnumData;
using Managers;
using ManagerX;
using Settings;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Utility;
using Logger = NKStudio.Logger;

namespace Character.Behaviour.State
{
    [PlayerFSMState(PlayerState.Hammer)]
    public class PlayerHammerState : PlayerFSMState
    {
        private enum State
        {
            None,
            Dash,  // 돌진 상태
            Swing, // 휘두르는 상태
        }
        
        private ObservableStateMachineTrigger _stateMachineTrigger;
        public PlayerHammerState(PlayerFSM player) : base(player, 0f)
        {
            View.RightKeyDownObservable()
                .Where(_ => Model.Health > 0f)
                .Where(_ => Model.CanInput(PlayerModel.InputType.Skill))
                .Subscribe(_ => Player.ChangeState(PlayerState.Hammer))
                .AddTo(Player);

            _stateMachineTrigger = View.Animator.GetBehaviour<ObservableStateMachineTrigger>();
            _stateMachineTrigger.OnStateEnterAsObservable()
                .Where(it => it.StateInfo.IsName("HammerUse"))
                .Subscribe(_ => OnHammerUseAnimation());

            Player.UpdateAsObservable()
                .Subscribe(UpdateIndicator)
                .AddTo(Player);
        }

        private void UpdateIndicator(Unit _)
        {
            
            var indicator = View.HammerDashIndicator;

            if (!indicator 
                // 해머 상태거나 ...
                || IsCurrentState && _isHammerMoving
                // 죽었거나 ...
                || Model.IsDead
                // 사용할 수 없거나 ...
                || !Model.CanInput(PlayerModel.InputType.Skill) 
                // 해머로 전환할 수 없는 상태인 경우 ...
                || CurrentState is PlayerState.Dash or PlayerState.Shoot or PlayerState.Stun or PlayerState.BulletChange
               ) {
                Fail();
                return;
            }
            
            // 1번 범위 안에 빙결된 적이 없었으므로
            // 2번 범위(전방 넓은 범위)의 빙결된 적 체크
            var forward = View.GetMouseOrGamepadStickDirection();
            if (forward.IsZero())
            {
                Fail();
                return;
            }
            _enemiesInHammerFarRange.Clear();
            bool hasDashTarget = false;
            View.HammerFarRange.transform.forward = forward;
            foreach (var obj in View.HammerFarRange.FilteredPulse())
            {
                if (!obj.TryGetComponent(out IFreezable m))
                {
                    continue;
                }

                if (CanBeDashTarget(m))
                {
                    hasDashTarget = true;
                }
                _enemiesInHammerFarRange.Add(m);
            }

            // 2번 범위에도 마땅한 빙결된 적이 없으면 인디케이터 비활성화
            if (!hasDashTarget)
            {
                Fail();
                return;
            }
            
            // 여기까지 오면 Dash: 즉 멀리 끌어당길 적이 있다는 뜻
            // 마우스 방향에 가장 가까운 빙결 적에게 조준
            var origin = _dashStartPosition = transform.position;
            float nearestDot = -1;
            IFreezable nearest = null;
            foreach (var m in _enemiesInHammerFarRange)
            {
                if(!CanBeDashTarget(m)) continue;
                var enemyPosition = m.transform.position;
                var direction = (enemyPosition - origin).Copy(y: 0f).normalized;

                var dot = Vector3.Dot(forward, direction);
                if (dot > nearestDot)
                {
                    nearestDot = dot;
                    nearest = m;
                }
            }

            // ??? 있는데 왜 없어
            if (nearest == null)
            {
                Fail();
                return;
            }
            Model.HammerDashTarget = nearest.gameObject;
            
            // 몬스터 방향으로 인디케이터 설정
            var targetPosition = nearest.transform.position;
            var toTarget = (targetPosition - origin).Copy(y: 0f);
            var length = toTarget.magnitude;
            
            indicator.transform.forward = toTarget * (1f / length);
            indicator.Length = length;
            indicator.gameObject.SetActive(true);
            return;

            void Fail()
            {
                Model.HammerDashTarget = null;
                indicator.gameObject.SetActive(false);
            }
        }

        
        private struct HammerVictim
        {
            public IEntity Monster;
            public float Angle;
        }
        
        private readonly List<HammerVictim> _enemiesInHammerRange = new();
        private readonly List<IFreezable> _enemiesInHammerFarRange = new();
        private AnimationCurve _angleCurve;
        private float _oldCameraDistance;
        private float _timeStopDurationPerMonster;
        private Vector3 _usedPosition;
        private Vector3 _usedDirection;
        private Vector3 _usedRightDirection;
        private bool _isHammerMoving;
        private bool _initialized;
        private State _state;
        private float _dashTime;
        private float _dashExpectedDistanceSquared;
        private Vector3 _dashStartPosition;
        private Vector3 _dashCurrentPosition;

        /// <summary>
        /// 보정 대상이 될 수 있는지 체크합니다.
        /// </summary>
        /// <param name="m">대상 객체입니다.</param>
        /// <returns>살아있고, 빙결 상태이며 아직 빙결 밀림이 동작하지 않았고, 객체 자체가 대상이 될 수 있는지 여부를 체크합니다.</returns>
        private static bool CanBeDashTarget(IFreezable m)
            => m.Health > 0f && m.IsFreeze && !(m.IsFreezeFalling || m.IsFreezeSlipping) && m.CanBeDashTarget;
        
        public override void OnStart()
        {
            base.OnStart();
            GameManager.Instance.CanActiveMenu = false; // 타임스케일 연출 사용으로 인해 메뉴 열기 차단
            _isHammerMoving = true;
            _initialized = false;
            _state = State.None;
            
            // 방향 마우스로 설정
            View.ChangeDirectionToMouseOrGamepadStick();
            // 캔슬 관련 flag 해제
            Model.CanInterruptSkill = false;
            { // NavMesh 정지 처리
                Model.IsStop = true;
                View.NavMeshAgent.isStopped = true;
                View.NavMeshAgent.velocity = Vector3.zero;
                Model.ApplyCurrentTargetPosition();
                View.NavMeshAgent.SetDestination(transform.position);
            }

            // 1번 범위(근접 해머 범위) 에 있는 적들 체크 
            View.HammerRange.Pulse();
            foreach (var obj in View.HammerRange.Detections)
            {
                if (!obj.TryGetComponent(out IFreezable m))
                {
                    continue;
                }

                // 1번 범위 안에 빙결된 적이 있으면, 즉시 Swing
                if (CanBeDashTarget(m))
                {
                    _state = State.Swing;
                    break;
                }
            }

            if (_state == State.Swing)
            {
                // 즉시 스윙
                // View.OnTriggerAnimation(PlayerAnimation.HammerPrepare);
                View.OnTriggerAnimation(PlayerAnimation.Hammer);
                return;
            }
            
            // 1번 범위 안에 빙결된 적이 없었으므로
            // 2번 범위(전방 넓은 범위)의 빙결된 적 체크
            _enemiesInHammerFarRange.Clear();
            View.HammerFarRange.transform.localRotation = Quaternion.identity;
            foreach (var obj in View.HammerFarRange.FilteredPulse())
            {
                if (!obj.TryGetComponent(out IFreezable m))
                {
                    continue;
                }

                if (CanBeDashTarget(m))
                {
                    _state = State.Dash;
                }
                _enemiesInHammerFarRange.Add(m);
            }

            // 2번 범위에도 마땅한 빙결된 적이 없으면
            if (_state == State.None)
            {
                // 마찬가지로 일반 애니메이션 흐름에 따라감 (그냥 넉백 먹일 예정)
                _state = State.Swing;
                View.OnTriggerAnimation(PlayerAnimation.HammerPrepare);
                return;
            }
            
            // 여기까지 오면 Dash: 즉 멀리 끌어당길 적이 있다는 뜻
            // 마우스 방향에 가장 가까운 빙결 적에게 돌진
            var origin = _dashStartPosition = transform.position;
            var forward = View.TurnTowardController.GetForward();
            float nearestDot = -1;
            IFreezable nearest = null;
            foreach (var m in _enemiesInHammerFarRange)
            {
                if(!CanBeDashTarget(m)) continue;
                var enemyPosition = m.transform.position;
                var direction = (enemyPosition - origin).Copy(y: 0f).normalized;

                var dot = Vector3.Dot(forward, direction);
                if (dot > nearestDot)
                {
                    nearestDot = dot;
                    nearest = m;
                }
            }

            // ??? 있는데 왜 없어
            if (nearest == null)
            {
                // 예외처리: 애니메이션 흐름에 따라감 (그냥 넉백 먹일 예정)
                LogWarning("far range 안에 빙결 적이 있었는데 없었습니다", gameObject);
                _state = State.Swing;
                View.OnTriggerAnimation(PlayerAnimation.HammerPrepare);
                return;
            }
            
            // 몬스터 방향으로 설정
            var targetPosition = nearest.transform.position;
            var toTarget = (targetPosition - origin).Copy(y: 0f);
            _dashExpectedDistanceSquared = (toTarget.magnitude - Settings.HammerSettings.DashErrorThreshold).Squared();
            View.TurnTowardController.SetRotation(toTarget);
            // 해머 대시 애니메이션 출력
            View.OnTriggerAnimation(PlayerAnimation.HammerDash);
            // 타임스케일 연출 시작
            // View.SetTimeScale(0f, true);
            // 무적 플래그 켜기
            Model.InvincibleFlag = true;
            // 어둡게
            View.SetFullScreenFillerAlpha(Settings.HammerSettings.ScreenFillerAlphaCurve[0].value);

            _dashTime = 0f;
            _dashCurrentPosition = transform.position;
        }


        private float _elapsedTime;
        private float _angleCurveLength;
        private float _oldAngle;
        private readonly List<IEntity> _enemiesInAngle = new();
        private float _timeStopDuration;
        private int _freezeMonsterCount;
        private ParticleSystemRoot _attackEffect;
        /// <summary>
        /// 해머를 휘두르기 위한 초기화를 진행합니다.
        /// </summary>
        private void InitializeHammer()
        {
            _hammerSettings = Settings.HammerSettings;
            var origin = _usedPosition = transform.position;
            var direction = _usedDirection = View.TurnTowardController.GetForward();
            var right = _usedRightDirection = Vector3.Cross(Vector3.up, direction).normalized;
            var oldCameraDistance = _oldCameraDistance = Model.PlayerFollowCameraDistance;
            var fakeHammer = View.HammerDummy;
            fakeHammer.gameObject.SetActive(true);
            fakeHammer.localRotation = Quaternion.identity;

            Transform modelRoot = View.TurnTowardController.transform;
            var effect = EffectManager.Instance.Get(EffectType.PlayerHammerAttack);
            var effectTransform = effect.transform;
            effectTransform.SetPositionAndRotation(
                modelRoot.TransformPoint(effectTransform.position),
                modelRoot.rotation * effectTransform.rotation
            );
            // Dash로 인해 timeScale = 0인 상태로 오는 경우 있으므로 ...
            if (effect.TryGetComponent(out _attackEffect))
            {
                _attackEffect.UseUnscaledTime = true;
            }
            
            _enemiesInHammerRange.Clear();
            View.HammerRange.Pulse();
            _freezeMonsterCount = 0;
            foreach (var obj in View.HammerRange.Detections)
            {
                if (!obj.TryGetComponent(out IFreezable m) || m.IsFreezeFalling || m.IsFreezeSlipping)
                {
                    continue;
                }

                if (m is BossAquus boss && boss.Health <= 1)
                {
                    _hammerSettings = Settings.SpecialHammerSettings;
                }
                
                var toMonsterDirection = (m.transform.position - origin).Copy(y: 0f).normalized;
                DebugX.DrawLine(origin, origin + toMonsterDirection * 3f, Color.white, 5f);
                DebugX.DrawLine(origin + toMonsterDirection * 3f, origin + toMonsterDirection * 3f + Vector3.up * 2f, Color.white, 5f);
                var angleWithRight = Vector3.Angle(right, toMonsterDirection);
                if (m.IsFreeze) ++_freezeMonsterCount;
                _enemiesInHammerRange.Add(new HammerVictim() { Monster = m, Angle = Mathf.Clamp(angleWithRight, 0f, 180f) });
            }
            // 각도 순으로 정렬 (내림차순) -> 뒤에서부터 짜르려고 ㅋㅋ!
            _enemiesInHammerRange.Sort((x, y) => -x.Angle.CompareTo(y.Angle));
            _timeStopDurationPerMonster = _freezeMonsterCount > 0 ? _hammerSettings.TimeScaleDuration / _freezeMonsterCount : _hammerSettings.TimeScaleDuration;
            // if (freezeMonsterCount > 0)
            // {
                // model.PlayerFollowCameraTargetDistance = oldCameraDistance * settings.CameraMultiplier;
                // view.SetFullScreenFillerAlpha(settings.RadialBlurIntensity);
            // }
            Log($"enemiesInRange: ({_enemiesInHammerRange.Count})");
            foreach (var e in _enemiesInHammerRange)
            {
                Log((e.Monster.IsFreeze ? "<color=cyan>" : "<color=yellow>") + $"- ({e.Angle:F1}) {e.Monster}</color>");
            }

            _elapsedTime = 0f;
            _angleCurve = _hammerSettings.HammerAngleCurve;
            _angleCurveLength = _angleCurve.GetLength();
            _oldAngle = 0f;
            _timeStopDuration = 0f;
            _initialized = true;

            Model.InvincibleTime = _hammerSettings.HammerAngleCurve.GetLength() + _hammerSettings.InvincibleTimeAfterHammerSwing;
        }

        private void OnHammerUseAnimation()
        {
            InitializeHammer();
        }
        public override void OnUpdate()
        {
            // 상태별 갱신
            switch (_state)
            {
                case State.Dash:
                    UpdateDash();
                    return;
                case State.Swing:
                    UpdateHammerSwing();
                    return;
            }
        }

        private void UpdateDash()
        {
            
            // 대시 수행
            var dt = Time.unscaledDeltaTime;
            var shift = Settings.HammerSettings.DashSpeedCurveByTime.Evaluate(_dashTime) * Settings.HammerSettings.DashSpeed * dt;
            var newPosition = _dashCurrentPosition + View.TurnTowardController.GetForward() * shift; 
            
            // 약간 짧은 1번 범위(근접 해머 범위)에 빙결된 적이 있는지 확인
            // 프레임 드랍 시 보정을 위해 60fps 기준 지나간 범위에 이동량만큼 곱해서 판정
            View.HammerDashStopRange.Pulse();
            foreach (var obj in View.HammerDashStopRange.Detections)
            {
                if (!obj.TryGetComponent(out IEntity m))
                {
                    continue;
                }

                // 빙결된 적 확인 시 스윙 시작
                if (m.IsFreeze)
                {
                    StartSwing();
                    return;
                }
            }
            
            
            _dashCurrentPosition = newPosition;
            // 예상 거리를 넘었을 경우 해당 위치에서 바로 시작
            var expectedDistance = _dashExpectedDistanceSquared;
            var fromTo = _dashCurrentPosition - _dashStartPosition;
            var currentDistance = (_dashCurrentPosition - _dashStartPosition).sqrMagnitude;
            var direction = fromTo * (1f / Mathf.Sqrt(currentDistance));
            if (currentDistance >= expectedDistance)
            {
                newPosition = _dashStartPosition + direction * Mathf.Sqrt(expectedDistance);
                transform.position = newPosition;
                View.NavMeshAgent.Warp(newPosition);
                StartSwing();
                return;
            }
            transform.position = newPosition;
            View.NavMeshAgent.Warp(newPosition);

            _dashTime += dt;
            
            return;

            void StartSwing()
            {
                _state = State.Swing;
                View.OnTriggerAnimation(PlayerAnimation.Hammer);
                Model.InvincibleFlag = false;
            }
        }

        private CharacterSettings.HammerSkillSettings _hammerSettings;
        private void UpdateHammerSwing()
        {
            if (!_initialized)
            {
                return;
            }
            
            var fakeHammer = View.HammerDummy;
            var curve = _angleCurve;
            float oldCameraDistance = _oldCameraDistance;

            // 해머 로직 종료. 후속동작 예정, 캔슬 가능 
            if (_enemiesInHammerRange.Count <= 0 && _elapsedTime >= _angleCurveLength && _timeStopDuration <= 0f)
            {
                _isHammerMoving = false;
                Model.CanInterruptSkill = true;
                return;
            }
            
            Vector3 origin = _usedPosition;
            Vector3 direction = _usedDirection;
            float t = _elapsedTime;
            float oldAngle = _oldAngle;
            float timeStopDurationPerMonster = _timeStopDurationPerMonster;
            bool hasFreezeInAngle;
            bool hasSpecialInAngle = false;
            var enemiesInRange = _enemiesInHammerRange;
            var enemiesInAngle = _enemiesInAngle;
            var settings = _hammerSettings;

            if (_timeStopDuration > 0f)
            {
                var curveInput = 1f - (_timeStopDuration / timeStopDurationPerMonster);
                var timeScaleCurveResult = _hammerSettings.TimeScaleCurve.Evaluate(curveInput);
                var timeScale = Mathf.Lerp(_hammerSettings.TimeScale, 1f, timeScaleCurveResult);
                View.SetTimeScale(timeScale, false);

                var radialBlurIntensity = _hammerSettings.RadialBlurIntensityCurve.Evaluate(curveInput);
                View.SetRadialBlurIntensity(radialBlurIntensity);
                
                var chromaticAberrationIntensity = _hammerSettings.ChromaticAberrationIntensityCurve.Evaluate(curveInput);
                View.SetChromaticAberrationIntensity(chromaticAberrationIntensity);

                // 마지막 몬스터일 경우에만 적용
                if (_freezeMonsterCount <= 0)
                {
                    var screenFillerAlpha = _hammerSettings.ScreenFillerAlphaCurve.Evaluate(curveInput);
                    View.SetFullScreenFillerAlpha(screenFillerAlpha);
                }

                var cameraDistance = _hammerSettings.CameraMultiplierCurve.Evaluate(curveInput);
                Model.PlayerFollowCameraTargetDistance = oldCameraDistance * cameraDistance;
                Model.PlayerFollowCameraDistance = Model.PlayerFollowCameraTargetDistance;
                // Log($"input: {curveInput:F3}, result: {curveResult:F3}, timeScale: {timeScale:F3}, tSD: {_hammerTimeStopDuration:F3}, hUT: {_hammerUsedTime:F3}");
                var udt = Time.unscaledDeltaTime;
                _timeStopDuration -= udt;
                _elapsedTime += Time.deltaTime;

                if (_timeStopDuration <= 0f)
                {
                    // 마지막 타격일 경우, 화면 fill 제거
                    if (_freezeMonsterCount <= 0)
                    {
                        View.SetRadialBlurIntensity(0f);
                        Model.PlayerFollowCameraTargetDistance = oldCameraDistance;
                        Model.PlayerFollowCameraDistance = Model.PlayerFollowCameraTargetDistance;
                    }
                    // Log("TimeScale Recovered");
                    // 시간 정지 회복
                    View.SetTimeScale(1f, false);
                    View.CinemachineIgnoreTimescale = false;
                }
                return;
            }

            enemiesInAngle.Clear();
            float newAngle = curve.Evaluate(t) * CharacterSettings.HammerSkillSettings.AttackAngle;
            fakeHammer.localRotation = Quaternion.Euler(0f, -newAngle, 0f);
            DebugX.DrawLine(origin, origin + fakeHammer.rotation * Vector3.right * 3f, Color.yellow.Copy(a: 0.25f), 5f);
            hasFreezeInAngle = false;
            int freezeInAngleCount = 0;
            // 이번 프레임에 이동한 각도 내의 몬스터 모아서
            while (enemiesInRange.Count > 0 && enemiesInRange[^1].Angle < newAngle + settings.AdditionalAngle)
            {
                var monster = enemiesInRange[^1].Monster;
                if (monster.IsFreeze)
                {
                    ++freezeInAngleCount;
                    hasFreezeInAngle = true;
                }
                enemiesInAngle.Add(enemiesInRange[^1].Monster);
                enemiesInRange.RemoveAt(enemiesInRange.Count - 1); // pop
            }
            
            // 타격 연출
            var directionRotation = Quaternion.LookRotation(direction);
            foreach (var m in enemiesInAngle)
            {
                var effectType = m.IsFreeze
                    ? EffectType.EnemyHitHammerToIce
                    : EffectType.EnemyHitHammerToNoneIce;
                var effect = EffectManager.Instance.Get(effectType);
                effect.transform.SetPositionAndRotation(
                    m.transform.position + Vector3.up * (m.Height * 0.5f),
                    effect.transform.rotation * directionRotation
                );
                m.Damage(EnemyDamageInfo.Get(settings.Damage, gameObject, DamageMode.Normal, DamageReaction.Stun,
                    new KnockBackInfo(settings.KnockBack, direction, ForceMode.VelocityChange)
                ));
            }

            // 연출: 시간 정지
            if (hasFreezeInAngle)
            {
                if (_attackEffect)
                {
                    _attackEffect.UseUnscaledTime = false;
                }
                _freezeMonsterCount -= freezeInAngleCount;
                _hammerSettings.RumbleOnHitFrozenEnemy.Pulse();
                Log($"- Stopped at [{oldAngle}, {newAngle + _hammerSettings.AdditionalAngle}] (hammer is on {newAngle})");
                DebugX.DrawLine(origin, origin + fakeHammer.rotation * Vector3.right * 3f, Color.green, 5f);
                View.SetRadialBlurIntensity(_hammerSettings.RadialBlurIntensityCurve[0].value);
                View.SetFullScreenFillerAlpha(_hammerSettings.ScreenFillerAlphaCurve[0].value);
                View.SetTimeScale(_hammerSettings.TimeScale, false);
                View.CinemachineIgnoreTimescale = true;
                Model.PlayerFollowCameraTargetDistance = oldCameraDistance * _hammerSettings.CameraMultiplierCurve[0].value;
                Model.PlayerFollowCameraDistance = Model.PlayerFollowCameraTargetDistance;

                _timeStopDuration = timeStopDurationPerMonster;
                return;
            }

            _oldAngle = newAngle;
            _elapsedTime += Time.unscaledDeltaTime;
            // Log($"Hammer Updated - <color=green>Hammer Used Time: {_hammerUsedTime}</color>");
        }

        public override bool OnNext(FSMState<PlayerFSM, PlayerState> nextState)
        {
            // 모든 해머 공격이 끝나고 후속 동작 실행 중인 경우 캔슬 가능
            // 또는 Idle 회귀인 경우 캔슬 가능
            return _isHammerMoving == false || nextState.Key is PlayerState.KnockBack or PlayerState.Dead;
        }

        public override void OnEnd()
        {
            GameManager.Instance.CanActiveMenu = true; // 타임스케일 연출 사용으로 인한 메뉴 열기 차단 해제
            View.HammerDummy.gameObject.SetActive(false);
            Model.IsStop = false;
            Model.CanFlash = true;
            Model.CanInterruptSkill = true;
            Model.ApplyCurrentTargetPosition();
            View.NavMeshAgent.isStopped = false;
            View.SetRadialBlurIntensity(0f);
            Model.PlayerFollowCameraTargetDistance = _oldCameraDistance;
            _attackEffect = null;
        }
    }
}