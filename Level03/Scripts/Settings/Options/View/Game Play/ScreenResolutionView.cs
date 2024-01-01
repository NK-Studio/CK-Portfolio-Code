using System.Collections.Generic;
using Managers;
using ManagerX;
using TMPro;
using UnityEngine;

namespace Option
{
    public class ScreenResolutionView : DefaultListView
    {
        [SerializeField]
        private TMP_Text title;
        
        /// <summary>
        /// 해상도 개수를 반환합니다.
        /// </summary>
        public int GetOptionCount => AutoManager.Get<GameManager>().Options.Count;

        public override void Refresh(int screenResolutionIndex)
        {
            SetActiveCircle(screenResolutionIndex);
            title.text = AutoManager.Get<GameManager>().Options[screenResolutionIndex].ToString();
        }
        
        /// <summary>
        /// 화면 해상도 변경을 적용합니다.
        /// </summary>
        public void Apply(int screenResolutionIndex)
        {
            AutoManager.Get<GameManager>().SetScreenResolution(screenResolutionIndex);
            PlayerPrefs.SetInt("ScreenResolution", screenResolutionIndex);
        }
    }
}
