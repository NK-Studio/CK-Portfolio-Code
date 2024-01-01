using Managers;
using ManagerX;
using UnityEngine;


namespace Option
{
    public class VibrationView : DefaultBoolView
    {
        public void Apply(bool active)
        {
            AutoManager.Get<DataManager>().VibrationEnable.Value = active;
            PlayerPrefs.SetInt(nameof(OptionModel.GamepadVibration), active ? 1 : 0);
        }
    }
}
