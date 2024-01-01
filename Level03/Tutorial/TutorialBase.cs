using UnityEngine;

namespace Tutorial
{
	public abstract class TutorialBase : MonoBehaviour
	{
		public enum Result
		{
			Done,
			Running,
		}
	
		public virtual void Initialize(TutorialController controller)
		{
		}

		// 해당 튜토리얼 과정을 시작할 때 1회 호출
		public abstract void Enter();

		// 해당 튜토리얼 과정을 진행하는 동안 매 프레임 호출
		public abstract Result Execute();
	
		// 해당 튜토리얼 과정을 종료할 때 1회 호출
		public abstract void Exit();

		public virtual void Bind() { }
	}
}