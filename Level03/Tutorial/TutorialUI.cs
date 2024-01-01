using Doozy.Runtime.UIManager.Containers;
using UnityEngine;

namespace Tutorial
{
	public class TutorialUI : TutorialBase
	{
		public bool IsShow;

		public override void Enter()
		{
		}

		public override Result Execute()
		{
			// 숨기려는데 이미 Hidden 상태인 경우
			if (!IsShow && (UIRenderer.Instance.UIContainer[(int)UIObjectType.RightBottomContents].isHidden))
			{
				Debug.Log("!IsShow && IsHidden -> SetNextTutorial()");
				return Result.Done;
			}
			// 띄우려는데 이미 Visible 상태인 경우
			if (IsShow && (UIRenderer.Instance.UIContainer[(int)UIObjectType.RightBottomContents].isVisible))
			{
				Debug.Log("IsShow && IsVisible -> SetNextTutorial()");
				return Result.Done;
			}
		
			// 띄울 때 Hidden 상태일 때에만 ShowUI 호출
			if (IsShow)
			{
				Debug.Log("IsShow:");
				if (UIRenderer.Instance.UIContainer[(int)UIObjectType.RightBottomContents].isHidden)
				{
					Debug.Log("IsShow - IsHidden => ShowUI() => SetNextTutorial()");
					UIRenderer.Instance.ShowUI((int)UIObjectType.RightBottomContents);
					return Result.Done;
				}
			}
			// 가릴 떄 Visible 상태일 때에만 HideUI 호출
			else
			{
				Debug.Log("!IsShow:");
				if (UIRenderer.Instance.UIContainer[(int)UIObjectType.RightBottomContents].isVisible)
				{
					Debug.Log("!IsShow - IsVisible => HideUI() => SetNextTutorial()");
					UIRenderer.Instance.HideUI((int)UIObjectType.RightBottomContents);
					return Result.Done;
				}
			}

			// 그 외엔 암것도 안함
			return Result.Done;
		}

		public override void Exit()
		{
		}
	}
}