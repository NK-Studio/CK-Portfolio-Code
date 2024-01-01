using EnumData;
using ManagerX;

namespace Managers
{
    [ManagerDefaultPrefab("ItemManager")]
    public class ItemManager : ObjectPoolByEnum<ItemType>
    {
        public static ItemManager Instance => AutoManager.Get<ItemManager>();
    }
}