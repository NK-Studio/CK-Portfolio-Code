using Character.Presenter;
using Cysharp.Threading.Tasks;
using Managers;
using UnityEngine;
using UnityEngine.Events;

namespace Level
{
    public class PlayerMoveSequence : MonoBehaviour
    {
        [SerializeField] private PlayerPresenter _player;
        [SerializeField] private Transform _target;
        [SerializeField] private UnityEvent _onExecuted;
        [SerializeField] private UnityEvent _onReached;
        [SerializeField] private float _epsilon = Vector3.kEpsilon;

        private void Start()
        {
            _player ??= GameManager.Instance.Player;
        }

        public void Execute()
        {
            var targetPosition = _target.position;
            _player.Model.CurrentTargetPosition = targetPosition;
            _player.View.UpdateDestination(targetPosition);
            
            _onExecuted?.Invoke();
            ReachChecker().Forget();
        }

        private bool IsReached() => (_player.transform.position - _target.position).sqrMagnitude <= _epsilon;
        
        private async UniTaskVoid ReachChecker()
        {
            await UniTask.WaitUntil(IsReached);
            _onReached?.Invoke();
        }
    }
}