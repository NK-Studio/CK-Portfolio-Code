using System.Collections.Generic;
using ManagerX;
using Option;
using UniRx;
// using MessagePack;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Managers
{
    [ManagerDefaultPrefab("DataManager")]
    public class DataManager : MonoBehaviour, AutoManager
    {
        public static DataManager Instance => AutoManager.Get<DataManager>();
        private string Path { get; set; }

        private const string GameName = "KonaAndSnowRabbit";

        private Dictionary<string, byte[]> _gameData;

        public IntReactiveProperty AntiAliasingIndex = new IntReactiveProperty(0);
        public BoolReactiveProperty HBAOEnable = new BoolReactiveProperty(true);
        public BoolReactiveProperty PanningEnable = new BoolReactiveProperty(true);
        public BoolReactiveProperty VibrationEnable = new BoolReactiveProperty(true);

        public static readonly int SMAAHight = 3;
        private void Awake()
        {
            //초기화
            Initialized();
        }

        /// <summary>
        /// 초기화
        /// </summary>
        private void Initialized()
        {
            //슬롯 3개를 생성
            _gameData = new Dictionary<string, byte[]>();

            Path = $"{Application.persistentDataPath}/{GameName}.bin";

            AntiAliasingIndex.Value = PlayerPrefs.GetInt("AntiAliasingQuality", SMAAHight);
            HBAOEnable.Value = PlayerPrefs.GetInt("HBAOEnable", 1) == 1;
            PanningEnable.Value = PlayerPrefs.GetInt("CameraPanning", 1) == 1;
            VibrationEnable.Value = PlayerPrefs.GetInt(nameof(OptionModel.GamepadVibration), 1) == 1;

            int languageIndex = PlayerPrefs.GetInt("Language", 1);
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[languageIndex];
        }

        /// <summary>
        /// 데이터를 저장합니다.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        public void Save<T>(string key, T value)
        {
            /*
            byte[] bin = MessagePackSerializer.Serialize(value);

            if (!_gameData.ContainsKey(key))
                _gameData.Add(key, bin);
            else
                _gameData[key] = bin;

            byte[] playerDataBin = MessagePackSerializer.Serialize(_gameData);
            File.WriteAllBytes(Path, playerDataBin);
            */
        }

        /// <summary>
        /// 특정 값을 불러옵니다.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Load<T>(string key, T defaultValue)
        {
            /*
            if (!File.Exists(Path)) return defaultValue;

            byte[] bin = File.ReadAllBytes(Path);
            _gameData = MessagePackSerializer.Deserialize<Dictionary<string, byte[]>>(bin);

            if (_gameData.TryGetValue(key, out var value))
            {
                T data = MessagePackSerializer.Deserialize<T>(value);
                return data;
            }
            */
            return defaultValue;
        }

        /// <summary>
        /// 카메라에 패닝을 처리할 여부를 반환합니다.
        /// </summary>
        /// <returns>허용시 true를 반환</returns>
        public bool IsEnableCameraPanning => PanningEnable.Value;

        /// <summary>
        /// 게임패드 진동 사용 여부를 반환합니다.
        /// </summary>
        public bool IsEnableVibration => VibrationEnable.Value;
    }
}
