using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace Utility
{
    [TaskDescription("Priority가 존재하는 Sequence입니다.")]
    [TaskIcon("{SkinColor}SequenceIcon.png")]
    public class PrioritizedSequence : Sequence
    {
        public SharedFloat Priority;
        [Tooltip("이 Sequence가 실행될 때 Priority를 지정된 값으로 초기화합니다.")]
        public SharedBool ResetPriorityWhenExecuted = true;
        [Tooltip("이 Sequence가 실행될 때 Priority를 초기화할 값입니다. ResetPriorityWhenExecuted가 true여야 동작합니다.")]
        public SharedFloat ResetValue = 0;

        [Tooltip("이 bool 값이 true면 Priority를 0으로 취급합니다.")]
        public SharedBool ZeroPriorityOnTrue = false;
        
        public override float GetPriority() => ZeroPriorityOnTrue.Value ? 0 : Priority.Value;


        public override void OnStart()
        {
            base.OnStart();
            if (ResetPriorityWhenExecuted.Value)
            {
                // DebugX.LogWarning($"RESET PRIORITY {FriendlyName}");
                Priority.Value = ResetValue.Value;
            }
        }

        public override void OnChildStarted()
        {
            base.OnChildStarted();
        }

        public override void OnChildStarted(int childIndex)
        {
            base.OnChildStarted(childIndex);
        }

        public override void OnChildExecuted(int childIndex, TaskStatus childStatus)
        {
            base.OnChildExecuted(childIndex, childStatus);
        }
    }
}