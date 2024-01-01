using System;
using System.Collections.Generic;
using Character.Presenter;
using DebuggingEssentials;
using Dummy.Scripts;
using Enemy.UI;
using EnumData;
using FMODPlus;
using FMODUnity;
using ManagerX;
using Option;
using SceneSystem;
using Settings;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Managers
{
    [ManagerDefaultPrefab("GameManager")]
    public class GameManager : MonoBehaviour, AutoManager
    {
        public static GameManager Instance => AutoManager.Get<GameManager>();

        //마우스 입력에 Time.deltaTime을 곱하지 마십시오.;
        public float MouseSensitivity = 1.0f;

        public EQualityLevel QualityLevel;
        
        public GameObject CheatModeSignature;
        public EventReference CheatModeSignatureSound;
        [field: SerializeField] public WindowManager DebuggingEssentials { get; set; }
        public bool CheatMode
        {
            get => m_cheatMode;
            private set
            {
                m_cheatMode = value;
                if (DebuggingEssentials)
                {
                    DebuggingEssentials.gameObject.SetActive(value);
                }
                if(CheatModeSignature)
                    CheatModeSignature.SetActive(value);
                AudioManager.Instance.PlayOneShot(CheatModeSignatureSound);
            }
        }

        [field: SerializeField, BoxGroup("치트키 설정")]
        public string CheatCode { get; private set; } = "rlawldndqkqh";
        [field: SerializeField, BoxGroup("치트키 설정")]
        private int _currentCheatCodeIndex = 0;

        [field: SerializeField] public bool DebugMode { get; private set; } = true;

        /// <summary>
        /// false일 시 플레이어가 ESC를 통해 메뉴를 열 수 없습니다.
        /// </summary>
        public bool CanActiveMenu { get; set; } = true;

        public FMODAudioSource SnapshotSource;
        private BoolReactiveProperty _isActiveMenu = new(false);

        public bool IsActiveMenu
        {
            get => _isActiveMenu.Value;
            set => _isActiveMenu.Value = value;
        }

        public bool IsPlayingTimeline { get; set; } = false;

        private IObservable<bool> _isActiveMenuObservable;
        [SerializeField, BoxGroup("치트키 설정")]
        private bool m_cheatMode = false;
        public IObservable<bool> IsActiveMenuObservable => _isActiveMenuObservable ??= _isActiveMenu.AsObservable();

        public List<ResolutionData> Options { get; private set; } = new();

        /// <summary>
        /// 해상도를 가져옵니다.
        /// </summary>
        public void InitOption()
        {
            Resolution[] resolutions = Screen.resolutions;
            foreach (Resolution resolution in resolutions)
            {
                if (ResolutionUtility.CheckMinimumResolution(resolution.width))
                    if (ResolutionUtility.Check16To9Ratio(resolution))
                        if (ResolutionUtility.CheckUniversalResolution(resolution))
                            if (ResolutionUtility.CheckMinimumRefreshRateRatio(resolution))
                            {
                                ResolutionData resolutionData = new ResolutionData(resolution.width, resolution.height,
                                    resolution.refreshRateRatio);
                                
                                Options.Add(resolutionData);
                            }
            }
        }

        private void Start()
        {
            if (SnapshotSource)
            {
                IsActiveMenuObservable.Subscribe(value => {
                    if (value)
                        SnapshotSource.Play();
                    else
                        SnapshotSource.Stop(true);
                });
            }

            InitOption();
            
            int screenResolution = PlayerPrefs.GetInt("ScreenResolution", GetBestResolutionIndex);
            SetScreenResolution(screenResolution);

            int windowMode = PlayerPrefs.GetInt("WindowMode", 1);
            ChangeFullScreenMode(windowMode);

            int textureQuality = PlayerPrefs.GetInt("TextureQuality", OptionPresenter.BestTextureQuality);
            ChangeTextureMipmapQuality(textureQuality);

            Keyboard.current.onTextInput += ProcessCheatCode;
        }

        private void OnDestroy()
        {
            Keyboard.current.onTextInput -= ProcessCheatCode;
        }

        private void ProcessCheatCode(char c)
        {
            if (c != CheatCode[_currentCheatCodeIndex])
            {
                _currentCheatCodeIndex = 0;
                return;
            }

            ++_currentCheatCodeIndex;
            if (_currentCheatCodeIndex < CheatCode.Length)
            {
                return;
            }

            CheatMode = !CheatMode;
            Debug.Log($"CHEAT MODE = {CheatMode}");
            _currentCheatCodeIndex = 0;
        }

        /// <summary>
        /// 플레이어의 세팅 데이터를 반환합니다.
        /// </summary>
        [field: SerializeField]
        public CharacterSettings Settings { get; private set; }

        [field: SerializeField] public PrototypeSettings Prototype { get; private set; }

        /// <summary>
        /// 현재 씬의 Player
        /// </summary>
        public PlayerPresenter Player { get; set; } = null;

        /// <summary>
        /// 현재 씬의 하위 씬 관리자
        /// </summary>
        public LevelPartHandler CurrentLevelPartHandler { get; set; } = null;

        /// <summary>
        /// 현재 씬의 적 HUD 체력바 풀 관리자 
        /// </summary>
        public EnemyHUDPoolManager CurrentHUDPoolManager { get; set; } = null;

        /// <summary>
        /// 저장된 체크포인트와 별개로, 현재 상태
        /// </summary>
        [field: SerializeField]
        public CheckPointStorage CurrentCheckPointStorage { get; private set; } = new();

        // private void Update()
        // {
        //     if (Keyboard.current[Key.Digit9].wasPressedThisFrame)
        //     {
        //         Debug.Log("저장 모두 제거");
        //         PlayerPrefs.DeleteAll();
        //     }
        // }

        /// <summary>
        /// 플레이어가 죽었는지 확인합니다.
        /// </summary>
        /// <returns>죽어있으면 True를 반환합니다.</returns>
        public bool IsGameOver()
        {
            if (!Player)
            {
                return true;
            }

            return Player.Model.IsDead;
        }

        /// <summary>
        /// 화면 해상도를 변경합니다.
        /// </summary>
        /// <param name="screenResolutionIndex"></param>
        public void SetScreenResolution(int screenResolutionIndex)
        {
            ResolutionData resolutionData;
            try
            {
                 resolutionData = Options[screenResolutionIndex];
            }
            catch (Exception)
            {
                 resolutionData = Options[GetBestResolutionIndex];
                 PlayerPrefs.SetInt("ScreenResolution", GetBestResolutionIndex);
            }

            FullScreenMode currentScreenMode = GetFullScreenMode();
            
            Screen.SetResolution(resolutionData.Width, resolutionData.Height, currentScreenMode,
                resolutionData.RefreshRateRatio);
        }

        /// <summary>
        /// 최상 모니터 해상도를 인덱스를 반환합니다. (Request : InitOption())
        /// </summary>
        public int GetBestResolutionIndex => Options.Count - 1;

        /// <summary>
        /// 전체화면 또는 창모드의 기록을 가져옵니다.
        /// </summary>
        /// <returns></returns>
        public FullScreenMode GetFullScreenMode()
        {
            int screenMode = PlayerPrefs.GetInt("WindowMode", 1);

            return screenMode switch {
                1 => Screen.fullScreenMode = FullScreenMode.FullScreenWindow,
                0 => Screen.fullScreenMode = FullScreenMode.Windowed,
                _ => Screen.fullScreenMode = FullScreenMode.FullScreenWindow
            };
        }

        /// <summary>
        /// 스크린의 전체 스크린 모드를 변경합니다.
        /// </summary>
        /// <param name="mode">변경할 스크린 모드</param>
        public void ChangeFullScreenMode(int mode)
        {
            switch (mode)
            {
                case 0:
                    Screen.fullScreenMode = FullScreenMode.Windowed;
                    break;
                case 1:
                    Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                    break;
            }
        }

        /// <summary>
        /// 텍스쳐의 퀄리티를 변경합니다.
        /// </summary>
        /// <param name="textureQualityIndex">0~3의 범위입니다.\n3이 최상 품질, 0이 낮은 품질입니다.</param>
        public void ChangeTextureMipmapQuality(int textureQualityIndex)
        {
            switch (textureQualityIndex)
            {
                case 0:
                    QualitySettings.globalTextureMipmapLimit = 3;
                    break;
                case 1:
                    QualitySettings.globalTextureMipmapLimit = 2;
                    break;
                case 2:
                    QualitySettings.globalTextureMipmapLimit = 1;
                    break;
                default:
                    QualitySettings.globalTextureMipmapLimit = 0;
                    break;
            }
        }
    }
}
