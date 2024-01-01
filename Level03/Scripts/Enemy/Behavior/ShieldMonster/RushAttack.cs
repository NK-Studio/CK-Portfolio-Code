using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Character.Presenter;
using EnumData;
using Micosmo.SensorToolkit;
using Micosmo.SensorToolkit.BehaviorDesigner;
using UnityEngine.AI;

[TaskDescription("돌진을 수행합니다.")]
public class RushAttack : Action
{
	public SharedFloat RushDistance;
	public SharedFloat RushSpeed;
	public SharedSensor Hitbox;
	public SharedGameObject PlayerObject;

	private Vector3 _direction;
	private float _distance;
	private NavMeshAgent _agent;
	private Rigidbody _rigidbody;
	private RangeSensor _hitbox;
	private bool _isOutOfNavMesh = false;
	public override void OnStart()
	{
		if (!_agent)
		{
			_agent = Owner.GetComponent<NavMeshAgent>();
		}
		
		if (!_rigidbody)
		{
			_rigidbody = Owner.GetComponent<Rigidbody>();
		}

		if (!_hitbox)
		{
			_hitbox = Hitbox.Value as RangeSensor;
		} 
		
		_hitbox.Clear();

		_agent.updatePosition = false;
		_agent.updateRotation = false;
		_agent.isStopped = true;
		_agent.ResetPath();
		_agent.velocity = Vector3.zero;

		// _rigidbody.isKinematic = false;

		_distance = 0f;
		_direction = Owner.transform.forward;
		_direction.y = 0f; _direction.Normalize();
		_isOutOfNavMesh = false; 
	}

	public override void OnFixedUpdate()
	{
		var origin = Owner.transform.position;
		var movementDistance = RushSpeed.Value * Time.deltaTime;
		var movement = _direction * movementDistance;
		var newPosition = origin + movement;

		// 새 위치가 NavMesh 바깥일 경우
		if (!NavMesh.SamplePosition(newPosition, out var hit, movementDistance, NavMesh.AllAreas))
		{
			DebugX.Log($"NavMesh::SamplePosition({newPosition}) failed");
			_isOutOfNavMesh = true;
			return;
		}
		
		// NavMesh ClosestPoint가 새 위치라랑 일정 이상 차이날 경우
		var error = Vector3.Distance(hit.position, newPosition);
		if (error >= 0.1)
		{
			DebugX.Log($"NavMesh::SamplePosition({newPosition}) failed with {hit.position} (error: {error} >= {0.1})");
			_isOutOfNavMesh = true;
			return;
		}
		
		// 앞으로 이동
		_rigidbody.MovePosition(newPosition);
		_distance += movementDistance;
		
		_hitbox.Pulse();
	}

	public override TaskStatus OnUpdate()
	{
		// NavMesh를 벗어난 경우
		if (_isOutOfNavMesh)
		{
			Debug.Log($"돌진형 {Owner.name} NavMesh 이탈");
			return TaskStatus.Success;
		}
		
		// 감지된 오브젝트(벽, 장애물, 플레이어 등) 가 있을 경우
		if (_hitbox.Detections.Count > 0)
		{
			// 플레이어 감지됐을 경우
			if (_hitbox.IsDetected(PlayerObject.Value))
			{
				if(PlayerObject.Value.TryGetComponent(out PlayerPresenter player))
				{
					player.Damage(10f, Owner.gameObject, DamageReaction.Stun);
					Debug.Log($"돌진형 {Owner.name}에 의해 플레이어 피격");
				}
			}
			// 벽이 감지된 경우
			else
			{
				Debug.Log($"돌진형 {Owner.name} 벽에 닿음");
			}
			return TaskStatus.Success;
		}

		// 일정 거리 이상 돌진했을 경우
		if (_distance >= RushDistance.Value)
		{
			Debug.Log($"돌진형 {Owner.name} 최대거리 도달");
			return TaskStatus.Success;
		}

		return TaskStatus.Running;
	}

	public override void OnEnd()
	{
		var lastPosition = _rigidbody.position;
		_agent.updatePosition = true;
		_agent.updateRotation = true;
		_agent.Warp(lastPosition);
		
		// _rigidbody.isKinematic = true;

		_isOutOfNavMesh = false;
	}

	public override void OnReset()
	{
		_distance = 0f;
		_isOutOfNavMesh = false; 
		_hitbox.Clear();
	}
}