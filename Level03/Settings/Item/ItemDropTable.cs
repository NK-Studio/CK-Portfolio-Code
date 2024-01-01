using System;
using System.Collections.Generic;
using System.Linq;
using EnumData;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Utility;
using Random = UnityEngine.Random;

namespace Settings.Item
{
    [CreateAssetMenu(fileName = "New ItemDropTable", menuName = "Settings/Item Drop Table", order = 0)]
    public class ItemDropTable : SerializedScriptableObject
    {
        [field: SerializeField, DictionaryDrawerSettings(KeyLabel = "아이템 종류", ValueLabel = "가중치")]
        public Dictionary<ItemType, float> Table = new();
        
        [Serializable]
        public struct WeightModifier
        {
            public enum OperationType
            {
                Add,
                Set,
            }

            public ItemType Target;
            public OperationType Type;
            public float Value;

            public float ApplyOperation(float originalValue)
            {
                switch (Type)
                {
                    case OperationType.Add:
                        return originalValue + Value;
                    case OperationType.Set:
                        return Value;
                }
                return originalValue;
            }
            public void Apply(Dictionary<ItemType, float> table)
            {
                if (!table.TryGetValue(Target, out var originalValue))
                {
                    return;
                }
                table[Target] = ApplyOperation(originalValue);
            }
        }

        [LabelText("디버그 로그")]
        public bool DebugLog = true;
        [field: SerializeField, DictionaryDrawerSettings(KeyLabel = "드랍 종류", ValueLabel = "조정자")]
        public Dictionary<ItemType, List<WeightModifier>> WeightModifierOnSelect = new();

        public bool IsEmpty => Table.IsEmpty();


        private float? _weightSum = null;
        public float WeightSum => _weightSum ??= Table.Sum(it => it.Value);
        
        public ItemType Get()
        {
            float sum = WeightSum;
            float selection = Random.Range(0f, sum);
            float rangeStart = 0f;
            foreach (var (type, weight) in Table)
            {
                // 이번 가중치 범위에 들어있는가?
                if (rangeStart <= selection && selection <= rangeStart + weight)
                {
                    return type;
                }

                rangeStart += weight;
            }

            return ItemType.None;
        }

        private InstantiatedItemDropTable _instance;
        /// <summary>
        /// 같은 종류의 테이블이 공유하는 인스턴스화된 테이블입니다.
        /// </summary>
        public InstantiatedItemDropTable Instantiated => _instance ??= Instantiate();
        /// <summary>
        /// 새로운 인스턴스화된 테이블을 만듭니다.
        /// </summary>
        /// <returns></returns>
        public InstantiatedItemDropTable Instantiate() => new(this);

    }
}