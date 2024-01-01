using EnumData;
using ManagerX;

namespace Managers
{
    [ManagerDefaultPrefab("EnemyPoolManager")]
    public class EnemyPoolManager : ObjectPoolByEnum<EnemyType>
    {
        public static EnemyPoolManager Instance => AutoManager.Get<EnemyPoolManager>();
    }
}