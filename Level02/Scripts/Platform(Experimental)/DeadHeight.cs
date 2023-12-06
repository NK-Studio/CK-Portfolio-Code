using System;
using Character.Model;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Platform_Experimental_
{
    public class DeadHeight : MonoBehaviour
    {
        private PlayerModel _playerModel;
        private Transform _playerTransform;
        
        [InfoBox("플레이어가 이 오브젝트의 Y보다 밑으로 떨어지면 Kill Event를 발동합니다.")]
        public UnityEvent killEvent;

        private void Awake()
        {
            _playerModel = FindObjectOfType<PlayerModel>();
            _playerTransform = _playerModel.transform;
        }

        private void Start()
        {
            KillEventTrigger().Forget();
        }

        /// <summary>
        /// 플레이어가 이 오브젝트의 Y보다 밑으로 떨어지면 Kill Event를 발동합니다.
        /// 다시 되돌리기 위한 Restore도 같이 실행됩니다.
        /// </summary>
        private async UniTask KillEventTrigger()
        {
            await UniTask.WaitUntil(() => _playerTransform.position.y < transform.position.y,
                cancellationToken: this.GetCancellationTokenOnDestroy());
            
            killEvent?.Invoke();
            Restore().Forget();
        }

        /// <summary>
        /// 플레이어가 Y초과로 다시 올라가면 킬 이벤트 트리거를 다시 실행합니다.
        /// </summary>
        private async UniTask Restore()
        {
            await UniTask.WaitUntil(() => _playerTransform.position.y > transform.position.y,
                cancellationToken: this.GetCancellationTokenOnDestroy());
         
            KillEventTrigger().Forget();
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, new Vector3(10f, 0.05f, 10f));
        }
#endif
    }
}