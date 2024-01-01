using UnityEngine;

namespace NKStudio
{
    public enum EAudioType
    {
        BGM,
        AMB,
    }
    public class FMODParameterChanger : MonoBehaviour
    {
        public EAudioType AudioType;

        public string ParameterName;
        public float Value;

        /// <summary>
        /// 파라미터를 체인지합니다.
        /// </summary>
        public void ChangeParameter()
        {
            switch (AudioType)
            {
                case EAudioType.BGM:
                    ManagerX.AutoManager.Get<AudioManager>().SetBGMParameter(ParameterName, Value);
                    break;
                case EAudioType.AMB:
                    ManagerX.AutoManager.Get<AudioManager>().SetAMBParameter(ParameterName, Value);
                    break;
            }
        }
    }
}