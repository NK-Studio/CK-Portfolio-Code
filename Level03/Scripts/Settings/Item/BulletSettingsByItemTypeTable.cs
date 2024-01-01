using System.Collections.Generic;
using EnumData;
using Settings.Player;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Settings.Item
{
    [CreateAssetMenu(fileName = "BulletSettingsByItemTypeTable", menuName = "Settings/BulletSettings By ItemType Table", order = 0)]
    public class BulletSettingsByItemTypeTable : SerializedScriptableObject
    {
        [field: SerializeField, DictionaryDrawerSettings(KeyLabel = "아이템 종류", ValueLabel = "탄환")]
        public Dictionary<ItemType, PlayerBulletSettings> Table { get; private set; }
    }
}