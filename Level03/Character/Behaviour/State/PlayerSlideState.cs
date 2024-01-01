using Character.Core.FSM;
using Character.Model;
using Cysharp.Threading.Tasks;
using Dummy.Scripts;
using EnumData;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Utility;

namespace Character.Behaviour.State
{
    [PlayerFSMState(PlayerState.Sliding)]
    public class PlayerSlideState : PlayerFSMState
    {
        public PlayerSlideState(PlayerFSM player) : base(player, 0f)
        {
            
        }

        private UniTask.Awaiter _awaiter;
        public override void OnStart()
        {
            base.OnStart();
            _awaiter = OnSlide().GetAwaiter();
        }

        private async UniTask OnSlide()
        {
            var plane = Model.CurrentSlidePlane;
            
            // NavMeshAgent 해제
            View.NavMeshAgent.enabled = false;
            // 카메라 백 뷰 전환
            View.SlideFollowCamera.gameObject.SetActive(true);
            // 이동 제외 모든 종류 입력 차단
            Model.AddDisabledInput(PlayerModel.InputType.Attack 
                                   | PlayerModel.InputType.Flash 
                                   | PlayerModel.InputType.Skill
            );
            
            // 진입 도약 & 착지
            View.OnTriggerAnimation(PlayerAnimation.SlideEnterLeap);
            await MoveParabola(plane, plane.EnterParabola);
            View.OnTriggerAnimation(PlayerAnimation.SlideEnterLand);
            
            // 이동
            var origin = plane.transform.position;
            plane.GetDirections(out var primaryDirection, out var secondaryDirection);
            var start = origin - primaryDirection * plane.StartFromOrigin;
            var end = origin + primaryDirection * plane.EndFromOrigin;
            Model.SlideHorizontalInput = Vector2.zero;
            {
                transform.position = start;
                float x = 0f;
                float length = plane.EndFromOrigin + plane.StartFromOrigin;
                while (x < length)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update);
                    var delta = Time.deltaTime * plane.MoveSpeed;
                    // end 선 넘지 않게 제한
                    var forwardDelta = x + delta > length ? (length - x) : delta; 
                    var forwardMove = primaryDirection * forwardDelta;
                    // 수평 이동은 순수하게 이동 속도만큼
                    var horizontalDelta = delta * -Model.SlideHorizontalInput.x;
                    // width만큼 수평 이동 제한
                    var horizontalPositionFromOrigin = Vector3.Dot((transform.position - origin), secondaryDirection);
                    if (Mathf.Abs(horizontalPositionFromOrigin + horizontalDelta) > plane.WidthFromOrigin)
                    {
                        horizontalDelta = (plane.WidthFromOrigin - horizontalPositionFromOrigin);
                    }
                    var horizontalMove = secondaryDirection * horizontalDelta;
                    var shift = forwardMove + horizontalMove;
                    transform.position += shift;
                    View.TurnTowardController.SetRotation(shift); // 이동하는 방향으로 회전
                    x += forwardDelta;
                }
            }
            
            // 탈출 도약 & 착지
            View.OnTriggerAnimation(PlayerAnimation.SlideExitLeap);
            await MoveParabola(plane, plane.ExitParabola);
            View.OnTriggerAnimation(PlayerAnimation.SlideExitLand); // -> Idle, ResetAnimation()
            
            // NavMeshAgent 적용
            View.NavMeshAgent.enabled = true;
            // 카메라 쿼터뷰로 전환
            View.SlideFollowCamera.gameObject.SetActive(false);
            // 입력 차단 해제
            Model.IsInputDisabled = false;
        }
        
        private async UniTask MoveParabola(SlidePlane plane, ParabolaByMaximumHeightGenerator generator)
        {
            // 현재 위치 기준으로 포물선 시작
            generator.Start = transform.position; 
            generator.Generate();
            // 플레이어 방향 회전
            View.TurnTowardController.SetRotation((generator.End - generator.Start).Copy(y: 0f));
            
            var parabola = generator.Parabola;
            float t = 0f, x = 0f;
            float length = parabola.HorizontalLength, lengthInv = 1f / length;
            while (t < 1f)
            {
                transform.position = parabola.GetPosition(t);
                await UniTask.Yield(PlayerLoopTiming.Update);
                x += Time.deltaTime * plane.MoveSpeed;
                t = x * lengthInv;
            }
        }

        public override bool OnNext(FSMState<PlayerFSM, PlayerState> nextState)
            => _awaiter.IsCompleted;

    }
}