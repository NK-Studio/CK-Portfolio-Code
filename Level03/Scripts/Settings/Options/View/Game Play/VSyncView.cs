using UnityEngine;

namespace Option
{
    public class VSyncView : DefaultBoolView
    {
        /// <summary>
        /// V-Sync를 적용합니다.
        /// </summary>
        /// <param name="active"></param>
        public void Apply(bool active)
        {
            QualitySettings.vSyncCount = active ? 1 : 0;
            PlayerPrefs.SetInt("VSync", active ? 1 : 0);
        }
    }
}
