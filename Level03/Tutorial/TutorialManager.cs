using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tutorial
{
    public class TutorialManager : MonoBehaviour
    {
        private const string _levelKey = "level";
        public int Level { get; private set; }
        public static TutorialManager Instance { get; private set; }
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }
    
        private void Start()
        {
            // 저장된 데이터가 있는지 확인하고, 있다면 가져옵니다.
            if (PlayerPrefs.HasKey(_levelKey))
            {
                Level = PlayerPrefs.GetInt(_levelKey);
            }
        }

        public void SaveData()
        {
            PlayerPrefs.SetInt(_levelKey, Level);
            PlayerPrefs.Save();
        }

        public void NextLevel()
        {
            Level++;
        }
    }
}