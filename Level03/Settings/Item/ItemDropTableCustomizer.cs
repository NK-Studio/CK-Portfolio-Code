using System.Collections.Generic;
using EnumData;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Settings.Item
{
    public abstract class ItemDropTableCustomizer : ScriptableObject
    {
        [field: SerializeField, LabelText("대상 테이블")]
        protected ItemDropTable Target { get; private set; }

        public InstantiatedItemDropTable Table => Target.Instantiated;
        public void UpdateTable()
        {
            Table.UpdateWeightSum();
            
        }
    }
}