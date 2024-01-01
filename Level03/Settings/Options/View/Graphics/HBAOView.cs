using Managers;
using ManagerX;
using Option;
using UnityEngine;

public class HBAOView : DefaultBoolView
{
    public void Apply(bool active)
    {
        AutoManager.Get<DataManager>().HBAOEnable.Value = active;
        PlayerPrefs.SetInt("HBAOEnable", active ? 1 : 0);
    }
}
