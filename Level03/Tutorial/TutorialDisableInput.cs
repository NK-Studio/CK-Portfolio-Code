using Character.Model;
using Character.Presenter;
using Doozy.Runtime.UIManager.Containers;
using Managers;
using UnityEngine;

namespace Tutorial
{
	public class TutorialDisableInput : TutorialBase
	{
		public PlayerModel.InputType Type = PlayerModel.InputType.None;

		private PlayerPresenter _player;

		public override void Initialize(TutorialController controller)
		{
			_player = GameManager.Instance.Player;
		}

		public override void Enter()
		{
			_player.Model.DisabledInput = Type;
		}

		public override Result Execute()
		{
			return Result.Done;
		}

		public override void Exit()
		{
		}
	}
}