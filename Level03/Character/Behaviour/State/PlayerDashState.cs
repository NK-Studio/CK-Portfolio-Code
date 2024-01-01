using System;
using System.Threading;
using Character.Core.FSM;
using Character.Model;
using Cysharp.Threading.Tasks;
using EnumData;
using FMODUnity;
using ManagerX;
using Micosmo.SensorToolkit;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.AI;
using Utility;

namespace Character.Behaviour.State
{
    [PlayerFSMState(PlayerState.Dash)]
    public class PlayerDashState : PlayerFSMState
    {
        public PlayerDashState(PlayerFSM player) : base(player, 0f)
        {
            player.View.DodgeFlashKeyDownObservable()
                .Where(_ => Model.CanInput(PlayerModel.InputType.Flash)
                            && Model.FlashCooldown <= 0 // 쿨타임 없음
                            && Model.FlashGauge >= View.Settings.FlashUseGaugeAmount // 필요 게이지 이상
                            && Model.CanFlash // 사용 가능 여부 체크
                )
                .Subscribe(_ => Player.ChangeState(PlayerState.Dash))
                .AddTo(Player);
            
            Player.UpdateAsObservable()
                .Subscribe(_ =>
                {
                    var dt = Time.deltaTime;
                    if (Model.FlashCooldown > 0)
                    {
                        Model.FlashCooldown -= dt;
                        return;
                    }

                    if (Model.FlashGauge < 1f)
                    {
                        Model.FlashGauge =
                            Mathf.Clamp01(Model.FlashGauge + View.Settings.FlashFillSpeed * dt);
                    }
                })
                .AddTo(Player);
        }

        private bool _isMoving;
        private CancellationTokenSource _tokenSource;
        private CancellationToken _token;
        
        public override void OnStart()
        {
            base.OnStart();
            if (_tokenSource != null)
            {
                _tokenSource.Dispose();
                _tokenSource = null;
            }

            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
            View.ColliderEnabled = false;
            OnFlash().Forget();
        }

        private async UniTaskVoid OnFlash()
        {
            _isMoving = true;
            
            if (Model.IsSkillPrepareState())
                View.OnExitSkillPrepareState(Model);

            View.OnTriggerAnimation(PlayerAnimation.Flash);

            // 점멸 쿨다운
            Model.FlashCooldown = Settings.FlashCooldown;
            Model.FlashGauge -= Settings.FlashUseGaugeAmount;
            Model.CanInterruptSkill = false;

            // 모델에 붙어있는 공격 이펙트를 뗌 
            View.DisconnectAttackEffectFromModelRoot();

            // 현재 위치로 정지
            Model.IsStop = true;
            Model.ApplyCurrentTargetPosition();
            View.NavMeshAgent.SetDestination(transform.position);

            // 선딜 대기
            await UniTask.Delay(TimeSpan.FromSeconds(Settings.FlashTestBeforeDelay), cancellationToken: _token);

            // 1. 앞으로 Raycast
            Vector3 startPosition = transform.position;
            Vector3 forwardSensorPosition = startPosition;
            Transform forwardSensorTransform = View.ForwardRaySensor.transform;
            forwardSensorPosition.y += Settings.FlashForwardRayStartHeight; // 높이 설정
            forwardSensorTransform.position = forwardSensorPosition;
            View.ForwardRaySensor.Length = Settings.FlashDistance; // 길이 설정

            // 방향 구하기
            Vector3 forward = Behaviour.CalculateInputDirectionOrMouse(View, Model);
            forwardSensorTransform.rotation = Quaternion.LookRotation(forward);
            View.TurnTowardController.SetRotation(forward);
            
            View.ForwardRaySensor.Pulse(); // Physics.Raycast
            RayHit forwardHit = View.ForwardRaySensor.GetObstructionRayHit();

            Vector3 downRayOrigin = forwardSensorTransform.position + forward * Settings.FlashDistance; 
            // 장애물 충돌 시 장애물 기준으로 0.5m 뒤로 설정
            if (forwardHit.IsObstructing)
            {
                downRayOrigin = forwardHit.Point + (-forward * Settings.FlashForwardRayBackAmount);
            }
            // 전방 장애물이 없다고 판단되면 ...
            // 경계면에 닿은 경우에는 특수 대시 발생
            else if (NavMesh.Raycast(
                    startPosition,
                    startPosition + forward * Settings.FlashDistance,
                    out var navRaycastHit,
                    Behaviour.NavMeshAreaMask
            ))
            {
                downRayOrigin = navRaycastHit.position;
                // const float debugDuration = 5f;
                // bool found = false;
                // Log("Raycast success - start edge dash");
                // DrawUtility.DrawWireSphere(navRaycastHit.position, 0.1f, 16, DrawUtility.DebugDrawer(Color.cyan, debugDuration, false));
                // Debug.DrawLine(startPosition, navRaycastHit.position, Color.cyan, debugDuration, false);
                var fartherPosition = navRaycastHit.position + forward * Settings.FlashDistance;
                // 1. 전진하면서 좁은 범위의 SamplePosition
                for (int i = 0; i < Settings.EdgeDashSamplePositionCount; i++)
                {
                    // DrawUtility.DrawWireSphere(fartherPosition, Settings.EdgeDashSamplePositionRange, 16, DrawUtility.DebugDrawer(Color.yellow, debugDuration));
                    // SamplePosition 성공하고 ...
                    if (NavMesh.SamplePosition(
                            fartherPosition, 
                            out var navSampleHit, 
                            Settings.EdgeDashSamplePositionRange,
                            Behaviour.NavMeshAreaMask)
                        // 전진하는 방향과 거의 일치할 경우
                        && Vector3.Dot((navSampleHit.position - startPosition).normalized, forward) >= Settings.EdgeDashSamplePositionLimitAngleInCos) 
                    {
                        // Log($"[{i}] sample success");
                        // DrawUtility.DrawWireSphere(navRaycastHit.position, Settings.EdgeDashSamplePositionRange, 16, DrawUtility.DebugDrawer(Color.green, debugDuration, false));
                        // 해당 위치에서 원점 방향으로 다시 Raycast해서 경계선 찾기
                        if ((navRaycastHit.position - navSampleHit.position).Copy(y: 0f).magnitude > Settings.FlashDistance && NavMesh.Raycast(
                                navSampleHit.position,
                                startPosition,
                                out var navReverseRaycastHit,
                                Behaviour.NavMeshAreaMask
                        ) && (navReverseRaycastHit.position - navRaycastHit.position).Copy(y: 0f).magnitude > Settings.FlashDistance)
                        {
                            // Debug.DrawLine(navSampleHit.position, navReverseRaycastHit.position, Color.green, debugDuration, false);
                            downRayOrigin = navReverseRaycastHit.position;
                        }
                        // 반대방향 못 찾으면 일단 찾은 원점으로 설정
                        else
                        {
                            downRayOrigin = navSampleHit.position;
                        }

                        // found = true;
                        break;
                    }
                    else
                    {
                        // Log($"[{i}] sample failed");
                    }

                    // 전진
                    fartherPosition += forward * Settings.EdgeDashSamplePositionRange;
                }

                /*
                if (!found)
                {
                    // 2. 실패 시 넓은 범위의 SamplePosition
                    if (NavMesh.SamplePosition(startPosition, out var navLargeSampleHit, Settings.EdgeDashSamplePositionLargeRange, Behaviour.NavMeshAreaMask))
                    {
                        // Log($"large sampled");
                        // DrawUtility.DrawWireSphere(navRaycastHit.position, Settings.EdgeDashSamplePositionLargeRange, 16, DrawUtility.DebugDrawer(Color.green, debugDuration, false));
                        downRayOrigin = navLargeSampleHit.position;
                    }
                    else
                    {
                        // Log($"failed to edge dash");
                        // DrawUtility.DrawWireSphere(navRaycastHit.position, Settings.EdgeDashSamplePositionLargeRange, 16, DrawUtility.DebugDrawer(Color.red, debugDuration, false));
                    }
                }
                */
            }
            
            View.DownRaySensor.transform.position = downRayOrigin + Vector3.up;

            // 점멸 선딜(이었던 것, 스킬에만 적용예정)
            // await UniTask.Delay(TimeSpan.FromSeconds(5f / 30));

            //매쉬를 안보이게 합니다.
            // view.IsActiveGFX = (false);

            #region 카메라

            //올드 카메라를 활성화 합니다.
            Vector3 oldPos = View.VirtualCamera.Follow.position;
            Quaternion oldRot = View.VirtualCamera.Follow.rotation;
            View.OldCameraRoot.SetPositionAndRotation(oldPos, oldRot);

            #endregion

            //활성화
            // View.OldPlayerFollowCamera.gameObject.SetActive(true);

            //도착지점 레이를 가져옵니다.
            View.DownRaySensor.Pulse(); // Physics.Raycast
            RayHit groundHit = View.DownRaySensor.GetObstructionRayHit();
            Vector3 targetPoint;
            // 닿았으면 이동
            if (groundHit.IsObstructing)
            {
                targetPoint = groundHit.Point;
            }

            // 안 닿았으면 SamplePosition
            else if (Behaviour.NavMeshHandler.GetMovablePosition(
                         transform.position,
                         View.DownRaySensor.transform.position,
                         out targetPoint,
                         View.ForwardRaySensor.Length
                     ))
            {
                DrawUtility.DrawWireSphere(
                    targetPoint, View.ForwardRaySensor.Length, 24,
                    (a, b) => DebugX.DrawLine(a, b, Color.green, 5f)
                );
            }
            else
            {
                targetPoint = transform.position;
            }

            //점멸 이펙트 추가
            View.CreateFlashEffect(startPosition);
            Debug.DrawLine(startPosition, targetPoint, Color.white, 3f, false);

            await UniTask.Delay(TimeSpan.FromSeconds(Settings.FlashCameraFollowDelay), cancellationToken: _token);

            View.OldPlayerFollowCamera.gameObject.SetActive(false);

            var positionCurve = Settings.FlashPositionCurve;
            float t = 0f;
            float duration = Settings.FlashDuration;
            while (t < duration)
            {
                var threshold = positionCurve.Evaluate(t / duration);
                var position = Vector3.Lerp(startPosition, targetPoint, threshold);
                transform.position = position;
                View.NavMeshAgent.Warp(position);
                await UniTask.Yield(cancellationToken: _token);
                t += Time.deltaTime;
            }
            
            //도착지점 처리
            Model.CurrentTargetPosition = targetPoint;
            View.NavMeshAgent.SetDestination(targetPoint);
            Model.CanInterruptSkill = true;

            await UniTask.Delay(TimeSpan.FromSeconds(Settings.FlashCameraFollowDelay), cancellationToken: _token);

            //활성화

            await UniTask.Delay(TimeSpan.FromSeconds(Settings.FlashMeshHideTime), cancellationToken: _token);
            //매시를 다시 보이게 합니다.
            // view.IsActiveGFX = (true);

            await UniTask.Delay(TimeSpan.FromSeconds(
                Mathf.Max(0f, Settings.FlashTestAfterDelay - Settings.FlashCameraFollowDelay - Settings.FlashMeshHideTime)
            ), cancellationToken: _token);

            // 로직 종료: 캔슬 가능
            _isMoving = false;
            _tokenSource.Dispose();
            _tokenSource = null;
        }

        public override bool OnNext(FSMState<PlayerFSM, PlayerState> nextState)
        {
            // 대시가 다 끝나면 후속 동작이므로 캔슬 가능
            // 또는 상태가 넉백/사망인 경우는 거기서 중지
            return _isMoving == false || nextState.Key is PlayerState.KnockBack or PlayerState.Dead;
        }

        public override void OnEnd()
        {
            if (_isMoving)
            {
                _tokenSource.Cancel();
                _tokenSource.Dispose();
                _tokenSource = null;
            }
            _isMoving = false;
            var currentPosition = transform.position;
            Model.CurrentTargetPosition = currentPosition;
            View.NavMeshAgent.SetDestination(currentPosition);
            Model.IsStop = false;
            View.OldPlayerFollowCamera.gameObject.SetActive(false);
            Model.CanInterruptSkill = true;
            View.ColliderEnabled = true;
        }
    }
}