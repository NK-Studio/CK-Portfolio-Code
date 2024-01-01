using Sirenix.OdinInspector;
using Tutorial.Helper;
using UnityEngine;

namespace Tutorial
{
	[RequireComponent(typeof(DialogPresenter))]
	public class TutorialDialog : TutorialBase
	{
		private	DialogPresenter _dialogPresenter;
		private SpeakerChanger _speakerChanger;
		[SerializeField, LabelText("대기 여부")] private bool _await = true;

		public override void Initialize(TutorialController controller)
		{
			_dialogPresenter = GetComponent<DialogPresenter>();
			_speakerChanger = GetComponent<SpeakerChanger>();
		}

		public override void Enter()
		{
			_dialogPresenter.Setup();

			if (_speakerChanger)
			{
				_speakerChanger.Execute();
			}
		}

		public override Result Execute()
		{
			if (!_await)
			{
				return Result.Done;
			}
			// 대사 흐름 완료까지 대기
			var isCompleted = _dialogPresenter.IsCompleted();
			return isCompleted ? Result.Done : Result.Running;
		}
	

		public override void Exit()
		{
		
		}
	}
}