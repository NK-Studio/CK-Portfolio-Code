using FMODUnity;
using ManagerX;
using NKStudio;
using UnityEngine;

namespace Sound
{
    public enum EAudioBehaviour
    {
        Play,
        Stop
    }
    
    public class FMODMusicEmitter : MonoBehaviour
    {
        public EAudioType AudioType;
        
        public string Key;

        public bool Fade;

        public EAudioBehaviour Behaviour;
        
        private static AudioManager AudioManager => AutoManager.Get<AudioManager>();
        
        private void Start()
        {
            EventReference clip;

            switch (Behaviour)
            {
                case EAudioBehaviour.Play:
                    switch (AudioType)
                    {
                        case EAudioType.BGM:
                            if (AudioManager.BGMSounds.TryGetValue(Key, out clip))
                            {
                                AudioManager.BGMEmitter.ChangeEvent(clip,Fade);
                                AudioManager.BGMEmitter.Play();
                            }
                            break;
                        case EAudioType.AMB:
                            if (AudioManager.AMBSounds.TryGetValue(Key, out clip))
                            {
                                AudioManager.AMBEmitter.ChangeEvent(clip,Fade);
                                AudioManager.AMBEmitter.Play();
                            }
                            break;
                    }
                    break;
                case EAudioBehaviour.Stop:
                    switch (AudioType)
                    {
                        case EAudioType.BGM:
                            AudioManager.BGMEmitter.AllowFadeout = Fade;
                            AudioManager.BGMEmitter.Stop();
                            break;
                        case EAudioType.AMB:
                            AudioManager.AMBEmitter.AllowFadeout = Fade;
                            AudioManager.AMBEmitter.Stop();
                            break;
                    }
                    break;
            }
        }
    }
}