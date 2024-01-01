using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Tutorial
{
    public class TutorialParallel : TutorialBase
    {
        
        [SerializeField] private List<TutorialBase> _children = new();
        [SerializeField] private bool _SkipWhenAnyCompleted = false;
        
        [Button]
        public override void Bind()
        {
            _children.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if(!child.gameObject.activeSelf) continue;
                if(!child.TryGetComponent(out TutorialBase tut)) continue;
                _children.Add(tut);
                
                tut.Bind();
            }
        }

        private Dictionary<TutorialBase, bool> _executing = new();

        public override void Initialize(TutorialController controller)
        {
            foreach (var tut in _children)
            {
                if(!tut || !tut.isActiveAndEnabled) continue;
                tut.Initialize(controller);
            }
        }

        public override void Enter()
        {
            _executing.Clear();
            var map = _children.ToDictionary(it => it, _ => false, EqualityComparer<TutorialBase>.Default);
            foreach (var (key, value) in map)
            {
                _executing.Add(key, value);
            }
            
            foreach (var (tut, cleared) in _executing)
            {
                tut.Enter();
            }
        }

        private readonly List<TutorialBase> _clearedTutorials = new();
        public override Result Execute()
        {
            bool allCleared = true;
            foreach (var (tut, cleared) in _executing)
            {
                if(cleared) continue;
                allCleared = false;
                if (tut.Execute() == Result.Done)
                {
                    DebugX.Log($"[<color=cyan>{gameObject.name} ({GetType().Name})</color>] <color=yellow>{tut.name} ({tut.GetType().Name})</color> CLEARED");
                    _clearedTutorials.Add(tut);
                }
            }

            if (allCleared)
            {
                return Result.Done;
            }

            foreach (var tut in _clearedTutorials)
            {
                _executing[tut] = true;
            }

            if (_SkipWhenAnyCompleted && _clearedTutorials.Count > 0)
            {
                return Result.Done;
            }
            _clearedTutorials.Clear();
            return Result.Running;
        }

        public override void Exit()
        {
            foreach (var (tut, cleared) in _executing)
            {
                tut.Exit();
            }
        }
    }
}