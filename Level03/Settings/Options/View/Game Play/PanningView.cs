using Managers;
using ManagerX;
using UnityEngine;


namespace Option
{
    public class PanningView : DefaultBoolView
    {
        public void Apply(bool active)
        {
            AutoManager.Get<DataManager>().PanningEnable.Value = active;
            PlayerPrefs.SetInt("CameraPanning", active ? 1 : 0);
        }
    }
}
