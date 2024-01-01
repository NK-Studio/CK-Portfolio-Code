using System.Collections.Generic;
using System.Linq;
using EnumData;
using UnityEngine;
using Utility;

namespace Settings.Item
{
    /// <summary>
    /// 가중치가 변할 수 있는 테이블입니다.
    /// </summary>
    public class InstantiatedItemDropTable
    {
        public ItemDropTable OriginalTable { get; }

        public Dictionary<ItemType, float> DynamicTable { get; private set; } = new();
        
        public InstantiatedItemDropTable(ItemDropTable table)
        {
            OriginalTable = table;
            foreach (var (type, weight) in table.Table)
            {
                DynamicTable.Add(type, weight);
            }
        }

        public bool IsEmpty => DynamicTable.Count <= 0;
        private float? _weightSum = null;
        public float WeightSum => _weightSum ??= UpdateWeightSum();

        public float UpdateWeightSum()
        {
            var result = (float)(_weightSum = DynamicTable.Sum(it => it.Value));
#if UNITY_EDITOR
            if(OriginalTable.DebugLog)
                Log($"UpdateWeightSum called - {{{this}}}");
#endif
            return result;
        }
        
        public ItemType Get()
        {
            var selected = GetInternal();
            OnSelected(selected);
            return selected;
        }
        
        private ItemType GetInternal()
        {
            float sum = WeightSum;
            float selection = Random.Range(0f, sum);
            float rangeStart = 0f;
            foreach (var (type, weight) in DynamicTable)
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
        private void OnSelected(ItemType type)
        {
            if (!OriginalTable.WeightModifierOnSelect.TryGetValue(type, out var modifiers))
            {
                return;
            }

            foreach (var modifier in modifiers)
            {
                modifier.Apply(DynamicTable);
            }

            UpdateWeightSum();

#if UNITY_EDITOR
            if(OriginalTable.DebugLog)
                Log($"Selected {type} -> {{{this}}}");
#endif
        }

        public override string ToString()
        {
            return $"SUM={_weightSum}, "+DynamicTable.ToList().JoinToString(", ", (pair) => $"{pair.Key}={pair.Value:F2}({(pair.Value/_weightSum)*100f:F1}%)");
        }


        private void Log(string message)
        {
            Debug.Log($"[{OriginalTable.name}] {message}");
        }
    }
}