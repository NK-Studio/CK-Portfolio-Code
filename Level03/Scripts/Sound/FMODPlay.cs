using FMODUnity;
using UnityEngine;

public class FMODPlay : MonoBehaviour
{
    [Tooltip("재생할 효과음 이름")]
    public string KeyName;

    /// <summary>
    /// 오디오를 재생합니다.
    /// </summary>
    public void PlayAudio()
    {
        AudioManager audioManager = ManagerX.AutoManager.Get<AudioManager>();

        bool findRef = audioManager.SFXSounds.TryGetValue(KeyName, out EventReference clip);

        if (findRef)
            ManagerX.AutoManager.Get<AudioManager>().PlayOneShot(clip);
    }
}