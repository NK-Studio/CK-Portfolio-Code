using UnityEngine;
using UnityEngine.Playables;

namespace Tutorial
{
    public class TutorialTimeline : TutorialBase
    {
        [SerializeField]
        private PlayableDirector _playableDirector;

        private bool IsStop = false;
    
        //[Button("PlayTimeLine")]
        // private void Start()
        // {
        //     PlayerChanger.Instance.ChangePlayer();
        //     _playableDirector.Play();
        //     _playableDirector.stopped += OnTimeLineStopped;
        // }
        public override void Enter()
        {
            // PlayerChanger.Instance.ChangePlayer();
            _playableDirector.Play();
            _playableDirector.stopped += OnTimeLineStopped;
        }

        public override Result Execute()
        {
            if (IsStop)
            {
                return Result.Done;
            }

            return Result.Running;
        }

        public override void Exit()
        {
        
        }
    
        private void OnTimeLineStopped(PlayableDirector director)
        {
            // PlayerChanger.Instance.ResetPlayer();
            IsStop = true;
            // 타임라인이 끝난 후 이벤트 리스너 제거
            _playableDirector.stopped -= OnTimeLineStopped;
        }
    }
}