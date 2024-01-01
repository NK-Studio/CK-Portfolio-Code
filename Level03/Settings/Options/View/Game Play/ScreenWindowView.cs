using Managers;
using ManagerX;
using UnityEngine;
using UnityEngine.Localization;

namespace Option
{
    public class ScreenWindowView : DefaultListView
    {
        public LocalizedString FullScreen;
        public LocalizedString Windowed;

        public override void Refresh(int windowModeIndex)
        {
            var windowModeText = windowModeIndex == 1 ? FullScreen : Windowed;
            base.Refresh(windowModeText, windowModeIndex);
        }
        
        /// <summary>
        /// 스크린 모드를 적용합니다.
        /// </summary>
        /// <param name="windowModeIndex">윈도우/창모드를 전환합니다.</param>
        public void Apply(int windowModeIndex)
        {
            AutoManager.Get<GameManager>().ChangeFullScreenMode(windowModeIndex);
            PlayerPrefs.SetInt("WindowMode", windowModeIndex);
        }
    }
}
