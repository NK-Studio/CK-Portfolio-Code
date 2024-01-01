using EnumData;
using UnityEngine;

namespace Settings.Item
{
    [CreateAssetMenu(fileName = "New DropPercentageCustomizer", menuName = "Settings/Item Drop Table Percentage Customizer", order = 0)]
    public class ItemDropTablePercentageCustomizer : ItemDropTableCustomizer
    {
        public ItemType Type;

        public void UpdatePercentage(float percentage)
        {
            // 우리가 수정할 타입의 가중치 합이 없는 상태로 전환
            Table.DynamicTable.Remove(Type);
            Table.UpdateWeightSum();
            // 수정된 가중치는 기존 가중치합을 유지한 상태로 추가 가중치가 되어야 함
            // 근데 기존 weight sum을 당연히 건드리면은 안됨
            // 그래서 S = originalWeightSum, S' = modifiedWeightSum, k = percentage 이라 치면 ...
            // (1-k)S' = S, 즉 S' = S/(1-k) 여야 함.
            // 즉, 변경된 가중치에서 우리가 원하는 선택 확률의 가중치는 kS', 나머지 가중치는 (1-k)S'임
            var originalWeightSum = Table.WeightSum; // S
            var modifiedWeightSum = originalWeightSum / (1 - percentage); // S'
            var calculatedWeight = percentage * modifiedWeightSum; // kS'
            // 테이블에 가중치 삽입.
            Table.DynamicTable.Add(Type, calculatedWeight);
            // 테이블 가중치 갱신
            UpdateTable();
        }
    }
}