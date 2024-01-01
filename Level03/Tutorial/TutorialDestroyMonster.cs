using Enemy.Behavior;
using ManagerX;
using UnityEngine;

namespace Tutorial
{
	public class TutorialDestroyMonster : TutorialBase
	{
		[SerializeField] private Monster[] objectList;

		public override void Enter()
		{
			// 파괴해야할 오브젝트들을 활성화
			for (int i = 0; i < objectList.Length; ++i)
			{
				objectList[i].gameObject.SetActive(true);
			}
		}

		public override Result Execute()
		{
			bool allFalse = true;

			foreach (var obj in objectList)
			{
				if (obj.Health > 0f)
				{
					allFalse = false;
					break;
				}
			}
			if (allFalse)
			{
				return Result.Done;
			}

			return Result.Running;
		}

		public override void Exit()
		{

		}
	}
}