using AutoManager;
using Enemys;
using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventHandleOnlyHaribo : MonoBehaviour
{
    public HariboSoldier HariboSoldier;
    private HariboSoldierSettings _settings;

    private void Start()
    {
        _settings = HariboSoldier.Settings;
    }

    public void OnAnimationEvent(int id)
    {
        switch (id)
        {
            //  @하리보 발소리
            case 0:
                Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[0], transform.position);
                break;
            case 1:
                Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[0], transform.position);
                break;
        }
    }
}
