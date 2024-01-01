using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine.AI;

[TaskDescription("점멸을 수행합니다.")]
public class BossFlash : Action
{
	public SharedGameObject TargetObject;
	public SharedFloat DistanceFromTarget;
	
	private NavMeshAgent _agent;
	
	public override void OnStart()
	{
		if (!_agent)
		{
			_agent = GetComponent<NavMeshAgent>();
		}
	}
	
	public override TaskStatus OnUpdate()
	{
		return Teleport() ? TaskStatus.Success : TaskStatus.Failure;
	}

	private bool Teleport()
	{
		var origin = transform.position;
		var targetTransform = TargetObject.Value.transform;
		var targetPosition = targetTransform.position + Vector3.up * 1f;
		
		var front = origin - targetPosition;
		front.y = 0f; front.Normalize();
		
		var side = Vector3.Cross(Vector3.up, front);
		front.y = 0f; front.Normalize();
		
		var distance = DistanceFromTarget.Value;
		// 플레이어 기준 4방위
		var positions = new Vector3[]
		{
			targetPosition +  front * distance,
			targetPosition + -front * distance,
			targetPosition + -side * distance,
			targetPosition +  side * distance,
		};

		var index = 0;
		var found = false;
		NavMeshHit hit = default;
		foreach (var position in positions)
		{
			// DebugX.Log($"{index}: {position}");
			// DrawUtility.DrawWireSphere(position, 5f, 16, (a, b) => DebugX.DrawLine(a, b, Color.cyan, 5f));
			// DebugX.DrawLine(position, position + Vector3.down * 5f, Color.cyan, 5f);
			// 4방위에 Raycast
			if (!NavMesh.SamplePosition(
				    position, 
				    out hit, 5f, NavMesh.AllAreas
			    ))
			{
				++index;
				continue;
			}
			// 찾으면 그 위치로 이동
			found = true;
			break;
		}

		if (!found)
		{
			DebugX.LogWarning("플레이어에게 점멸 실패 ...");
			return false;
		}

		var teleportPosition = hit.position;
		// DrawUtility.DrawWireSphere(teleportPosition, 1f, 16, (a, b) => DebugX.DrawLine(a, b, Color.green, 5f));
		DebugX.Log($"{teleportPosition} 위치로 이동 :: {index}");
		_agent.Warp(teleportPosition);

		var teleportDirection = (targetPosition - _agent.transform.position);
		teleportDirection.y = 0; teleportDirection.Normalize();
		_agent.transform.rotation = Quaternion.LookRotation(teleportDirection);

		return true;
	}
}