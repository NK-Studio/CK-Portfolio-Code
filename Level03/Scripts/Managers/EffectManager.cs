using EnumData;
using ManagerX;

namespace Managers
{
    [ManagerDefaultPrefab("EffectManager")]
    public class EffectManager : ObjectPoolByEnum<EffectType>
    {
        public static EffectManager Instance => AutoManager.Get<EffectManager>();
    }
}