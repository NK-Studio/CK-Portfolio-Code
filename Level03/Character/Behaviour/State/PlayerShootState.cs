using System;
using Character.Core.FSM;
using Character.Core.Weapon;
using Character.Model;
using Cysharp.Threading.Tasks;
using Dummy.Scripts;
using Effect;
using Enemy.Behavior;
using Enemy.Behavior.Boss;
using EnumData;
using Managers;
using ManagerX;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Utility;
using Logger = NKStudio.Logger;

namespace Character.Behaviour.State
{
    [PlayerFSMState(PlayerState.Shoot)]
    public class PlayerShootState : PlayerFSMState
    {
        private int _shootCount;
        private static readonly int ShootAnimationSpeed = Animator.StringToHash("ShootAnimationSpeed");
        public const float DefaultShootAnimationDuration = 0.2f;
        public PlayerShootState(PlayerFSM player) : base(player, 1f)
        {
            View.AttackKeyDownObservable()
                .Where(_ => Model.CanInput(PlayerModel.InputType.Attack) && Model.Health > 0f && Model.Magazine.Ammo > 0 && Model.Magazine.CoolTime <= 0f)
                .Subscribe(_ => Player.ChangeState(PlayerState.Shoot))
                .AddTo(Player);
            
            // 상시 쿨타임 갱신
            Player.UpdateAsObservable()
                .Subscribe(_ =>
                {
                    if (Model.Magazine.CoolTime > 0f)
                    {
                        Model.Magazine.CoolTime -= Time.deltaTime;
                    }
                })
                .AddTo(Player);

            Model.OnMagazineChanged += (prev, next) =>
            {
                if (IsCurrentState)
                {
                    Model.UpdateMovementSpeed();
                }
                View.Animator.SetFloat(ShootAnimationSpeed, DefaultShootAnimationDuration / next.Settings.AnimationDuration);
            };
        }

        public override float GetWeightCrossFadeTime(PlayerState previous) => 0.05f;

        public override void OnStart()
        {
            base.OnStart();
            Model.RegisterMovementSpeedModifier(BulletShootMovementSpeedModifier);
            _shootCount = 0;
        }

        // 방아쇠 당김: 발사
        private void Shoot()
        {
            var magazine = Model.Magazine;
            magazine.Ammo -= 1;
            magazine.CoolTime = magazine.CoolTimeDuration;
            var settings = magazine.Settings;

            // 조이스틱 진동
            settings.RumbleOnShoot.Pulse();
            
            // 애니메이션 트리거
            View.OnTriggerAnimation(PlayerAnimation.Shoot);
            View.OnShootBullet(settings);
            
            var shootPosition = transform.position;
            var maxDistance = settings.MaxDistance;
            var shootCount = settings.Count;

            var playerRotation = View.TurnTowardController.GetRotation();

            var rotation = shootCount <= 1 
                // 1개 발사 시 그냥 플레이어 방향으로만 발사
                ? playerRotation 
                // 여러 발 발사 시 부채꼴 발사
                : Quaternion.AngleAxis(settings.CompensationAngle * 0.5f, Vector3.down) * playerRotation;
            var rotator = shootCount <= 1 
                ? Quaternion.identity 
                : Quaternion.AngleAxis(settings.CompensationAngle / (shootCount - 1), Vector3.up);
            for (int i = 0; i < shootCount; i++)
            {
                // 총알 생성
                var bullet = EffectManager.Instance.Get(settings.BulletType);
                
                // 위치 설정
                if (!bullet.TryGetComponent(out PlayerBullet b))
                {
                    Debug.LogWarning($"플레이어 Bullet {bullet} 에 PlayerBullet이 없습니다.", bullet);
                    continue;
                }
                // 탄환 초기 세팅
                b.Initialize(settings, shootPosition, maxDistance);
                b.SetPositionAndRotation(this, shootPosition, rotation);
                
                // 총알에 트레일 붙이기
                GameObject trail = null;
                FakeChild f = null;
                ParticleTrail pt = null;
                if (settings.TrailType != EffectType.None)
                {
                    trail = EffectManager.Instance.Get(settings.TrailType);
                    if (!trail.TryGetComponent(out f) || !trail.TryGetComponent(out pt))
                    {
                        Debug.LogWarning($"Trail {trail} 에 FakeChild 또는 ParticleTrail이 없습니다.", trail);
                        continue;
                    }
                    f.TargetParent = bullet.transform;
                    pt.Reset();
                }
                
                
                b.OnDisabled.AddListener(() =>
                {
                    if (f)
                    {
                        f.Follow(true);
                        f.TargetParent = null;
                    }
                    // f.transform.position = b.transform.position;
                    // trail.gameObject.SetActive(false);
                    if (pt)
                    {
                        pt.Execute().Forget();
                    }

                    if (trail)
                    {
                        DisableAfter(3f, trail).Forget();
                    }
                });

                rotation *= rotator;
            }

            return;

            // t초 후 비활성화
            static async UniTaskVoid DisableAfter(float t, GameObject target)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(t));
                target.SetActive(false);
            }
        }

        private void UpdateAiming()
        {
            var direction = CompensateShootDirection().Copy(y: 0f).normalized;
            View.TurnTowardController.SetRotation(direction);
        }
        
        public override void OnUpdate()
        {
            // 메뉴 떠 있는 동안에는 갱신하지 않음 !!
            // 이거 없애면 TriggerPressing 때문에 ESC 누르고 마우스 떼면 일시정지가 풀림
            if(GameManager.Instance.IsActiveMenu) return;
            
            UpdateAiming();
            
            // 발사 애니 끝나고 미입력중일 시 탈출
            if (Model.Magazine.CoolTime <= Model.Magazine.CoolTimeDuration - Model.Magazine.Settings.AnimationDuration 
                && !View.GetInput().IsTriggerPressing)
            {
                // Logger.Log("PlayerShootState - Released Key, goto Idle");
                Player.ChangeState(PlayerState.Idle);
                return;
            }
            
            // 발사 이후 후딜 대기
            if (Model.Magazine.CoolTime > 0f)
            {
                return;
            }
            
            // 남은 탄환 수가 없을 경우 재장전
            if (Model.Magazine.Ammo <= 0f)
            {
                // 재장전 불가능한 형태일 경우 기본 무기로 초기화
                if (!Model.Magazine.CanReload)
                {
                    Model.DefaultMagazine.Reset(fullAmmo: false);
                    Model.Magazine = null;
                    Player.ChangeState(PlayerState.PlayerBulletReload);
                    return;
                }
                // Logger.Log("PlayerShootState - Ammo <= 0, goto Idle (would be Reload)");
                Player.ChangeState(PlayerState.Idle);
                return;
            }

            
            // 입력중일 시 반복
            View.OnTriggerAnimation(PlayerAnimation.Behaviour, _shootCount++); // Walk 여러번 재생 방지
            Shoot();
        }

        private float BulletShootMovementSpeedModifier(in float value) =>
            value * Model.Magazine.Settings.Deceleration;

        private Vector3 CompensateShootDirection()
        {
            bool isStrongCompensate = View.GetInput().GetControllerType() == ControllerType.Gamepad;
            
            var bulletSettings = Model.Magazine.Settings;
            var compensationAngleInCos = isStrongCompensate 
                ? bulletSettings.CompensationAngleOnStrongCompensateInCos 
                : bulletSettings.CompensationAngleInCos;
            var curve = bulletSettings.CompensationCurve;
            var multiCurve = bulletSettings.MultiCompensationCurve;
            var maxDistance = bulletSettings.MaxDistance;
            
            var rawDirection = View.GetMouseOrGamepadStickDirection().normalized;
            var origin = transform.position;

#if UNITY_EDITOR
            var compensationAngle = isStrongCompensate ? bulletSettings.CompensationAngleOnStrongCompensate : bulletSettings.CompensationAngle;
            // Debug.Log($"isStrong: {isStrongCompensate}, angleInCos: {compensationAngleInCos}, angle: {compensationAngle}");
            var leftRotation = Quaternion.AngleAxis(compensationAngle * 0.5f, Vector3.up);
            var leftDirection = leftRotation * rawDirection;
            var rightDirection = Quaternion.Inverse(leftRotation) * rawDirection;
            NKStudio.Logger.DrawLine(origin, origin + rawDirection * maxDistance, Color.yellow.Copy(a: 0.5f));
            NKStudio.Logger.DrawLine(origin, origin + leftDirection * maxDistance, Color.white.Copy(a: 0.3f));
            NKStudio.Logger.DrawLine(origin, origin + rightDirection * maxDistance, Color.white.Copy(a: 0.3f));
#endif
            
            var range = View.CompensateRange;
            range.Pulse();
            // 범위 내 모든 대상에 대해, 정규화된 dot 값을 가중치로 사용하여 벡터 선형 결합
            Vector3 direction = Vector3.zero;
            int count = 0;
            float nearestDot = -1f;
            foreach (var c in range.Detections)
            {
                if (!c.CompareTag("Enemy") && !c.CompareTag("Destructible") || !c.TryGetComponent(out Monster m))
                {
                    continue;
                }

                Vector3 toMonster = m.transform.position - origin;
                float distanceSquared = toMonster.sqrMagnitude;

                // 특정 각도 이내 몬스터에 대해
                Vector3 directionToMonster = toMonster * (1f / Mathf.Sqrt(distanceSquared));
                float dot = Vector3.Dot(directionToMonster, rawDirection);

                if (dot < compensationAngleInCos)
                {
                    continue;
                }

                var normalizedArcDistance = Mathf.InverseLerp(compensationAngleInCos, 1f, dot);
                var threshold = multiCurve.Evaluate(normalizedArcDistance);
                var v = directionToMonster * threshold;
                
                if (isStrongCompensate)
                {
                    // 가장 의도한 방향과 가까운 대상 한 개를 대상으로 발사
                    if(dot > nearestDot)
                    {
                        direction = directionToMonster;
                        nearestDot = dot;
                        count = 1;
                    }
                }
                else
                {
                    direction += v;
                    ++count;
                }
                
                NKStudio.Logger.DrawLine(origin, origin + v * 5f, Color.cyan.Copy(a: 0.75f));
            }

            if (direction.IsZero())
            {
                NKStudio.Logger.DrawLine(origin, origin + rawDirection * maxDistance, Color.red);
                return rawDirection;
            }
            var finalDirection = direction.normalized;
            {
                if (count <= 1)
                {
                    float threshold;
                    // 게임패드의 경우 단일 대상 보정 시 snap함
                    if (isStrongCompensate)
                    {
                        threshold = 1f;
                    }
                    else
                    {
                        var normalizedArcDistance = Mathf.InverseLerp(compensationAngleInCos, 1f, Vector3.Dot(rawDirection, finalDirection));
                        threshold = curve.Evaluate(normalizedArcDistance);
                    }
                    var compensatedDirection = Vector3.Slerp(rawDirection, finalDirection, threshold);
                    // 잘못 보정되어 보정 각도를 벗어난 경우 유효하지 않은 보정, 플레이어 raw input 그대로 사용
                    if (Vector3.Dot(rawDirection, compensatedDirection) < compensationAngleInCos)
                    {
                        NKStudio.Logger.DrawLine(origin, origin + rawDirection * maxDistance, Color.yellow);
                        // Debug.Log("Compensate Error on count=1");
                        // Debug.Log($"nAD={normalizedArcDistance}, t={threshold}, cD={compensatedDirection}, rD={rawDirection}");
                        // NKStudio.Logger.DrawLine(origin, origin + rawDirection * maxDistance, Color.yellow, 10f, false);
                        // NKStudio.Logger.DrawLine(origin, origin + compensatedDirection * maxDistance, Color.red, 10f, false);
                        return rawDirection;
                    }
                    NKStudio.Logger.DrawLine(origin, origin + compensatedDirection * maxDistance, Color.green);
                    return compensatedDirection;
                }
                else
                {
                    // 잘못 보정되어 보정 각도를 벗어난 경우 유효하지 않은 보정, 플레이어 raw input 그대로 사용
                    if (Vector3.Dot(rawDirection, finalDirection) < compensationAngleInCos)
                    {
                        NKStudio.Logger.DrawLine(origin, origin + rawDirection * maxDistance, Color.yellow);
                        // Debug.Log($"Compensate Error on count={count}");
                        // Debug.Log($"fD={finalDirection}, rD={rawDirection}, sum={sum}");
                        // NKStudio.Logger.DrawLine(origin, origin + rawDirection * maxDistance, Color.yellow, 10f, false);
                        // NKStudio.Logger.DrawLine(origin, origin + finalDirection * maxDistance, Color.red, 10f, false);
                        return rawDirection;
                    }
                    NKStudio.Logger.DrawLine(origin, origin + finalDirection * maxDistance, Color.green);
                    return finalDirection;
                }

            }
        }

        public override bool OnNext(FSMState<PlayerFSM, PlayerState> nextState)
        {
            // 단순히 입력 종료로 Idle로 돌아가는 경우 Idle 트리거(OnEndAnimation)로 Shoot 애니 FSM 탈출 
            if (nextState.Key == PlayerState.Idle)
            {
                View.OnTriggerAnimation(PlayerAnimation.ShootEnd);
            }
            return nextState.Key 
                is PlayerState.Idle // Idle로 회귀 
                or PlayerState.PlayerBulletReload  // 재장전
                or PlayerState.BulletChange // 재장전
                or PlayerState.Dash // 대시로 캔슬
                or PlayerState.Hammer // 해머로 캔슬
                or PlayerState.Stun
                or PlayerState.KnockBack
                or PlayerState.Dead;
        }

        public override void OnEnd()
        {
            Model.UnregisterMovementSpeedModifier(BulletShootMovementSpeedModifier);
            View.OnTriggerAnimation(PlayerAnimation.Behaviour, 0);
            View.ResetTrigger(PlayerAnimation.Shoot);
        }
    }
}