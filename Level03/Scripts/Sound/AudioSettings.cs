using System;
using Doozy.Runtime.UIManager.Components;
using FMODUnity;
using Managers;
using ManagerX;
using TMPro;
using UnityEngine;


namespace NMProject
{
    public class AudioSettings : MonoBehaviour
    {
        // [SerializeField] private UISlider[] _audioSliders;
        //
        // [SerializeField] private TMP_Text[] _audioTexts;
        //
        // private bool _initialized = false;
        // private void Start()
        // {
        //     _initialized = true;
        //     LoadAllAudioVolume();
        // }
        //
        // private void Update()
        // {
        //     RefreshUI();
        // }
        //
        // public void RefreshUI()
        // {
        //     int sliderCount = _audioSliders.Length;
        //     
        //     for (int i = 0; i < sliderCount; i++)
        //         if (_audioTexts[i])
        //             _audioTexts[i].text = $"{_audioSliders[i].value * 100f:N0} %";
        //         else
        //         {
        //             DebugX.LogWarning("AudioSettings에 TMP_Text가 Null 없습니다.");
        //             return;
        //         }
        // }
        //
        // /// <summary>
        // ///  AudioType의 Enum의 개수에 맞춰서 기본 값으로 값을 로드합니다.
        // /// </summary>
        // private void LoadAllAudioVolume()
        // {
        //     var audioManager = AudioManager.Instance;
        //     _audioSliders[(int)AudioManager.AudioType.Master].value = audioManager.MasterVolume;
        //     _audioSliders[(int)AudioManager.AudioType.BGM].value = audioManager.BGMVolume;
        //     _audioSliders[(int)AudioManager.AudioType.SFX].value = audioManager.SFXVolume;
        //     _audioSliders[(int)AudioManager.AudioType.AMB].value = audioManager.AMBVolume;
        // }
        //
        // /// <summary>
        // /// 오디오 값을 저장합니다.
        // /// </summary>
        // public void SaveAllAudio()
        // {
        //     // 오디오 타입 개수를 가져옵니다.
        //     int enumCount = Enum.GetValues(typeof(AudioManager.AudioType)).Length;
        //
        //     // 오디오 개수만큼 반복
        //     for (int i = 0; i < enumCount; i++)
        //     {
        //         //오디오 타입을 가져옵니다.
        //         string audioType = Enum.GetName(typeof(AudioManager.AudioType), i);
        //
        //         //오디오 슬라이더 값을 가져옵니다.
        //         float audioValue = _audioSliders[i].value;
        //
        //         //가져온 오디오 종류와 값을 저장합니다.
        //         AutoManager.Get<DataManager>().Save(audioType, audioValue);
        //     }
        // }
        //
        // /// <summary>
        // /// 마스터 볼륨을 업데이트 합니다.
        // /// </summary>
        // public void UpdateMasterVolume() => UpdateVolume(AudioManager.AudioType.Master);
        //
        // /// <summary>
        // /// BGM 볼륨을 업데이트합니다.
        // /// </summary>
        // public void UpdateBGMVolume() => UpdateVolume(AudioManager.AudioType.BGM);
        //
        // /// <summary>
        // /// 효과음 볼륨을 업데이트합니다.
        // /// </summary>
        // public void UpdateSFXVolume() => UpdateVolume(AudioManager.AudioType.SFX);
        //
        // /// <summary>
        // /// 환경음 볼륨을 업데이트합니다.
        // /// </summary>
        // public void UpdateAMBVolume() => UpdateVolume(AudioManager.AudioType.AMB);
        //
        // public void UpdateVolume(int audioType)
        // {
        //     UpdateVolume((AudioManager.AudioType)audioType);
        // }
        //
        // private void UpdateVolume(AudioManager.AudioType audioType)
        // {
        //     if (!_initialized)
        //     {
        //         return;
        //     }
        //     var value = _audioSliders[(int)audioType].value;
        //     AutoManager.Get<AudioManager>().SetVolume(audioType, value);
        // }
    }
}