using Cysharp.Threading.Tasks;
using Managers;
using ManagerX;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tutorial
{
	public class TutorialTrigger : TutorialBase
	{
		[SerializeField]
		private	Transform _triggerObject;

		[SerializeField] private TutorialDecalTrigger _decal;

		[SerializeField] private float _distance = 3f;

		public override void Initialize(TutorialController controller)
		{
			_triggerObject.gameObject.SetActive(false);
		}

		public override void Enter()
		{
			// Trigger 오브젝트 활성화
			_triggerObject.gameObject.SetActive(true);
			_decal.StartDecal();
			DebugX.Log($"StartDecal on {_decal}", _decal);
		}

		public override Result Execute()
		{
			// 거리 기준
			if ( (_triggerObject.position - GameManager.Instance.Player.transform.position).sqrMagnitude < _distance * _distance )
			{
				return Result.Done;
			}

			return Result.Running;
		}
		public override void Exit()
		{
			// Trigger 오브젝트 비활성화
			OnExit().Forget();
		}

		private async UniTaskVoid OnExit()
		{
			DebugX.Log($"EndDecal on {_decal}", _decal);
			await _decal.EndDecal();
			_triggerObject.gameObject.SetActive(false);
		}
	}
}