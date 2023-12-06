using AutoManager;
using Enemys;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventHandleOnlyStarCandy : MonoBehaviour
{
    public StarCandy StarCandy;
    private StarCandySettings _settings;

    private void Start()
    {
        _settings = StarCandy.Settings;
    }

    public void OnSoundEvent(int id)
    {
        switch (id)
        {
            //  @별사탕 발소리
            case 0:
                Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[0], transform.position);
                break;
            case 1:
                Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[0], transform.position);
                break;
        }
    }
}
