using System;
using FMOD.Studio;
using FMODUnity;
using Managers;
using ManagerX;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utility;

/// <summary>
/// 파라미터 이름을 struct로 만들어둠
/// </summary>
public struct ParameterID
{
    public static string StageID = "Stage";
}

[ManagerDefaultPrefab("AudioManager")]
public class AudioManager : MonoBehaviour, AutoManager
{
    public static AudioManager Instance => AutoManager.Get<AudioManager>();
    public enum AudioType
    {
        Master,
        BGM,
        SFX,
        AMB
    }
    
    public StudioEventEmitter AMBEmitter;
    public StudioEventEmitter BGMEmitter;
    public StudioEventEmitter SFXEmitter;

    [Space(10)] public AudioPathByString AMBSounds;
    public AudioPathByString BGMSounds;
    public AudioPathByString SFXSounds;

    [Space(10)] public string[] Buses;

    public float MasterVolume { get; private set; }
    public float BGMVolume { get; private set; }
    public float SFXVolume { get; private set; }
    public float AMBVolume { get; private set; }

    private Bus _masterBus;
    private Bus _bgmBus;
    private Bus _sfxBus;
    private Bus _ambBus;

    private bool _isMuteByError;

    public void Awake()
    {
        try
        {
            _masterBus = RuntimeManager.GetBus(Buses[0]);
            _bgmBus = RuntimeManager.GetBus(Buses[1]);
            _sfxBus = RuntimeManager.GetBus(Buses[2]);
            _ambBus = RuntimeManager.GetBus(Buses[3]);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            _isMuteByError = true;
        }
    }

    private void Start()
    {
        if (_isMuteByError)
            return;

        // 볼륨 값을 불럽옵니다.
        LoadVolume();
    }

    /// <summary>
    /// 볼륨 값을 불러옵니다.
    /// </summary>
    private void LoadVolume()
    {
        MasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AMBVolume = PlayerPrefs.GetFloat("AmbientVolume", 1f);
        BGMVolume = PlayerPrefs.GetFloat("BackgroundVolume", 1f);
        SFXVolume = PlayerPrefs.GetFloat("EffectVolume", 1f);
        
        SetVolume(AudioType.Master, MasterVolume);
        SetVolume(AudioType.BGM, BGMVolume);
        SetVolume(AudioType.SFX, SFXVolume);
        SetVolume(AudioType.AMB, AMBVolume);
    }
    
    /// <summary>
    /// Master의 볼륨을 조절합니다.
    /// </summary>
    /// <param name="value">0~1사이의 값, 0이면 뮤트됩니다.</param>
    private void SetMasterVolume(float value)
    {
        if (_isMuteByError)
            return;

        MasterVolume = value;
        _masterBus.setVolume(value);
    }
    
    public void SetVolume(AudioType type, float value)
    {
        if (_isMuteByError)
            return;

        switch (type)
        {
            case AudioType.Master:
                MasterVolume = value;
                _masterBus.setVolume(value);
                return;
            case AudioType.BGM:
                BGMVolume = value;
                _bgmBus.setVolume(value);
                return;
            case AudioType.SFX:
                SFXVolume = value;
                _sfxBus.setVolume(value);
                return;
            case AudioType.AMB:
                AMBVolume = value;
                _ambBus.setVolume(value);
                return;
        }
    }

    /// <summary>
    /// 인스턴스를 내부에서 만들어서 효과음을 재생하고, 즉시 파괴합니다.
    /// </summary>
    /// <param name="path">재생할 효과음 경로</param>
    /// <param name="position">해당 위치에서 소리를 재생합니다.</param>
    public void PlayOneShot(EventReference path, Vector3 position = default)
    {
        if (_isMuteByError)
            return;
        RuntimeManager.PlayOneShot(path, position);
    }


    /// <summary>
    /// 파라미터를 호환하고 인스턴스를 내부에서 만들어서 효과음을 재생하고, 즉시 파괴합니다.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="parameterName"></param>
    /// <param name="parameterValue"></param>
    /// <param name="position"></param>
    public void PlayOneShot(string path, string parameterName, float parameterValue, Vector3 position = new Vector3())
        => PlayOneShot(RuntimeManager.CreateInstance(path), parameterName, parameterValue, position);

    /// <summary>
    /// 파라미터를 호환하고 인스턴스를 내부에서 만들어서 효과음을 재생하고, 즉시 파괴합니다.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="parameterName"></param>
    /// <param name="parameterValue"></param>
    /// <param name="position"></param>
    public void PlayOneShot(EventReference path, string parameterName, float parameterValue,
        Vector3 position = new Vector3())
        => PlayOneShot(RuntimeManager.CreateInstance(path), parameterName, parameterValue, position);

    private void PlayOneShot(EventInstance instance, string parameterName, float parameterValue,
        Vector3 position = new Vector3())
    {
        instance.set3DAttributes(position.To3DAttributes());
        instance.setParameterByName(parameterName, parameterValue);
        instance.start();
        instance.release();
    }

    public void SetBGMParameter(string parameter, float value)
    {
        BGMEmitter.SetParameter(parameter, value);
    }

    public void SetAMBParameter(string parameter, float value)
    {
        AMBEmitter.SetParameter(parameter, value);
    }
}