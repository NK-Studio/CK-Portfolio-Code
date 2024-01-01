using System.Threading;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Enemy.Behavior.Boss
{
    public abstract class BossExecuteUniTask : Action
    {
        public SharedBool ForgetTask = false;
        
        protected BossAquus Boss;
        public override void OnAwake()
        {
            Boss = Owner.GetComponent<BossAquus>();
            if (!Boss)
            {
                Debug.LogWarning($"BossExecuteUniTask init but no boss {gameObject.name}", gameObject);
            }
        }

        private CancellationTokenSource _tokenSource;
        private CancellationToken _token;
        private UniTask.Awaiter _awaiter;

        protected abstract UniTask.Awaiter Execute(CancellationToken token);
        
        public override void OnStart()
        {
            if (_tokenSource != null)
            {
                _tokenSource.Dispose();
                _tokenSource = null;
            }

            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;
            _awaiter = Execute(_token);
        }

        public override TaskStatus OnUpdate()
        {
            if (ForgetTask.Value)
            {
                return TaskStatus.Success;
            }
            return _awaiter.IsCompleted ? TaskStatus.Success : TaskStatus.Running;
        }

        public override void OnEnd()
        {
            // 어쨌든 Task가 끝나면 cancel
            _tokenSource.Cancel();
            _tokenSource.Dispose();
            _tokenSource = null;
        }
    }
}