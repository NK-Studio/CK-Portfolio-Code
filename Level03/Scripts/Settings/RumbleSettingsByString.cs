using System.Collections.Generic;
using Managers;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Settings
{
    [CreateAssetMenu(fileName = "New RumbleSettingsByString", menuName = "Settings/Rumble Settings by String", order = 0)]
    public class RumbleSettingsByString : SerializedScriptableObject
    {
        public Dictionary<string, GamePadManager.RumbleSettings> Map = new();

        public bool TryGet(string key, out GamePadManager.RumbleSettings settings)
        {
            return Map.TryGetValue(key, out settings);
        }
    }
}