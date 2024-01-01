using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.Assertions.Comparers;
using Random = UnityEngine.Random;

namespace Utility
{
    [TaskDescription("Priority Selector인데 같은 priority 내에서 섞여 있습니다.")]
    [TaskIcon("{SkinColor}PrioritySelectorIcon.png")]
    public class PriorityAsWeightRandomSelector : Composite
    {
        public SharedBool SkipLastExecuted = true;

        private class TaskToWeight
        {
            public int Index;
            public Task Task;
            public float Weight;

            public TaskToWeight(int index, Task task, float weight)
            {
                Index = index;
                Task = task;
                Weight = weight;
            }
        }
        
        private static int FloatComparsion(TaskToWeight x, TaskToWeight y)
        {
            return x.Weight.CompareTo(y.Weight);
        }

        public override void OnStart()
        {
            InitializeList();
            _selectedTask = Select();
            _lastExecutedTask = _selectedTask.Task;
        }

        private List<TaskToWeight> _childTaskToWeights = new();
        private float _weightSum;
        private TaskToWeight _selectedTask;
        private Task _lastExecutedTask;
        private void InitializeList()
        {
            _childTaskToWeights.Clear(); 
            // weight 수집
            for (int i = 0; i < children.Count; i++)
            {
                if(children[i].Disabled) continue;
                if (SkipLastExecuted.Value && _lastExecutedTask?.ID == children[i].ID)
                {
                    DebugX.Log($"SKIPPED LAST EXECUTED [{i}]: {children[i].FriendlyName} - {children[i].GetPriority()}");
                    continue;
                }
                DebugX.Log($"PriorityWeightRandom - [{i}]: {children[i].FriendlyName} - {children[i].GetPriority()}");
                _childTaskToWeights.Add(new TaskToWeight(i, children[i], children[i].GetPriority()));
            }
            
            // weight 순으로 정렬
            _childTaskToWeights.Sort(FloatComparsion);
            // weight 합 저장
            _weightSum = _childTaskToWeights.Sum(it => it.Weight);
        }

        // 가중치 기반 랜덤 선택
        private TaskToWeight Select()
        {
            // 0 ~ 최대값까지 랜덤 선택
            var rand = Random.Range(0f, _weightSum);
            // 가중치 구간 검사용 변수
            var currentWeight = 0f;
            TaskToWeight selectedTask = null;
            foreach (var pair in _childTaskToWeights)
            {
                // 랜덤값이 가중치 구간에 해당할 경우
                if (currentWeight <= rand && rand <= currentWeight + pair.Weight)
                {
                    selectedTask = pair;
                    DebugX.Log($"PriorityWeightRandom - <color=green>selected {selectedTask} by {currentWeight} <= {rand} <= {currentWeight + pair.Weight})</color>");
                    break;
                }
                DebugX.Log($"PriorityWeightRandom - <color=yellow>NOT selected {pair} -> {currentWeight} <= {rand} <= {currentWeight + pair.Weight})</color>");

                currentWeight += pair.Weight;
            }

            return selectedTask;
        }

        public override int CurrentChildIndex()
        {
            DebugX.Log($"PriorityWeightRandom - _selectedTask.Index: {_selectedTask.Index} ");
            // Use the execution order list in order to determine the current child index.
            return _selectedTask.Index;
        }

        public override bool CanExecute()
        {
            DebugX.Log($"PriorityWeightRandom - CanExecute: {_selectedTask != null} ");
            // We can continue to execuate as long as we have children that haven't been executed and no child has returned success.
            return _selectedTask != null;
        }

        public override void OnChildStarted()
        {
            DebugX.Log($"PriorityWeightRandom - OnChildStarted, set _selectedTask to null");
            _selectedTask = null;
        }

        public override void OnChildStarted(int childIndex)
        {
            DebugX.Log($"PriorityWeightRandom - OnChildStarted(int), set _selectedTask to null");
            _selectedTask = null;
        }

        public override void OnChildExecuted(int childIndex, TaskStatus childStatus)
        {
            DebugX.Log($"PriorityWeightRandom - OnChildExecuted, set _selectedTask to null");
            _selectedTask = null;
        }

        public override void OnEnd()
        {
            DebugX.Log($"PriorityWeightRandom - OnEnd, set _selectedTask to null");
            _selectedTask = null;
        }
    }
}