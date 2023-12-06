using System;
using AutoManager;
using FMODUnity;
using Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Pointer {
    public class SettingsUIManager : MonoBehaviour {
        
        [SerializeField] private Slider BGMSlider;
        [SerializeField] private Slider SFXSlider;
        [SerializeField] private EventReference ExampleSFX;
        [SerializeField] private Slider MouseSensitivitySlider;

        private AudioManager AudioManager => Manager.Get<AudioManager>();
        private GameManager GameManager => Manager.Get<GameManager>();

        private void Start() {
            var audioManager = AudioManager;
            // 불러오기
            BGMSlider.value = audioManager.GetBGMVolume();
            SFXSlider.value = audioManager.GetSFXVolume();
            MouseSensitivitySlider.value = GameManager.NormalizedMouseSensitivity.Value;
        }

        /// <summary>
        /// BGM 볼륨을 업데이트합니다.
        /// </summary>
        public void UpdateBGMVolume(float value)
        {
            //BGM 슬라이더의 값을 실제 BGM 볼륨에 반영합니다.
            AudioManager.SetBGMVolume(value);
        }

        /// <summary>
        /// 효과음 볼륨을 업데이트합니다.
        /// </summary>
        public void UpdateSFXVolume(float value)
        {
            //SFX 슬라이더의 값을 실제 BGM 볼륨에 반영합니다.
            AudioManager.SetSFXVolume(value);
        }

        /// <summary>
        /// 효과음을 재생합니다.
        /// </summary>
        public void PlaySFX()
        {
            AudioManager.PlayOneShot(ExampleSFX);
        }

        /// <summary>
        /// 마우스 감도를 설정합니다.
        /// </summary>
        public void UpdateMouseSensitivity(float value) {
            GameManager.NormalizedMouseSensitivity.Value = value;
        }
    }
}