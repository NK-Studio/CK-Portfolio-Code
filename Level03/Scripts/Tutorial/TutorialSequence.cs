using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Utility;

namespace Tutorial
{
    public class TutorialSequence : TutorialBase
    {
        [SerializeField] private List<TutorialBase> _children = new();

        [Button]
        public override void Bind()
        {
            _children.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                // if(!child.gameObject.activeSelf) continue;
                if(!child.TryGetComponent(out TutorialBase tut)) continue;
                _children.Add(tut);
                
                tut.Bind();
            }
        }
        
        private int _index = -1;
        private TutorialBase _tut = null;

        public override void Initialize(TutorialController controller)
        {
            foreach (var tut in _children)
            {
                if(!tut || !tut.isActiveAndEnabled) continue;
                tut.Initialize(controller);
            }
        }


        private bool Next()
        {
            if (_tut.IsNotNull())
            {
                _tut.Exit();
                DebugX.Log($"[<color=cyan>{gameObject.name} ({GetType().Name})</color>] <color=yellow>{_tut.name} ({_tut.GetType().Name})</color> CLEARED");
            }

            // 마지막 Task면 그냥 종료
            if (_index + 1 >= _children.Count)
            {
                return true;
            }
            
            // 비활성화된 children은 스킵하고 다음 Task 찾기
            do
            {
                _tut = _children[++_index];
            } while (!_tut.isActiveAndEnabled && _index < _children.Count);

            // 다음 거 다 꺼져있으면 종료
            if (_index >= _children.Count)
            {
                return true;
            }

            // 다음 Task 시작
            _tut.Enter();
            return false;
        }
        public override void Enter()
        {
            _index = -1;
            Next();
        }

        public override Result Execute()
        {
            if (_index >= _children.Count)
            {
                return Result.Done;
            }

            // Success인 Task들 한번에 처리
            while (_tut.Execute() == Result.Done)
            {
                if (Next())
                {
                    return Result.Done;
                }
            }

            return Result.Running;

        }

        public override void Exit()
        {
        }
    }
}