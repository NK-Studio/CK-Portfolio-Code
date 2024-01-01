using System.Collections;
using UnityEngine;

namespace Tutorial
{
	public class TutorialMovement : TutorialBase
	{
		[SerializeField]
		private	RectTransform	rectTransform;
		[SerializeField]
		private	Vector3			endPosition;
		private	bool			isCompleted = false;

		public override void Enter()
		{
			StartCoroutine(nameof(Movement));
		}

		public override Result Execute()
		{
			if ( isCompleted == true )
			{
				return Result.Done;
			}

			return Result.Running;
		}

		public override void Exit()
		{
		}

		private IEnumerator Movement()
		{
			float	current = 0;
			float	percent = 0;
			float	moveTime = 0.5f;
			Vector3	start = rectTransform.anchoredPosition;

			while ( percent < 1 )
			{
				current += Time.deltaTime;
				percent = current / moveTime;

				rectTransform.anchoredPosition = Vector3.Lerp(start, endPosition, percent);

				yield return null;
			}

			isCompleted = true;
		}
	}
}