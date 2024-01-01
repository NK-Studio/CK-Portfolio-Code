using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace BehaviorDesigner.Runtime.Tasks.Unity.SharedVariables
{
	[TaskCategory("Unity/Math")]
	[TaskDescription("초당 타겟 변수에 1씩 증감시켜줍니다.")]
	public class AddOnePerSecond : Action
	{
		[Tooltip("타겟 변수")]
		public SharedFloat TargetValue;

		public override TaskStatus OnUpdate()
		{
			TargetValue.Value += Time.deltaTime;
			return TaskStatus.Success;
		}
	}
}