using FMODUnity;

namespace BehaviorDesigner.Runtime.Tasks.Unity.FMOD
{
    [TaskDescription("AudioManager에서 효과음을 재생합니다.")]
    public class FMODPlay : Action
    {
        [Tooltip("재생할 효과음 이름")] 
        public SharedString KeyName;

        public override TaskStatus OnUpdate()
        {
            AudioManager audioManager = ManagerX.AutoManager.Get<AudioManager>();

            bool findRef = audioManager.SFXSounds.TryGetValue(KeyName.Value, out EventReference clip);

            if (findRef)
                ManagerX.AutoManager.Get<AudioManager>().PlayOneShot(clip);
            
            return TaskStatus.Success;
        }

        public override void OnReset()
        {
            KeyName.Value = string.Empty;
        }
    }
}