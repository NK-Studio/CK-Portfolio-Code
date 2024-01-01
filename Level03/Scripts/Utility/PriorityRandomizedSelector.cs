using System;
using System.Collections.Generic;
using System.Linq;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Utility
{
    [TaskDescription("Priority Selector인데 같은 priority 내에서 섞여 있습니다.")]
    [TaskIcon("{SkinColor}PrioritySelectorIcon.png")]
    public class PriorityRandomizedSelector : Composite
    {
        [Serializable]
        public class MaximumContinuousSelectCountByTaskName
        {
            public string TaskName = "Task_이름";
            [Min(1)]
            public int Count = 1;
        }

        public SharedInt DefaultContinuousSelectCount = 99;
        public List<MaximumContinuousSelectCountByTaskName> ContinuousSettings = new();
        private Dictionary<string, int> _maximumContinuousSelectCountByTaskName = new();
        
        public SharedBool ShowDebugLog = false;
        // The index of the child that is currently running or is about to run.
        private int currentChildIndex = 0;
        // The task status of every child task.
        private TaskStatus executionStatus = TaskStatus.Inactive;

        private List<Task> shuffledChildren = new List<Task>();
        // The order to run its children in. 
        private List<int> childrenExecutionOrderBuffer = new List<int>();
        private List<int> childrenExecutionOrder = new List<int>();

        public override void OnAwake()
        {
            _maximumContinuousSelectCountByTaskName.Clear();
            foreach (var pair in ContinuousSettings)
            {
                var name = pair.TaskName;
                var count = pair.Count;
                if (!_maximumContinuousSelectCountByTaskName.TryAdd(name, count))
                {
                    Debug.LogWarning($"PriorityRandomizedSelector::OnAwake - {name} 중복됨 !!!");
                    continue;
                }
                Log($"{name} 최대 연속 선택: {count}회");
            }

            foreach (var task in children)
            {
                var name = task.FriendlyName;
                if (_maximumContinuousSelectCountByTaskName.TryAdd(name, DefaultContinuousSelectCount.Value))
                {
                    Log($"{name} 최대 연속 선택: {DefaultContinuousSelectCount}회 (기본)");
                }
            }
        }

        public override void OnStart()
        {
            ShuffleAndOrderChildren();
        }

        private void Log(string message)
        { 
            if(ShowDebugLog.Value) DebugX.Log("[PRS] "+message);
        }
        private void LogWarning(string message)
        { 
            if(ShowDebugLog.Value) DebugX.LogWarning("[PRS] "+message);
        }

        private Task _lastSelected = null;
        private int _lastSelectedCount = 0;
        private void ShuffleAndOrderChildren()
        {
            // Make sure the list is empty before we add child indexes to it.
            childrenExecutionOrderBuffer.Clear();
            childrenExecutionOrder.Clear();

            // index들 섞기
            for (int i = 0; i < children.Count; i++)
            {
                var task = children[i];
                if(task.Disabled) continue;

                // 최대 연속
                var taskName = task.FriendlyName;
                var maximumContinuousSelectCount = _maximumContinuousSelectCountByTaskName[taskName];
                if (_lastSelected == task && _lastSelectedCount >= maximumContinuousSelectCount)
                {
                    Log($"{taskName}이 {_lastSelectedCount}회 연속 선택되어 제외됨");
                    _lastSelected = null;
                    _lastSelectedCount = 0;
                    continue;
                }
                childrenExecutionOrderBuffer.Add(i);
            }
            childrenExecutionOrderBuffer.Shuffle();
            
            Log($"Shuffled by: {string.Join(", ", childrenExecutionOrderBuffer)}");

            // index를 우선순위에 따라 insertion sort (stable함)
            for (int i = 0; i < childrenExecutionOrderBuffer.Count; i++)
            {
                var index = childrenExecutionOrderBuffer[i];
                var task = children[index];
                var priority = task.GetPriority();
                var insertIndex = childrenExecutionOrder.Count;
                for (int j = 0; j < childrenExecutionOrder.Count; j++)
                {
                    if (children[childrenExecutionOrder[j]].GetPriority() < priority)
                    {
                        insertIndex = j;
                        break;
                    }
                }
                childrenExecutionOrder.Insert(insertIndex, index);
                // DebugX.Log($"[{i}] Inserted {index}: {string.Join(", ", childrenExecutionOrder)}");
            }

            Log($"PriorityRandomizedSelector Reordered by ...");
            foreach (var index in childrenExecutionOrder)
            {
                Log($"[{index}]: {children[index].FriendlyName} :: {children[index].GetPriority()}");
            }
            Log($"<color=#FFFF00>Pattern Selected: [{childrenExecutionOrder[0]}] :: {children[childrenExecutionOrder[0]].FriendlyName} :: {children[childrenExecutionOrder[0]].GetPriority()}</color>");
        }

        public override int CurrentChildIndex()
        {
            // Use the execution order list in order to determine the current child index.
            return childrenExecutionOrder[currentChildIndex];
        }

        public override bool CanExecute()
        {
            // We can continue to execuate as long as we have children that haven't been executed and no child has returned success.
            return currentChildIndex < children.Count && executionStatus != TaskStatus.Success;
        }

        public override void OnChildExecuted(TaskStatus childStatus)
        {
            // Increase the child index and update the execution status after a child has finished running.
            executionStatus = childStatus;
            if (childStatus == TaskStatus.Success)
            {
                // 정상적으로 선택된 태스크를 설정
                var oldTask = _lastSelected;
                var newTask = _lastSelected = children[CurrentChildIndex()];
                if (oldTask == newTask)
                {
                    _lastSelectedCount += 1;
                    Log($"{newTask.FriendlyName}이 {_lastSelectedCount}회 연속 선택");
                }
                else
                {
                    Log($"{newTask.FriendlyName} 선택 => 연속 선택 횟수 1회로 설정");
                    _lastSelectedCount = 1;
                }
            }
            currentChildIndex++;
        }

        public override void OnConditionalAbort(int childIndex)
        {
            // Set the current child index to the index that caused the abort
            currentChildIndex = childIndex;
            executionStatus = TaskStatus.Inactive;
        }

        public override void OnEnd()
        {
            // All of the children have run. Reset the variables back to their starting values.
            executionStatus = TaskStatus.Inactive;
            currentChildIndex = 0;
        }
    }
}