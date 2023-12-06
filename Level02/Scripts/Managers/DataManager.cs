using System.Collections.Generic;
using System.IO;
using AutoManager;
using MessagePack;
using UnityEngine;

namespace SaveLoadSystem
{
    [ManagerDefaultPrefab("DataManager")]
    public class DataManager : Manager
    {
        private string Path { get; set; }

        private const string GameName = "Jellowin";

        private Dictionary<string, byte[]> _playerData;
        
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
            _playerData = new Dictionary<string, byte[]>();

            Path = $"{Application.persistentDataPath}/{GameName}.bin";
        }

        /// <summary>
        /// 데이터를 저장합니다.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        public void Save<T>(string key, T value)
        {
            byte[] bin = MessagePackSerializer.Serialize(value);

            if (!_playerData.ContainsKey(key))
                _playerData.Add(key, bin);
            else
                _playerData[key] = bin;

            byte[] playerDataBin = MessagePackSerializer.Serialize(_playerData);
            File.WriteAllBytes(Path, playerDataBin);
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
            if (!File.Exists(Path)) return defaultValue;

            byte[] bin = File.ReadAllBytes(Path);
            _playerData = MessagePackSerializer.Deserialize<Dictionary<string, byte[]>>(bin);

            if (_playerData.ContainsKey(key))
            {
                T data = MessagePackSerializer.Deserialize<T>(_playerData[key]);
                return data;
            }

            return defaultValue;
        }
    }
}