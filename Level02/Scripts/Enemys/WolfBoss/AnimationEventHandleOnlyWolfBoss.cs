using AutoManager;
using Enemys.WolfBoss;
using UnityEngine;

public class AnimationEventHandleOnlyWolfBoss : MonoBehaviour
{
    public WolfBoss WolfBoss;
    private WolfBossSettings _settings;

    private void Start()
    {
        _settings = WolfBoss.Settings;
    }

    public void OnSoundEvent(int id)
    {
        switch (id)
        {
            case 0:     //  @ 보스 걷기 사운드
                Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[0], transform.position);
                break;
            case 1:     //  @ 보스 돌진 준비 사운드
                Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[1], transform.position);
                break;
            case 2:     //  @ 보스 돌진 사운드
                Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[2], transform.position);
                break;
            case 3:     //  @ 보스 할퀴기 사운드 
                Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[3], transform.position);
                break;
            case 4:     //  @ 보스 도약 사운드
                Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[4], transform.position);
                break;
            case 5:     //  @ 보스 낙하 공격 사운드
                Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[5], transform.position);
                break;
            case 6:     //  @ 보스 포효 사운드
                Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[10], transform.position);
                break;
            case 7:     //  @ 보스 사망 후 쓰러지는 사운드
                Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[7], transform.position);
                break;
            case 8:     //  @ 보스 사망 울음소리 사운드
                Manager.Get<AudioManager>().PlayOneShot(_settings.SFXClips[8], transform.position);
                break;
        }
    }
}
