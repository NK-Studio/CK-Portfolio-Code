using System;
using System.Collections.Generic;
using Managers;
using ManagerX;
using SceneSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Utility;

namespace Tutorial
{
    public class TutorialController : MonoBehaviour, ITutorialController
    {
        [SerializeField, BoxGroup("튜토리얼")] private List<TutorialBase> _tutorials;
        [SerializeField, BoxGroup("클리어 시")] private UnityEvent _onCompleted;
        [SerializeField, BoxGroup("클리어 시")] private CheckPoint _nextSceneCheckPoint;

        private TutorialBase _currentTutorial = null;
        private int _currentIndex = -1;

        private void Awake()
        {
            if (_tutorials.Count <= 0)
            {
                Bind();
            }
        }

        [Button, BoxGroup("튜토리얼")]
        private void Bind()
        {
            _tutorials.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                // if(!child.gameObject.activeSelf) continue;
                if(!child.TryGetComponent(out TutorialBase tut)) continue;
                tut.Bind();
                _tutorials.Add(tut);
            }
        }

        public virtual void Start()
        {
            DialogManager.Instance.Disabled = true;
            foreach (var tut in _tutorials)
            {
                tut.Initialize(this);
            }
        
            Next();
        }

        public virtual void Update()
        {
            if (_currentTutorial.IsNull())
            {
                return;
            }

            var result = _currentTutorial.Execute();
            if (result == TutorialBase.Result.Done)
            {
                Next();
            }
        }

        public virtual void Next()
        {
            // 현재 튜토리얼의 FlowExit() 메소드 호출
            if (_currentTutorial.IsNotNull())
            {
                DebugX.Log($"[Tutorial] Exiting <color=yellow>{_currentTutorial.name} ({_currentTutorial.GetType().Name})</color>", _currentTutorial);
                _currentTutorial.Exit();
            }

            // 마지막 튜토리얼을 진행했다면 CompletedAllTutorials() 메소드 호출
            if (_currentIndex >= _tutorials.Count - 1)
            {
                CompletedAllTutorials();
                return;
            }

            // 다음 튜토리얼 과정을 currentTutorial로 등록
            do
            {
                if (_currentIndex >= _tutorials.Count)
                {
                    CompletedAllTutorials();
                    return;
                }
                ++_currentIndex;
                _currentTutorial = _tutorials[_currentIndex];
            } while (!_currentTutorial.isActiveAndEnabled);

            // 새로 바뀐 튜토리얼의 Enter() 메소드 호출
            DebugX.Log($"[Tutorial] Starting <color=lime>{_currentTutorial.name} ({_currentTutorial.GetType().Name})</color>", _currentTutorial);
            _currentTutorial.Enter();
        }

        public virtual void CompletedAllTutorials()
        {
            _currentTutorial = null;

            DebugX.Log("[Tutorial] Complete All Tutorial !!!");
        
            _onCompleted?.Invoke();
            if (_nextSceneCheckPoint.IsValid())
            {
                AutoManager.Get<SceneController>().LoadLevel(_nextSceneCheckPoint);
            }

            DialogManager.Instance.Disabled = false;
        }

        private void OnDestroy()
        {
            DialogManager.Instance.Disabled = false;
        }
    }

    public interface ITutorialController
    {
        public void Next();
    }
}