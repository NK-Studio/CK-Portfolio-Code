using Doozy.Runtime.UIManager.Components;
using Doozy.Runtime.UIManager.Input;
using Managers;
using ManagerX;
using NKStudio.Option;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Option
{
    public class OptionPresenter : MonoBehaviour
    {
        public OptionModel Model;
        public Circle CirclePrefab;

        [Header("Game Play")]
        public GraphicsHardwareView graphicsHardwareView;
        public LanguageView languageView;
        public ScreenWindowView WindowModeView;
        public ScreenResolutionView ScreenResolutionView;
        public VSyncView VSyncView;
        public PanningView panningView;

        [Header("Graphics")]
        public GraphicsQualityView GraphicsQualityView;
        public TextureQualityView TextureQualityView;
        public AntiAliasingView AntiAliasingView;
        public HBAOView HBAOView;

        [Header("Audio")]
        public MasterAudioView MasterAudioView;
        public AmbientAudioView AmbientAudioView;
        public BackgroundAudioView BackgroundAudioView;
        public EffectAudioView EffectAudioView;
        
        [Header("Control")]
        public VibrationView VibrationView;

        // 중간 퀄리티
        public const int MediumQuality = 2;
        
        // 최고 텍스쳐 퀄리티
        public const int BestTextureQuality = 3;

        // View Init (2 is FullScreen And Windowed)
        public const int WindowModeCount = 2;

        [Header("Button")]
        public UIButton SaveButton;

        private void Start()
        {
            Init();
        }

        private void Init()
        {
            #region Game Play
            InitGraphicsHardwareView();
            InitLanguage();
            InitScreenWindowView();
            InitScreenResolutionView();
            InitVSyncView();
            InitPanning();
            #endregion

            #region Graphics
            InitGraphicsQualityView();
            InitTextureQualityView();
            InitAntiAliasingView();
            InitHBAOView();
            #endregion

            #region Audio
            InitMasterAudioView();
            InitAmbientAudioView();
            InitBackgroundAudioView();
            InitEffectAudioView();
            #endregion

            #region Control
            InitGamepadVibration();
            #endregion
        }

        #region Game Play
        /// <summary>
        /// 스크린 해상도를 설정합니다.
        /// </summary>
        private void InitScreenResolutionView()
        {
            if (Model.ScreenResolution > ScreenResolutionView.GetOptionCount)
            {
                int bestResolutionIndex = AutoManager.Get<GameManager>().GetBestResolutionIndex;
                PlayerPrefs.SetInt("ScreenResolution", bestResolutionIndex);
                Model.ScreenResolution = bestResolutionIndex;
            }

            ScreenResolutionView.InitCircle(CirclePrefab, ScreenResolutionView.GetOptionCount, Model.ScreenResolution);
            ScreenResolutionView.Refresh(Model.ScreenResolution);

            // Event Register
            ScreenResolutionView.OnLeftClickEvent().AddListener(() => {
                Model.ScreenResolution = Mathf.Max(0, Model.ScreenResolution - 1);
                int activeIndex = Model.ScreenResolution;
                ScreenResolutionView.Refresh(activeIndex);
                SaveButton.interactable = true;
            });

            ScreenResolutionView.OnRightClickEvent().AddListener(() => {
                Model.ScreenResolution = Mathf.Min(Model.ScreenResolution + 1, ScreenResolutionView.GetOptionCount - 1);
                int activeIndex = Model.ScreenResolution;
                ScreenResolutionView.Refresh(activeIndex);
                SaveButton.interactable = true;
            });
        }

        /// <summary>
        /// 스크린을 전체화면, 창모드로 설정합니다.
        /// </summary>
        private void InitScreenWindowView()
        {

            WindowModeView.InitCircle(CirclePrefab, WindowModeCount, Model.WindowMode);
            WindowModeView.Refresh(Model.WindowMode);

            // Event Register
            WindowModeView.OnLeftClickEvent().AddListener(() => {
                Model.WindowMode = Mathf.Max(0, Model.WindowMode - 1);
                int activeIndex = Model.WindowMode;
                WindowModeView.Refresh(activeIndex);
                SaveButton.interactable = true;
            });

            WindowModeView.OnRightClickEvent().AddListener(() => {
                Model.WindowMode = Mathf.Min(Model.WindowMode + 1, WindowModeCount - 1);
                int activeIndex = Model.WindowMode;
                WindowModeView.Refresh(activeIndex);
                SaveButton.interactable = true;
            });
        }

        /// <summary>
        /// 언어 설정을 합니다.
        /// </summary>
        private void InitLanguage()
        {
            // View Init
            languageView.InitCircle(CirclePrefab, LocalizationSettings.AvailableLocales.Locales.Count, Model.CurrentLanguage);
            languageView.Refresh(Model.CurrentLanguage);

            // Event Register
            languageView.OnLeftClickEvent().AddListener(() => {
                Model.CurrentLanguage = Mathf.Max(0, Model.CurrentLanguage - 1);
                int activeIndex = Model.CurrentLanguage;
                languageView.Refresh(activeIndex);
                SaveButton.interactable = true;
            });

            languageView.OnRightClickEvent().AddListener(() => {
                Model.CurrentLanguage = Mathf.Min(Model.CurrentLanguage + 1, LocalizationSettings.AvailableLocales.Locales.Count - 1);
                int activeIndex = Model.CurrentLanguage;
                languageView.Refresh(activeIndex);
                SaveButton.interactable = true;
            });
        }

        /// <summary>
        /// 카메라 패닝 설정을 합니다.
        /// </summary>
        private void InitPanning()
        {
            // View Init
            panningView.Refresh(Model.CameraPanning);

            // Event Register
            panningView.OnLeftClickEvent().AddListener(() => {
                Model.CameraPanning = false;
                panningView.Refresh(Model.CameraPanning);
                SaveButton.interactable = true;
            });

            panningView.OnRightClickEvent().AddListener(() => {
                Model.CameraPanning = true;
                panningView.Refresh(Model.CameraPanning);
                SaveButton.interactable = true;
            });
        }

        /// <summary>
        /// VSync 설정을 합니다.
        /// </summary>
        private void InitVSyncView()
        {
            // View Init
            VSyncView.Refresh(Model.VSyncEnable);

            // Event Register
            VSyncView.OnLeftClickEvent().AddListener(() => {
                Model.VSyncEnable = false;
                VSyncView.Refresh(Model.VSyncEnable);
                SaveButton.interactable = true;
            });

            VSyncView.OnRightClickEvent().AddListener(() => {
                Model.VSyncEnable = true;
                VSyncView.Refresh(Model.VSyncEnable);
                SaveButton.interactable = true;
            });
        }
        #endregion

        #region Graphics
        /// <summary>
        /// 현재 어떤 그래픽 하드웨어를 사용하는지 표시합니다.
        /// </summary>
        private void InitGraphicsHardwareView()
        {
            graphicsHardwareView.SetText(SystemInfo.graphicsDeviceName);
        }

        /// <summary>
        /// VSync 설정을 합니다.
        /// </summary>
        private void InitGraphicsQualityView()
        {
            int qualityCount = QualitySettings.names.Length;

            // View Init
            GraphicsQualityView.InitCircle(CirclePrefab, qualityCount, Model.GraphicsQuality);
            GraphicsQualityView.Refresh(Model.GraphicsQuality);

            // Event Register
            GraphicsQualityView.OnLeftClickEvent().AddListener(() => {
                Model.GraphicsQuality = Mathf.Max(0, Model.GraphicsQuality - 1);
                GraphicsQualityView.Refresh(Model.GraphicsQuality);
                SaveButton.interactable = true;
            });

            GraphicsQualityView.OnRightClickEvent().AddListener(() => {
                Model.GraphicsQuality = Mathf.Min(Model.GraphicsQuality + 1, qualityCount - 1);
                GraphicsQualityView.Refresh(Model.GraphicsQuality);
                SaveButton.interactable = true;
            });
        }

        /// <summary>
        /// 텍스쳐 퀄리티를 설정합니다.
        /// </summary>
        private void InitTextureQualityView()
        {
            const int globalMipmapLimitCount = 4;

            // View Init
            TextureQualityView.InitCircle(CirclePrefab, globalMipmapLimitCount, Model.TextureQuality);
            TextureQualityView.Refresh(Model.TextureQuality);

            // Event Register
            TextureQualityView.OnLeftClickEvent().AddListener(() => {
                Model.TextureQuality = Mathf.Max(0, Model.TextureQuality - 1);
                TextureQualityView.Refresh(Model.TextureQuality);
                SaveButton.interactable = true;
            });

            TextureQualityView.OnRightClickEvent().AddListener(() => {
                Model.TextureQuality = Mathf.Min(Model.TextureQuality + 1, globalMipmapLimitCount - 1);
                TextureQualityView.Refresh(Model.TextureQuality);
                SaveButton.interactable = true;
            });
        }

        /// <summary>
        /// 안티 앨리어싱을 설정합니다.
        /// </summary>
        private void InitAntiAliasingView()
        {
            const int antiAliasingCount = 4;

            // View Init
            AntiAliasingView.InitCircle(CirclePrefab, antiAliasingCount, Model.AntiAliasingQuality);
            AntiAliasingView.Refresh(Model.AntiAliasingQuality);

            // Event Register
            AntiAliasingView.OnLeftClickEvent().AddListener(() => {
                Model.AntiAliasingQuality = Mathf.Max(0, Model.AntiAliasingQuality - 1);
                AntiAliasingView.Refresh(Model.AntiAliasingQuality);
                SaveButton.interactable = true;
            });

            AntiAliasingView.OnRightClickEvent().AddListener(() => {
                Model.AntiAliasingQuality = Mathf.Min(Model.AntiAliasingQuality + 1, antiAliasingCount - 1);
                AntiAliasingView.Refresh(Model.AntiAliasingQuality);
                SaveButton.interactable = true;
            });
        }

        private void InitHBAOView()
        {
            // View Init
            HBAOView.Refresh(Model.HBAOEnable);

            // Event Register
            HBAOView.OnLeftClickEvent().AddListener(() => {
                Model.HBAOEnable = false;
                HBAOView.Refresh(Model.HBAOEnable);
                SaveButton.interactable = true;
            });

            HBAOView.OnRightClickEvent().AddListener(() => {
                Model.HBAOEnable = true;
                HBAOView.Refresh(Model.HBAOEnable);
                SaveButton.interactable = true;
            });
        }
        #endregion

        #region Audio
        /// <summary>
        /// MasterAudio 설정을 합니다.
        /// </summary>
        private void InitMasterAudioView()
        {
            // View Init
            Model.MasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            MasterAudioView.Refresh(Model.MasterVolume);

            const float changeAmount = 0.1f;

            MasterAudioView.OnValueChanged().AddListener(value => {
                Model.MasterVolume = value;
                AutoManager.Get<AudioManager>().SetVolume(AudioManager.AudioType.Master, value);
                PlayerPrefs.SetFloat("MasterVolume", Model.MasterVolume);
            });

            // Event Register
            MasterAudioView.OnLeftClickEvent().AddListener(() => {
                Model.MasterVolume = Mathf.Max(0, Model.MasterVolume - changeAmount);
                MasterAudioView.Refresh(Model.MasterVolume);
            });

            MasterAudioView.OnRightClickEvent().AddListener(() => {
                Model.MasterVolume = Mathf.Min(Model.MasterVolume + changeAmount, 1f);
                MasterAudioView.Refresh(Model.MasterVolume);
            });
        }

        /// <summary>
        /// AmbientAudio 설정을 합니다.
        /// </summary>
        private void InitAmbientAudioView()
        {
            // View Init
            Model.AmbientVolume = PlayerPrefs.GetFloat("AmbientVolume", 1f);
            AmbientAudioView.Refresh(Model.AmbientVolume);

            const float changeAmount = 0.1f;

            AmbientAudioView.OnValueChanged().AddListener(value => {
                Model.AmbientVolume = value;
                AutoManager.Get<AudioManager>().SetVolume(AudioManager.AudioType.AMB, value);
                PlayerPrefs.SetFloat("AmbientVolume", Model.AmbientVolume);
            });

            // Event Register
            AmbientAudioView.OnLeftClickEvent().AddListener(() => {
                Model.AmbientVolume = Mathf.Max(0, Model.AmbientVolume - changeAmount);
                AmbientAudioView.Refresh(Model.AmbientVolume);
            });

            AmbientAudioView.OnRightClickEvent().AddListener(() => {
                Model.AmbientVolume = Mathf.Min(Model.AmbientVolume + changeAmount, 1f);
                AmbientAudioView.Refresh(Model.AmbientVolume);
            });
        }

        /// <summary>
        /// BackgroundAudio 설정을 합니다.
        /// </summary>
        private void InitBackgroundAudioView()
        {
            // View Init
            Model.BackgroundVolume = PlayerPrefs.GetFloat("BackgroundVolume", 1f);
            BackgroundAudioView.Refresh(Model.BackgroundVolume);

            const float changeAmount = 0.1f;

            BackgroundAudioView.OnValueChanged().AddListener(value => {
                Model.BackgroundVolume = value;
                AutoManager.Get<AudioManager>().SetVolume(AudioManager.AudioType.BGM, value);
                PlayerPrefs.SetFloat("BackgroundVolume", Model.BackgroundVolume);
            });

            // Event Register
            BackgroundAudioView.OnLeftClickEvent().AddListener(() => {
                Model.BackgroundVolume = Mathf.Max(0, Model.BackgroundVolume - changeAmount);
                BackgroundAudioView.Refresh(Model.BackgroundVolume);
            });

            BackgroundAudioView.OnRightClickEvent().AddListener(() => {
                Model.BackgroundVolume = Mathf.Min(Model.BackgroundVolume + changeAmount, 1f);
                BackgroundAudioView.Refresh(Model.BackgroundVolume);
            });
        }

        /// <summary>
        /// EffectAudio 설정을 합니다.
        /// </summary>
        private void InitEffectAudioView()
        {
            // View Init
            Model.EffectVolume = PlayerPrefs.GetFloat("EffectVolume", 1f);
            EffectAudioView.Refresh(Model.EffectVolume);

            const float changeAmount = 0.1f;

            EffectAudioView.OnValueChanged().AddListener(value => {
                Model.EffectVolume = value;
                AutoManager.Get<AudioManager>().SetVolume(AudioManager.AudioType.SFX, value);
                PlayerPrefs.SetFloat("EffectVolume", Model.EffectVolume);
            });

            // Event Register
            EffectAudioView.OnLeftClickEvent().AddListener(() => {
                Model.EffectVolume = Mathf.Max(0, Model.EffectVolume - changeAmount);
                EffectAudioView.Refresh(Model.EffectVolume);
            });

            EffectAudioView.OnRightClickEvent().AddListener(() => {
                Model.EffectVolume = Mathf.Min(Model.EffectVolume + changeAmount, 1f);
                EffectAudioView.Refresh(Model.EffectVolume);
            });
        }
        #endregion

        #region Control

        /// <summary>
        /// 카메라 패닝 설정을 합니다.
        /// </summary>
        private void InitGamepadVibration()
        {
            // View Init
            VibrationView.Refresh(Model.GamepadVibration);

            // Event Register
            VibrationView.OnLeftClickEvent().AddListener(() => {
                Model.GamepadVibration = false;
                VibrationView.Refresh(Model.GamepadVibration);
                SaveButton.interactable = true;
            });

            VibrationView.OnRightClickEvent().AddListener(() => {
                Model.GamepadVibration = true;
                VibrationView.Refresh(Model.GamepadVibration);
                SaveButton.interactable = true;
            });
        }
        

        #endregion
        
        /// <summary>
        /// 각 뷰의 설정 정보를 적용합니다.
        /// </summary>
        public void ApplyAll()
        {
            languageView.Apply(Model.CurrentLanguage);
            ScreenResolutionView.Apply(Model.ScreenResolution);
            WindowModeView.Apply(Model.WindowMode);
            VSyncView.Apply(Model.VSyncEnable);

            GraphicsQualityView.Apply(Model.GraphicsQuality);
            TextureQualityView.Apply(Model.TextureQuality);
            AntiAliasingView.Apply(Model.AntiAliasingQuality);
            HBAOView.Apply(Model.HBAOEnable);
            panningView.Apply(Model.CameraPanning);
            VibrationView.Apply(Model.GamepadVibration);

            SaveButton.interactable = false;
        }

        /// <summary>
        /// Esc를 누른 것처럼 Back을 합니다.
        /// </summary>
        public void Back()
        {
            BackButton.Fire();
        }

        /// <summary>
        /// 모델 데이터를 리프래쉬합니다.
        /// </summary>
        public void Refresh()
        {
            // Data Bind
            Model.ScreenResolution = PlayerPrefs.GetInt("ScreenResolution", AutoManager.Get<GameManager>().GetBestResolutionIndex);
            ScreenResolutionView.Refresh(Model.ScreenResolution);

            // Data Bind
            Model.WindowMode = PlayerPrefs.GetInt("WindowMode", 1);
            WindowModeView.Refresh(Model.WindowMode);

            // Data Bind
            Model.CurrentLanguage = PlayerPrefs.GetInt("Language", 1);
            languageView.Refresh(Model.CurrentLanguage);

            // Data Bind
            Model.CameraPanning = PlayerPrefs.GetInt("CameraPanning", 1) == 1;
            panningView.Refresh(Model.CameraPanning);

            // Data Bind
            Model.VSyncEnable = PlayerPrefs.GetInt("VSync", 1) == 1;
            VSyncView.Refresh(Model.VSyncEnable);

            // Data Bind
            Model.GraphicsQuality = PlayerPrefs.GetInt("GraphicsQuality", MediumQuality);
            GraphicsQualityView.Refresh(Model.GraphicsQuality);

            // Data Bind
            Model.TextureQuality = PlayerPrefs.GetInt("TextureQuality", BestTextureQuality);
            TextureQualityView.Refresh(Model.TextureQuality);

            // Data Bind
            Model.AntiAliasingQuality = PlayerPrefs.GetInt("AntiAliasingQuality", DataManager.SMAAHight);
            AntiAliasingView.Refresh(Model.AntiAliasingQuality);

            // Data Bind
            Model.HBAOEnable = PlayerPrefs.GetInt("HBAOEnable", 1) == 1;
            HBAOView.Refresh(Model.HBAOEnable);

            // Data Bind
            Model.GamepadVibration = PlayerPrefs.GetInt(nameof(OptionModel.GamepadVibration), 1) == 1;
            VibrationView.Refresh(Model.GamepadVibration);
            
            SaveButton.interactable = false;
        }
    }
}
