using UnityEngine;

namespace NKStudio
{
    public class FMODStopChanger : MonoBehaviour
    {
        public enum EAudioType
        {
            BGM,
            AMB,
        }

        public EAudioType AudioType;

        public bool IsFadeOut;
        
        /// <summary>
        /// 오디오를 스톱합니다.
        /// </summary>
        public void StopAudio()
        {
            // switch (AudioType)
            // {
            //     case EAudioType.BGM:
            //         ManagerX.AutoManager.Get<AudioManager>().StopBGM(IsFadeOut);
            //         break;
            //     case EAudioType.AMB:
            //         ManagerX.AutoManager.Get<AudioManager>().StopAMB(IsFadeOut);
            //         break;
            // }
        }
    }
}